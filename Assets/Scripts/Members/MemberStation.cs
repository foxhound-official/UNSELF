using UnityEngine;

public class MemberStation : MonoBehaviour, IInteractable
{
    [SerializeField] private MemberMenuController memberMenu;

    public string InteractionPrompt => "ACCESS MEMBER STATION";

    public void Interact()
    {
        memberMenu.OpenAssemblyEditor();
    }
}