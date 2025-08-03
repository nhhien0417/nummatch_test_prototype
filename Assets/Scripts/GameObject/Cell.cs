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
        if (!isActive) MatchCell();

        _value = value;
        _isActive = isActive;
        _gemType = gemType;

        _text.text = value.ToString();
        _text.color = Utils.GetHexColor(isActive ? (gemType == GemType.None ? "#1E5564" : "#EEEEEE") : "#D1D9D4");
        _gem.GetComponent<Image>().sprite = GemManager.Instance.GetGemEntries().TryGetValue(gemType, out var sprite) ? sprite : null;
    }

    public void Spawn(float delay = 0f)
    {
        DOTween.Sequence()
        .AppendInterval(delay)
        .AppendCallback(() =>
        {
            _foreground.transform.localScale = Vector3.one;
            _gem.SetActive(_gemType != GemType.None);
            _text.gameObject.SetActive(true);
        })
       .Append(_foreground.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
    }

    private void MatchCell()
    {
        DOTween.Sequence()
        .AppendCallback(() =>
        {
            _foreground.transform.localScale = Vector3.one;
            _text.gameObject.SetActive(true);
        })
        .Append(_foreground.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));

        if (_gemType != GemType.None)
        {
            CollectGem(_gemType);
            _gem.SetActive(false);
        }
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

    public void HideCell()
    {
        DOTween.Sequence()
        .AppendCallback(() =>
        {
            _foreground.transform.localScale = Vector3.one;
        })
        .Append(_foreground.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack))
        .Join(_text.DOFade(0f, 0.2f).SetEase(Ease.InSine));
    }

    public void ShiftCellUp(int rows)
    {
        var rect = GetComponent<RectTransform>();
        var shiftY = rows * rect.rect.height;
        var targetPos = rect.anchoredPosition + new Vector2(0, shiftY);

        rect.DOKill();
        rect.DOAnchorPos(targetPos, 0.25f).SetEase(Ease.OutCubic);
    }

    public void Hint()
    {
        _text.rectTransform.localScale = Vector3.one;
        _text.rectTransform.DOKill();
        _text.rectTransform.DOScale(1.15f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    public void CollectGem(GemType gemType)
    {
        var gem = Instantiate(_gem, GameplayUI.Instance.transform, false);
        var rect = gem.GetComponent<RectTransform>();

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(100f, 100f);
        gem.transform.GetChild(0).gameObject.SetActive(false);

        var startPos = transform.position;
        var endPos = GameplayUI.Instance.GetGemTarget(gemType).position;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect.parent as RectTransform,
            RectTransformUtility.WorldToScreenPoint(Camera.main, startPos), Camera.main, out Vector2 startLocalPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect.parent as RectTransform,
            RectTransformUtility.WorldToScreenPoint(Camera.main, endPos), Camera.main, out Vector2 endLocalPos);

        rect.anchoredPosition = startLocalPos;
        var peak = startLocalPos + new Vector2(0, 50f);
        var control = Vector2.Lerp(peak, endLocalPos, 0.5f) + new Vector2(Random.Range(-200f, 200f), Random.Range(0f, 200f));

        DOTween.Sequence()
        .Append(rect.DOAnchorPos(peak, 0.25f).SetEase(Ease.OutQuad))
        .Append(DOTween.To(() => 0f, t => rect.anchoredPosition =
                Mathf.Pow(1 - t, 2) * peak + 2 * (1 - t) * t * control +
                Mathf.Pow(t, 2) * endLocalPos, 1f, 0.5f).SetEase(Ease.InSine))
        .Join(rect.DOScale(0.8f, 0.5f).SetEase(Ease.InBack))
        .OnComplete(() =>
        {
            GemManager.Instance.UpdateGemProgress(gemType);
            Destroy(gem);
        });
    }
}
