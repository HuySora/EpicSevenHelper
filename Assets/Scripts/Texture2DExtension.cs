using UnityEngine;

public static class Texture2DExtension {
    public static Texture2D NewRescale(this Texture2D srcTex, int width, int height) {
        var rt = RenderTexture.GetTemporary(width, height);
        rt.filterMode = FilterMode.Bilinear;

        RenderTexture.active = rt;
        Graphics.Blit(srcTex, rt);

        var retTex = new Texture2D(width, height, srcTex.format, false);
        retTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        retTex.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return retTex;
    }

    public static Texture2D NewCropped(this Texture2D srcTex, float startX, float startY, float width,
        float height) {
        // Clamp values to [0,1]
        startX = Mathf.Clamp01(startX);
        startY = Mathf.Clamp01(startY);
        width = Mathf.Clamp(width, 0, 1 - startX);
        height = Mathf.Clamp(height, 0, 1 - startY);

        // Convert normalized coordinates to pixel coordinates
        var x = Mathf.RoundToInt(srcTex.width * startX);
        var y = Mathf.RoundToInt(srcTex.height * startY);
        var w = Mathf.RoundToInt(srcTex.width * width);
        var h = Mathf.RoundToInt(srcTex.height * height);

        // Get pixels and create cropped texture
        var pixels = srcTex.GetPixels(x, y, w, h);
        var retTex = new Texture2D(w, h, srcTex.format, false);
        retTex.SetPixels(pixels);
        retTex.Apply();

        return retTex;
    }
    
    public static Texture2D ApplyWhiteMask(this Texture2D srcTex, float threshold = 0.01f, bool invert = false) {
        Color[] pixels = srcTex.GetPixels();
        for (int i = 0; i < pixels.Length; i++) {
            pixels[i] = (IsPixelWhite(pixels[i], threshold) ^ invert) ? Color.white : Color.black;
        }
        
        srcTex.SetPixels(pixels);
        srcTex.Apply();
        return srcTex;
    }
    private static bool IsPixelWhite(Color color, float threshold = 0.01f) {
        return Mathf.Abs(color.r - 1f) < threshold &&
               Mathf.Abs(color.g - 1f) < threshold &&
               Mathf.Abs(color.b - 1f) < threshold;
    }

    public static Texture2D ApplyBlackMask(this Texture2D srcTex, float threshold = 0.01f, bool invert = false) {
        Color[] pixels = srcTex.GetPixels();
        for (int i = 0; i < pixels.Length; i++) {
            pixels[i] = (IsPixelBlack(pixels[i], threshold) ^ invert) ? Color.black : Color.white;
        }
    
        srcTex.SetPixels(pixels);
        srcTex.Apply();
        return srcTex;
    }
    private static bool IsPixelBlack(Color color, float threshold = 0.01f) {
        return Mathf.Abs(color.r) < threshold &&
               Mathf.Abs(color.g) < threshold &&
               Mathf.Abs(color.b) < threshold;
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

    public static Texture2D NewBlendMultiply(this Texture2D srcTex, Texture2D blendTex) {
        if (srcTex.width != blendTex.width || srcTex.height != blendTex.height) {
            Debug.LogError("Textures dimensions must match");
            return null;
        }
        
        Texture2D retTex = new Texture2D(srcTex.width, srcTex.height, srcTex.format, false);
        
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
        
        retTex.SetPixels(resultPixels);
        retTex.Apply();
        return retTex;
    }
    
    public static Texture2D NewRGB24Texture(this Texture2D srcTex) {
        // Create a new Texture2D with RGB24 format
        Texture2D retTex = new Texture2D(srcTex.width, srcTex.height, TextureFormat.RGB24, false);

        // Retains the RGB pixels from the source texture
        Color[] rgbaPixels = srcTex.GetPixels();
        Color[] rgbPixels = new Color[rgbaPixels.Length];
        for (int i = 0; i < rgbaPixels.Length; i++) {
            rgbPixels[i] = new Color(rgbaPixels[i].r, rgbaPixels[i].g, rgbaPixels[i].b);
        }

        retTex.SetPixels(rgbPixels);
        retTex.Apply();
        return retTex;
    }
}