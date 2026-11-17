using System;

// A finite mission resource. Waiting has no effect; only a new level/run refills it.
public sealed class UXSprintReserve
{
    public const float CapacitySeconds = 10f;
    public float Remaining { get; private set; }
    public bool Empty => Remaining <= 0f;
    public UXSprintReserve(float remaining = CapacitySeconds)
    {
        Remaining = float.IsNaN(remaining) || float.IsInfinity(remaining) ? 0f :
            Math.Max(0f, Math.Min(CapacitySeconds, remaining));
    }
    public float Spend(float seconds)
    {
        if(seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))return 0f;
        float spent = Math.Min(seconds, Remaining);
        Remaining = Math.Max(0f, Remaining - spent);
        return spent;
    }
}
