using UnityEngine;

public class SnowPathDrawer : MonoBehaviour
{
    [Header("compute shader setup")]
    public ComputeShader snowComputeShader;
    public RenderTexture snowRT;

    [Header("footstep settings")]
    public float spotSize = 5f;


    private PlayerMovement playerMovement;

    private GameObject[] snowControllerObjs;

    private SnowController snowController;

    private Vector2Int position = new Vector2Int(256, 256);


    //property names
    private string snowImageProperty = "snowImage";
    private string colorValueProperty = "colorValueToAdd";
    private string resolutionProperty = "resolution";
    private string positionXProperty = "positionX";
    private string positionYProperty = "positionY";
    private string spotSizeProperty = "spotSize";

    // compute shader kernel name
    private string drawSpotKernel = "DrawSpot";


    private void Awake()
    {
        // grab all snow surfaces in the scene
        snowControllerObjs = GameObject.FindGameObjectsWithTag("Ground");

        // used for grounded checks before drawing footprints
        playerMovement = GetComponent<PlayerMovement>();
    }


    private void FixedUpdate()
    {
        // dont draw trails while the player is airborne
        if (playerMovement == null || !playerMovement.Grounded)
        {
            return;
        }

        // check nearby snow surfaces and draw onto them
        for (int i = 0; i < snowControllerObjs.Length; i++)
        {
            // skip surfaces that are too far away
            if (Vector3.Distance(snowControllerObjs[i].transform.position, transform.position) > spotSize * 5f)
            {
                continue;
            }

            // get the snow controller for this surface
            snowController = snowControllerObjs[i].GetComponent<SnowController>();

            // use this surface's render texture
            snowRT = snowController.snowRT;


            // convert world position into texture space
            GetPosition();
            // draw the footprint into the render texture
            DrawSpot();
        }
    }


    // converts the player world position into render texture coordinates
    private void GetPosition()
    {

        float scaleX = snowController.transform.localScale.x;
        float scaleY = snowController.transform.localScale.z;

        float snowPosX = snowController.transform.position.x;
        float snowPosY = snowController.transform.position.z;

        int posX = snowRT.width / 2 - (int)(((transform.position.x - snowPosX) * snowRT.width / 2) / scaleX);
        int posY = snowRT.height / 2 - (int)(((transform.position.z - snowPosY) * snowRT.height / 2) / scaleY);


        position = new Vector2Int(posX, posY);
    }


    // sends the footprint data into the compute shader
    private void DrawSpot()
    {
        // nothing to draw onto
        if (snowRT == null)
        {
            return;
        }

        // compute shader missing
        if (snowComputeShader == null)
        {
            return;
        }

        int kernelHandle = snowComputeShader.FindKernel(drawSpotKernel);

        // assign the texture the shader will modify
        snowComputeShader.SetTexture(kernelHandle, snowImageProperty, snowRT);

        // shader values used for drawing the footprint
        snowComputeShader.SetFloat(colorValueProperty, 0);

        snowComputeShader.SetFloat(resolutionProperty, snowRT.width);
        snowComputeShader.SetFloat(positionXProperty, position.x);
        snowComputeShader.SetFloat(positionYProperty, position.y);
        snowComputeShader.SetFloat(spotSizeProperty, spotSize);


        // run the shader across the texture
        snowComputeShader.Dispatch(kernelHandle, snowRT.width / 8, snowRT.height / 8, 1);
    }
}