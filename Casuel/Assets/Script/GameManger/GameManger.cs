using CartoonFX;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public bool playerEnteredTemple = false;

    [System.Serializable]
    public struct SlotAnswerPair
    {
        public GameObject slot;
        public float correctAnswer;
    }

    [System.Serializable]
    public struct StringAnswer
    {
        public GameObject slot2;
        public string CorrectString;
    }

    // ----------------------------------------------------
    // Behold disse arrays, som de er
    [Header("String Svar")]
    public StringAnswer[] stringAnswers;
    public StringAnswer[] stringAnswersligemed;

    [Header("Tal Svar")]
    public SlotAnswerPair[] equalsSlotPairs;
    public SlotAnswerPair[] questionSlotPairs;

    // ----------------------------------------------------
    [Header("UI")]
    public GameObject winMenu;

    // ----------------------------------------------------
    // Én prefab til "enkelt" rigtige svar
    // (Du havde før et array "correctAnswersEffects"; nu har du ÉN prefab)
    [Header("Effekt Prefab til ENKELTE rigtige svar")]
    public GameObject correctAnswerPrefab;

    // Behold finalAllAnswersEffects uændret
    [Header("Effekter til NÅR ALLE svar er korrekte")]
    public CFXR_Effect[] finalAllAnswersEffects;

    private bool hasPlayedFinalEffects = false;

    // Eksempel: spor om stringAnswers[i] var korrekt før
    private bool[] previouslyCorrectString;

    void Start()
    {
        // Skjul WinMenu
        if (winMenu != null)
            winMenu.SetActive(false);

        // Deaktiver final-effekter
        if (finalAllAnswersEffects != null)
        {
            for (int i = 0; i < finalAllAnswersEffects.Length; i++)
            {
                if (finalAllAnswersEffects[i] != null)
                    finalAllAnswersEffects[i].gameObject.SetActive(false);
            }
        }

        // Lav bool-array til stringAnswers
        previouslyCorrectString = new bool[stringAnswers.Length];
    }

    void Update()
    {
        ValidateAllAnswers();
    }

    private void ValidateAllAnswers()
    {
        bool allCorrect = true;

        // 1) Tjek stringAnswers -> med "forkert->rigtigt" logik + spawn-effekt
        allCorrect &= ValidateStringAnswers(stringAnswers);

        // 2) Tjek stringAnswersligemed -> ingen effekter
        if (!ValidateStringAnswersNoEffect(stringAnswersligemed))
            allCorrect = false;

        // 3) Tjek equalsSlotPairs -> ingen "enkelt"-effekt
        //    men du kan let tilføje samme logik, hvis du vil
        if (!ValidateNumberAnswersNoEffect(equalsSlotPairs))
            allCorrect = false;

        // 4) Tjek questionSlotPairs -> ingen effekter
        if (!ValidateNumberAnswersNoEffect(questionSlotPairs))
            allCorrect = false;

        // 5) Hvis alt korrekt -> final effekter
        if (allCorrect)
        {
            playerEnteredTemple = true;
            PlayFinalEffectsOnce();
        }
    }

    // =============== Validate STRING med "forkert->rigtigt" ===============
    private bool ValidateStringAnswers(StringAnswer[] answers)
    {
        bool localAll = true;

        for (int i = 0; i < answers.Length; i++)
        {
            if (answers[i].slot2 == null)
            {
                localAll = false;
                previouslyCorrectString[i] = false;
                continue;
            }

            var coll = answers[i].slot2.GetComponent<Collider2D>();
            if (coll == null)
            {
                localAll = false;
                previouslyCorrectString[i] = false;
                continue;
            }

            GameObject placedObj = GetObjectInSlot(coll);
            if (placedObj == null || placedObj.CompareTag("Player"))
            {
                localAll = false;
                previouslyCorrectString[i] = false;
                continue;
            }

            string text = GetStringFromObject(placedObj);
            bool isCorrectNow = (text == answers[i].CorrectString);

            if (isCorrectNow)
            {
                // Tjek om den IKKE var korrekt før
                if (!previouslyCorrectString[i])
                {
                    // Spawn effekt (1 prefab) ved brikkens position
                    ShowCorrectAnswerEffect(placedObj.transform.position);

                    // Marker at nu er den korrekt
                    previouslyCorrectString[i] = true;
                }
            }
            else
            {
                localAll = false;
                previouslyCorrectString[i] = false;
            }
        }

        return localAll;
    }

    // =============== Validate STRING-ligemed ingen effekter ===============
    private bool ValidateStringAnswersNoEffect(StringAnswer[] answers)
    {
        bool all = true;
        foreach (var ans in answers)
        {
            if (ans.slot2 == null)
            {
                all = false;
                continue;
            }

            var coll = ans.slot2.GetComponent<Collider2D>();
            if (coll == null)
            {
                all = false;
                continue;
            }

            GameObject placedObj = GetObjectInSlot(coll);
            if (placedObj == null || placedObj.CompareTag("Player"))
            {
                all = false;
                continue;
            }

            string text = GetStringFromObject(placedObj);
            if (text != ans.CorrectString)
                all = false;
        }
        return all;
    }

    // =============== Validate NUMBER-ligemed ingen effekter ===============
    private bool ValidateNumberAnswersNoEffect(SlotAnswerPair[] pairs)
    {
        bool all = true;
        foreach (var pair in pairs)
        {
            if (pair.slot == null)
            {
                all = false;
                continue;
            }
            var coll = pair.slot.GetComponent<Collider2D>();
            if (coll == null)
            {
                all = false;
                continue;
            }

            GameObject placedObj = GetObjectInSlot(coll);
            if (placedObj == null || placedObj.CompareTag("Player"))
            {
                all = false;
                continue;
            }

            float num = GetNumberFromObject(placedObj);
            if (!Mathf.Approximately(num, pair.correctAnswer))
                all = false;
        }
        return all;
    }

    // =============== Spawn "rigtig svar" effekt ved en position ===============
    private void ShowCorrectAnswerEffect(Vector3 spawnPos)
    {
        // Tjek om du har sat correctAnswerPrefab i Inspector
        if (correctAnswerPrefab == null)
        {
            Debug.LogWarning("correctAnswerPrefab er ikke sat! Ingen effekt at vise.");
            return;
        }

        // Lav en HELT NY instans af effekten i scenen
        Instantiate(correctAnswerPrefab, spawnPos, Quaternion.identity);
    }

    // =============== final effekter ===============
    private void PlayFinalEffectsOnce()
    {
        if (hasPlayedFinalEffects) return;
        hasPlayedFinalEffects = true;

        if (finalAllAnswersEffects == null) return;

        foreach (var fx in finalAllAnswersEffects)
        {
            if (fx == null) continue;

            fx.gameObject.SetActive(true);

            ParticleSystem ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }
            fx.ResetState();
        }
    }

    // =============== Hjælpemetoder ===============
    private GameObject GetObjectInSlot(Collider2D slot)
    {
        if (slot == null) return null;

        Collider2D[] results = new Collider2D[10];
        slot.Overlap(new ContactFilter2D().NoFilter(), results);

        foreach (var r in results)
        {
            if (r != null && r.gameObject != null && !r.CompareTag("Player"))
                return r.gameObject;
        }
        return null;
    }

    private float GetNumberFromObject(GameObject obj)
    {
        if (obj == null) return -1;
        NumberObject numberObj = obj.GetComponent<NumberObject>();
        if (numberObj != null)
            return numberObj.value;

        return -1;
    }

    private string GetStringFromObject(GameObject obj)
    {
        if (obj == null) return null;

        StringVariabler strv = obj.GetComponent<StringVariabler>();
        return strv?.textValue;
    }

    // ================== kollision med Temple ====================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && playerEnteredTemple)
        {
            ShowWinMenu();
            playerEnteredTemple = false;
        }
    }

    public void ShowWinMenu()
    {
        if (winMenu != null)
            winMenu.SetActive(true);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
