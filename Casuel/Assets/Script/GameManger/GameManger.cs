using UnityEngine;
using UnityEngine.SceneManagement; // Til håndtering af levels

public class GameManager : MonoBehaviour
{
    public AudioSource audioSource;

    public bool playerEnteredTemple = false;

    public GameObject temple;
    // Referencer til de objekter, der bruges til ligningen
    [System.Serializable]

    public struct SlotAnswerPair
    {
        public GameObject slot;
        public float correctAnswer;
    }

    public SlotAnswerPair[] equalsSlotPairs; // Par af slots og deres korrekte svar for "="
    public SlotAnswerPair[] questionSlotPairs; // Par af slots og deres korrekte svar for "?"
    public GameObject winMenu; // Referencer til Win Menu UI

    private SpriteRenderer[] equalsSlotRenderers;
    private SpriteRenderer[] questionSlotRenderers;

    void Start()
    {
        playerEnteredTemple = false;
        // Hent SpriteRenderer-komponenter til farveskift
        equalsSlotRenderers = new SpriteRenderer[equalsSlotPairs.Length];
        for (int i = 0; i < equalsSlotPairs.Length; i++)
        {
            if (equalsSlotPairs[i].slot != null)
                equalsSlotRenderers[i] = equalsSlotPairs[i].slot.GetComponent<SpriteRenderer>();
        }

        questionSlotRenderers = new SpriteRenderer[questionSlotPairs.Length];
        for (int i = 0; i < questionSlotPairs.Length; i++)
        {
            if (questionSlotPairs[i].slot != null)
                questionSlotRenderers[i] = questionSlotPairs[i].slot.GetComponent<SpriteRenderer>();
        }

        if (winMenu != null)
            winMenu.SetActive(false); // Sørg for, at Win Menu starter skjult
    }

    void Update()
    {
        // Tjek svaret, hvis spilleren placerer et objekt i "=" eller "?" positionerne
        CheckAnswers();
        UpdateSlotColors();
    }

    private void CheckAnswers()
    {
        bool allCorrect = true;

        // Tjek alle equalsSlots
        for (int i = 0; i < equalsSlotPairs.Length; i++)
        {
            Collider2D equalsCollider = equalsSlotPairs[i].slot?.GetComponent<Collider2D>();

            if (equalsCollider != null)
            {
                GameObject placedObject = GetObjectInSlot(equalsCollider);
                if (placedObject != null && !placedObject.CompareTag("Player"))
                {
                    float placedNumber = GetNumberFromObject(placedObject);
                    if (placedNumber != -1)
                    {
                        if (!Mathf.Approximately(placedNumber, equalsSlotPairs[i].correctAnswer))
                        {
                            allCorrect = false;
                        }
                    }
                    else
                    {
                        allCorrect = false;
                    }
                }
                else
                {
                    allCorrect = false;
                }
            }
        }

        // Tjek alle questionSlots
        for (int i = 0; i < questionSlotPairs.Length; i++)
        {
            Collider2D questionCollider = questionSlotPairs[i].slot?.GetComponent<Collider2D>();

            if (questionCollider != null)
            {
                GameObject placedObject = GetObjectInSlot(questionCollider);
                if (placedObject != null && !placedObject.CompareTag("Player"))
                {
                    float placedNumber = GetNumberFromObject(placedObject);
                    if (placedNumber != -1)
                    {
                        if (!Mathf.Approximately(placedNumber, questionSlotPairs[i].correctAnswer))
                        {
                            allCorrect = false;
                        }
                    }
                    else
                    {
                        allCorrect = false;
                    }
                }
                else
                {
                    allCorrect = false;
                }
            }
        }

        // Hvis alle svar er korrekte, vis win menu
        if (allCorrect)
        {

            audioSource.Play(); // afspillere en lyd når spilleren winner. 
            playerEnteredTemple = true;
        }
    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        GameObject temple = other.gameObject;

        if(other.collider.CompareTag("Player") && playerEnteredTemple == true)
        {
            ShowWinMenu(); // spillet er 100% complet!
            playerEnteredTemple = false;
        }
    }

    private void UpdateSlotColors()
    {
        // Opdater farver for equalsSlots
        for (int i = 0; i < equalsSlotPairs.Length; i++)
        {
            Collider2D equalsCollider = equalsSlotPairs[i].slot?.GetComponent<Collider2D>();

            if (equalsCollider != null)
            {
                GameObject placedObject = GetObjectInSlot(equalsCollider);
                if (placedObject != null && !placedObject.CompareTag("Player"))
                {
                    float placedNumber = GetNumberFromObject(placedObject);
                    if (placedNumber != -1 && Mathf.Approximately(placedNumber, equalsSlotPairs[i].correctAnswer))
                    {
                        if (equalsSlotRenderers[i] != null)
                            equalsSlotRenderers[i].color = Color.green;
                    }
                    else
                    {
                        if (equalsSlotRenderers[i] != null)
                            equalsSlotRenderers[i].color = Color.red;
                    }
                }
                else
                {
                    if (equalsSlotRenderers[i] != null)
                        equalsSlotRenderers[i].color = Color.red;
                }
            }
        }

        // Opdater farver for questionSlots
        for (int i = 0; i < questionSlotPairs.Length; i++)
        {
            Collider2D questionCollider = questionSlotPairs[i].slot?.GetComponent<Collider2D>();

            if (questionCollider != null)
            {
                GameObject placedObject = GetObjectInSlot(questionCollider);
                if (placedObject != null && !placedObject.CompareTag("Player"))
                {
                    float placedNumber = GetNumberFromObject(placedObject);
                    if (placedNumber != -1 && Mathf.Approximately(placedNumber, questionSlotPairs[i].correctAnswer))
                    {
                        if (questionSlotRenderers[i] != null)
                            questionSlotRenderers[i].color = Color.green;
                    }
                    else
                    {
                        if (questionSlotRenderers[i] != null)
                            questionSlotRenderers[i].color = Color.red;
                    }
                }
                else
                {
                    if (questionSlotRenderers[i] != null)
                        questionSlotRenderers[i].color = Color.red;
                }
            }
        }
    }

    private GameObject GetObjectInSlot(Collider2D slot)
    {
        // Tjek for objekter, der overlapper med denne slot
        Collider2D[] results = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D().NoFilter();
        int count = slot.Overlap(filter, results);

        if (count > 0)
        {
            foreach (var result in results)
            {
                if (result != null && !result.CompareTag("Player"))
                {
                    return result.gameObject; // Returner det første overlappende objekt, der ikke er spilleren
                }
            }
        }
        return null;
    }

    private float GetNumberFromObject(GameObject obj)
    {
        // Tjek først, om objektet har NumberObject
        if (obj == null) return -1;

        NumberObject numberObject = obj.GetComponent<NumberObject>();
        if (numberObject != null)
        {
            return numberObject.value; // Returner værdien fra NumberObject
        }

        //Debug.LogWarning($"Objektet '{obj.name}' har ikke et NumberObject script!");
        return -1;
    }

    private void ShowWinMenu()
    {
        if (winMenu != null)
        {
            winMenu.SetActive(true); // Vis Win Menu
        }
    }

    public void RestartLevel()
    {
        // Genindlæs det aktuelle level
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevel()
    {
        // NOTE: Tilføj det næste level til build-indstillingerne og kald det her
        // SceneManager.LoadScene("NextLevelSceneName");
        Debug.Log("Load næste level");
    }
}
