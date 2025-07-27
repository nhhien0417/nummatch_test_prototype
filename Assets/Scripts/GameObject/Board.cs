using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class Board : Singleton<Board>
{
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
        var totalRows = _cells.Count / BoardCols;
        var clearedRows = new List<int>();

        for (var row = 0; row < totalRows; row++)
        {
            var isEmpty = true;
            for (var col = 0; col < BoardCols; col++)
            {
                var index = row * BoardCols + col;
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

        var finalSeq = DOTween.Sequence();
        var hideSeq = DOTween.Sequence();
        var shiftSeq = DOTween.Sequence();

        foreach (var row in clearedRows)
        {
            for (var col = 0; col < BoardCols; col++)
            {
                var index = row * BoardCols + col;
                hideSeq.Join(_cells[index].HideTextTween());
            }
        }

        foreach (var cell in _cells)
        {
            var index = _cells.IndexOf(cell);
            var row = index / BoardCols;
            var shift = clearedRows.Count(clearedRow => row > clearedRow);

            if (shift > 0)
            {
                shiftSeq.Join(cell.ShiftCellUpTween(shift));
            }
        }

        finalSeq.Append(hideSeq);
        finalSeq.Append(shiftSeq);
        finalSeq.AppendCallback(() =>
        {
            foreach (var row in clearedRows.OrderByDescending(r => r))
            {
                for (var col = BoardCols - 1; col >= 0; col--)
                {
                    var index = row * BoardCols + col;
                    Destroy(_cells[index].gameObject);
                    _cells.RemoveAt(index);
                }
            }
        });

        finalSeq.OnComplete(() =>
        {
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
}
