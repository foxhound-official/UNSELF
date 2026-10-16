using UnityEngine;

public struct TraversalCandidate
{
    public Collider Collider { get; }
    public Vector3 ObstaclePoint { get; }
    public Vector3 ObstacleNormal { get; }
    public Vector3 TopPoint { get; }
    public Vector3 TopNormal { get; }
    public float Height { get; }

    public TraversalCandidate(Collider collider, Vector3 obstaclePoint, Vector3 obstacleNormal, Vector3 topPoint, Vector3 topNormal, float height)
    {
        Collider = collider;
        ObstaclePoint = obstaclePoint;
        ObstacleNormal = obstacleNormal;
        TopPoint = topPoint;
        TopNormal = topNormal;
        Height = height;
    }
}