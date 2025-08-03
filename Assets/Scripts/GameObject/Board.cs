using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Board : Singleton<Board>
{
    [SerializeField] private GameObject _topFade, _bottomFade;
    [SerializeField] private RectTransform _boardContainer;

    private const int BoardCols = 9;
    private bool _isAnimating;

    private List<Cell> _cells = new();
    private Cell _selectedCell;

    public int TotalRows => Mathf.CeilToInt((float)_cells.Count / BoardCols);
    public List<Cell> GetCells()
    {
        return _cells;
    }

    #region Checker
    public bool CanMatch(Cell selectedCell, Cell targetCell)
    {
        if (selectedCell == null || targetCell == null || selectedCell == targetCell)
            return false;

        var indexA = _cells.IndexOf(selectedCell);
        var indexB = _cells.IndexOf(targetCell);

        var valueA = selectedCell.Value;
        var valueB = targetCell.Value;

        // Điều kiện 1: Cùng số hoặc tổng = 10
        if (!(valueA == valueB || valueA + valueB == 10))
            return false;

        // Điều kiện 2: Trên cùng hàng, cột, hoặc chéo và không bị chặn
        var rowA = indexA / 9;
        var colA = indexA % 9;
        var rowB = indexB / 9;
        var colB = indexB % 9;

        var dRow = rowB - rowA;
        var dCol = colB - colA;

        if (rowA == rowB || colA == colB || Mathf.Abs(dRow) == Mathf.Abs(dCol))
        {
            var stepRow = Mathf.Clamp(dRow, -1, 1);
            var stepCol = Mathf.Clamp(dCol, -1, 1);

            var row = rowA + stepRow;
            var col = colA + stepCol;
            var hasBlock = false;

            while (row != rowB || col != colB)
            {
                var index = row * 9 + col;

                if (_cells[index].IsActive)
                {
                    _cells[index].ShakeBlockCell();
                    hasBlock = true;
                }

                row += stepRow;
                col += stepCol;
            }

            if (hasBlock)
            {
                targetCell.NotMatchDeselect();
                return false;
            }

            return true;
        }

        // Điều kiện 3: Nằm liên tiếp trong list một chiều và không bị chặn
        var min = Mathf.Min(indexA, indexB);
        var max = Mathf.Max(indexA, indexB);
        var blocked = false;

        for (var i = min + 1; i < max; i++)
        {
            if (_cells[i].IsActive)
            {
                _cells[i].ShakeBlockCell();
                blocked = true;
            }
        }

        if (blocked)
        {
            targetCell.NotMatchDeselect();
            return false;
        }

        return true;
    }

    private bool CheckClearRow()
    {
        var clearedRows = new List<int>();

        for (var row = 0; row < TotalRows; row++)
        {
            var isEmpty = true;
            for (var col = 0; col < BoardCols; col++)
            {
                var index = row * BoardCols + col;
                if (index >= _cells.Count) break;

                if (_cells[index].IsActive)
                {
                    isEmpty = false;
                    break;
                }
            }

            if (isEmpty) clearedRows.Add(row);
        }

        if (clearedRows.Count == 0) return false;
        _isAnimating = true;

        CellGenerator.Instance.SetGridLayout(false);
        AudioManager.Instance.PlaySFX("clear_row");

        var seq = DOTween.Sequence();
        var delay = 0.05f;

        foreach (var row in clearedRows)
        {
            var rowSeq = DOTween.Sequence();
            for (var col = 0; col < BoardCols; col++)
            {
                var index = row * BoardCols + col;
                if (index >= _cells.Count) continue;

                var cell = _cells[index];
                rowSeq.AppendCallback(() => cell.HideCell());
                rowSeq.AppendInterval(delay);
            }

            seq.Join(rowSeq);
        }

        seq.AppendInterval(0.25f);
        seq.AppendCallback(() =>
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                var row = i / BoardCols;
                var shift = clearedRows.Count(r => row > r);

                if (shift > 0) _cells[i].ShiftCellUp(shift);
            }
        });

        seq.AppendInterval(0.25f);
        seq.AppendCallback(() =>
        {
            foreach (var row in clearedRows.OrderByDescending(r => r))
            {
                var start = row * BoardCols;
                var end = Mathf.Min(start + BoardCols, _cells.Count);

                for (int i = end - 1; i >= start; i--)
                {
                    _cells[i].transform.DOKill();
                    Destroy(_cells[i].gameObject);
                    _cells.RemoveAt(i);
                }
            }

            CheckClearBoard();
            UpdateContainerHeight();

            CellGenerator.Instance.UpdateBoardValues();
            GameManager.Instance.CheckLoseGame();
            CellGenerator.Instance.SetGridLayout(true);

            _isAnimating = false;
        });

        return true;
    }

    private void CheckClearBoard()
    {
        if (_cells.Count > 0) return;

        GameManager.Instance.UpdateNewStage(GameManager.Instance.CurrentStage + 1);
    }
    #endregion

    #region Cell Interaction
    public void OnCellSelected(Cell targetCell)
    {
        if (_selectedCell == null)
        {
            _selectedCell = targetCell;
            _selectedCell.Select();
            return;
        }

        if (_selectedCell == targetCell)
        {
            _selectedCell.Deselect();
            _selectedCell = null;
            return;
        }

        if (CanMatch(_selectedCell, targetCell))
        {
            if (_isAnimating) return;

            var (index1, index2) = CellGenerator.Instance.GetHintedPair();
            _cells[index1].UnHint();
            _cells[index2].UnHint();

            _selectedCell.Deselect();
            _selectedCell.SetState(false, _selectedCell.Value, GemType.None);
            targetCell.SetState(false, targetCell.Value, GemType.None);

            if (!CheckClearRow())
            {
                AudioManager.Instance.PlaySFX("match_cell");
                CellGenerator.Instance.UpdateBoardValues();
                GameManager.Instance.CheckLoseGame();
            }

            _selectedCell = null;
            return;
        }

        if (_selectedCell.Value != targetCell.Value && _selectedCell.Value + targetCell.Value != 10)
        {
            _selectedCell.Deselect();
            _selectedCell = targetCell;
            _selectedCell.Select();
        }
        else
        {
            _selectedCell.Deselect();
            _selectedCell = null;
        }
    }
    #endregion

    #region Scroll View
    public void UpdateContainerHeight()
    {
        var targetContentHeight = (TotalRows + 3) * 100f;
        var viewportHeight = GetComponent<RectTransform>().sizeDelta.y;

        var finalHeight = Mathf.Ceil(Mathf.Max(targetContentHeight, viewportHeight) / 100f) * 100f;
        _boardContainer.sizeDelta = new Vector2(_boardContainer.sizeDelta.x, finalHeight);

        var scrollRect = GetComponent<ScrollRect>();
        scrollRect.vertical = targetContentHeight > viewportHeight;

        Canvas.ForceUpdateCanvases();

        var maxScrollable = finalHeight - scrollRect.viewport.rect.height;
        var targetScroll = Mathf.Clamp01(maxScrollable > 0 ? 50f / maxScrollable : 0f);

        DOTween.Kill(scrollRect, complete: false);
        DOTween.To(() => scrollRect.verticalNormalizedPosition, value => scrollRect.verticalNormalizedPosition = value,
                         targetScroll, targetScroll == 1 ? 0f : 1f).SetEase(Ease.OutCubic).SetTarget(scrollRect);
    }

    public void HandleScrollPosition(Vector2 normalizedPosition)
    {
        const float threshold = 0.0001f;
        var isAtTop = normalizedPosition.y >= 1f - threshold;
        var isAtBottom = normalizedPosition.y <= threshold;

        _topFade.SetActive(!isAtTop);
        _bottomFade.SetActive(!isAtBottom);
    }
    #endregion
}
