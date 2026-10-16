using UnityEngine;

public class PullUpPoint : MonoBehaviour
{
    [SerializeField] private Transform hangPoint;
    [SerializeField] private Transform standPoint;
    [SerializeField] private float activationRange = 0.6f;

    public Transform HangPoint => hangPoint;
    public Transform StandPoint => standPoint;

    public bool IsInRange(Vector3 position)
    {
        if (hangPoint == null)
            return false;

        return Vector3.Distance(position, hangPoint.position) <= activationRange;
    }

    public float GetDistance(Vector3 position)
    {
        if (hangPoint == null)
            return float.MaxValue;

        return Vector3.Distance(position, hangPoint.position);
    }
}