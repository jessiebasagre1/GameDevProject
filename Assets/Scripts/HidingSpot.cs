using UnityEngine;

// Put this on the bed (or any hiding place). The bed needs a Collider so the player can look at it.
// Create two empty child objects and drag them in:
//   hidePoint - on the FLOOR under the bed. Its blue (forward) arrow points the way you look out from under it.
//   exitPoint - on the FLOOR beside the bed, where you get in and out.
public class HidingSpot : MonoBehaviour
{
    public Transform hidePoint;
    public Transform exitPoint;

    [Tooltip("Camera height above the floor while hiding. Under a bed ~0.25, inside a wardrobe ~1.5")]
    public float cameraHeightAboveFloor = 0.25f;

    [Tooltip("How far you can look left/right and up/down while hiding")]
    public float lookYawLimit = 70f;
    public float lookPitchLimit = 25f;

    [HideInInspector] public bool IsOccupied;

    void OnDrawGizmosSelected()
    {
        if (hidePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(hidePoint.position, 0.25f);
            Gizmos.DrawRay(hidePoint.position, hidePoint.forward);
        }
        if (exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(exitPoint.position, 0.25f);
        }
    }
}
