using UnityEngine;

public class GridTilTal : MonoBehaviour
{
    public float gridSize = 1f; // Størrelsen af hver gittercelle
    public float snapRange = 2f; // Rækkevidde for at justere til spillerens position
    private bool isBeingPushed = false; // Gemmer om objektet er i bevægelse
    private Transform playerTransform; // Gemmer spillerens transform
    public bool IsPushing { get; private set; }
    private void Update()
    {
        if (!isBeingPushed)
        {
            SnapToGrid();
        }
    }
    private void SnapToGrid()
    {
        Vector2 targetPosition;

        // Tjek om spilleren er inden for rækkevidde
        if (playerTransform != null && 
            Vector2.Distance(transform.position, playerTransform.position) <= snapRange)
        {
            // Juster til gitterpunkt tættest på spilleren
            targetPosition = RoundToGrid(playerTransform.position);
        }
        else
        {
            // Juster til gitterpunkt tættest på objektets nuværende position
            targetPosition = RoundToGrid(transform.position);
        }

        // Flyt objektet til den beregnede position
        transform.position = targetPosition;
    }
    private Vector2 RoundToGrid(Vector2 position)
    {
        // Beregn nærmeste gitterposition
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float y = Mathf.Round(position.y / gridSize) * gridSize;

        // Tjek for overlap med spiller
        if (playerTransform != null)
        {
            Vector2 playerGridPos = new Vector2(
                Mathf.Round(playerTransform.position.x / gridSize) * gridSize,
                Mathf.Round(playerTransform.position.y / gridSize) * gridSize);

            // Tjek om spilleren aktivt skubber
            PlayerControllerMobile playerControllerMobile = playerTransform.GetComponent<PlayerControllerMobile>();
            if (new Vector2(x, y) == playerGridPos && playerControllerMobile != null && playerControllerMobile.IsPushing)  // <-- Fjernet parenteser
            {
                // Juster position væk fra spilleren
                Vector2 dir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                x = Mathf.Round((position.x + dir.x * gridSize) / gridSize) * gridSize;
                y = Mathf.Round((position.y + dir.y * gridSize) / gridSize) * gridSize;
            }
        }

        return new Vector2(x, y);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerTransform = collision.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerTransform = null;
        }
    }

    public void StartPushing()
    {
        isBeingPushed = true;
    }

    public void StopPushing()
    {
        isBeingPushed = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualiser snap-rækkevidde
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, snapRange);
    }
}
