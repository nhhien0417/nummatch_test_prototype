using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Gem : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _amount;

    public void UpdateGemInfo(int amount, Sprite sprite = null)
    {
        if (sprite != null)
            _image.sprite = sprite;

        _amount.text = amount.ToString();
    }
}
