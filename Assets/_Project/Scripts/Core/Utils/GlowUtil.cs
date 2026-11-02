// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Cambia el color y el brillo de varios renderers sin crear materiales nuevos (MaterialPropertyBlock).

using UnityEngine;

public static class GlowUtil
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static MaterialPropertyBlock block;

    public static void SetColor(Renderer[] renderers, Color color, float emissionIntensity = 2f)
    {
        if (renderers == null) return;
        if (block == null) block = new MaterialPropertyBlock();

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(EmissionColorId, color * emissionIntensity);
            r.SetPropertyBlock(block);
        }
    }
}
