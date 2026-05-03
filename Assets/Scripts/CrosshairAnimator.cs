using UnityEngine;

public class CrosshairUIAnimator : MonoBehaviour
{
    public RectTransform root;

    public float rotationSpeed = 360f;
    public float animationDuration = 0.5f;

    private bool isAnimating = false;
    private float timer = 0f;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = root.localScale;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StartAnimation();
        }

        if (isAnimating == true)
        {
            Animate();
        }
    }

    void StartAnimation()
    {
        isAnimating = true;
        timer = 0f;
    }

    void Animate()
    {
        timer += Time.deltaTime;

        float progress = timer / animationDuration;

        //rotation
        float rotation = rotationSpeed * Time.deltaTime;
        root.Rotate(0f, 0f, rotation, Space.Self);

        // scale up down
        float scaleCurve = Mathf.Sin(progress * Mathf.PI);

        float scale = Mathf.Lerp(1f, 0.5f, scaleCurve);

        root.localScale = originalScale * scale;

        if (progress >= 1f)
        {
            isAnimating = false;
            root.localScale = originalScale;
            
            root.localRotation = Quaternion.identity;
        }
    }
}