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

        public override bool Equals(object obj)
        {
            if (obj is not Move other) return false;
            return (r1 == other.r1 && c1 == other.c1 && r2 == other.r2 && c2 == other.c2)
                || (r1 == other.r2 && c1 == other.c2 && r2 == other.r1 && c2 == other.c1);
        }

        public override int GetHashCode()
        {
            var a = Math.Min(r1 * 9 + c1, r2 * 9 + c2);
            var b = Math.Max(r1 * 9 + c1, r2 * 9 + c2);
            return HashCode.Combine(a, b);
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
    private Vector2 scroll;

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

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(100));
        input = EditorGUILayout.TextArea(input, inputStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Solve & Export"))
        {
            input = input.Trim();

            if (ValidateInput(input))
            {
                SolveAndExport(input);
            }
            else
            {
                Debug.LogWarning("Invalid input: Only digits 1-9 allowed, at least 1 digit.");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Top 10 Solutions:");
        EditorGUILayout.TextArea(output, outputStyle, GUILayout.Height(400));
    }

    private bool ValidateInput(string digits) => digits.All(c => c is >= '1' and <= '9') && digits.Length >= 1;

    private void SolveAndExport(string digits)
    {
        var rows = Mathf.CeilToInt(digits.Length / 9f);
        var board = new int[rows, Cols];

        for (var i = 0; i < digits.Length; i++)
            board[i / Cols, i % Cols] = digits[i] - '0';

        var solutions = SolveBoard(board);
        var path = Path.Combine(Application.dataPath, "Resources/output.txt");

        File.WriteAllLines(path, solutions);
        Debug.Log($"✅ Saved to: {path}");

        output = string.Join("\n", solutions.Select((s, i) => $"{i + 1}. {s}"));
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

        var uniqueSolutions = new HashSet<string>();
        var result = new List<string>();
        var queue = new Queue<State>();

        queue.Enqueue(new State { board = (int[,])original.Clone() });

        while (queue.Count > 0 && result.Count < 10)
        {
            var nextStates = new List<State>();

            while (queue.Count > 0)
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

                    // Dừng ngay khi đủ
                    if (next.gemsCollected >= gemTarget)
                    {
                        var sol = string.Join("|", next.moves.Select(m => m.ToString()));

                        if (uniqueSolutions.Add(sol)) result.Add(sol);
                        if (result.Count >= 10) break;

                        continue;
                    }

                    nextStates.Add(next);
                }
            }

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
        var valid = new List<Move>();

        int[] dr = { 0, 1, 1, 1 };
        int[] dc = { 1, 0, 1, -1 };

        for (var r1 = 0; r1 < rows; r1++)
        {
            for (var c1 = 0; c1 < cols; c1++)
            {
                var v1 = board[r1, c1];
                if (v1 == 0) continue;

                // Duyệt theo 4 hướng xa
                for (var dir = 0; dir < 4; dir++)
                {
                    for (var d = 1; d < Math.Max(rows, cols); d++)
                    {
                        var r2 = r1 + dr[dir] * d;
                        var c2 = c1 + dc[dir] * d;
                        if (r2 < 0 || r2 >= rows || c2 < 0 || c2 >= cols) break;

                        var v2 = board[r2, c2];
                        if (v2 == 0) continue;

                        if ((v1 == v2 || v1 + v2 == 10) && !IsBlocked(board, r1, c1, r2, c2))
                        {
                            valid.Add(new Move { r1 = r1, c1 = c1, r2 = r2, c2 = c2 });
                        }

                        break;
                    }
                }

                // Liền kề 8 hướng
                for (var dr2 = -1; dr2 <= 1; dr2++)
                {
                    for (var dc2 = -1; dc2 <= 1; dc2++)
                    {
                        if (dr2 == 0 && dc2 == 0) continue;

                        var nr = r1 + dr2;
                        var nc = c1 + dc2;
                        if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;

                        var v2 = board[nr, nc];
                        if (v2 == 0) continue;

                        if (v1 + v2 == 10 || v1 == v2)
                        {
                            valid.Add(new Move { r1 = r1, c1 = c1, r2 = nr, c2 = nc });
                        }
                    }
                }
            }
        }

        return valid;
    }

    private bool IsBlocked(int[,] board, int r1, int c1, int r2, int c2)
    {
        var dr = Math.Sign(r2 - r1);
        var dc = Math.Sign(c2 - c1);

        var r = r1 + dr;
        var c = c1 + dc;

        while (r != r2 || c != c2)
        {
            if (board[r, c] != 0) return true;

            r += dr;
            c += dc;
        }

        return false;
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
