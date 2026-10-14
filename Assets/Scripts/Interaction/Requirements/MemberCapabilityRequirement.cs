using UnityEngine;

public class MemberCapabilityRequirement : InteractionRequirement
{
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private MemberCapability requiredCapability;

    public override bool IsMet =>
        memberAssembly != null &&
        requiredCapability != null &&
        memberAssembly.HasCapability(requiredCapability);
}