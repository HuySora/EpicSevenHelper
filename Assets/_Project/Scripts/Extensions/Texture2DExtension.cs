using UnityEngine;

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
}