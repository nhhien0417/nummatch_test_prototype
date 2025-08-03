using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

public class NumMatchSolverEditor : EditorWindow
{
    private string input = "";

    [MenuItem("Tools/NumMatch Solver")]
    public static void ShowWindow()
    {
        GetWindow<NumMatchSolverEditor>("NumMatch Solver");
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
        Stopwatch stopwatch = Stopwatch.StartNew();

        const int COLS = 9;
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

            if (cell.IsGem) gemCount++;
            board[row, col] = cell;
            cells.Add(cell);
        }

        var requiredGem = gemCount / 2 * 2;

        var solutions = new List<List<Match>>();
        DFS(cells, new List<Match>(), 0, requiredGem, solutions);

        var top10 = solutions.OrderBy(s => s.Count).Take(10).ToList();
        SaveToFile(top10);

        stopwatch.Stop();
        UnityEngine.Debug.Log($"✅ Done in {stopwatch.ElapsedMilliseconds}ms. Saved to output.txt");
    }

    static void DFS(List<Cell> board, List<Match> current, int gemCollected, int gemTarget, List<List<Match>> results)
    {
        if (gemCollected >= gemTarget)
        {
            results.Add(new List<Match>(current));
            return;
        }

        for (var i = 0; i < board.Count; i++)
        {
            var a = board[i];
            if (!a.Active) continue;

            for (var j = i + 1; j < board.Count; j++)
            {
                var b = board[j];
                if (!b.Active) continue;

                if (!CanMatch(a, b, board)) continue;

                var m = new Match { A = a, B = b };
                current.Add(m);
                a.Active = b.Active = false;

                int addedGem = (a.IsGem ? 1 : 0) + (b.IsGem ? 1 : 0);
                DFS(board, current, gemCollected + addedGem, gemTarget, results);

                // Backtrack
                current.RemoveAt(current.Count - 1);
                a.Active = b.Active = true;
            }
        }
    }

    static bool CanMatch(Cell a, Cell b, List<Cell> all)
    {
        if (a.Value + b.Value != 10 && a.Value != b.Value)
            return false;

        // 1D adjacent
        if (Mathf.Abs(a.Index - b.Index) == 1)
            return true;

        var dr = b.Row - a.Row;
        var dc = b.Col - a.Col;

        if (dr != 0) dr /= Mathf.Abs(dr);
        if (dc != 0) dc /= Mathf.Abs(dc);

        var r = a.Row + dr;
        var c = a.Col + dc;
        
        while (r != b.Row || c != b.Col)
        {
            if (all.Any(cell => cell.Row == r && cell.Col == c && cell.Active))
                return false;
            r += dr;
            c += dc;
        }

        return true;
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
