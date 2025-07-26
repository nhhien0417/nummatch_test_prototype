using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Board : Singleton<Board>
{
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private Transform _container;

    private const int Rows = 3;
    private const int Cols = 9;
    private const int GenerateCells = Rows * Cols;

    private List<(int, int)> _matchPairs = new();
    private List<Cell> _cells = new();

    private int[] _boardValues = new int[GenerateCells];
    private int _selectedCellIndex = -1;

    private void Start()
    {
        GenerateBoard();
    }

    #region GenerateBoard
    private void GenerateBoard()
    {
        var attempt = 0;

        while (true)
        {
            if (TryGenerateBoard(3))
            {
                SpawnCells();
                PrintMatchPairs();
                ScanAllMatches();

                Debug.Log($"✅ Generated after {attempt} attempts");
                return;
            }

            attempt++;
        }
    }

    private void Reset()
    {
        foreach (Transform child in _container)
        {
            Destroy(child.gameObject);
        }

        _cells.Clear();
    }

    private void SpawnCells()
    {
        Reset();

        for (int i = 0; i < GenerateCells; i++)
        {
            var obj = Instantiate(_cellPrefab, _container);
            var cell = obj.GetComponent<Cell>();

            cell.SetState(true, _boardValues[i]);
            cell.SetIndex(i);
            _cells.Add(cell);
        }
    }

    private bool TryGenerateBoard(int pairsCount)
    {
        _boardValues = new int[GenerateCells];
        _matchPairs = PickMatchPairs(pairsCount);

        var valuePool = new List<int>();
        for (var i = 1; i <= 9; i++)
            for (var j = 0; j < 3; j++)
                valuePool.Add(i);

        for (var index = 0; index < GenerateCells; index++)
        {
            var value = 0;
            var pair = _matchPairs.FirstOrDefault(p => p.Item1 == index || p.Item2 == index);

            if (pair != default)
            {
                var otherIndex = pair.Item1 == index ? pair.Item2 : pair.Item1;

                if (_boardValues[otherIndex] != 0)
                {
                    var otherVal = _boardValues[otherIndex];
                    value = PickMatchingValue(otherVal, valuePool);
                    if (value == -1) return false;
                }
                else
                {
                    value = PickRandomValue(valuePool);
                }
            }
            else
            {
                value = PickNonMatchingValue(index, valuePool);
                if (value == -1) return false;
            }

            _boardValues[index] = value;
            valuePool.Remove(value);
        }

        return true;
    }

    private List<(int, int)> PickMatchPairs(int PairsCount)
    {
        var allValidPairs = new List<(int, int)>();

        for (int i = 0; i < GenerateCells; i++)
        {
            var row = i / Cols;
            var col = i % Cols;

            if (col < Cols - 1)
                allValidPairs.Add((i, i + 1));
            if (row < Rows - 1)
                allValidPairs.Add((i, i + Cols));
            if (row < Rows - 1 && col < Cols - 1)
                allValidPairs.Add((i, i + Cols + 1));
            if (row < Rows - 1 && col > 0)
                allValidPairs.Add((i, i + Cols - 1));

            if (col == Cols - 1 && row < Rows - 1)
            {
                var nextRowStart = (row + 1) * Cols;
                allValidPairs.Add((i, nextRowStart));
            }
        }

        allValidPairs = allValidPairs.OrderBy(_ => Random.Range(0, 10000)).ToList();

        var selected = new List<(int, int)>();
        var used = new HashSet<int>();

        foreach (var (a, b) in allValidPairs)
        {
            if (used.Contains(a) || used.Contains(b)) continue;

            selected.Add((a, b));
            used.Add(a); used.Add(b);

            if (selected.Count == PairsCount)
                break;
        }

        return selected;
    }

    private int PickRandomValue(List<int> pool) => pool[Random.Range(0, pool.Count)];

    private int PickMatchingValue(int other, List<int> pool)
    {
        var candidates = pool.Where(v => v == other || v + other == 10).ToList();
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : -1;
    }

    private int PickNonMatchingValue(int index, List<int> pool)
    {
        var candidates = new List<int>(pool);

        foreach (var neighbor in GetNeighborIndices(index))
        {
            var other = _boardValues[neighbor];
            candidates = candidates.Where(v => v != other && v + other != 10).ToList();
            if (candidates.Count == 0) return -1;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private List<int> GetNeighborIndices(int index)
    {
        var neighbors = new List<int>();
        var row = index / Cols;
        var col = index % Cols;

        if (col > 0 || col == 0 && row > 0) neighbors.Add(index - 1);
        if (row > 0) neighbors.Add(index - Cols);
        if (row > 0 && col > 0) neighbors.Add(index - Cols - 1);
        if (row > 0 && col < Cols - 1) neighbors.Add(index - Cols + 1);

        return neighbors;
    }

    private void PrintMatchPairs()
    {
        Debug.Log("==== MATCH PAIRS ====");

        foreach (var (a, b) in _matchPairs)
        {
            var valA = _boardValues[a];
            var valB = _boardValues[b];
            var type = valA == valB ? "Same" : (valA + valB == 10 ? "Sum10" : "Invalid");

            Debug.Log($"Pair: [{a}]({valA}) ↔ [{b}]({valB}) => {type}");
        }
    }

    private void ScanAllMatches()
    {
        var foundPairs = new HashSet<(int, int)>();

        for (var i = 0; i < GenerateCells; i++)
        {
            var valA = _boardValues[i];
            var row = i / Cols;
            var col = i % Cols;

            CheckAndAddPair(i, row, col + 1, valA, foundPairs);
            CheckAndAddPair(i, row + 1, col, valA, foundPairs);
            CheckAndAddPair(i, row + 1, col + 1, valA, foundPairs);
            CheckAndAddPair(i, row + 1, col - 1, valA, foundPairs);

            if (col == Cols - 1 && row < Rows - 1)
            {
                var j = (row + 1) * Cols;
                var valB = _boardValues[j];

                if (valA == valB || valA + valB == 10)
                {
                    var pair = i < j ? (i, j) : (j, i);
                    foundPairs.Add(pair);
                }
            }
        }

        Debug.Log("==== ALL MATCHES FOUND ====");

        foreach (var (a, b) in foundPairs)
        {
            var valA = _boardValues[a];
            var valB = _boardValues[b];
            var type = valA == valB ? "Same" : "Sum10";

            Debug.Log($"Pair: [{a}]({valA}) ↔ [{b}]({valB}) => {type}");
        }
    }

    private void CheckAndAddPair(int i, int row, int col, int valA, HashSet<(int, int)> foundPairs)
    {
        if (row < 0 || row >= Rows || col < 0 || col >= Cols) return;

        var j = row * Cols + col;
        var valB = _boardValues[j];

        if (valA == valB || valA + valB == 10)
        {
            var pair = i < j ? (i, j) : (j, i);
            foundPairs.Add(pair);
        }
    }
    #endregion

    #region CheckMatch
    private bool CanMatch(Cell a, Cell b)
    {
        if (a == null || b == null || a == b)
            return false;

        var valueA = a.Value;
        var valueB = b.Value;
        var indexA = a.Index;
        var indexB = b.Index;

        // Điều kiện 1: Giống nhau hoặc tổng = 10
        if (!(valueA == valueB || valueA + valueB == 10))
            return false;

        int rowA = indexA / 9, colA = indexA % 9;
        int rowB = indexB / 9, colB = indexB % 9;

        var deltaRow = rowB - rowA;
        var deltaCol = colB - colA;

        // Điều kiện 2: Trên cùng hàng / cột / chéo
        if (rowA == rowB || colA == colB || Mathf.Abs(deltaRow) == Mathf.Abs(deltaCol))
        {
            var stepRow = Mathf.Clamp(deltaRow, -1, 1);
            var stepCol = Mathf.Clamp(deltaCol, -1, 1);

            var currRow = rowA + stepRow;
            var currCol = colA + stepCol;

            while (currRow != rowB || currCol != colB)
            {
                var currIndex = currRow * 9 + currCol;
                if (currIndex >= 0 && currIndex < _cells.Count && _cells[currIndex].IsActive)
                    return false;

                currRow += stepRow;
                currCol += stepCol;
            }

            return true;
        }

        // Điều kiện 3: Nằm liên tiếp qua các ô trống (zero-path)
        if (IsConnectedByEmptyCells(indexA, indexB))
            return true;

        return false;
    }

    private bool IsConnectedByEmptyCells(int fromIndex, int toIndex)
    {
        var start = Mathf.Min(fromIndex, toIndex);
        var end = Mathf.Max(fromIndex, toIndex);

        for (int i = start + 1; i < end; i++)
        {
            if (_cells[i].IsActive)
                return false;
        }

        return true;
    }
    #endregion

    #region Cell Interaction
    public void OnCellSelected(int index)
    {
        if (index < 0 || index >= GenerateCells) return;

        if (_selectedCellIndex == -1)
        {
            _selectedCellIndex = index;
            _cells[index].Select();
        }
        else
        {
            if (_selectedCellIndex == index)
            {
                _selectedCellIndex = -1;
                _cells[index].Deselect();
            }
            else
            {
                var selectedCell = _cells[_selectedCellIndex];
                var targetCell = _cells[index];

                if (CanMatch(selectedCell, targetCell))
                {
                    _selectedCellIndex = -1;

                    targetCell.Select();
                    targetCell.SetState(false, targetCell.Value);
                    selectedCell.SetState(false, selectedCell.Value);
                }
                else
                {
                    _selectedCellIndex = index;

                    selectedCell.Deselect();
                    targetCell.Select();
                }
            }
        }
    }
    #endregion
}
