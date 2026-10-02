using UnityEngine;

public class MemberPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private MemberDefinition member;
    [SerializeField] private MemberInventory memberInventory;

    public string InteractionPrompt => "TAKE MEMBER";

    public void Interact()
    {
        if (!memberInventory.TryAddMember(member))
            return;

        Debug.Log($"MEMBER ACQUIRED: {member.Code} // {member.MemberName}");

        gameObject.SetActive(false);
    }
}