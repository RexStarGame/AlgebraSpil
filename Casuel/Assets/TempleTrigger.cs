using UnityEngine;

public class TempleTrigger : MonoBehaviour
{
    // Reference til GameManager
    [SerializeField] private GameManager gameManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Tjekker, om det er spilleren, der g�r ind i templets trigger
        if (other.CompareTag("Player"))
        {
            // Hvis ALLE svar er korrekte (playerEnteredTemple == true) ...
            if (gameManager.playerEnteredTemple)
            {
                // ... s� vind spillet
                gameManager.ShowWinMenu();
                // S�t til false, s� man ikke kan �win� flere gange
                gameManager.playerEnteredTemple = false;
            }
        }
    }
}
