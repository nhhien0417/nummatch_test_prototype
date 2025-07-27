using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] private GameObject _background;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Button _button;

    private int _value;
    private bool _isActive;

    public int Value => _value;
    public bool IsActive => _isActive;

    public void OnClick()
    {
        if (_isActive) Board.Instance.OnCellSelected(this);
    }

    public void SetState(bool isActive, int value = 0)
    {
        _value = value;
        _isActive = isActive;

        _text.text = value > 0 ? value.ToString() : "";
        _text.color = Utils.GetHexColor(isActive ? "#1E5564" : "#D1D9D4");
    }

    public void Select()
    {
        AudioManager.Instance.PlaySFX("choose_number");

        _background.transform.DOKill();
        _background.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        _background.GetComponent<Image>().DOFade(1f, 0.2f).SetEase(Ease.OutSine);
    }

    public void Deselect()
    {
        _background.transform.DOKill();
        _background.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
        _background.GetComponent<Image>().DOFade(0f, 0.2f).SetEase(Ease.InSine);
    }

    public void ShakeBlockCell()
    {
        _text.rectTransform.DOKill();
        _text.rectTransform.DOPunchPosition(new(-15f, 0, 0), 0.2f, 20, -15f, true);
    }

    public void NotMatchDeselect()
    {
        DOTween.Sequence()
            .Append(_background.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack))
            .Join(_background.GetComponent<Image>().DOFade(1f, 0.2f).SetEase(Ease.OutSine))
            .Append(_background.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack))
            .Join(_background.GetComponent<Image>().DOFade(0f, 0.2f).SetEase(Ease.InSine));
    }

    public void HideTextTween()
    {
        _text.DOKill();
        _text.DOFade(0f, 0.2f).SetEase(Ease.InSine);
    }

    public void ShiftCellUpTween(int rows)
    {
        var rect = GetComponent<RectTransform>();
        var shiftY = rows * rect.rect.height;
        var targetPos = rect.anchoredPosition + new Vector2(0, shiftY);

        rect.DOKill();
        rect.DOAnchorPos(targetPos, 0.25f).SetEase(Ease.OutCubic);
    }
}
