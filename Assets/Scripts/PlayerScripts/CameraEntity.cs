using UnityEngine;

public class CameraEntity : MonoBehaviour
{
    public enum CameraRotationAxis
    {
        Roll,
        Yaw,
        Pitch
    }

    [Header("refs")]
    private ThirdPersonCamera cam;

    [Header("screen damage")]
    public float screenDamage;

    [Header("push settings")]
    [SerializeField] private float pushStrength = 1.2f;
    [SerializeField] private float pushReturnSpeed = 2f;
    [SerializeField] private Vector3 pushAxisMask = new Vector3(1f, 0f, 1f); // y = 0 prevents vertical push

    [Header("rotation settings")]
    [SerializeField] private float rotateStrength = 10f;
    [SerializeField] private float rotateReturnSpeed = 3f;
    [SerializeField] private CameraRotationAxis rotationAxis = CameraRotationAxis.Roll;

    [Header("screen impact shake")]
    [SerializeField] private float screenShakeStrength = 0.15f;
    [SerializeField] private float screenShakeReturnSpeed = 6f;


    private bool isBusy;

    private void Start()
    {
        cam = GetComponent<ThirdPersonCamera>();
    }

    public void HitRotate(Vector3 attackDir)
    {
        if (isBusy)
        {
            return;
        }

        if (cam != null)
        {
            cam.AddExternalRotation(attackDir, rotateStrength, rotateReturnSpeed, rotationAxis);
        }
    }

    public void HitPush(Vector3 attackDir)
    {
        if (isBusy)
        {
            return;
        }

        if (cam != null)
        {
            cam.AddExternalImpulse(attackDir, pushStrength, pushReturnSpeed, pushAxisMask);
        }
    }

    public void HitScreen(float amount)
    {
        screenDamage += amount;

        Debug.Log("screen damage: " + screenDamage);

        TriggerScreenShake();
    }

    public void TriggerScreenShake()
    {
        if (cam != null)
        {
            cam.AddScreenShake(screenShakeStrength, screenShakeReturnSpeed, 0.2f);
        }
    }
}