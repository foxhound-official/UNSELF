using UnityEngine;

public class DisableObjectAction : WorldAction
{
    [SerializeField] private GameObject target;

    public override void Execute()
    {
        if (target != null)
            target.SetActive(false);
    }
}