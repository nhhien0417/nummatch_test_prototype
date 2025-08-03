using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

public class NumMatchSolverEditor : EditorWindow
{
    private string input = "";

    [MenuItem("Tools/NumMatch Solver Optimized")]
    public static void ShowWindow()
    {
        GetWindow<NumMatchSolverEditor>("NumMatch Solver Optimized");
    }

    void OnGUI()
    {
        GUILayout.Label("Input (digits only):", EditorStyles.boldLabel);
        input = EditorGUILayout.TextArea(input, GUILayout.Height(100));

        if (GUILayout.Button("Solve"))
        {
            Solve(input);
        }
    }

    void Solve(string input)
    {
        var COLS = 9;
        var stopwatch = Stopwatch.StartNew();
        
        var rows = Mathf.CeilToInt(input.Length / (float)COLS);
        var board = new Cell[rows, COLS];
        var cells = new List<Cell>();
        var gemCount = 0;

        for (var i = 0; i < input.Length; i++)
        {
            var val = input[i] - '0';
            var row = i / COLS;
            var col = i % COLS;

            var cell = new Cell
            {
                Value = val,
                Row = row,
                Col = col,
                Index = i,
                Active = true,
                IsGem = val == 5
            };

            board[row, col] = cell;
            cells.Add(cell);
            if (cell.IsGem) gemCount++;
        }

        var requiredGem = gemCount / 2 * 2;
        var matches = GetAllValidMatches(board);

        var solutions = new List<List<Match>>();
        DFS(matches, 0, new List<Match>(), 0, requiredGem, new HashSet<int>(), solutions);

        var top10 = solutions.OrderByDescending(s => s.Count(m => m.A.IsGem || m.B.IsGem))
                             .ThenBy(s => s.Count).Take(10).ToList();

        SaveToFile(top10);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"\u2705 Done in {stopwatch.ElapsedMilliseconds}ms. Saved to output.txt");
    }

    static List<Match> GetAllValidMatches(Cell[,] board)
    {
        var result = new List<Match>();
        var rows = board.GetLength(0);
        var cols = board.GetLength(1);

        var directions = new (int dr, int dc)[]
        {
            (0,1), (1,0), (1,1), (1,-1), (0,-1), (-1,0), (-1,-1), (-1,1)
        };

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var a = board[r, c];
                if (a == null || !a.Active) continue;

                foreach (var (dr, dc) in directions)
                {
                    var nr = r + dr;
                    var nc = c + dc;
                    if (!IsInBounds(board, nr, nc)) continue;

                    var b = board[nr, nc];
                    if (b == null || !b.Active || b.Index <= a.Index) continue;

                    if ((a.Value + b.Value == 10 || a.Value == b.Value) && IsClearPath(a, b, board))
                    {
                        result.Add(new Match { A = a, B = b });
                    }
                }
            }
        }

        return result.OrderByDescending(m => (m.A.IsGem ? 1 : 0) + (m.B.IsGem ? 1 : 0)).ToList();
    }

    static bool IsInBounds(Cell[,] board, int r, int c) =>
        r >= 0 && c >= 0 && r < board.GetLength(0) && c < board.GetLength(1);

    static bool IsClearPath(Cell a, Cell b, Cell[,] board)
    {
        var dr = b.Row - a.Row;
        var dc = b.Col - a.Col;

        var steps = Mathf.Max(Mathf.Abs(dr), Mathf.Abs(dc));
        if (steps <= 1) return true;

        dr = dr != 0 ? dr / Mathf.Abs(dr) : 0;
        dc = dc != 0 ? dc / Mathf.Abs(dc) : 0;

        var r = a.Row + dr;
        var c = a.Col + dc;

        while (r != b.Row || c != b.Col)
        {
            if (!IsInBounds(board, r, c)) return false;
            if (board[r, c] != null && board[r, c].Active)
                return false;
            r += dr;
            c += dc;
        }

        return true;
    }

    static void DFS(List<Match> matches, int index, List<Match> path, int gemCollected, int gemTarget, HashSet<int> used, List<List<Match>> results)
    {
        if (gemCollected >= gemTarget)
        {
            results.Add(new List<Match>(path));
            return;
        }

        for (int i = index; i < matches.Count; i++)
        {
            var m = matches[i];
            if (used.Contains(m.A.Index) || used.Contains(m.B.Index)) continue;

            path.Add(m);
            used.Add(m.A.Index);
            used.Add(m.B.Index);
            var addedGem = (m.A.IsGem ? 1 : 0) + (m.B.IsGem ? 1 : 0);

            DFS(matches, i + 1, path, gemCollected + addedGem, gemTarget, used, results);

            path.RemoveAt(path.Count - 1);
            used.Remove(m.A.Index);
            used.Remove(m.B.Index);
        }
    }

    static void SaveToFile(List<List<Match>> solutions)
    {
        using StreamWriter writer = new("Assets/Resources/output.txt");
        foreach (var sol in solutions)
        {
            writer.WriteLine(string.Join("|", sol.Select(m => m.ToString())));
        }
        AssetDatabase.Refresh();
    }

    class Cell
    {
        public int Value;
        public int Row, Col, Index;
        public bool Active;
        public bool IsGem;
    }

    class Match
    {
        public Cell A, B;
        public override string ToString() => $"{A.Row},{A.Col},{B.Row},{B.Col}";
    }
}
