// using UnityEngine;

// public class CameraProjectileHitbox : MonoBehaviour
// {
//     private CameraThrowAbility ability;

//     private void Start()
//     {
//         ability = GetComponentInParent<CameraThrowAbility>();
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         EnemyScript enemy = other.GetComponentInParent<EnemyScript>();

//         if (enemy == null)
//         {
//             return;
//         }

//         ability?.RegisterHit(enemy);
//     }
// }