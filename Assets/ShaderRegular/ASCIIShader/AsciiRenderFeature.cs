using UnityEngine;

// keeps this active even in edit mode so you can preview the effect without entering play mode
[ExecuteAlways]
public class ASCII_PostProcess : MonoBehaviour
{
    public Material material;


    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // if no material is assigned just pass the image through unchanged
        if (material == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // apply post process material
        Graphics.Blit(src, dest, material);
    }
}