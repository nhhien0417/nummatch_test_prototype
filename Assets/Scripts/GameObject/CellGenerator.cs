using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public struct CellData
{
    public int Value;
    public bool IsActive;

    public CellData(int value, bool isActive)
    {
        Value = value;
        IsActive = isActive;
    }
}

public class CellGenerator : Singleton<CellGenerator>
{
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private Transform _container;

    private const int GenRows = 3;
    private const int GenCols = 9;
    private const int GenCells = GenRows * GenCols;

    private List<(int, int)> _matchPairs = new();
    private HashSet<(int, int)> _foundPairs = new();
    private CellData[] _boardValues = new CellData[GenCells];

    public int TotalRows => Mathf.CeilToInt((float)_boardValues.Length / GenCols);

    #region Update State
    private Cell SpawnCell(int value, GemType gemType)
    {
        var cell = Instantiate(_cellPrefab, _container).GetComponent<Cell>();
        cell.name = "Cell";
        cell.SetState(true, value, gemType);

        return cell;
    }

    public void SetGridLayout(bool enabled)
    {
        _container.GetComponent<GridLayoutGroup>().enabled = enabled;
    }

    public void UpdateBoardValues(List<Cell> newCells = null)
    {
        var allCells = Board.Instance.GetCells().Concat(newCells ?? Enumerable.Empty<Cell>()).ToList();

        _boardValues = new CellData[allCells.Count];

        for (int i = 0; i < allCells.Count; i++)
        {
            var cell = allCells[i];
            _boardValues[i] = new CellData(cell.Value, cell.IsActive);
        }

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _boardValues.Length; i++)
        {
            var cell = _boardValues[i];
            sb.Append(cell.IsActive ? cell.Value.ToString() : "X");
            sb.Append(' ');

            if ((i + 1) % 9 == 0)
                sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }
    #endregion

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
        PrintInitMatchPairs();

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
        FindAllMatchPairs();

        var spawnedGems = 0;
        var maxGems = GemManager.Instance.GemProgresses.Count(); // Z

        var spawnGemCounter = 0;
        var gemInterval = Mathf.CeilToInt((GenCells + 1) / 2); // Y

        var forceSpawnGem = false;
        var spawnedGemIndexes = new List<int>();

        for (var i = 0; i < GenCells; i++)
        {
            var value = _boardValues[i].Value;
            var shouldSpawnGem = false;
            var gemTypeToSpawn = GemType.None;

            if (spawnedGems < maxGems)
            {
                var chance = Random.Range(5f, 8f); // X%

                if ((forceSpawnGem || Random.Range(0f, 100f) < chance) && IsSafeToSpawnGem(i, spawnedGemIndexes))
                {
                    var validGemTypes = GemManager.Instance.AvailableGemTypes;
                    gemTypeToSpawn = validGemTypes[Random.Range(0, validGemTypes.Count)];
                    shouldSpawnGem = true;
                }
            }

            // Spawn cell
            var cell = SpawnCell(value, gemTypeToSpawn);
            Board.Instance.GetCells().Add(cell);

            if (shouldSpawnGem)
            {
                spawnedGems++;
                spawnGemCounter = 0;
                spawnedGemIndexes.Add(i);
            }
            else
            {
                spawnGemCounter++;
            }

            forceSpawnGem = spawnGemCounter >= gemInterval || !AnySafeIndexRemaining(i + 1, spawnedGemIndexes);
        }
    }

    private bool TryGenerateBoard(int pairsCount)
    {
        _boardValues = new CellData[GenCells];
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

                if (_boardValues[otherIndex].Value != 0)
                {
                    var otherVal = _boardValues[otherIndex].Value;
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

            _boardValues[index] = new CellData(value, true);
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
            var other = _boardValues[neighbor].Value;
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

    #region Find Pairs
    private void PrintInitMatchPairs()
    {
        Debug.Log("==== MATCH PAIRS ====");

        foreach (var (a, b) in _matchPairs)
        {
            var valA = _boardValues[a].Value;
            var valB = _boardValues[b].Value;
            var type = valA == valB ? "Same" : (valA + valB == 10 ? "Sum10" : "Invalid");

            Debug.Log($"Pair: [{a}]({valA}) ↔ [{b}]({valB}) => {type}");
        }
    }

    private void FindAllMatchPairs()
    {
        _foundPairs.Clear();

        var total = _boardValues.Length;
        for (var i = 0; i < total; i++)
        {
            if (!_boardValues[i].IsActive) continue;

            var valA = _boardValues[i].Value;
            var row = i / GenCols;
            var col = i % GenCols;

            // → Right
            FindMatchInDirection(i, row, col, 0, +1, valA);
            // ↓ Down
            FindMatchInDirection(i, row, col, +1, 0, valA);
            // ↘ Diagonal Right-Down
            FindMatchInDirection(i, row, col, +1, +1, valA);
            // ↙ Diagonal Left-Down
            FindMatchInDirection(i, row, col, +1, -1, valA);

            // ➕ Linear: Nearest active in index order without blocking
            for (var j = i + 1; j < total; j++)
            {
                if (!_boardValues[j].IsActive) continue;

                var blocked = false;
                for (var k = i + 1; k < j; k++)
                {
                    if (_boardValues[k].IsActive)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked) break;

                var valB = _boardValues[j].Value;
                if (valA == valB || valA + valB == 10)
                    _foundPairs.Add((i, j));

                break;
            }
        }

        Debug.Log("==== ALL MATCHES FOUND ====");
        foreach (var (a, b) in _foundPairs)
        {
            var valA = _boardValues[a].Value;
            var valB = _boardValues[b].Value;
            var type = valA == valB ? "Same" : "Sum10";
            Debug.Log($"Pair: [{a}]({valA}) ↔ [{b}]({valB}) => {type}");
        }
    }

    private void FindMatchInDirection(int startIndex, int row, int col, int dRow, int dCol, int valA)
    {
        var r = row + dRow;
        var c = col + dCol;

        while (r >= 0 && r < TotalRows && c >= 0 && c < GenCols)
        {
            var targetIndex = r * GenCols + c;
            if (targetIndex >= _boardValues.Length) break;

            if (_boardValues[targetIndex].IsActive)
            {
                var valB = _boardValues[targetIndex].Value;
                if (valA == valB || valA + valB == 10)
                    _foundPairs.Add((startIndex, targetIndex));

                break;
            }

            r += dRow;
            c += dCol;
        }
    }

    public bool AnyPair()
    {
        FindAllMatchPairs();
        return _foundPairs.Count() > 0;
    }
    #endregion

    #region Clone Cells
    public void CloneCells()
    {
        if (GameManager.Instance.AddCount <= 0) return;
        GameManager.Instance.UpdateAddCount();

        var originalCells = Board.Instance.GetCells();
        var cellsCopy = originalCells.ToList();
        var startIndex = originalCells.Count;

        UpdateBoardValues(cellsCopy.Where(c => c.IsActive).ToList());
        FindAllMatchPairs();

        var spawnedGems = 0;
        var maxGems = GemManager.Instance.GemProgresses.Count(); // Z

        var spawnGemCounter = 0;
        var gemInterval = Mathf.CeilToInt((cellsCopy.Count + 1) / 2); // Y

        var forceSpawnGem = false;
        var spawnedGemIndexes = new List<int>();

        for (int i = 0; i < cellsCopy.Count; i++)
        {
            var originalCell = cellsCopy[i];
            if (!originalCell.IsActive) continue;
            var currentIndex = startIndex + i;

            var value = originalCell.Value;
            var shouldSpawnGem = false;
            var gemTypeToSpawn = GemType.None;

            if (spawnedGems < maxGems)
            {
                var chance = Random.Range(5f, 8f); // X%

                if ((forceSpawnGem || Random.Range(0f, 100f) < chance) && IsSafeToSpawnGem(currentIndex, spawnedGemIndexes))
                {
                    var validGemTypes = GemManager.Instance.AvailableGemTypes;
                    gemTypeToSpawn = validGemTypes[Random.Range(0, validGemTypes.Count)];
                    shouldSpawnGem = true;
                }
            }

            var clone = SpawnCell(value, gemTypeToSpawn);
            originalCells.Add(clone);

            if (shouldSpawnGem)
            {
                spawnedGems++;
                spawnGemCounter = 0;
                spawnedGemIndexes.Add(currentIndex);
            }
            else
            {
                spawnGemCounter++;
            }

            forceSpawnGem = spawnGemCounter >= gemInterval || !AnySafeIndexRemaining(currentIndex + 1, spawnedGemIndexes);
        }

        Board.Instance.UpdateContainerHeight();
        GameManager.Instance.CheckLoseGame();
    }
    #endregion

    #region Spawn Conditions
    private bool IsSafeToSpawnGem(int index, List<int> spawnedGemIndexes)
    {
        foreach (var gemIndex in spawnedGemIndexes)
            if (_foundPairs.Contains((gemIndex, index)) || _foundPairs.Contains((index, gemIndex)))
                return false;

        return true;
    }

    private bool AnySafeIndexRemaining(int startIndex, List<int> spawnedGemIndexes)
    {
        for (int i = startIndex; i < _boardValues.Length; i++)
            if (IsSafeToSpawnGem(i, spawnedGemIndexes))
                return true;

        return false;
    }
    #endregion
}
