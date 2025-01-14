using UnityEditor.Tilemaps;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f; // Bevægelseshastighed
    public float gridSize = 1f; // Størrelsen af hver celle i gitteret
    public Vector2 gridOrigin = Vector2.zero; // Startpunktet for gitteret
    public int gridWidth = 10; // Antal celler i bredden
    public int gridHeight = 10; // Antal celler i højden

    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 targetPosition; // Position spilleren er på vej mod
    private bool isMoving = false; // Om spilleren bevæger sig

    private GameObject currentPushable; // Skubbebart objekt
    private bool isPushing = false; // Om spilleren aktivt skubber et objekt

    public Transform pushPoint; // Punkt hvor objektet placeres foran spilleren
    public Vector2 pushPointOffset = new Vector2(1f, 0f); // Justerbar offset for pushPoint

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Juster spillerens startposition til nærmeste gittercelle
        targetPosition = RoundToGrid(transform.position);
        transform.position = targetPosition;
    }

    void Update()
    {
        // Hvis spilleren ikke bevæger sig, tjek input
        if (!isMoving)
        {
            movement.x = Input.GetAxisRaw("Horizontal"); // A/D eller venstre/højre
            movement.y = Input.GetAxisRaw("Vertical");   // W/S eller op/ned

            if (movement != Vector2.zero)
            {
                Vector2 direction = movement.normalized;
                Vector2 potentialTarget = RoundToGrid((Vector2)transform.position + direction * gridSize);

                if (IsInsideGrid(potentialTarget)) // Tjek om målpositionen er inden for gitteret
                {
                    targetPosition = potentialTarget;
                    isMoving = true;
                }

                // Roter spilleren baseret på retning
                Flip(direction);
            }
        }

        // Tjek om spilleren vil stoppe med at skubbe objektet
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
        {
            StopPushingObject();
        }
    }

    void FixedUpdate()
    {
        // Flyt spilleren mod targetPosition
        if (isMoving)
        {
            rb.MovePosition(Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime));
            if (Vector2.Distance(rb.position, targetPosition) < 0.01f)
            {
                rb.position = targetPosition;
                isMoving = false;
            }
        }

        // Hvis spilleren skubber et objekt, placer det foran spilleren uanset om spilleren bevæger sig eller ej
        if (isPushing && currentPushable != null)
        {
            currentPushable.transform.position = pushPoint.position;
        }
    }

    private Vector2 RoundToGrid(Vector2 position)
    {
        // Juster positionen til nærmeste gittercelle
        float x = Mathf.Round((position.x - gridOrigin.x) / gridSize) * gridSize + gridOrigin.x;
        float y = Mathf.Round((position.y - gridOrigin.y) / gridSize) * gridSize + gridOrigin.y;
        return new Vector2(x, y);
    }

    private bool IsInsideGrid(Vector2 position)
    {
        // Tjek om positionen er inden for gitterets grænser
        float xMin = gridOrigin.x;
        float xMax = gridOrigin.x + gridWidth * gridSize;
        float yMin = gridOrigin.y;
        float yMax = gridOrigin.y + gridHeight * gridSize;

        return position.x >= xMin && position.x < xMax && position.y >= yMin && position.y < yMax;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check om spilleren kolliderer med et objekt, der kan skubbes
        if (collision.gameObject.CompareTag("Pushable"))
        {
            if (!isPushing)
            {
                currentPushable = collision.gameObject;
                isPushing = true;
                // Flyt objektet til pushPoint, selv hvis spilleren er idle
                currentPushable.transform.position = pushPoint.position;
            }
        }
    }

    private void StopPushingObject()
    {
        if (currentPushable != null)
        {
            isPushing = false;
            currentPushable = null;
        }
    }

    private void Flip(Vector2 direction)
    {
        // Beregn vinklen for spillerens retning
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);

        if (pushPoint != null)
        {
            // Sørg for, at pushPoint følger spillerens retning korrekt baseret på offset
            Vector3 offset = new Vector3(direction.x, direction.y, 0).normalized;
            //pushPoint.localPosition = new Vector3(pushPointOffset.x * offset.x, pushPointOffset.y * offset.y, 0);
        }

        // Hvis spilleren ikke bevæger sig, behold objektet ved pushPoint
        if (isPushing && currentPushable != null)
        {
            currentPushable.transform.position = pushPoint.position;
        }
    }

    void OnDrawGizmos()
    {
        // Tegn gitteret for at visualisere grænserne
        Gizmos.color = Color.green;
        for (int x = 0; x <= gridWidth; x++)
        {
            for (int y = 0; y <= gridHeight; y++)
            {
                Vector2 cellPosition = new Vector2(gridOrigin.x + x * gridSize, gridOrigin.y + y * gridSize);
                Gizmos.DrawWireCube(cellPosition, new Vector3(gridSize, gridSize, 0));
            }
        }
    }
}
