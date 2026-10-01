using UnityEngine;

public class TechnicalTerminal : MonoBehaviour, IInteractable
{
    [SerializeField] private MemberLoadout memberLoadout;

    public void Interact()
    {
        if (memberLoadout.HasMemberClass(MemberClass.Technical))
        {
            Debug.Log("TERMINAL: TECHNICAL ACCESS GRANTED");
            return;
        }

        Debug.Log("TERMINAL: ACCESS DENIED");
    }
}