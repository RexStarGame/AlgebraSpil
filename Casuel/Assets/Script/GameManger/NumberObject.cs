using UnityEngine;

public class NumberObject : MonoBehaviour
{
    // V�rdien, som dette objekt repr�senterer
    public float value;

    // Du kan ogs� tilf�je en metode til at debugge v�rdien
    public void PrintValue()
    {
        Debug.Log($"Dette objekt har v�rdien: {value}");
    }
}
