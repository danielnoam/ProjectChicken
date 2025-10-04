using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PassthroughTrigger : MonoBehaviour
{
    
    
    public bool playerPassedThrough;
    public bool playerEnteredTrigger;
    public bool PlayerIsPassingThrough => !playerPassedThrough && playerEnteredTrigger;
    
    public event Action OnPlayerPassedThrough;
    public event Action OnPlayerEnteredTrigger;
    

    private void OnTriggerEnter(Collider other)
    {
        if (playerPassedThrough || playerEnteredTrigger) return;

        if (other.TryGetComponent(out RailPlayer player))
        {
            playerEnteredTrigger = true;
            OnPlayerEnteredTrigger?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerPassedThrough || !playerEnteredTrigger) return;

        playerPassedThrough = true;
        OnPlayerPassedThrough?.Invoke();
    }
}
