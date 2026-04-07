using UnityEngine;

[ExecuteAlways]
public class ASCII_PostProcess : MonoBehaviour
{
    public Material material;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (material == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        Graphics.Blit(src, dest, material);
    }
}