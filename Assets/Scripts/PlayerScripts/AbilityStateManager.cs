using UnityEngine;
using System.Collections;


public class AbilityStateManager : MonoBehaviour
{
    public static AbilityStateManager Instance;

    public bool isAbilityActive;
    public bool isFreezeAbilityActive;

    public bool isCameraThrowActive;

    public bool isCameraReturning;


    private void Awake()
    {
        Instance = this;
    }

    public bool IsAnyAbilityActive()
    {
        return isAbilityActive || isCameraThrowActive || isFreezeAbilityActive;
    }

    
}
