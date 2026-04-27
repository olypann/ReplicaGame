using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;

    private int attackType;
    private float screenDamageAmount;

    public void Init(Vector3 dir, float spd, int type, float dmg)
    {
        direction = dir;
        speed = spd;
        attackType = type;
        screenDamageAmount = dmg;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        CameraEntity cam = other.GetComponentInParent<CameraEntity>();

        if (cam != null)
        {
            if (attackType == 0)
            {
                cam.HitRotate(direction);
            }
            else if (attackType == 1)
            {
                cam.HitPush(direction);
            }
            else if (attackType == 2)
            {
                cam.HitScreen(screenDamageAmount);
            }
            else if (attackType == 3)
            {
                cam.HitScreen(screenDamageAmount);
                cam.HitPush(direction);
            }

            Destroy(gameObject);
        }
    }
}