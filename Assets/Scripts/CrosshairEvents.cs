using UnityEngine;

public class CrosshairEvents : MonoBehaviour
{
    public static CrosshairEvents Instance;

    private void Awake()
    {
        Instance = this;
    }

    public System.Action OnPlayerTargeted;
    public System.Action OnCameraTargeted;

    public void TriggerPlayerTarget()
    {
        if (OnPlayerTargeted != null)
        {
            OnPlayerTargeted.Invoke();
        }
    }

    public void TriggerCameraTarget()
    {
        if (OnCameraTargeted != null)
        {
            OnCameraTargeted.Invoke();
        }
    }
}