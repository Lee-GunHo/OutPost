using UnityEngine;

// Flight state and combat data; no rendering or scene queries.
public sealed class SwordWaveModel
{
    public Vector3 Position { get; private set; }
    public Vector3 Direction { get; }
    public int Damage { get; }
    public float RemainingDistance { get; private set; }
    public float Radius => 0.55f;
    public bool Resolved { get; private set; }

    public SwordWaveModel(Vector3 origin, Vector3 direction, int damage, float range)
    {
        Position = origin;
        Direction = direction.normalized;
        Damage = Mathf.Max(1, damage);
        RemainingDistance = Mathf.Max(0f, range);
    }

    public float StepDistance(float deltaTime) => Mathf.Min(RemainingDistance, 18f * deltaTime);
    public void Advance(float distance)
    {
        Position += Direction * distance;
        RemainingDistance = Mathf.Max(0f, RemainingDistance - distance);
    }
    public void Resolve() { Resolved = true; }
}
