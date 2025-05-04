using System;
using System.Collections;
using System.Runtime.InteropServices;
using PixelSquare.TesseractOCR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using uWindowCapture;

public class EquipmentOcr : MonoBehaviour {
    [field: Header("References")]
    [field: SerializeField] public UwcWindowTexture WindowCapture { get; private set; }

    [field: SerializeField] public TMP_Text GearRankText { get; private set; }
    [field: SerializeField] public TMP_Text GearScore1Text { get; private set; }
    [field: SerializeField] public TMP_Text GearScore2Text { get; private set; }
    [field: SerializeField] public TMP_Text GearScore3Text { get; private set; }
    [field: SerializeField] public TMP_Text GearScore4Text { get; private set; }
    [field: SerializeField] public TMP_Text GearScoreTotalText { get; private set; }
    [field: SerializeField] public TMP_Text GearScoreTotalModdedText { get; private set; }
    [field: SerializeField] public RawImage OcrOutputImage { get; private set; }
    [field: SerializeField] public RawImage OcrOutputImage1 { get; private set; }
    [field: SerializeField] public RawImage OcrOutputImage2 { get; private set; }
    [field: SerializeField] public Text OcrOutputText { get; private set; }

    [field: Header("Data")]
    [field: SerializeField] public Rect MainStatRect { get; private set; } = new Rect(0, 890, 780, 80);

    [field: SerializeField] public Rect SubStatsRect { get; private set; } = new Rect(0, 640, 780, 220);
    [field: SerializeField] public string OcrLanguageId { get; private set; } = "eng";
    [field: SerializeField] public float OcrScanRate { get; private set; } = 1f;
    
    [field: Range(0f, 1f)]
    [field: SerializeField] public float BlackThreshold { get; private set; } = 0.54f;
    [field: Range(0f, 1f)]
    [field: SerializeField] public float AlphaThreshold { get; private set; } = 0.88f;

    [field: Header("Runtime")] [SerializeField]
    private Texture2D m_BufferTexture2D;

    [SerializeField] private Texture2D m_MainStatTexture2D;
    [SerializeField] private Texture2D m_SubStatsTexture2D;
    [SerializeField] private string m_CurrText = "";

    private IOpticalCharacterReader m_Ocr;
    private Color32[] m_CurrPixels32;
    private GCHandle m_GcHandle;
    private IntPtr m_CurrPixels32Ptr = IntPtr.Zero;

    [ContextMenu("Encode to PNG")]
    public void EncodeToPng() {
        byte[] pngData = m_BufferTexture2D.EncodeToPNG();
        if (pngData != null) {
            string path = Application.dataPath + "/template.png";
            System.IO.File.WriteAllBytes(path, pngData);
            Debug.Log($"Texture saved to {path}");
        }
        else {
            Debug.LogError("Failed to encode texture to PNG.");
        }
    }

    private void Start() {
        m_Ocr = new TesseractOCRImpl();
        m_Ocr.Initialize(OcrLanguageId);
        StartCoroutine(ScanCoroutine());
    }

    private void OnDestroy() {
        if (m_CurrPixels32Ptr != IntPtr.Zero) {
            m_GcHandle.Free();
        }
    }

    IEnumerator ScanCoroutine() {
        yield return new WaitUntil(WindowCaptureTextureValid);
        while (true) {
            Scan();
            yield return new WaitForSeconds(OcrScanRate);
        }
    }

    private bool WindowCaptureTextureValid() {
        return WindowCapture != null
               && WindowCapture.window != null
               && WindowCapture.window.buffer != IntPtr.Zero
               && WindowCapture.window.texture != null;
    }

    [ContextMenu("Scan")]
    public void Scan() {
        if (!WindowCaptureTextureValid()) {
            Debug.Log("WindowCapture is not valid!");
            return;
        }

        var window = WindowCapture.window;
        var width = window.texture.width;
        var height = window.texture.height;
        var format = window.texture.format;

        // Only re-pin if the WindowCapture texture changed in (memory) size
        if (m_BufferTexture2D == null || width != m_BufferTexture2D.width || height != m_BufferTexture2D.height) {
            if (m_BufferTexture2D != null) {
                DestroyImmediate(m_BufferTexture2D);
            }

            m_BufferTexture2D = new Texture2D(width, height, format, false);
            m_BufferTexture2D.filterMode = FilterMode.Bilinear;
            // Pin the target texture's pixel data
            m_CurrPixels32 = m_BufferTexture2D.GetPixels32();
            m_GcHandle = GCHandle.Alloc(m_CurrPixels32, GCHandleType.Pinned);
            m_CurrPixels32Ptr = m_GcHandle.AddrOfPinnedObject();
        }

        // Copy the pixel data (this will directly set m_CurrPixels32)
        DllUtil.memcpy(m_CurrPixels32Ptr, window.buffer, width * height * sizeof(byte) * 4);

        // Fix texture rotation (being upside down flipped)
        var flippedPixels = new Color32[m_CurrPixels32.Length];
        for (var y = 0; y < height; y++) {
            var srcRow = y * width;
            var destRow = (height - 1 - y) * width;
            for (var x = 0; x < width; x++) flippedPixels[destRow + x] = m_CurrPixels32[srcRow + x];
        }

        // Apply the pixel data to the texture
        m_BufferTexture2D.SetPixels32(flippedPixels);
        m_BufferTexture2D.Apply();

        // Cropping the main stat
        if (m_MainStatTexture2D != null) {
            DestroyImmediate(m_MainStatTexture2D);
        }

        m_MainStatTexture2D = m_BufferTexture2D
            .NewRescale(2560, 1369)
            // Bottom-left corner x=140, y=900, width=636, height=100 (2560:1369)
            .NewCropped(MainStatRect.x / 2560f, MainStatRect.y / 1369f, MainStatRect.width / 2560f,
                MainStatRect.height / 1369f)
            .ApplyBlackMask(BlackThreshold, true)
            // Convert texture to TextureFormat.RGB24 because Tesseract only supports 3 bytes per pixel
            .NewRGB24Texture();

        // Scan
        m_Ocr.SetImage(m_MainStatTexture2D);
        string scannedStr = m_Ocr.GetText().Trim() + "\n";

        // Cropping the sub stats
        if (m_SubStatsTexture2D != null) {
            DestroyImmediate(m_SubStatsTexture2D);
        }
        
        var whiteMaskTex = m_BufferTexture2D
            .NewRescale(2560, 1369)
            // Bottom-left corner x=140, y=660, width=636, height=224 (2560:1369)
            .NewCropped(SubStatsRect.x / 2560f, SubStatsRect.y / 1369f, SubStatsRect.width / 2560f, SubStatsRect.height / 1369f)
            .ApplyBlackMask(BlackThreshold, true);
        OcrOutputImage1.texture = whiteMaskTex;
        var rectTransform = OcrOutputImage1.GetComponent<RectTransform>();
        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            rectTransform.rect.width * whiteMaskTex.height / whiteMaskTex.width
        );
        var alphaContrastTex = m_BufferTexture2D
            .NewRescale(2560, 1369)
            // Bottom-left corner x=140, y=660, width=636, height=224 (2560:1369)
            .NewCropped(SubStatsRect.x / 2560f, SubStatsRect.y / 1369f, SubStatsRect.width / 2560f, SubStatsRect.height / 1369f)
            .ApplyAlphaContrast(AlphaThreshold);
        OcrOutputImage2.texture = alphaContrastTex;
        rectTransform = OcrOutputImage2.GetComponent<RectTransform>();
        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            rectTransform.rect.width * alphaContrastTex.height / alphaContrastTex.width
        );
        
        m_SubStatsTexture2D = Texture2DExtension.NewBlendMultiply(whiteMaskTex, alphaContrastTex)
            .NewRGB24Texture();

        // Scan
        m_Ocr.SetImage(m_SubStatsTexture2D);
        scannedStr += m_Ocr.GetText();

        // Update debugging UI
        OcrOutputImage.texture = m_SubStatsTexture2D;
        rectTransform = OcrOutputImage.GetComponent<RectTransform>();
        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            rectTransform.rect.width * m_SubStatsTexture2D.height / m_SubStatsTexture2D.width
        );
        OcrOutputText.text = scannedStr;

        // Process scanned string
        m_CurrText = scannedStr;

        var equipment = new Equipment();
        equipment.TryParse(m_CurrText);

        GearRankText.text = equipment.Rank.ToString();

        var gearScoreTextArr = new TMP_Text[] {
            GearScore1Text,
            GearScore2Text,
            GearScore3Text,
            GearScore4Text
        };
        foreach (var gsText in gearScoreTextArr) {
            gsText.text = 0m.ToString("F1");
        }

        decimal gearScoreTotal = 0m;
        decimal gearScoreLowest = 8m;
        decimal gearScoreTotalModded = 0m;
        for (var i = 0; i < equipment.Stats.Count; i++) {
            decimal gearScore = equipment.Stats[i].GetGearScore();
            gearScoreTextArr[i].text = gearScore.ToString("F1");
            gearScoreTotal += gearScore;
            gearScoreTotalModded += gearScore;
            // Finding out which are the lowest gear score stat
            gearScoreLowest = Math.Min(gearScoreLowest, gearScore);
        }
        // Replace the lowest gear score stat with 8
        gearScoreTotalModded += -gearScoreLowest + 8;

        GearScoreTotalText.text = gearScoreTotal.ToString("F2");
        GearScoreTotalModdedText.text = gearScoreTotalModded.ToString("F2");

        // Text formating for fast gear check
        if (Constants.EquipmentRank2GearScoreMinMax.TryGetValue(equipment.Rank, out var value)) {
            GearScoreTotalText.color = gearScoreTotal < value.Min ? Color.red :
                gearScoreTotal >= value.Max ? Color.green : Color.yellow;
            GearScoreTotalModdedText.color = gearScoreTotalModded < value.Min ? Color.red :
                gearScoreTotalModded >= value.Max ? Color.green : Color.yellow;
        }
        else {
            GearScoreTotalText.color = Color.white;
            GearScoreTotalModdedText.color = Color.white;
        }
#if UNITY_EDTIOR
        var sb = new StringBuilder();
        foreach (var t in equipment.Stats) {
            sb.Append($"{t.Type.ToString()}({t.RollCount}): {t.Value}");
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
#endif
    }
}