using UnityEngine;
using Rect = UnityEngine.Rect;

public static class Texture2DExtension {
    public static void CopyRescaledPixels(Texture2D desTex, Texture2D srcTex) {
        int width = desTex.width;
        int height = desTex.height;

        var rt = RenderTexture.GetTemporary(width, height);
        rt.filterMode = FilterMode.Bilinear;

        RenderTexture.active = rt;
        // Use unlit/texture shader to preserve colors exactly (no gamma/lighting changes)
        Graphics.Blit(srcTex, rt);

        desTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        desTex.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
    }

    public static void CopyCroppedPixels(Texture2D desTex, Texture2D srcTex, int srcStartX, int srcStartY) {
        // Clamp to ensure we don't read outside source texture bounds
        int copyWidth = Mathf.Min(desTex.width, srcTex.width - srcStartX);
        int copyHeight = Mathf.Min(desTex.height, srcTex.height - srcStartY);

        // Validate we have a valid region to copy
        if (copyWidth <= 0 || copyHeight <= 0 || srcStartX < 0 || srcStartY < 0) {
            uDebug.LogError($"[CopyCroppedPixels]Invalid rect: x={srcStartX}, y={srcStartY}, w={copyWidth}, h={copyHeight}");
            return;
        }

        // Copy pixels from the source texture
        Color[] srcPixels = srcTex.GetPixels(srcStartX, srcStartY, copyWidth, copyHeight);
        desTex.SetPixels(0, 0, copyWidth, copyHeight, srcPixels);
        desTex.Apply();
    }

    public static Texture2D ApplyGrayscale(this Texture2D srcTex) {
        var pixels = srcTex.GetPixels();

        for (int i = 0; i < pixels.Length; i++) {
            float g = pixels[i].grayscale;
            pixels[i] = new Color(g, g, g, 1f);
        }

        srcTex.SetPixels(pixels);
        srcTex.Apply();
        return srcTex;
    }

    public static Texture2D ApplyAlphaContrast(this Texture2D srcTex, float threshold = 0.9f) {
        var originalPixels = srcTex.GetPixels32();
        var bwPixels = new Color32[originalPixels.Length];

        for (var i = 0; i < originalPixels.Length; i++) {
            var alpha = originalPixels[i].a;
            var alphaNormalized = alpha / 255f;

            var value = alphaNormalized >= threshold ? (byte)255 : (byte)0;
            bwPixels[i] = new Color32(value, value, value, 255);
        }

        srcTex.SetPixels32(bwPixels);
        srcTex.Apply();
        return srcTex;
    }

    public static Texture2D ApplyBinaryThreshold(this Texture2D srcTex, Color tarColor, float threshold = 0.01f, bool invert = false) {
        Color opposite = new Color(1f - tarColor.r, 1f - tarColor.g, 1f - tarColor.b, tarColor.a);
        Color[] pixels = srcTex.GetPixels();
        for (int i = 0; i < pixels.Length; i++) {
            bool matches = IsPixelNearColor(pixels[i], tarColor, threshold);
            pixels[i] = (matches ^ invert) ? tarColor : opposite;
        }
        srcTex.SetPixels(pixels);
        srcTex.Apply();
        return srcTex;
    }
    private static bool IsPixelNearColor(Color color, Color target, float threshold = 0.01f) {
        return Mathf.Abs(color.r - target.r) < threshold &&
            Mathf.Abs(color.g - target.g) < threshold &&
            Mathf.Abs(color.b - target.b) < threshold;
    }

    public static void BlendMultiply(Texture2D desTex, Texture2D srcTex, Texture2D blendTex) {
        // Validate
        if (desTex.width != srcTex.width || desTex.width != blendTex.width ||
            desTex.height != srcTex.height || desTex.height != blendTex.height) {
            uDebug.LogError($"[BlendMultiply] Mismatch size: desTex=({desTex.width}x{desTex.height}), srcTex=({srcTex.width}x{srcTex.height}), blendTex=({blendTex.width}x{blendTex.height})");
            return;
        }

        Color[] srcPixels = srcTex.GetPixels();
        Color[] blendPixels = blendTex.GetPixels();
        Color[] resultPixels = new Color[srcPixels.Length];
        for (int i = 0; i < srcPixels.Length; i++) {
            // Multiply color channels component-wise (RGB)
            float r = srcPixels[i].r * blendPixels[i].r;
            float g = srcPixels[i].g * blendPixels[i].g;
            float b = srcPixels[i].b * blendPixels[i].b;
            // Alpha can be handled separately; here we multiply as well
            float a = srcPixels[i].a * blendPixels[i].a;
            resultPixels[i] = new Color(r, g, b, a);
        }

        desTex.SetPixels(resultPixels);
        desTex.Apply();
    }
}