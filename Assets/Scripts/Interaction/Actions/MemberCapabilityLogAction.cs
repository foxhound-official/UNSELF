using UnityEngine;

public class MemberCapabilityLogAction : WorldAction
{
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberHudMenuController memberHud;
    [SerializeField] private MemberCapability capability;
    [SerializeField] private string message;

    public override void Execute()
    {
        MemberDefinition member = memberAssembly.GetFirstMemberWithCapability(capability);

        if (member != null && memberHud != null)
            memberHud.ShowMemberLog(member, message);
    }
}