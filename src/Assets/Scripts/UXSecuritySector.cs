using UnityEngine;

// Membership is fixed while patrolling or investigating; guards do not flood
// another sector when the player crosses a bulkhead.
public sealed class UXSecuritySector : MonoBehaviour
{
    public int sector;
}
