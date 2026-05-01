using UnityEngine;

public class SnowController : MonoBehaviour
{
    [Header("snow setup")]
    public ComputeShader snowComputeShader;

    [HideInInspector]
    public RenderTexture snowRT;

    public int resolution = 512;
    public float colorValueToAdd;


    // shader property names
    private string snowImageProperty = "snowImage";
    private string colorValueProperty = "colorValueToAdd";
    private string resolutionProperty = "resolution";
    private string positionXProperty = "positionX";
    private string positionYProperty = "positionY";
    private string spotSizeProperty = "spotSize";

    // kernels
    private string csMainKernel = "CSMain";
    private string fillWhiteKernel = "FillWhite";



    private MeshRenderer meshRenderer;


    private void Awake()
    {
        CreateRenderTexture();
        SetRTColorToWhite();
        SetMaterialTexture();

        // slowly fills the texture back over time
        InvokeRepeating(nameof(AddSnowLayer), 0.1f, 0.1f);

        ExtendBoundsOfMesh();
    }



    //creates the texture the shader writes into
    private void CreateRenderTexture()
    {
        snowRT = new RenderTexture(resolution, resolution, 24);

        snowRT.enableRandomWrite = true;
        snowRT.Create();
    }


    // starts the texture fully white
    private void SetRTColorToWhite()
    {
        int kernelHandle = snowComputeShader.FindKernel(fillWhiteKernel);

        snowComputeShader.SetTexture(kernelHandle, snowImageProperty, snowRT);

        snowComputeShader.SetFloat(colorValueProperty, colorValueToAdd);
        snowComputeShader.SetFloat(resolutionProperty, resolution);

        snowComputeShader.SetFloat(positionXProperty, 0);
        snowComputeShader.SetFloat(positionYProperty, 0);
        snowComputeShader.SetFloat(spotSizeProperty, 0);

        snowComputeShader.Dispatch(kernelHandle, snowRT.width / 8, snowRT.height / 8, 1);
    }


    //sends the render texture to the material
    private void SetMaterialTexture()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.material.SetTexture("_PathTexture", snowRT);
    }


    // slowly restores snow over footprints
    private void AddSnowLayer()
    {
        int kernelHandle = snowComputeShader.FindKernel(csMainKernel);

        snowComputeShader.SetTexture(kernelHandle, snowImageProperty, snowRT);

        snowComputeShader.SetFloat(colorValueProperty, colorValueToAdd);
        snowComputeShader.SetFloat(resolutionProperty, resolution);

        snowComputeShader.SetFloat(positionXProperty, 0);
        snowComputeShader.SetFloat(positionYProperty, 0);
        snowComputeShader.SetFloat(spotSizeProperty, 0);

        snowComputeShader.Dispatch(kernelHandle, snowRT.width / 8, snowRT.height / 8, 1);
    }


    // expands the mesh bounds so the shader effect doesnt get culled
    private void ExtendBoundsOfMesh()
    {
        Bounds bounds = GetComponent<MeshFilter>().mesh.bounds;

        bounds.extents = new Vector3(2, 0, 2);

        GetComponent<MeshFilter>().mesh.bounds = bounds;
    }
}