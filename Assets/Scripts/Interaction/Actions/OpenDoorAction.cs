using UnityEngine;

public class OpenDoorAction : WorldAction
{
    [SerializeField] private SimpleDoor door;

    public override void Execute()
    {
        if (door == null)
            return;

        door.Unlock();
        door.Open();
    }
}