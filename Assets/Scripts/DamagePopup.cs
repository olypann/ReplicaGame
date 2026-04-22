using EasyTextEffects;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float lifetime = 1f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private TextMeshPro damageText;
    private Color textColor;
    private float startLifetime;

    public void Setup(string damageTaken, Color color, bool effects)
    {
        // cache lifetime
        startLifetime = lifetime;

        // get components
        damageText = GetComponent<TextMeshPro>();

        // fallback camera 
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // set text
        damageText.SetText(damageTaken);
        damageText.color = color;
        textColor = color;

        // effects
        var textEffect = GetComponent<TextEffect>();

        if (textEffect != null)
        {
            if (effects)
            {
                textEffect.StartManualEffects();
            }

            else
            {
                textEffect.StartManualEffect("killwave");
            }
                
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        // face camera
        transform.LookAt(2 * transform.position - cameraTransform.position);

        // move up
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // lifetime
        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
        {
            textColor.a -= 5f * Time.deltaTime;
            damageText.color = textColor;

            if (textColor.a <= 0f)
            {
                Destroy(gameObject);
            }
        }

        // scale over lifetime
        float scaleSpeed = 0.8f;

        if (lifetime > startLifetime * 0.5f)
        {
            transform.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
        }
        else
        {
            transform.localScale -= Vector3.one * scaleSpeed * Time.deltaTime;
        }
    }
}