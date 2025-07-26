using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] private GameObject _background;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Button _button;

    private int _index;
    private int _value;
    private bool _isActive;

    public int Index => _index;
    public int Value => _value;
    public bool IsActive => _isActive;

    public void OnClick()
    {
        if (_isActive)
            Board.Instance.OnCellSelected(_index);
    }

    public void SetIndex(int index) => _index = index;

    public void SetState(bool isActive, int value = 0)
    {
        _value = value;
        _isActive = isActive;

        _text.text = value > 0 ? value.ToString() : "";
        _text.color = Utils.GetHexColor(isActive ? "#1E5564" : "#D1D9D4");
    }

    public void Select() => AnimateScale(Vector3.one);
    public void Deselect() => AnimateScale(Vector3.zero);

    private void AnimateScale(Vector3 target)
    {
        _background.transform.DOKill();
        _background.transform.DOScale(target, 0.2f).SetEase(Ease.OutBack);
    }
}
