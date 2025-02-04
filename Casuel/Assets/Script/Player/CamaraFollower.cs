using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Follow Target Settings")]
    [Tooltip("Transformen (fx spilleren) som kameraet skal følge.")]
    public Transform target;

    [Header("Movement Settings")]
    [Tooltip("Bestemmer hvor hurtig/blød følgebevægelsen er.")]
    public float smoothSpeed = 0.125f;

    [Tooltip("Offset fra target-positionen. For 2D er z typisk -10.")]
    public Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Optional Bounds")]
    [Tooltip("Slå bounding til/fra. Hvis aktiv, kan kameraet ikke rykke uden for min/max.")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    // Intern variabel til at gemme hastighed for SmoothDamp
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (!target)
        {
            Debug.LogWarning("CameraFollow2D: 'target' er ikke sat i Inspector!");
            return;
        }

        // Udregn ønsket position ud fra spillerens position + offset
        Vector3 desiredPosition = target.position + offset;

        // Hvis bounding er slået til, så begræns x og y, så kameraet ikke kommer uden for min/max
        if (useBounds)
        {
            float clampedX = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            float clampedY = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
            desiredPosition = new Vector3(clampedX, clampedY, desiredPosition.z);
        }

        // SmoothDamp gør kamerabevægelsen blød ved at interpolere mellem nuværende og ønsket position
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothSpeed
        );

        // Opdater kameraets position
        transform.position = smoothedPosition;
    }
}
