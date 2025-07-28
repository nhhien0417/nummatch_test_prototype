using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Board : Singleton<Board>
{
    [SerializeField] private GameObject _topFade, _bottomFade;
    [SerializeField] private RectTransform _boardContainer;

    private List<Cell> _cells = new();
    private int _selectedCellIndex = -1;
    private const int BoardCols = 9;

    public List<Cell> GetCells()
    {
        return _cells;
    }

    #region CheckMatch
    private bool CanMatch(Cell selectedCell, Cell targetCell)
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

    private void CheckClearRow()
    {
        var totalRows = Mathf.CeilToInt((float)_cells.Count / BoardCols);
        var clearedRows = new List<int>();

        for (var row = 0; row < totalRows; row++)
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

        if (clearedRows.Count == 0) return;

        StageManager.Instance.SetGridLayout(false);
        AudioManager.Instance.PlaySFX("clear_row");

        var seq = DOTween.Sequence();
        seq.AppendCallback(() =>
        {
            foreach (var row in clearedRows)
            {
                for (var col = 0; col < BoardCols; col++)
                {
                    var index = row * BoardCols + col;
                    if (index >= _cells.Count) break;

                    _cells[index].HideTextTween();
                }
            }
        });

        seq.AppendInterval(0.2f);
        seq.AppendCallback(() =>
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                var row = i / BoardCols;
                var shift = clearedRows.Count(r => row > r);

                if (shift > 0) _cells[i].ShiftCellUpTween(shift);
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
                    Destroy(_cells[i].gameObject);
                    _cells.RemoveAt(i);
                }
            }

            UpdateContainerHeight();
            StageManager.Instance.SetGridLayout(true);
        });
    }
    #endregion

    #region Cell Interaction
    public void OnCellSelected(Cell cell)
    {
        var index = _cells.IndexOf(cell);

        if (_selectedCellIndex == -1)
        {
            _selectedCellIndex = index;
            _cells[index].Select();
            return;
        }

        var targetCell = _cells[index];
        var selectedCell = _cells[_selectedCellIndex];

        selectedCell.Deselect();

        if (_selectedCellIndex == index)
        {
            _selectedCellIndex = -1;
            return;
        }

        if (CanMatch(selectedCell, targetCell))
        {
            _selectedCellIndex = -1;

            selectedCell.SetState(false, selectedCell.Value);
            targetCell.SetState(false, targetCell.Value);

            CheckClearRow();
            AudioManager.Instance.PlaySFX("match_cell");
        }
        else
        {
            if (selectedCell.Value != targetCell.Value && selectedCell.Value + targetCell.Value != 10)
            {
                _selectedCellIndex = index;
                targetCell.Select();
            }
            else
            {
                _selectedCellIndex = -1;
            }
        }
    }
    #endregion

    #region Scroll View
    public void UpdateContainerHeight()
    {
        var totalRows = Mathf.CeilToInt((float)_cells.Count / BoardCols);
        var targetContentHeight = (totalRows + 3) * 100f;
        var viewportHeight = GetComponent<RectTransform>().sizeDelta.y;

        var finalHeight = Mathf.Ceil(Mathf.Max(targetContentHeight, viewportHeight) / 100f) * 100f;
        _boardContainer.sizeDelta = new Vector2(_boardContainer.sizeDelta.x, finalHeight);

        GetComponent<ScrollRect>().vertical = targetContentHeight > viewportHeight;
    }

    public void HandleScrollPosition(Vector2 normalizedPosition)
    {
        const float threshold = 0.01f;
        var isAtTop = normalizedPosition.y >= 1f - threshold;
        var isAtBottom = normalizedPosition.y <= threshold;

        _topFade.SetActive(!isAtTop);
        _bottomFade.SetActive(!isAtBottom);
    }
    #endregion
}
