using UnityEngine;

public class BlobShadowFollow : MonoBehaviour
{
    public Transform target;

    public float rayDistance = 50f;

    public float projectorHeight = 1f;

    void LateUpdate()
    {
        if (!target) return;

        Ray ray = new Ray(target.position + Vector3.up, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            // Place projector ABOVE ground
            transform.position = hit.point + Vector3.up * projectorHeight;

            // Project downward
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}