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
    public bool IsAnimating => _isAnimating;
    public List<Cell> GetCells()
    {
        return _cells;
    }

    #region Checker
    // Returns true if two cells can be matched according to game rules
    public bool CanMatch(Cell selectedCell, Cell targetCell)
    {
        if (selectedCell == null || targetCell == null || selectedCell == targetCell)
            return false;

        var indexA = _cells.IndexOf(selectedCell);
        var indexB = _cells.IndexOf(targetCell);

        var valueA = selectedCell.Value;
        var valueB = targetCell.Value;

        // Rule 1: Match if same number or their sum is 10
        if (!(valueA == valueB || valueA + valueB == 10))
            return false;

        // Rule 2: Check if cells are aligned horizontally, vertically, or diagonally and not blocked
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

            // Traverse the path between selected and target cell to check for blocking active cells
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

        // Rule 3: Match if in a straight sequence (index-wise) and no cells in between are active
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

    // Checks if any full rows are empty to trigger clear
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

        BoardController.Instance.SetGridLayout(false);
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
                // Calculate how many cleared rows are above current row to shift it upward
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

                // Remove cells in cleared rows and destroy their GameObjects
                for (int i = end - 1; i >= start; i--)
                {
                    _cells[i].transform.DOKill();
                    Destroy(_cells[i].gameObject);
                    _cells.RemoveAt(i);
                }
            }

            CheckClearBoard();
            UpdateContainerHeight();

            BoardController.Instance.UpdateBoardValues();
            GameManager.Instance.CheckLoseGame();
            BoardController.Instance.SetGridLayout(true);

            _isAnimating = false;
        });

        return true;
    }

    // If all cells are cleared, move to the next stage
    private void CheckClearBoard()
    {
        if (_cells.Count > 0) return;

        GameManager.Instance.UpdateNewStage(GameManager.Instance.CurrentStage + 1);
    }
    #endregion

    #region Cell Interaction
    // Handles what happens when a player selects a cell
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

            var (index1, index2) = Hint.Instance.GetHintedPair();
            _cells[index1].UnHint();
            _cells[index2].UnHint();

            _selectedCell.Deselect();
            _selectedCell.SetState(false, _selectedCell.Value, GemType.None);
            targetCell.SetState(false, targetCell.Value, GemType.None);

            if (!CheckClearRow())
            {
                AudioManager.Instance.PlaySFX("match_cell");
                BoardController.Instance.UpdateBoardValues();
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
    // Update the height of the scrollable board container based on current number of rows
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

    // Toggle fade effects based on scroll position (top or bottom)
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
