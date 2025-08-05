using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Gem : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _amount;

    private GemType _gemType;
    public GemType GemType => _gemType;

    /// Updates the gem display information
    public void UpdateGemInfo(int amount, GemType gemType, Sprite sprite = null, bool hasText = true)
    {
        if (sprite != null)
            _image.sprite = sprite; // Update sprite only if a new one is provided

        _gemType = gemType;
        _amount.text = amount.ToString();
        _amount.gameObject.SetActive(hasText);
    }

    /// Updates only the amount text of the gem (used during progress update).
    public void UpdateProgress(int amount)
    {
        _amount.text = amount.ToString();
    }
}
