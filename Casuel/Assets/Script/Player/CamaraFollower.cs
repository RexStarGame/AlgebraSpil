using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Follow Target Settings")]
    public Transform target;

    [Header("Grid Settings (Player Grid)")]
    [Tooltip("Dette er det grid, hvor kameraet vil forsøge at centrere sig om spilleren. Typisk samme som playerens, men kan afvige.")]
    public float gridSize = 1f;
    public Vector2 gridOrigin = Vector2.zero;
    public int gridWidth = 10;
    public int gridHeight = 10;

    [Tooltip("Snap kameraets X og Y til nærmeste grid-celle.")]
    public bool snapToGrid = true;

    [Header("Camera Movement Settings")]
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 0, -10f);

    [Header("Camera Bounds for Player Grid")]
    [Tooltip("Hvis true, begræns kameraet i spillerens grid. (minBounds til maxBounds+topPeekOffset)")]
    public bool useBounds = true;

    [Tooltip("Hvor meget kameraet kan bevæge sig ud over den øverste grænse.")]
    public float topPeekOffset = 2.0f;

    [Header("Camera's Own Grid (Optional)")]
    [Tooltip("Hvis true, bruger vi separat grid til kameraet selv, så det ikke følger spilleren, når kameraet er udenfor dette grid.")]
    public bool useCameraGridBounds = false;
    public Vector2 cameraGridOrigin = Vector2.zero;
    public int cameraGridWidth = 10;
    public int cameraGridHeight = 10;

    [Header("Top 3D Tilt Settings")]
    [Tooltip("Vinkel på kameraet i toppen (0 = fladt, 15 eller 30 = mere vip).")]
    public float topRotationAngle = 0f;
    [Tooltip("Hvor tidligt kameraet begynder at vippe. Jo større værdi, jo tidligere tiltes der.")]
    public float tiltDistance = 3f;

    private Vector2 minBounds;       // Player-grid minimum
    private Vector2 maxBounds;       // Player-grid maximum

    private Vector2 camGridMin;      // Kamera-grid minimum
    private Vector2 camGridMax;      // Kamera-grid maksimum

    private Vector3 velocity = Vector3.zero;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (!cam.orthographic)
        {
            Debug.LogWarning("Camera is not orthographic! For 2D, it's recommended to use an Orthographic camera.");
        }

        CalculatePlayerGridBounds();
        CalculateCameraGridBounds();
    }

    void LateUpdate()
    {
        if (!target)
        {
            Debug.LogWarning("CameraFollow2D: 'target' er ikke sat!");
            return;
        }

        // 1) Find spillerens (x,y) + offset
        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = offset.z; // hold altid Z til offset.z

        // 2) Snap X/Y til grid, hvis nødvendigt
        if (snapToGrid)
        {
            desiredPosition = RoundToGrid(desiredPosition);
        }

        // 3) Hvis vi bruger player-bounds, clamp i X og Y
        //    topPeekOffset tillader at "kigge" ud over øverste grænse.
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            float upperBoundWithPeek = maxBounds.y + topPeekOffset;
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, upperBoundWithPeek);
        }

        // 4) Tjek om kameraet selv må følge spilleren (ift. kameraets eget grid)
        if (useCameraGridBounds)
        {
            // Hvis den ønskede position ligger uden for kameraets grid,
            // så opdaterer vi IKKE kameraets position => kameraet "følger ikke med".
            if (!IsInsideCameraGrid(desiredPosition))
            {
                // Returner blot, så vi ikke flytter kameraet
                return;
            }
        }

        // 5) Udregn en smidig bevægelse
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothSpeed
        );

        transform.position = smoothedPosition;

        // 6) Lav en glidende 3D-lignende kipning, når vi nærmer os toppen
        //    Ide: Jo nærmere desiredPosition.y er maxBounds.y + topPeekOffset,
        //    desto mere tilt.
        if (topRotationAngle > 0f)
        {
            // Afstanden fra "øverste punkt" (max + topPeekOffset)
            float distToTop = (maxBounds.y + topPeekOffset) - smoothedPosition.y;

            // Eksempel: Hvis distToTop = tiltDistance -> angle = 0
            //           Hvis distToTop = 0 -> angle = topRotationAngle
            float t = Mathf.InverseLerp(tiltDistance, 0f, distToTop);
            float angle = Mathf.Lerp(0f, topRotationAngle, t);

            // Roter kun i X-aksen (skråt set ovenfra)
            transform.rotation = Quaternion.Euler(angle, 0f, 0f);
        }
        else
        {
            // Hvis topRotationAngle er 0, hold kameraet fladt
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    // --- Hjælpefunktioner til at udregne bounds ---
    private void CalculatePlayerGridBounds()
    {
        minBounds = gridOrigin;
        maxBounds = new Vector2(
            gridOrigin.x + gridWidth * gridSize,
            gridOrigin.y + gridHeight * gridSize
        );
    }

    private void CalculateCameraGridBounds()
    {
        camGridMin = cameraGridOrigin;
        camGridMax = new Vector2(
            cameraGridOrigin.x + cameraGridWidth * gridSize,
            cameraGridOrigin.y + cameraGridHeight * gridSize
        );
    }

    private bool IsInsideCameraGrid(Vector3 pos)
    {
        // Tjekker kun X/Y for at se, om pos er inden for kameraets grid
        if (pos.x < camGridMin.x || pos.x > camGridMax.x ||
            pos.y < camGridMin.y || pos.y > camGridMax.y)
        {
            return false;
        }
        return true;
    }

    // Snapper en (x,y,z) til nærmeste grid (x,y). Z bibeholdes uændret
    private Vector3 RoundToGrid(Vector3 position)
    {
        float snappedX = Mathf.Round((position.x - gridOrigin.x) / gridSize) * gridSize + gridOrigin.x;
        float snappedY = Mathf.Round((position.y - gridOrigin.y) / gridSize) * gridSize + gridOrigin.y;
        return new Vector3(snappedX, snappedY, position.z);
    }

    // Hvis man ændrer Player Grid-run-time:
    public void UpdateGridBounds(Vector2 newOrigin, int newWidth, int newHeight)
    {
        gridOrigin = newOrigin;
        gridWidth = newWidth;
        gridHeight = newHeight;
        CalculatePlayerGridBounds();
    }

    // Hvis man ændrer Kameraets eget grid run-time:
    public void UpdateCameraGridBounds(Vector2 newCamOrigin, int newCamWidth, int newCamHeight)
    {
        cameraGridOrigin = newCamOrigin;
        cameraGridWidth = newCamWidth;
        cameraGridHeight = newCamHeight;
        CalculateCameraGridBounds();
    }

    #region Gizmos
    private void OnDrawGizmos()
    {
        // Opdater bounds i editoren (så linjerne kan ses rigtigt)
        if (!Application.isPlaying)
        {
            CalculatePlayerGridBounds();
            CalculateCameraGridBounds();
        }

        // Tegn Player-grid + topPeek
        Gizmos.color = Color.green;
        Vector2 visualMin = minBounds;
        Vector2 visualMax = new Vector2(maxBounds.x, maxBounds.y + topPeekOffset);
        DrawGizmoRect(visualMin, visualMax);

        // Tegn linjer for at vise Player-gridets celler
        DrawGridLines(gridOrigin, gridWidth, gridHeight, Color.green);

        // Hvis kameraets eget grid er i brug, tegn det i en anden farve
        if (useCameraGridBounds)
        {
            Gizmos.color = Color.yellow;
            DrawGizmoRect(camGridMin, camGridMax);
            DrawGridLines(cameraGridOrigin, cameraGridWidth, cameraGridHeight, Color.yellow);
        }
    }

    private void DrawGizmoRect(Vector2 min, Vector2 max)
    {
        Vector2 topLeft = new Vector2(min.x, max.y);
        Vector2 topRight = max;
        Vector2 bottomRight = new Vector2(max.x, min.y);
        Vector2 bottomLeft = min;

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }

    private void DrawGridLines(Vector2 origin, int width, int height, Color color)
    {
        Color old = Gizmos.color;
        Gizmos.color = color;

        for (int x = 0; x <= width; x++)
        {
            float lineX = origin.x + x * gridSize;
            Vector2 start = new Vector2(lineX, origin.y);
            Vector2 end = new Vector2(lineX, origin.y + height * gridSize);
            Gizmos.DrawLine(start, end);
        }
        for (int y = 0; y <= height; y++)
        {
            float lineY = origin.y + y * gridSize;
            Vector2 start = new Vector2(origin.x, lineY);
            Vector2 end = new Vector2(origin.x + width * gridSize, lineY);
            Gizmos.DrawLine(start, end);
        }

        Gizmos.color = old;
    }
    #endregion
}
