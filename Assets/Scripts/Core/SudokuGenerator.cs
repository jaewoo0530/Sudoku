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

        public static GeneratedPuzzle Generate(int targetGivens, Random random = null)
        {
            random ??= new Random();

            int[,] solution = GenerateFullSolution(random);
            int[,] givens = DigHoles(solution, targetGivens, random);

            return new GeneratedPuzzle { Givens = givens, Solution = solution };
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

        private static int[,] DigHoles(int[,] solution, int targetGivens, Random random)
        {
            var puzzle = (int[,])solution.Clone();
            var cells = new (int row, int col)[Size * Size];
            int index = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    cells[index++] = (r, c);

            for (int i = cells.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }

            int givensCount = Size * Size;

            foreach (var (row, col) in cells)
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
            }

            return puzzle;
        }
    }
}
