using TMPro;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    private int _value;
    public int Value => _value;

    public void SetValue(int value)
    {
        _value = value;
        _text.text = value > 0 ? value.ToString() : "";
    }

    public void Clear()
    {
        _value = 0;
        _text.text = "";
    }
}
