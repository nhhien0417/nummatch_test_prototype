// NumMatchSolverOptimized.cs
// Unity Editor tool: Solves NumMatch board using gem-priority strategy with special rules

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

public class NumMatchSolverOptimized : EditorWindow
{
    private string input = "";
    private const int COLS = 9;

    [MenuItem("Tools/NumMatch Solver Optimized")]
    public static void ShowWindow()
    {
        GetWindow<NumMatchSolverOptimized>("NumMatch Solver Optimized");
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
        var stopwatch = Stopwatch.StartNew();

        var rows = Mathf.CeilToInt(input.Length / (float)COLS);
        var board = new Cell[rows, COLS];
        var gems = new List<Cell>();

        for (var i = 0; i < input.Length; i++)
        {
            var val = input[i] - '0';
            var row = i / COLS;
            var col = i % COLS;

            var cell = new Cell(val, row, col, i);
            board[row, col] = cell;
            if (val == 5) gems.Add(cell);
        }

        int totalGems = gems.Count;
        int requiredGems = totalGems % 2 == 0 ? totalGems : totalGems - 1;

        var solutions = new List<List<Match>>();
        var visited = new HashSet<string>();
        var queue = new PriorityQueue<State>();

        queue.Enqueue(new State(CloneBoard(board), gems, new List<Match>(), 0), 0);

        while (queue.Count > 0 && solutions.Count < 10)
        {
            var (state, _) = queue.Dequeue();

            if (state.Gems.Count(g => !g.Active) >= requiredGems)
            {
                solutions.Add(state.Matches);
                continue;
            }

            foreach (var m in GetAllValidMatches(state.Board))
            {
                var hash = HashState(state.Matches, m);
                if (visited.Contains(hash)) continue;
                visited.Add(hash);

                var newBoard = CloneBoard(state.Board);
                newBoard[m.A.Row, m.A.Col].Active = false;
                newBoard[m.B.Row, m.B.Col].Active = false;

                var nextMatches = new List<Match>(state.Matches) { m };
                var nextGems = state.Gems.Select(g => newBoard[g.Row, g.Col]).ToList();

                queue.Enqueue(new State(newBoard, nextGems, nextMatches, state.MoveCount + 1), nextMatches.Count);
            }
        }

        SaveToFile(solutions);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"✅ Done in {stopwatch.ElapsedMilliseconds}ms. Saved to output.txt");
    }

    string HashState(List<Match> current, Match next)
    {
        return string.Join("|", current.Select(m => m.ToString())) + "|" + next.ToString();
    }

    List<Match> GetAllValidMatches(Cell[,] board)
    {
        var result = new List<Match>();
        var rows = board.GetLength(0);
        var cols = board.GetLength(1);

        var directions = new (int dr, int dc)[] {
            (0,1), (1,0), (1,1), (1,-1),
            (0,-1), (-1,0), (-1,-1), (-1,1)
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
                        result.Add(new Match(a, b));
                    }
                }
            }
        }

        return result;
    }

    bool IsClearPath(Cell a, Cell b, Cell[,] board)
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
            if (board[r, c] != null && board[r, c].Active) return false;

            r += dr;
            c += dc;
        }

        return true;
    }

    bool IsInBounds(Cell[,] board, int r, int c) =>
        r >= 0 && c >= 0 && r < board.GetLength(0) && c < board.GetLength(1);

    Cell[,] CloneBoard(Cell[,] board)
    {
        var rows = board.GetLength(0);
        var cols = board.GetLength(1);
        var clone = new Cell[rows, cols];

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var cell = board[r, c];
                if (cell != null)
                    clone[r, c] = new Cell(cell.Value, cell.Row, cell.Col, cell.Index) { Active = cell.Active };
            }
        }
        return clone;
    }

    void SaveToFile(List<List<Match>> solutions)
    {
        using StreamWriter writer = new("Assets/Resources/output.txt");
        foreach (var sol in solutions.OrderBy(s => s.Count))
        {
            writer.WriteLine(string.Join("|", sol.Select(m => m.ToString())));
        }
        AssetDatabase.Refresh();
    }

    class Cell
    {
        public int Value;
        public int Row, Col, Index;
        public bool Active = true;

        public Cell(int v, int r, int c, int i)
        {
            Value = v; Row = r; Col = c; Index = i;
        }
    }

    class Match
    {
        public Cell A, B;
        public Match(Cell a, Cell b) { A = a; B = b; }
        public override string ToString() => $"{A.Row},{A.Col},{B.Row},{B.Col}";
    }

    class State
    {
        public Cell[,] Board;
        public List<Cell> Gems;
        public List<Match> Matches;
        public int MoveCount;

        public State(Cell[,] b, List<Cell> g, List<Match> m, int move)
        {
            Board = b; Gems = g; Matches = m; MoveCount = move;
        }
    }

    class PriorityQueue<T>
    {
        private List<(T item, int priority)> elements = new();
        public int Count => elements.Count;

        public void Enqueue(T item, int priority)
        {
            elements.Add((item, priority));
            elements.Sort((a, b) => a.priority.CompareTo(b.priority));
        }

        public (T, int) Dequeue()
        {
            var first = elements[0];
            elements.RemoveAt(0);
            return first;
        }
    }
}
