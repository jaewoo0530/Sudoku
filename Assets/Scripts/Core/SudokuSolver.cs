namespace Sudoku.Core
{
    public static class SudokuSolver
    {
        public const int Size = 9;
        private const int BoxSize = 3;

        public static bool IsSafe(int[,] grid, int row, int col, int digit)
        {
            for (int i = 0; i < Size; i++)
            {
                if (grid[row, i] == digit) return false;
                if (grid[i, col] == digit) return false;
            }

            int boxRow = (row / BoxSize) * BoxSize;
            int boxCol = (col / BoxSize) * BoxSize;
            for (int r = boxRow; r < boxRow + BoxSize; r++)
                for (int c = boxCol; c < boxCol + BoxSize; c++)
                    if (grid[r, c] == digit) return false;

            return true;
        }

        // Counts solutions up to `limit` (stops early once reached). Never mutates `grid`.
        public static int CountSolutions(int[,] grid, int limit)
        {
            int[,] working = (int[,])grid.Clone();
            int count = 0;
            CountSolutionsRecursive(working, ref count, limit);
            return count;
        }

        private static bool CountSolutionsRecursive(int[,] grid, ref int count, int limit)
        {
            if (!FindEmpty(grid, out int row, out int col))
            {
                count++;
                return count >= limit;
            }

            for (int digit = 1; digit <= Size; digit++)
            {
                if (!IsSafe(grid, row, col, digit)) continue;
                grid[row, col] = digit;
                bool stop = CountSolutionsRecursive(grid, ref count, limit);
                grid[row, col] = 0;
                if (stop) return true;
            }

            return false;
        }

        public static bool TrySolve(int[,] grid, out int[,] solution)
        {
            solution = (int[,])grid.Clone();
            return SolveRecursive(solution);
        }

        private static bool SolveRecursive(int[,] grid)
        {
            if (!FindEmpty(grid, out int row, out int col)) return true;

            for (int digit = 1; digit <= Size; digit++)
            {
                if (!IsSafe(grid, row, col, digit)) continue;
                grid[row, col] = digit;
                if (SolveRecursive(grid)) return true;
                grid[row, col] = 0;
            }

            return false;
        }

        private static bool FindEmpty(int[,] grid, out int row, out int col)
        {
            for (row = 0; row < Size; row++)
                for (col = 0; col < Size; col++)
                    if (grid[row, col] == 0) return true;
            row = -1;
            col = -1;
            return false;
        }
    }
}