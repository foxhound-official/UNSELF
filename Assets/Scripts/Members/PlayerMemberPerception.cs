using UnityEngine;

public class PlayerMemberPerception : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private MemberAssembly memberAssembly;
    [SerializeField] private float perceptionDistance = 8f;

    private IMemberPerceptionTarget currentTarget;

    private void Update()
    {
        IMemberPerceptionTarget newTarget = FindTarget();

        if (ReferenceEquals(newTarget, currentTarget))
            return;

        currentTarget?.OnMemberFocusExit();

        currentTarget = newTarget;
        currentTarget?.OnMemberFocusEnter(memberAssembly);
    }

    private IMemberPerceptionTarget FindTarget()
    {
        Ray ray =
            new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, perceptionDistance))
            return null;

        return hit.collider.GetComponentInParent<IMemberPerceptionTarget>();
    }
}