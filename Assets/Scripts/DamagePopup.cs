// //using EasyTextEffects;
// using TMPro;
// using UnityEngine;

// public class DamagePopup : MonoBehaviour
// {

//     [SerializeField] float moveSpeed = 1f;
//     [SerializeField] float lifetime = 1f;

//     private TextMeshPro _damageIndicator;
//     private Color _textColor;
//     private Transform _cameraTransform;

//     public void Setup(string damageTaken, Color textColor, bool effects)
//     {
//         _cameraTransform = Camera.main.transform;
//         _damageIndicator = GetComponent<TextMeshPro>();
        
//         _damageIndicator.SetText($"{damageTaken}");
        
//         _damageIndicator.color = textColor;
//         _textColor = _damageIndicator.color; 
  
//         var currentText = gameObject.GetComponent<TextEffect>(); // Easy Text Effects
//         if (effects)
//             currentText.StartManualEffects();
//         else
//             currentText.StartManualEffect("killwave");
//     }

//     private void LateUpdate()
//     {
//         transform.LookAt(2 * transform.position - _cameraTransform.position);

//         transform.position += new Vector3(0f, moveSpeed * Time.deltaTime, 0f);

//         lifetime -= Time.deltaTime;
//         if (lifetime <= 0f)
//         {
//             _textColor.a -= 5f * Time.deltaTime;
//             _damageIndicator.color = _textColor;

//             if (_textColor.a <= 0f)
//             {
//                 Destroy(gameObject);
//             }         
//         }

//         if (lifetime > lifetime * 0.5f)
//         {
//             float incraseScaleAmount = 0.8f;
//             transform.localScale += incraseScaleAmount * Time.deltaTime * Vector3.one;
//         }
//         else
//         {
//             float incraseScaleAmount = 0.8f;
//             transform.localScale -= incraseScaleAmount * Time.deltaTime * Vector3.one;
//         }
//     }
// }
