using TMPro;
using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] private GameObject damagePopupPrefab;

    [Header("Text")]
    [SerializeField] private string killText;
    [SerializeField] private string weakText;
    [SerializeField] private string resText;

    [Header("Colors")]
    [SerializeField] private Color playerHitColor;
    [SerializeField] private Color enemyHitColor;
    [SerializeField] private Color highDamageColor;
    [SerializeField] private Color weakColor;
    [SerializeField] private Color resColor;


    [Header("Settings")]
    [SerializeField] private float highDamageThreshold = 20f;
    [SerializeField] private int sortingOrder = 1;


    public void SpawnDamagePopup(float damage, Transform target, bool isPlayerHit, bool isKill, bool isWeak, bool isRes)
    {
        if (damage <= 0f || damagePopupPrefab == null || target == null)
        {
            return;
        }

        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(0.1f, 0.5f);

        Vector3 spawnPos = new Vector3(
            target.position.x + randomX,
            target.position.y + randomY,
            target.position.z
        );

        GameObject popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
        //popup.GetComponent<DamagePopup>().SetCamera(Camera.main.transform);

        TextMeshPro text = popup.GetComponent<TextMeshPro>();
        DamagePopup popupScript = popup.GetComponent<DamagePopup>();

        float finalDamage = Mathf.FloorToInt(damage);

        // base color
        Color color = isPlayerHit ? playerHitColor : enemyHitColor;

        // high damage override
        if (finalDamage >= highDamageThreshold)
        {
            color = highDamageColor;
        }

        // kill text
        if (isKill)
        {
            popupScript.Setup($"{killText}", Color.red, true);
        }
        else
        {
            popupScript.Setup(finalDamage.ToString(), color, false);
        }

        text.sortingOrder = sortingOrder;


        // weak popup
        if (isWeak)
        {
            SpawnExtraText(target, weakText, weakColor);
        }

        // resist popup
        if (isRes)
        {
            SpawnExtraText(target, resText, resColor);
        }

        // scale with damage
        if (finalDamage >= highDamageThreshold)
        {
            text.fontSize *= finalDamage / highDamageThreshold;
        }

    }

    private void SpawnExtraText(Transform target, string message, Color color)
    {
        float randomX = Random.Range(-1f, 1f);
        float randomY = Random.Range(0.1f, 0.5f);

        Vector3 spawnPos = new Vector3(
            target.position.x + randomX,
            target.position.y + randomY,
            target.position.z
        );
        

        GameObject popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);

        TextMeshPro text = popup.GetComponent<TextMeshPro>();
        DamagePopup popupScript = popup.GetComponent<DamagePopup>();

        popupScript.Setup($"{message}", color, false);

        text.sortingOrder = sortingOrder;
    }
}