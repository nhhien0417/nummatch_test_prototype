using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : Singleton<PopupController>
{
    [SerializeField] private GameObject _gemPrefab, _background;
    [SerializeField] private GameObject _winPopup, _losePopup;
    [SerializeField] private Transform _winContainer, _loseContainer;

    private Popup _lastPopup;

    public enum Popup
    {
        WinPopup,
        LosePopup
    }

    public void BackToHome() => HidePopup(() => GameplayUI.Instance.BackToHome());
    public void NewGame() => HidePopup(() => GameManager.Instance.NewGame());

    public void ShowPopup(Popup popup)
    {
        var (popupObject, container) = GetPopupComponents(popup);
        if (popupObject.activeSelf) return;

        _lastPopup = popup;

        InstantiateGemList(container, false);
        AnimatePopupIn(popupObject);
    }

    public void HidePopup(Action onComplete = null)
    {
        var (popupObject, _) = GetPopupComponents(_lastPopup);
        if (!popupObject.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        AnimatePopupOut(popupObject, onComplete);
    }

    private void InstantiateGemList(Transform container, bool isLose)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (var gemProgress in GemManager.Instance.GemProgresses)
        {
            var gemObject = Instantiate(_gemPrefab, container).GetComponent<Gem>();
            gemObject.UpdateGemInfo(gemProgress.RequiredAmount, gemProgress.Type, GemManager.Instance.GetGemEntries()[gemProgress.Type], isLose);
        }
    }

    private void AnimatePopupIn(GameObject popupObject)
    {
        var bgImage = _background.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0);
        _background.SetActive(true);

        popupObject.transform.localScale = Vector3.zero;
        popupObject.SetActive(true);

        DOTween.Sequence()
        .Append(popupObject.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
        .Join(bgImage.DOFade(0.9f, 0.3f).SetEase(Ease.OutSine));
    }

    private void AnimatePopupOut(GameObject popupObject, Action onComplete)
    {
        var bgImage = _background.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.9f);
        popupObject.transform.localScale = Vector3.one;

        DOTween.Sequence()
        .Append(popupObject.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack))
        .Join(bgImage.DOFade(0f, 0.3f).SetEase(Ease.InSine))
        .OnComplete(() =>
        {
            popupObject.SetActive(false);
            _background.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private (GameObject popupObject, Transform container) GetPopupComponents(Popup popup)
    {
        return popup switch
        {
            Popup.WinPopup => (_winPopup, _winContainer),
            Popup.LosePopup => (_losePopup, _loseContainer),
            _ => throw new ArgumentOutOfRangeException(nameof(popup), popup, null)
        };
    }
}
