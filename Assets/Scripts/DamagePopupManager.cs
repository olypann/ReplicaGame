// //using EasyTextEffects;
// using TMPro;
// using UnityEngine;

// public class DamagePopupManager : MonoBehaviour
// {
//     public static DamagePopupManager Instance;
//     private void Awake() => Instance = this;

//     [SerializeField] GameObject _damageIndicator;

//     [Header("Text Popups")]
//     [SerializeField] string KillText;
//     [SerializeField] string WeakText;
//     [SerializeField] string ResText;
//     [SerializeField] int popUpSortingOrder = 1; // 1 being in front of everything
    
//     [Header("Colours")]
//     [SerializeField] Color Neutral;
//     [SerializeField] Color Fire;
//     [SerializeField] Color Water;
//     [SerializeField] Color Earth;
//     [SerializeField] Color Electricity;
//     [SerializeField] Color Air;
//     [SerializeField] Color Ice;
//     [SerializeField] Color Light;
//     [SerializeField] Color Dark;
//     [SerializeField] Color Poison;
    
//     [SerializeField] Color WeakColor;
//     [SerializeField] Color ResColor;
    
//     private Color textColor;

//     public void SpawnDamageIndicator(float damageTaken, Transform parent, Element damageType, bool Kill, bool Weak, bool Res)
//     {
//         damageTaken = (int)damageTaken;
//         if (damageTaken <= 0) return;
//         float randomPosx = Random.Range(-1f, 1f);
//         float randomPosy = Random.Range(0.1f, 0.5f);

//         float randomScale = Random.Range(1f, 2f);

//         #region DamageTypeStack

//         switch (damageType)
//         {
//             case Element.None:
//                 textColor = Neutral;
//                 break;
//             case Element.Fire:
//                 textColor = Fire;
//                 break;
//             case Element.Water:
//                 textColor = Water;
//                 break;
//             case Element.Earth:
//                 textColor = Earth;
//                 break;
//             case Element.Electricity:
//                 textColor = Electricity;
//                 break;
//             case Element.Air:
//                 textColor = Air;
//                 break;
//             case Element.Ice:
//                 textColor = Ice;
//                 break;
//             case Element.Light:
//                 textColor = Light;
//                 break;
//             case Element.Dark:
//                 textColor = Dark;
//                 break;
//             case Element.Poison:
//                 textColor = Poison;
//                 break;
//         }

//         #endregion

//         GameObject _dmg_ = Instantiate(_damageIndicator, 
//             new Vector3(parent.transform.position.x + randomPosx, parent.transform.position.y + randomPosy, parent.transform.position.z), Quaternion.identity);

//         _dmg_.GetComponent<TextMeshPro>().fontSize *= randomScale;

//         if (Kill) // Kill Text
//         {
//             _dmg_.GetComponent<DamageIndicator>().Setup($"{KillText}<br><br> ", Color.red, true);
//             _dmg_.GetComponent<TextMeshPro>().sortingOrder = popUpSortingOrder;
            
//         }
//         else // Normal Dmg
//         {
//             _dmg_.GetComponent<DamageIndicator>().Setup(damageTaken.ToString(), textColor, false);
//         }

//         if (Weak) // Weak text
//         {
//             GameObject _dmgweak_ = Instantiate(_damageIndicator,
//                 new Vector3(parent.transform.position.x + randomPosx, parent.transform.position.y + randomPosy, parent.transform.position.z), Quaternion.identity);
            
//             _dmgweak_.GetComponent<TextMeshPro>().fontSize *= randomScale;
//             _dmgweak_.GetComponent<DamageIndicator>().Setup($"{WeakText}<br><br> ", WeakColor, false);
 
//             _dmgweak_.GetComponent<TextMeshPro>().sortingOrder = popUpSortingOrder;
//         }

//         if (Res) // Resistant text
//         {
//             GameObject _dmgres_ = Instantiate(_damageIndicator,
//                     new Vector3(parent.transform.position.x + randomPosx, parent.transform.position.y + randomPosy, parent.transform.position.z), Quaternion.identity);

//             _dmgres_.GetComponent<TextMeshPro>().fontSize *= randomScale;
//             _dmgres_.GetComponent<DamageIndicator>().Setup($"{ResText}<br><br> ", ResColor, false);

//             _dmgres_.GetComponent<TextMeshPro>().sortingOrder = popUpSortingOrder;
//         }

//         if (damageTaken >= 15f) // Bigger number == bigger number
//         {
//             _dmg_.GetComponent<TextMeshPro>().fontSize *= damageTaken / 15f;
//         }
//     }
// }
