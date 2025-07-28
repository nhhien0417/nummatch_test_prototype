using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : Singleton<StageManager>
{
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private Transform _container;

    private const int GenRows = 3;
    private const int GenCols = 9;
    private const int GenCells = GenRows * GenCols;

    private List<(int, int)> _matchPairs = new();
    private int[] _boardValues = new int[GenCells];

    public void SetGridLayout(bool enabled)
    {
        _container.GetComponent<GridLayoutGroup>().enabled = enabled;
    }

    private Cell SpawnCell(int value)
    {
        var cell = Instantiate(_cellPrefab, _container).GetComponent<Cell>();
        cell.name = "Cell";
        cell.SetState(true, value);

        MEC.Timing.CallDelayed(1f, () =>
        {
            // cell.Spawn();
        });

        return cell;
    }

    #region Generate Board
    public void GenerateBoard()
    {
        var attempt = 0;
        var matchPairs = GameManager.Instance.CurrentStage switch
        {
            1 => 15,
            2 => 10,
            _ => 5
        };

        while (!TryGenerateBoard(matchPairs)) attempt++;

        SpawnCells();
        PrintMatchPairs();
        PrintAllMatches();

        Board.Instance.UpdateContainerHeight();
        Debug.Log($"✅ Generated after {attempt} attempts");
    }


    private void Reset()
    {
        foreach (Transform child in _container)
        {
            Destroy(child.gameObject);
        }

        Board.Instance.GetCells().Clear();
    }

    private void SpawnCells()
    {
        Reset();

        for (int i = 0; i < GenCells; i++)
        {
            var cell = SpawnCell(_boardValues[i]);
            Board.Instance.GetCells().Add(cell);
        }
    }

    private bool TryGenerateBoard(int pairsCount)
    {
        _boardValues = new int[GenCells];
        _matchPairs = PickMatchPairs(pairsCount);

        var valuePool = new List<int>();
        for (var i = 1; i <= GenCols; i++)
            for (var j = 0; j < GenRows; j++)
                valuePool.Add(i);

        for (var index = 0; index < GenCells; index++)
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

        for (int i = 0; i < GenCells; i++)
        {
            var row = i / GenCols;
            var col = i % GenCols;

            if (col < GenCols - 1)
                allValidPairs.Add((i, i + 1));
            if (row < GenRows - 1)
                allValidPairs.Add((i, i + GenCols));
            if (row < GenRows - 1 && col < GenCols - 1)
                allValidPairs.Add((i, i + GenCols + 1));
            if (row < GenRows - 1 && col > 0)
                allValidPairs.Add((i, i + GenCols - 1));

            if (col == GenCols - 1 && row < GenRows - 1)
            {
                var nextRowStart = (row + 1) * GenCols;
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
        var row = index / GenCols;
        var col = index % GenCols;

        if (col > 0 || col == 0 && row > 0) neighbors.Add(index - 1);
        if (row > 0) neighbors.Add(index - GenCols);
        if (row > 0 && col > 0) neighbors.Add(index - GenCols - 1);
        if (row > 0 && col < GenCols - 1) neighbors.Add(index - GenCols + 1);

        return neighbors;
    }
    #endregion

    #region Debugging
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

    private void PrintAllMatches()
    {
        var foundPairs = new HashSet<(int, int)>();

        for (var i = 0; i < GenCells; i++)
        {
            var valA = _boardValues[i];
            var row = i / GenCols;
            var col = i % GenCols;

            CheckAndAddPair(i, row, col + 1, valA, foundPairs);
            CheckAndAddPair(i, row + 1, col, valA, foundPairs);
            CheckAndAddPair(i, row + 1, col + 1, valA, foundPairs);
            CheckAndAddPair(i, row + 1, col - 1, valA, foundPairs);

            if (col == GenCols - 1 && row < GenRows - 1)
            {
                var j = (row + 1) * GenCols;
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
        if (row < 0 || row >= GenRows || col < 0 || col >= GenCols) return;

        var j = row * GenCols + col;
        var valB = _boardValues[j];

        if (valA == valB || valA + valB == 10)
        {
            var pair = i < j ? (i, j) : (j, i);
            foundPairs.Add(pair);
        }
    }
    #endregion

    #region Clone Cells
    public void CloneCells()
    {
        var originalCells = Board.Instance.GetCells();
        var cellsCopy = originalCells.ToList();

        foreach (var cell in cellsCopy)
        {
            if (!cell.IsActive) continue;

            var clone = SpawnCell(cell.Value);
            originalCells.Add(clone);
        }

        Board.Instance.UpdateContainerHeight();
    }
    #endregion
}
