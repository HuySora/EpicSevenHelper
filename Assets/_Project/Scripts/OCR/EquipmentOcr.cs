using System;
using Cysharp.Threading.Tasks;
using Freya;
using Newtonsoft.Json;
using PixelSquare.TesseractOCR;
using QFSW.QC;
using SaintsField.Playa;
using SoraTehk.Diagnostic;
using SoraTehk.Extensions;
using SoraTehk.Prepare;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SoraTehk.E7Helper {
#if UNITY_EDITOR
    public partial class EquipmentOcr : IPreparable {
        public bool Prepare() {
            bool isDirty = false;

            if (MainStatImage == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out MainStatImage!, "RawImage_EquipmentOcr.MainStatTexture");
            }
            if (SubStatsImage == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out SubStatsImage!, "RawImage_EquipmentOcr.SubStatsTexture");
            }
            if (MainStatRectController == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out MainStatRectController!, "MainStatPanel");
            }
            if (SubStatsRectController == null) {
                isDirty |= gameObject.scene.TryFindFirstComponent(out SubStatsRectController!, "SubStatsPanel");
            }

            return isDirty;
        }
    }
#endif
    public partial class EquipmentOcr : MonoBehaviour {
        [Command($"{nameof(EquipmentOcr)}.{nameof(LogOcrString)}", MonoTargetType.Registry)]
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        internal void LogOcrString() {
            SorLog.LogInfo(
                $".m_OcrString=\"{m_OcrString}\"{Environment.NewLine}" +
                $".m_OutputEquipmentJsonString=\"{m_OutputEquipmentJsonString}\""
            );
        }

        [LayoutStart("Debug", ELayout.FoldoutBox)]
        public RawImage MainStatImage = null!;
        public RawImage SubStatsImage = null!;

        [LayoutStart("Scene", ELayout.FoldoutBox)]
        public UIRectTransformScaler MainStatRectController = null!;
        public UIRectTransformScaler SubStatsRectController = null!;

        [LayoutStart("Config", ELayout.FoldoutBox)]
        public string OcrLanguageId = "eng";
        [Range(0f, 1f)] public float BlackThreshold = 0.26f;
        [Range(0f, 1f)] public float AlphaThreshold = 0.00f;

        [LayoutStart("Runtime", ELayout.FoldoutBox)]
        public Texture2D? m_MainStatTexture2D;
        public Texture2D? m_SubStatsTexture2D;

        #region Injected
        [Inject] private TextureManager m_TextureManager = null!;
        [Inject] private UwcWindowCaptureManager m_UwcWindowCaptureManager = null!;
        #endregion

        internal IOpticalCharacterReader m_Ocr = null!;

        internal bool m_IsScanning;
        internal string m_OcrString = string.Empty;

        internal Equipment m_NewEquipment = null!;
        internal Equipment m_CurrEquipment = null!;
        internal string m_OutputEquipmentJsonString = string.Empty;

        private void Awake() {
            m_Ocr = new TesseractOCRImpl();
            m_NewEquipment = new();
            m_CurrEquipment = new();
        }

        private void Start() {
            m_Ocr.Initialize(OcrLanguageId);
        }

        private void OnEnable() {
            m_UwcWindowCaptureManager.OnCaptured -= OnCaptured;
            m_UwcWindowCaptureManager.OnCaptured += OnCaptured;
        }
        private void OnDisable() {
            m_UwcWindowCaptureManager.OnCaptured -= OnCaptured;
        }

        private void OnDestroy() {
            m_Ocr.Dispose();
        }

        private void OnCaptured(Texture2D tex) {
            // TODO: We just drop new scan if old scan are still in progress
            if (m_IsScanning) return;
            // TODO: m_Ocr could be uninitialized here
            Async().Forget();
            // Local functions
            async UniTask Async() {
                m_IsScanning = true;

                m_OcrString = string.Empty;

                // Calculate the actual window scale
                var windowScaleX = tex.width / m_UwcWindowCaptureManager.WindowSyncImage.rectTransform.rect.width;
                var windowScaleY = tex.height / m_UwcWindowCaptureManager.WindowSyncImage.rectTransform.rect.height;

                // Cropping the main stat
                int mainStatTexStartX = Mathfs.RoundToInt(MainStatRectController.GetAnchoredX() * windowScaleX);
                int mainStatTexStartY = Mathfs.RoundToInt(MainStatRectController.GetAnchoredY() * windowScaleY);
                int mainStatTexWidth = Mathfs.RoundToInt(MainStatRectController.GetWidth() * windowScaleX);
                int mainStatTexHeight = Mathfs.RoundToInt(MainStatRectController.GetHeight() * windowScaleY);
                var mainStatTex = m_TextureManager.Acquire(mainStatTexWidth, mainStatTexHeight, tex.format);
                Texture2DExtension.CopyCroppedPixels(mainStatTex, tex, mainStatTexStartX, mainStatTexStartY);
                mainStatTex.ApplyBinaryThreshold(Color.black, BlackThreshold, true);

                // Ocr texture to crop
                m_TextureManager.Release(m_MainStatTexture2D);
                // TextureFormat.RGB24 because Tesseract only supports 3 bytes per pixel
                m_MainStatTexture2D = m_TextureManager.Acquire(mainStatTexWidth, mainStatTexHeight, TextureFormat.RGB24);
                m_MainStatTexture2D.SetPixels(mainStatTex.GetPixels());
                m_MainStatTexture2D.Apply();

                // Scan
                m_Ocr.SetImage(m_MainStatTexture2D);
                m_OcrString = m_Ocr.GetText().Trim() + "\n";

                // Cropping the sub stats
                int subStatsTexStartX = Mathfs.RoundToInt(SubStatsRectController.GetAnchoredX() * windowScaleX);
                int subStatsTexStartY = Mathfs.RoundToInt(SubStatsRectController.GetAnchoredY() * windowScaleY);
                int subStatsTexWidth = Mathfs.RoundToInt(SubStatsRectController.GetWidth() * windowScaleX);
                int subStatsTexHeight = Mathfs.RoundToInt(SubStatsRectController.GetHeight() * windowScaleY);
                var subStatsTex = m_TextureManager.Acquire(subStatsTexWidth, subStatsTexHeight, tex.format);
                Texture2DExtension.CopyCroppedPixels(subStatsTex, tex, subStatsTexStartX, subStatsTexStartY);

                var subStatsTexWithWhiteMask = m_TextureManager.AcquireClone(subStatsTex);
                subStatsTexWithWhiteMask.ApplyBinaryThreshold(Color.black, BlackThreshold, true);

                var subStatsTexWithAlphaContrast = m_TextureManager.AcquireClone(subStatsTex);
                subStatsTexWithAlphaContrast.ApplyAlphaContrast(AlphaThreshold);

                // Ocr texture to crop
                m_TextureManager.Release(m_SubStatsTexture2D);
                // TextureFormat.RGB24 because Tesseract only supports 3 bytes per pixel
                m_SubStatsTexture2D = m_TextureManager.Acquire(subStatsTexWidth, subStatsTexHeight, TextureFormat.RGB24);
                Texture2DExtension.BlendMultiply(m_SubStatsTexture2D, subStatsTexWithWhiteMask, subStatsTexWithAlphaContrast);

                // Scan
                m_Ocr.SetImage(m_SubStatsTexture2D);
                m_OcrString += m_Ocr.GetText().Trim();

                ParseEquipment(m_OcrString);

                MainStatImage.texture = m_MainStatTexture2D;
                MainStatImage.SetNativeSize();
                SubStatsImage.texture = m_SubStatsTexture2D;
                SubStatsImage.SetNativeSize();

                // Clean up
                await UniTask.NextFrame();
                m_TextureManager.Release(mainStatTex);
                m_TextureManager.Release(subStatsTex);
                m_TextureManager.Release(subStatsTexWithWhiteMask);
                m_TextureManager.Release(subStatsTexWithAlphaContrast);

                m_IsScanning = false;
            }
        }

        [LayoutStart("Legacy", ELayout.FoldoutBox)]
        public TMP_Text GearRankText = null!;
        public TMP_Text GearScore1Text = null!;
        public TMP_Text GearScore2Text = null!;
        public TMP_Text GearScore3Text = null!;
        public TMP_Text GearScore4Text = null!;
        public TMP_Text GearScoreTotalText = null!;
        public TMP_Text GearScoreTotalModdedText = null!;
        public TMP_Text GearScoreTotalReforgedText = null!;
        public TMP_Text GearScoreTotalModdedReforgedText = null!;

        private void ParseEquipment(string inputStr) {
            // TODO: Fix double parse
            m_NewEquipment.TryParse(inputStr);
            // Ignore if we're still scanning the same equipment (ignore a case for default equipment StatType.Null)
            if (m_NewEquipment.CompareTo(m_CurrEquipment) == 0 && m_NewEquipment.MainStat.Type != StatType.Null) return;
            m_CurrEquipment.TryParse(inputStr);

            m_OutputEquipmentJsonString = m_CurrEquipment.ToString();

            GearRankText.text = m_CurrEquipment.Rank.ToString();

            // TODO: Currently hard-coded sub stats as 4 only
            TMP_Text[] gearScoreTextArr = new[] {
                GearScore1Text,
                GearScore2Text,
                GearScore3Text,
                GearScore4Text
            };
            foreach (var gsText in gearScoreTextArr) {
                gsText.text = 0m.ToString("F1");
            }

            // ReSharper disable once InlineOutVariableDeclaration (Shared)
            (decimal Min, decimal Max) outRange;
            // Normal calculation
            decimal gsTotal = 0m;
            decimal gsLowest = 8m;
            decimal gsTotalModded = 0m;
            // Calculate total gear scores
            for (var i = 0; i < m_CurrEquipment.SubStatList.Count; i++) {
                decimal gearScore = m_CurrEquipment.SubStatList[i].GetGearScore();
                gearScoreTextArr[i].text = gearScore.ToString("F1");
                gsTotal += gearScore;
                // Finding out which are the lowest gear score stat
                gsLowest = Math.Min(gsLowest, gearScore);
            }

            // Replace the lowest gear score stat with 8
            gsTotalModded = gsTotal - gsLowest + 8;
            // Update UI
            GearScoreTotalText.text = gsTotal.ToString("F2");
            GearScoreTotalModdedText.text = gsTotalModded.ToString("F2");
            // Text formating for fast gear check
            if (Constants.EquipmentRank2GearScoreMinMax.TryGetValue(m_CurrEquipment.Rank, out outRange)) {
                GearScoreTotalText.color = gsTotal < outRange.Min ? Color.red :
                    gsTotal >= outRange.Max ? Color.green : Color.yellow;
                GearScoreTotalModdedText.color = gsTotalModded < outRange.Min ? Color.red :
                    gsTotalModded >= outRange.Max ? Color.green : Color.yellow;
            }
            else {
                GearScoreTotalText.color = Color.white;
                GearScoreTotalModdedText.color = Color.white;
            }

            // Expected reforged
            decimal reforgedGsTotal = 0m;
            decimal reforgedGsLowest = 9m;
            decimal reforgedGsTotalModded = 0m;
            // Calculate total gear scores
            for (var i = 0; i < m_CurrEquipment.SubStatList.Count; i++) {
                decimal reforgedGs = m_CurrEquipment.SubStatList[i].GetReforgedGearScore();
                reforgedGsTotal += reforgedGs;
                // Finding out which are the lowest gear score stat
                reforgedGsLowest = Math.Min(reforgedGsLowest, reforgedGs);
            }

            // Replace the lowest gear score stat with 9
            reforgedGsTotalModded = reforgedGsTotal - reforgedGsLowest + 9;
            // Update UI
            GearScoreTotalReforgedText.text = reforgedGsTotal.ToString("F2");
            GearScoreTotalModdedReforgedText.text = reforgedGsTotalModded.ToString("F2");
            // Text formating for fast gear check
            if (Constants.EquipmentRank2GearScoreMinMax.TryGetValue(m_CurrEquipment.Rank, out outRange)) {
                // Reforged average 10 more gear score
                outRange = (outRange.Min + 10m, outRange.Max + 10m);

                GearScoreTotalReforgedText.color = reforgedGsTotal < outRange.Min ? Color.red :
                    reforgedGsTotal >= outRange.Max ? Color.green : Color.yellow;
                GearScoreTotalModdedReforgedText.color = reforgedGsTotalModded < outRange.Min ? Color.red :
                    reforgedGsTotalModded >= outRange.Max ? Color.green : Color.yellow;
            }
            else {
                GearScoreTotalReforgedText.color = Color.white;
                GearScoreTotalModdedReforgedText.color = Color.white;
            }
        }
    }
}