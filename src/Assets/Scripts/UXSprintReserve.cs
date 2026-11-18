using System;

// A finite mission resource. Waiting has no effect; only a new level/run refills it.
public sealed class UXSprintReserve
{
    public const float CapacitySeconds = 10f;
    public const float CrimsonCapacitySeconds = 20f;
    public float Capacity { get; private set; }
    public float Remaining { get; private set; }
    public bool Empty => Remaining <= 0f;
    public UXSprintReserve(float remaining = CapacitySeconds, float capacity = CapacitySeconds)
    {
        Capacity = float.IsNaN(capacity) || float.IsInfinity(capacity) || capacity <= 0f ? CapacitySeconds : capacity;
        Remaining = float.IsNaN(remaining) || float.IsInfinity(remaining) ? 0f :
            Math.Max(0f, Math.Min(Capacity, remaining));
    }
    public float Spend(float seconds)
    {
        if(seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))return 0f;
        float spent = Math.Min(seconds, Remaining);
        Remaining = Math.Max(0f, Remaining - spent);
        return spent;
    }
}
