using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayUI : Singleton<GameplayUI>
{
    [SerializeField] private TextMeshProUGUI _stageText, _addText, _hintText;
    [SerializeField] private Transform _gemContainer;
    [SerializeField] private GameObject _gemPrefab, _addButton;

    private List<Gem> _gems = new();

    #region MainUI
    public void BackToHome()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("Home");
        AudioManager.Instance.PlaySFX("pop_button");
    }

    public void UpdateStageText()
    {
        _stageText.text = $"Stage: {GameManager.Instance.CurrentStage}";
    }

    public void UpdateAddText()
    {
        _addText.text = GameManager.Instance.AddCount.ToString();
    }

    public void UpdateHintText()
    {
        _hintText.text = GameManager.Instance.HintCount.ToString();
    }

    public void HighlightAddBtn()
    {
        _addButton.transform.DOKill();
        _addButton.transform.localScale = Vector3.one;
        _addButton.transform.DOScale(0.9f, 0.25f).SetEase(Ease.OutQuad)
        .OnComplete(() => _addButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
    }
    #endregion

    #region Gem Progresses
    public void SetupGems()
    {
        foreach (var gem in _gems)
        {
            Destroy(gem.gameObject);
        }

        _gems.Clear();

        foreach (var gemProgress in GemManager.Instance.GemProgresses)
        {
            var gemObject = Instantiate(_gemPrefab, _gemContainer).GetComponent<Gem>();
            gemObject.UpdateGemInfo(gemProgress.RequiredAmount, gemProgress.Type, GemManager.Instance.GetGemEntries()[gemProgress.Type]);

            _gems.Add(gemObject);
        }
    }

    public void UpdateGemProgresses(GemProgress gemProgress)
    {
        var gem = _gems.FirstOrDefault(g => g.GemType == gemProgress.Type);
        gem.UpdateProgress(gemProgress.RequiredAmount - gemProgress.Collected);
    }

    public RectTransform GetGemTarget(GemType gemType)
    {
        return _gems.FirstOrDefault(t => t.GemType == gemType).GetComponent<RectTransform>();
    }
    #endregion
}
