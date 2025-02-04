using UnityEngine;

public class NumberObject : MonoBehaviour
{
    // Værdien, som dette objekt repræsenterer
    public float value;

    // Du kan også tilføje en metode til at debugge værdien
    public void PrintValue()
    {
        Debug.Log($"Dette objekt har værdien: {value}");
    }
}
