using UnityEngine;

public enum FootstepSurfaceType
{
    Concrete,
    Metal,
    Dirt,
    Gravel,
    Asphalt
}

[DisallowMultipleComponent]
public class FootstepSurface : MonoBehaviour
{
    [SerializeField] private FootstepSurfaceType surfaceType = FootstepSurfaceType.Concrete;

    public string SoundId
    {
        get
        {
            switch (surfaceType)
            {
                case FootstepSurfaceType.Metal:
                    return "footstep_metal";
                case FootstepSurfaceType.Dirt:
                    return "footstep_dirt";
                case FootstepSurfaceType.Gravel:
                    return "footstep_gravel";
                case FootstepSurfaceType.Asphalt:
                    return "footstep_asphalt";
                default:
                    return "footstep_concrete";
            }
        }
    }
}