using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Gem : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _amount;

    private GemType _gemType;
    public GemType GemType => _gemType;

    public void UpdateGemInfo(int amount, GemType gemType, Sprite sprite = null)
    {
        if (sprite != null)
            _image.sprite = sprite;

        _gemType = gemType;
        _amount.text = amount.ToString();
    }

    public void UpdateProgress(int amount)
    {
        _amount.text = amount.ToString();
    }
}
