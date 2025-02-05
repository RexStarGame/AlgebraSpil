using UnityEngine;

public class StringVariabler : MonoBehaviour
{
    [SerializeField] private string _textValue;
    public string textValue; // Dette felt manglede
    public string TextValue => _textValue;
}