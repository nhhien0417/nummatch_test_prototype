using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : Singleton<PopupController>
{
    [SerializeField] private GameObject _background, _winPopup, _losePopup;

    private Popup _lastPopup;
    private Dictionary<Popup, GameObject> _popupMap;

    public enum Popup
    {
        WinPopup,
        LosePopup
    }

    private void Awake()
    {
        _popupMap = new Dictionary<Popup, GameObject>
        {
            { Popup.WinPopup, _winPopup },
            { Popup.LosePopup, _losePopup }
        };
    }

    public void ShowPopup(Popup popup)
    {
        _lastPopup = popup;

        if (!_popupMap.TryGetValue(popup, out var popupObject) || popupObject.activeSelf)
        {
            return;
        }

        _background.SetActive(true);
        var bgImage = _background.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0);

        popupObject.transform.localScale = Vector3.zero;
        popupObject.SetActive(true);

        DOTween.Sequence()
        .Append(popupObject.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
        .Join(bgImage.DOFade(0.9f, 0.3f).SetEase(Ease.OutSine));
    }

    public void HidePopup(Action onComplete = null)
    {
        if (!_popupMap.TryGetValue(_lastPopup, out var popupObject) || !popupObject.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

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

    public void BackToHome()
    {
        HidePopup(() =>
        {
            GameplayUI.Instance.BackToHome();
        });
    }

    public void NewGame()
    {
        HidePopup(() =>
        {
            GameManager.Instance.NewGame();
        });
    }
}
