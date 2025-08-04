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

        for (int i = 0; i < input.Length; i++)
        {
            int val = input[i] - '0';
            int row = i / COLS;
            int col = i % COLS;

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

        var topSolutions = SolveGemFirst(board, gemCount, beamWidth: 100);
        SaveToFile(topSolutions);

        stopwatch.Stop();
        UnityEngine.Debug.Log($"✅ Done in {stopwatch.ElapsedMilliseconds}ms. Saved to output.txt");
    }

    List<List<Match>> SolveGemFirst(Cell[,] board, int totalGem, int beamWidth)
    {
        var allMatches = GetAllValidMatches(board);
        var initial = new State { Matches = new(), Used = new(), CollectedGem = 0, Board = CloneBoard(board) };

        var queue = new List<State> { initial };
        var solutions = new List<State>();

        while (queue.Count > 0)
        {
            var nextQueue = new List<State>();

            foreach (var state in queue)
            {
                var validMatches = GetAllValidMatches(state.Board)
                    .Where(m => !state.Used.Contains(m.A.Index) && !state.Used.Contains(m.B.Index))
                    .ToList();

                foreach (var m in validMatches)
                {
                    var newState = new State
                    {
                        Matches = new List<Match>(state.Matches),
                        Used = new HashSet<int>(state.Used),
                        CollectedGem = state.CollectedGem,
                        Board = CloneBoard(state.Board)
                    };

                    newState.Matches.Add(m);
                    newState.Used.Add(m.A.Index);
                    newState.Used.Add(m.B.Index);
                    newState.CollectedGem += (m.A.IsGem ? 1 : 0) + (m.B.IsGem ? 1 : 0);
                    newState.Board[m.A.Row, m.A.Col].Active = false;
                    newState.Board[m.B.Row, m.B.Col].Active = false;

                    nextQueue.Add(newState);
                }
            }

            queue = nextQueue
                .OrderByDescending(s => s.CollectedGem)
                .ThenBy(s => GetBlockedGems(s.Board, s))
                .ThenBy(s => s.Matches.Count)
                .Take(beamWidth)
                .ToList();

            solutions.AddRange(queue.Where(s => s.CollectedGem >= totalGem));
        }

        return solutions
            .OrderByDescending(s => s.CollectedGem)
            .ThenBy(s => GetBlockedGems(s.Board, s))
            .ThenBy(s => s.Matches.Count)
            .Take(10)
            .Select(s => s.Matches)
            .ToList();
    }

    static List<Match> GetAllValidMatches(Cell[,] board)
    {
        var result = new List<Match>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        var directions = new (int dr, int dc)[]
        {
            (0,1), (1,0), (1,1), (1,-1), (0,-1), (-1,0), (-1,-1), (-1,1)
        };

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var a = board[r, c];
                if (a == null || !a.Active) continue;

                foreach (var (dr, dc) in directions)
                {
                    int nr = r + dr;
                    int nc = c + dc;
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

        return result;
    }

    static bool IsInBounds(Cell[,] board, int r, int c) =>
        r >= 0 && c >= 0 && r < board.GetLength(0) && c < board.GetLength(1);

    static bool IsClearPath(Cell a, Cell b, Cell[,] board)
    {
        int dr = b.Row - a.Row;
        int dc = b.Col - a.Col;
        int steps = Mathf.Max(Mathf.Abs(dr), Mathf.Abs(dc));
        if (steps <= 1) return true;

        dr = dr != 0 ? dr / Mathf.Abs(dr) : 0;
        dc = dc != 0 ? dc / Mathf.Abs(dc) : 0;
        int r = a.Row + dr;
        int c = a.Col + dc;

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

    static int GetBlockedGems(Cell[,] board, State state)
    {
        int count = 0;
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var cell = board[r, c];
                if (cell == null || !cell.IsGem || !cell.Active || state.Used.Contains(cell.Index))
                    continue;

                bool hasNeighbor = false;

                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int nr = r + dr;
                        int nc = c + dc;

                        if (!IsInBounds(board, nr, nc)) continue;
                        var neighbor = board[nr, nc];

                        if (neighbor != null && neighbor.Active && !state.Used.Contains(neighbor.Index))
                        {
                            if ((cell.Value + neighbor.Value == 10 || cell.Value == neighbor.Value) && IsClearPath(cell, neighbor, board))
                            {
                                hasNeighbor = true;
                                break;
                            }
                        }
                    }
                    if (hasNeighbor) break;
                }
                if (!hasNeighbor) count++;
            }
        }
        return count;
    }

    static Cell[,] CloneBoard(Cell[,] board)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        var clone = new Cell[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var cell = board[r, c];
                if (cell != null)
                {
                    clone[r, c] = new Cell
                    {
                        Value = cell.Value,
                        Row = cell.Row,
                        Col = cell.Col,
                        Index = cell.Index,
                        Active = cell.Active,
                        IsGem = cell.IsGem
                    };
                }
            }
        }
        return clone;
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

    class State
    {
        public List<Match> Matches = new();
        public HashSet<int> Used = new();
        public int CollectedGem = 0;
        public Cell[,] Board;
    }
}
