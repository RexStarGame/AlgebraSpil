using UnityEngine;

public class TempleTrigger : MonoBehaviour
{
    // Reference til GameManager
    [SerializeField] private GameManager gameManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Tjekker, om det er spilleren, der går ind i templets trigger
        if (other.CompareTag("Player"))
        {
            // Hvis ALLE svar er korrekte (playerEnteredTemple == true) ...
            if (gameManager.playerEnteredTemple)
            {
                // ... så vind spillet
                gameManager.ShowWinMenu();
                // Sæt til false, så man ikke kan “win” flere gange
                gameManager.playerEnteredTemple = false;
            }
        }
    }
}
