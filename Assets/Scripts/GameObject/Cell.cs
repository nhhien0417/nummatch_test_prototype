using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] private GameObject _background, _foreground, _gem, _gemBG;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Button _button;

    private int _value;
    private bool _isActive;
    private GemType _gemType;

    public int Value => _value;
    public bool IsActive => _isActive;

    public void OnClick()
    {
        if (_isActive) Board.Instance.OnCellSelected(this);
    }

    public void SetState(bool isActive, int value, GemType gemType)
    {
        if (!isActive)
        {
            var seq = DOTween.Sequence();
            seq.AppendCallback(() =>
            {
                _foreground.transform.localScale = Vector3.one;
                _text.gameObject.SetActive(true);
            });
            seq.Append(_foreground.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));

            if (_gemType != GemType.None)
            {
                GemManager.Instance.UpdateGemProgress(_gemType);
                _gem.SetActive(false);
            }
        }

        _value = value;
        _isActive = isActive;
        _gemType = gemType;

        _text.text = value.ToString();
        _text.color = Utils.GetHexColor(isActive ? (gemType == GemType.None ? "#1E5564" : "#EEEEEE") : "#D1D9D4");
        _gem.GetComponent<Image>().sprite = GemManager.Instance.GetGemEntries().TryGetValue(gemType, out var sprite) ? sprite : null;
    }

    public void Spawn(float delay = 0f)
    {
        var seq = DOTween.Sequence();
        seq.AppendInterval(delay);
        seq.AppendCallback(() =>
        {
            _foreground.transform.localScale = Vector3.one;
            _gem.SetActive(_gemType != GemType.None);
            _text.gameObject.SetActive(true);
        });
        seq.Append(_foreground.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
    }

    public void Select()
    {
        AudioManager.Instance.PlaySFX("choose_number");

        if (_gemType == GemType.None)
        {
            _background.transform.DOKill();
            _background.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            _background.GetComponent<Image>().DOFade(1f, 0.2f).SetEase(Ease.OutSine);
        }
        else
        {
            _gemBG.transform.DOKill();
            _gemBG.GetComponent<Image>().DOFade(1f, 0.2f).SetEase(Ease.OutSine);
        }
    }

    public void Deselect()
    {
        if (_gemType == GemType.None)
        {
            _background.transform.DOKill();
            _background.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
            _background.GetComponent<Image>().DOFade(0f, 0.2f).SetEase(Ease.InSine);
        }
        else
        {
            _gemBG.transform.DOKill();
            _gemBG.GetComponent<Image>().DOFade(0f, 0.2f).SetEase(Ease.InSine);
        }
    }

    public void NotMatchDeselect()
    {
        if (_gemType == GemType.None)
        {
            DOTween.Sequence()
            .Append(_background.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack))
            .Join(_background.GetComponent<Image>().DOFade(1f, 0.2f).SetEase(Ease.OutSine))
            .Append(_background.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack))
            .Join(_background.GetComponent<Image>().DOFade(0f, 0.2f).SetEase(Ease.InSine));
        }
        else
        {
            DOTween.Sequence()
            .Append(_gemBG.GetComponent<Image>().DOFade(1f, 0.2f).SetEase(Ease.OutSine))
            .Append(_gemBG.GetComponent<Image>().DOFade(0f, 0.2f).SetEase(Ease.InSine));
        }
    }

    public void ShakeBlockCell()
    {
        _text.rectTransform.DOKill();
        _text.rectTransform.DOPunchPosition(new(-15f, 0, 0), 0.2f, 20, -15f, true);
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
