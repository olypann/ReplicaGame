using UnityEngine;

public class ScreenCrackUI : MonoBehaviour
{
    public CameraEntity camEntity;
    public Material mat;

    [Header("fade settings")]
    public float restoreRate = 1f;

    void Update()
    {
        if (camEntity == null || mat == null)
        {
            return;
        }

        camEntity.screenDamage -= restoreRate * Time.deltaTime;

        if (camEntity.screenDamage < 0f)
        {
            camEntity.screenDamage = 0f;
        }

        mat.SetFloat("_Damage", camEntity.screenDamage);
    }
}