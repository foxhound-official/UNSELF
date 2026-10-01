using UnityEngine;

public class DebugInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log($"Interacted with {gameObject.name}");
    }
}