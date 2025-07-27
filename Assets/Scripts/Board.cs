using System.Collections.Generic;
using UnityEngine;

public class Board : Singleton<Board>
{
    private List<Cell> _cells = new();
    private int _selectedCellIndex = -1;

    private void Start()
    {
        StageManager.Instance.GenerateBoard();
    }

    public List<Cell> GetCells()
    {
        return _cells;
    }

    #region CheckMatch
    private bool CanMatch(Cell selectedCell, Cell targetCell)
    {
        if (selectedCell == null || targetCell == null || selectedCell == targetCell)
            return false;

        var indexA = selectedCell.Index;
        var indexB = targetCell.Index;

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

    #endregion

    #region Cell Interaction
    public void OnCellSelected(int index)
    {
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
            selectedCell.SetState(false, selectedCell.Value);
            targetCell.SetState(false, targetCell.Value);

            _selectedCellIndex = -1;
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
