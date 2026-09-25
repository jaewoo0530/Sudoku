using System;

namespace Sudoku.Core
{
    public struct GeneratedPuzzle
    {
        public int[,] Givens;
        public int[,] Solution;
    }

    public static class SudokuGenerator
    {
        private const int Size = SudokuSolver.Size;

        // Even with a multi-pass dig (see DigHoles below), some randomly
        // generated full solutions have a genuine local minimum above the
        // requested target - no further cell can be removed from THAT
        // particular grid while keeping the solution unique, regardless of
        // removal order. When that happens, retry with a fresh full
        // solution grid rather than settling for more givens than asked.
        private const int MaxSolutionAttempts = 50;

        public static GeneratedPuzzle Generate(int targetGivens, Random random = null)
        {
            random ??= new Random();

            int[,] solution;
            int[,] givens;
            int attempts = 0;
            do
            {
                solution = GenerateFullSolution(random);
                givens = DigHoles(solution, targetGivens, random);
                attempts++;
            }
            while (CountGivens(givens) > targetGivens && attempts < MaxSolutionAttempts);

            return new GeneratedPuzzle { Givens = givens, Solution = solution };
        }

        private static int CountGivens(int[,] givens)
        {
            int count = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (givens[r, c] != 0) count++;
            return count;
        }

        private static int[,] GenerateFullSolution(Random random)
        {
            var grid = new int[Size, Size];
            FillRecursive(grid, 0, 0, random);
            return grid;
        }

        private static bool FillRecursive(int[,] grid, int row, int col, Random random)
        {
            if (row == Size) return true;

            int nextRow = col == Size - 1 ? row + 1 : row;
            int nextCol = col == Size - 1 ? 0 : col + 1;

            foreach (int digit in ShuffledDigits(random))
            {
                if (!SudokuSolver.IsSafe(grid, row, col, digit)) continue;
                grid[row, col] = digit;
                if (FillRecursive(grid, nextRow, nextCol, random)) return true;
                grid[row, col] = 0;
            }

            return false;
        }

        private static int[] ShuffledDigits(Random random)
        {
            int[] digits = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (int i = digits.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (digits[i], digits[j]) = (digits[j], digits[i]);
            }
            return digits;
        }

        // A single greedy pass over one random cell order can get stuck
        // short of the target purely because of that order (removing a
        // different remaining cell first might have allowed further
        // removals). Repeat full passes over a freshly reshuffled order of
        // the still-remaining givens until either the target is reached or
        // a whole pass removes nothing further (a genuine local minimum).
        private static int[,] DigHoles(int[,] solution, int targetGivens, Random random)
        {
            var puzzle = (int[,])solution.Clone();
            int givensCount = Size * Size;

            bool progress = true;
            while (givensCount > targetGivens && progress)
            {
                progress = false;

                var remaining = new System.Collections.Generic.List<(int row, int col)>();
                for (int r = 0; r < Size; r++)
                    for (int c = 0; c < Size; c++)
                        if (puzzle[r, c] != 0) remaining.Add((r, c));

                for (int i = remaining.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (remaining[i], remaining[j]) = (remaining[j], remaining[i]);
                }

                foreach (var (row, col) in remaining)
                {
                    if (givensCount <= targetGivens) break;

                    int backup = puzzle[row, col];
                    puzzle[row, col] = 0;

                    if (SudokuSolver.CountSolutions(puzzle, 2) != 1)
                    {
                        puzzle[row, col] = backup;
                        continue;
                    }

                    givensCount--;
                    progress = true;
                }
            }

            return puzzle;
        }
    }
}
