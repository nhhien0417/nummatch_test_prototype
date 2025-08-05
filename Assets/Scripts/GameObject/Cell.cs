using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct CellData
{
    public int Value;
    public bool IsActive;

    public CellData(int value, bool isActive)
    {
        Value = value;
        IsActive = isActive;
    }
}

public class Cell : MonoBehaviour
{
    [SerializeField] private GameObject _background, _foreground, _gem, _gemBG, _hint;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Button _button;

    private int _value;
    private bool _isActive;
    private GemType _gemType;

    public int Value => _value;
    public bool IsActive => _isActive;

    #region Logic
    // Called when the cell is clicked; only triggers if the cell is active
    public void OnClick()
    {
        if (_isActive) Board.Instance.OnCellSelected(this);
    }

    // Updates visuals: text, color, and gem sprite based on new state
    public void SetState(bool isActive, int value, GemType gemType)
    {
        transform.DOKill();
        transform.localScale = Vector3.one;

        if (!isActive) MatchCell();

        _value = value;
        _isActive = isActive;
        _gemType = gemType;

        _text.text = value.ToString();
        _text.color = Utils.GetHexColor(isActive ? (gemType == GemType.None ? "#1E5564" : "#EEEEEE") : "#D1D9D4");
        _gem.GetComponent<Image>().sprite = GemManager.Instance.GetGemEntries().TryGetValue(gemType, out var sprite) ? sprite : null;
    }

    // Animates the cell spawn: waits for delay, then scales down the foreground to show entry
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

    // Creates a copy of the gem and animates it moving to the gem progress UI slot
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
    #endregion

    #region Interact
    // Animates cell disappearing when matched, and handles gem collection if any
    private void MatchCell()
    {
        DOTween.Sequence()
        .AppendCallback(() =>
        {
            _foreground.transform.localScale = Vector3.one;
            _hint.transform.localScale = Vector3.zero;
        })
        .Append(_foreground.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));

        if (_gemType != GemType.None)
        {
            CollectGem(_gemType);
            _gem.SetActive(false);
        }
    }

    // Highlights selected cell
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

    // Resets visual highlight of the selected cell
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

    // Plays select-deselect animation to give feedback when match is invalid
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

    // Shakes the number text to indicate a blocked cell or invalid action
    public void ShakeBlockCell()
    {
        _text.rectTransform.DOKill();
        _text.rectTransform.DOPunchPosition(new(-15f, 0, 0), 0.2f, 20, -15f, true);
    }
    #endregion

    #region Clear Row
    // Animates cell disappearing visually
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

    // Moves the cell up by a number of rows
    public void ShiftCellUp(int rows)
    {
        var rect = GetComponent<RectTransform>();
        var shiftY = rows * rect.rect.height;
        var targetPos = rect.anchoredPosition + new Vector2(0, shiftY);

        rect.DOKill();
        rect.DOAnchorPos(targetPos, 0.25f).SetEase(Ease.OutCubic);
    }
    #endregion

    #region Hint
    // Shows hint indicator
    public void Hint()
    {
        _hint.transform.DOKill();
        _hint.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        _hint.GetComponent<Image>().DOFade(1f, 0.2f).SetEase(Ease.OutSine);

        transform.localScale = Vector3.one;
        transform.DOKill();
        transform.DOScale(1.05f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    // Stops hint animation and hides hint indicator
    public void UnHint()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;

        _hint.transform.DOKill();
        _hint.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
        _hint.GetComponent<Image>().DOFade(0f, 0.2f).SetEase(Ease.InSine);
    }
    #endregion
}
