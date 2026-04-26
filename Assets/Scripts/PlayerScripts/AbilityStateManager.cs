using UnityEngine;
using System.Collections;


public class AbilityStateManager : MonoBehaviour
{
    public static AbilityStateManager Instance;

    public bool isAbilityActive;
    public bool isFreezeAbilityActive;

    public bool isCameraThrowActive;


    private void Awake()
    {
        Instance = this;
    }

    
}
