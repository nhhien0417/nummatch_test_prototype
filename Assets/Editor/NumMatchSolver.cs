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
            return new State
            {
                board = (int[,])board.Clone(),
                moves = new List<Move>(moves),
                gemsCollected = gemsCollected,
                movesUsed = movesUsed
            };
        }
    }

    private const int Cols = 9;
    private string input = "";
    private string output = "";
    private Vector2 inputScroll;
    private Vector2 outputScroll;

    private static readonly (int dr, int dc)[] Directions = new[]
    {
        (0, 1),
        (1, 0),
        (1, 1),
        (1, -1)
    };

    #region UI
    [MenuItem("Tools/NumMatch Solver")]
    public static void ShowWindow()
    {
        GetWindow<NumMatchSolverEditor>("NumMatch Solver");
    }

    private void OnGUI()
    {
        GUIStyle inputStyle = new(EditorStyles.textArea) { wordWrap = true, fontSize = 14 };
        GUIStyle outputStyle = new(EditorStyles.textArea) { wordWrap = true, fontSize = 12 };

        EditorGUILayout.LabelField("🧮 NumMatch Special Solver", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enter digits (1-9):");

        inputScroll = EditorGUILayout.BeginScrollView(inputScroll, GUILayout.Height(100));
        input = EditorGUILayout.TextArea(input, inputStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Solve & Export"))
        {
            input = input.Trim();
            if (ValidateInput(input))
                SolveAndExport(input);
            else
                Debug.LogWarning("Invalid input: Only digits 1-9 allowed, at least 1 digit.");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Top 10 Solutions:");

        outputScroll = EditorGUILayout.BeginScrollView(outputScroll, GUILayout.Height(400));
        output = EditorGUILayout.TextArea(output, outputStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private bool ValidateInput(string digits) => digits.All(c => c is >= '1' and <= '9') && digits.Length >= 1;

    private void SolveAndExport(string digits)
    {
        var rows = Mathf.CeilToInt(digits.Length / 9f);
        var board = new int[rows, Cols];

        for (var i = 0; i < digits.Length; i++)
            board[i / Cols, i % Cols] = digits[i] - '0';

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

        output = $"Gems: {gemCount}\nTime: {stopwatch.ElapsedMilliseconds} ms\n\n"
               + string.Join("\n\n", formattedSolutions);

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
        var gemCount = CountGems(original);
        var gemTarget = gemCount / 2 * 2;
        var beamWidth = 30;

        var solutions = new HashSet<string>();
        var result = new List<string>();
        var queue = new Queue<State>();

        queue.Enqueue(new State { board = (int[,])original.Clone() });

        while (queue.Count > 0 && result.Count < 10)
        {
            var nextStates = new List<State>();

            while (queue.Count > 0 && result.Count < 10)
            {
                var current = queue.Dequeue();
                var pairs = FindAllValidPairs(current.board);

                foreach (var move in pairs)
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

                    if (next.gemsCollected >= gemTarget)
                    {
                        var sol = string.Join("|", next.moves.Select(m => m.ToString()));

                        if (solutions.Add(sol))
                        {
                            result.Add(sol);
                            if (result.Count >= 10) break;
                        }

                        continue;
                    }

                    nextStates.Add(next);
                }
            }

            if (result.Count >= 10) break;

            queue.Clear();
            foreach (var state in nextStates.OrderByDescending(s => s.gemsCollected).ThenBy(s => s.movesUsed).Take(beamWidth))
            {
                queue.Enqueue(state);
            }
        }

        return result;
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

    private int CountGems(int[,] board)
    {
        var count = 0;
        foreach (var v in board)
            if (v == 5) count++;
        return count;
    }
    #endregion
}
