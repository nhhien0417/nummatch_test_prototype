using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class NumMatchSolverEditor : EditorWindow
{
    private class Move
    {
        public int r1, c1, r2, c2;

        public override string ToString()
        {
            if (r1 < r2 || (r1 == r2 && c1 <= c2))
                return $"{r1},{c1},{r2},{c2}";
            else
                return $"{r2},{c2},{r1},{c1}";
        }
    }

    private class State
    {
        public int[,] board;
        public List<Move> moves = new();
        public int gemsCollected;
        public int movesUsed;

        public State Clone()
        {
            var rows = board.GetLength(0);
            var cols = board.GetLength(1);
            var newBoard = new int[rows, cols];
            Array.Copy(board, newBoard, board.Length);
            return new State
            {
                board = newBoard,
                moves = new List<Move>(moves),
                gemsCollected = gemsCollected,
                movesUsed = movesUsed
            };
        }
    }

    private class PriorityQueue<T>
    {
        private SortedDictionary<int, Queue<T>> dict = new();
        public int Count { get; private set; } = 0;

        public void Enqueue(T item, int priority)
        {
            if (!dict.TryGetValue(priority, out var queue))
                dict[priority] = queue = new Queue<T>();
            queue.Enqueue(item);
            Count++;
        }

        public T Dequeue()
        {
            var first = dict.First();
            var queue = first.Value;
            var item = queue.Dequeue();

            if (queue.Count == 0) dict.Remove(first.Key);
            Count--;

            return item;
        }
    }

    private const int SAFETY_LIMIT = 100000;
    private const int COLS = 9;
    private const int TOPK = 30;

    private string _inputText = "";
    private string _outputText = "";

    private Vector2 _inputScroll;
    private Vector2 _outputScroll;

    private static readonly (int dr, int dc)[] Directions = new[] { (0, 1), (1, 0), (1, 1), (1, -1) };

    #region UI
    [MenuItem("Tools/NumMatch Solver")]
    public static void ShowWindow() => GetWindow<NumMatchSolverEditor>("NumMatch Solver");

    private void OnGUI()
    {
        GUIStyle inputStyle = new(EditorStyles.textArea) { wordWrap = true, fontSize = 14 };
        GUIStyle outputStyle = new(EditorStyles.textArea) { wordWrap = true, fontSize = 12 };

        EditorGUILayout.LabelField("Enter digits (1-9):");
        _inputScroll = EditorGUILayout.BeginScrollView(_inputScroll, GUILayout.Height(125));
        _inputText = EditorGUILayout.TextArea(_inputText, inputStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Solve & Export"))
        {
            _inputText = _inputText.Trim();
            if (ValidateInput(_inputText))
                SolveAndExport(_inputText);
            else
                Debug.LogWarning("Invalid input: Only digits 1-9 allowed, at least 1 digit.");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Top 10 Solutions:");

        _outputScroll = EditorGUILayout.BeginScrollView(_outputScroll, GUILayout.Height(400));
        _outputText = EditorGUILayout.TextArea(_outputText, outputStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private bool ValidateInput(string digits) => digits.All(c => c is >= '1' and <= '9') && digits.Length >= 1;

    private void SolveAndExport(string digits)
    {
        var rows = Mathf.CeilToInt(digits.Length / 9f);
        var board = new int[rows, COLS];

        for (var i = 0; i < digits.Length; i++)
            board[i / COLS, i % COLS] = digits[i] - '0';

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var gemCount = CountGems(board);
        var solutions = SolveBoard(board);
        stopwatch.Stop();

        var path = Path.Combine(Application.dataPath, "Resources/output.txt");
        File.WriteAllLines(path, solutions);

        var formattedSolutions = solutions.Select((s, i) =>
        {
            var moveCount = s.Split('|').Length;
            return $"{i + 1}. ({moveCount} moves)\n{s}";
        });

        _outputText = $"Gems: {gemCount}\nTime: {stopwatch.ElapsedMilliseconds} ms\n\n" +
                        string.Join("\n\n", formattedSolutions);

        Debug.Log($"Saved {solutions.Count} solutions to: {path}");
        Debug.Log($"Total gems on board: {gemCount}");
        Debug.Log($"Solved in {stopwatch.ElapsedMilliseconds} ms");
    }
    #endregion

    #region Logic
    private List<string> SolveBoard(int[,] original)
    {
        var rows = original.GetLength(0);
        var cols = original.GetLength(1);
        var gemTarget = CountGems(original) / 2 * 2;
        var expanded = 0;

        var solutions = new HashSet<string>();
        var allValidSolutions = new List<State>();

        var queue = new PriorityQueue<State>();
        queue.Enqueue(new State { board = (int[,])original.Clone() }, 0);

        while (queue.Count > 0)
        {
            if (++expanded > SAFETY_LIMIT) break;

            var current = queue.Dequeue();
            if (current.gemsCollected >= gemTarget)
            {
                var solStr = string.Join("|", current.moves.Select(m => m.ToString()));
                if (solutions.Add(solStr))
                    allValidSolutions.Add(current);
                if (solutions.Count >= 10) break;
                continue;
            }

            var allPairs = FindAllValidPairs(current.board);
            var gemPositions = GetGemPositions(current.board);

            var prioritized = allPairs.Select(move =>
            {
                var v1 = current.board[move.r1, move.c1];
                var v2 = current.board[move.r2, move.c2];

                var score = 0;
                if (v1 == 5 && v2 == 5) score += 100;

                var delta = Math.Abs(move.r1 - move.r2) + Math.Abs(move.c1 - move.c2);
                var dist = Mathf.Min(GetDistanceToNearestFive(move.r1, move.c1, gemPositions),
                                    GetDistanceToNearestFive(move.r2, move.c2, gemPositions));
                score -= dist;
                score -= delta;

                return (move, score);
            }).OrderByDescending(x => x.score).Take(TOPK).Select(x => x.move);

            foreach (var move in prioritized)
            {
                var v1 = current.board[move.r1, move.c1];
                var v2 = current.board[move.r2, move.c2];

                var next = current.Clone();
                next.board[move.r1, move.c1] = 0;
                next.board[move.r2, move.c2] = 0;
                next.moves.Add(move);
                next.movesUsed++;

                if (v1 == 5) next.gemsCollected++;
                if (v2 == 5 && (move.r1 != move.r2 || move.c1 != move.c2)) next.gemsCollected++;

                var f = next.movesUsed + (gemTarget - next.gemsCollected);
                queue.Enqueue(next, f);
            }
        }

        if (allValidSolutions.Count == 0) return new();

        var minMoves = allValidSolutions.Min(s => s.movesUsed);
        return allValidSolutions.Where(s => s.movesUsed == minMoves)
                                .OrderBy(_ => Guid.NewGuid())
                                .Take(10)
                                .Select(s => string.Join("|", s.moves.Select(m => m.ToString())))
                                .ToList();
    }

    private List<Move> FindAllValidPairs(int[,] board)
    {
        var rows = board.GetLength(0);
        var cols = board.GetLength(1);
        var size = rows * cols;
        var valid = new List<Move>();

        for (var i = 0; i < size; i++)
        {
            var r1 = i / cols;
            var c1 = i % cols;
            var v1 = board[r1, c1];
            if (v1 == 0) continue;

            foreach (var (dr, dc) in Directions)
            {
                var r = r1 + dr;
                var c = c1 + dc;

                while (r >= 0 && r < rows && c >= 0 && c < cols)
                {
                    var v2 = board[r, c];

                    if (v2 == 0)
                    {
                        r += dr;
                        c += dc;
                        continue;
                    }

                    if (v1 == v2 || v1 + v2 == 10)
                    {
                        valid.Add(new Move { r1 = r1, c1 = c1, r2 = r, c2 = c });
                    }

                    break;
                }
            }

            for (var j = i + 1; j < size; j++)
            {
                var r2 = j / cols;
                var c2 = j % cols;
                var v2 = board[r2, c2];

                if (v2 == 0)
                    continue;

                if (v1 == v2 || v1 + v2 == 10)
                {
                    valid.Add(new Move { r1 = r1, c1 = c1, r2 = r2, c2 = c2 });
                }

                break;
            }
        }

        return valid;
    }

    private List<(int r, int c)> GetGemPositions(int[,] board)
    {
        var list = new List<(int r, int c)>();
        var rows = board.GetLength(0);
        var cols = board.GetLength(1);

        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                if (board[r, c] == 5)
                    list.Add((r, c));
        return list;
    }

    private int GetDistanceToNearestFive(int r, int c, List<(int r, int c)> gems)
    {
        var minDist = int.MaxValue;
        foreach (var (gr, gc) in gems)
            minDist = Mathf.Min(minDist, Mathf.Abs(gr - r) + Mathf.Abs(gc - c));
        return minDist == int.MaxValue ? 99 : minDist;
    }

    private int CountGems(int[,] board)
    {
        var count = 0;
        foreach (var v in board)
            if (v == 5) count++;
        return count;
    }
    #endregion
}