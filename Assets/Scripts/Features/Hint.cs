using System.Linq;
using UnityEngine;

public class Hint : Singleton<Hint>
{
    private (int, int) _lastHint = default;

    #region Hint
    private (int, int) GetRandomPair()
    {
        var pairs = BoardController.Instance.GetFoundPairs();
        if (pairs.Count == 0)
            return default;

        var randomIndex = Random.Range(0, pairs.Count);
        return pairs.ElementAt(randomIndex);
    }

    public (int, int) GetHintedPair()
    {
        var pair = _lastHint;
        _lastHint = default;

        return pair;
    }

    public void ShowHint()
    {
        AudioManager.Instance.PlaySFX("pop_button");
        if (_lastHint != default || GameManager.Instance.HintCount <= 0 || Board.Instance.IsAnimating)
            return;

        var pair = GetRandomPair();
        if (pair == default)
        {
            GameplayUI.Instance.HighlightAddBtn();
            return;
        }

        _lastHint = pair;

        GameManager.Instance.UpdateHintCount();
        var cell1 = Board.Instance.GetCells()[pair.Item1];
        var cell2 = Board.Instance.GetCells()[pair.Item2];

        cell1.Hint();
        cell2.Hint();
    }
    #endregion
}
