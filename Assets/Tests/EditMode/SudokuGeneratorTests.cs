using System;
using NUnit.Framework;
using Sudoku.Core;

public class SudokuGeneratorTests
{
    [Test]
    public void Generate_ProducesACompleteValidSolution()
    {
        var puzzle = SudokuGenerator.Generate(32, new Random(1));

        AssertIsCompleteValidGrid(puzzle.Solution);
    }

    [Test]
    public void Generate_ProducesAPuzzleWithExactlyOneSolution()
    {
        var puzzle = SudokuGenerator.Generate(32, new Random(2));

        Assert.AreEqual(1, SudokuSolver.CountSolutions(puzzle.Givens, 2));
    }

    [Test]
    public void Generate_MatchesRequestedGivensCount()
    {
        var puzzle = SudokuGenerator.Generate(32, new Random(3));

        int count = 0;
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (puzzle.Givens[r, c] != 0) count++;

        Assert.AreEqual(32, count);
    }

    [Test]
    public void Generate_GivensAreConsistentWithSolution()
    {
        var puzzle = SudokuGenerator.Generate(32, new Random(4));

        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (puzzle.Givens[r, c] != 0)
                    Assert.AreEqual(puzzle.Solution[r, c], puzzle.Givens[r, c]);
    }

    [Test]
    public void Generate_ReliablyMatchesALowGivensTarget()
    {
        // 25 givens (Hard difficulty) is close enough to the 17-clue
        // theoretical minimum that a single greedy digging pass can get
        // stuck one or two short of the target purely based on removal
        // order. Try a spread of seeds to guard against that regression.
        for (int seed = 100; seed < 110; seed++)
        {
            var puzzle = SudokuGenerator.Generate(25, new Random(seed));

            int count = 0;
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (puzzle.Givens[r, c] != 0) count++;

            Assert.AreEqual(25, count, $"seed {seed} produced {count} givens instead of 25");
        }
    }

    private static void AssertIsCompleteValidGrid(int[,] grid)
    {
        for (int i = 0; i < 9; i++)
        {
            AssertIsPermutationOfOneToNine(RowValues(grid, i));
            AssertIsPermutationOfOneToNine(ColValues(grid, i));
        }

        for (int boxRow = 0; boxRow < 3; boxRow++)
            for (int boxCol = 0; boxCol < 3; boxCol++)
                AssertIsPermutationOfOneToNine(BoxValues(grid, boxRow, boxCol));
    }

    private static void AssertIsPermutationOfOneToNine(int[] values)
    {
        var seen = new bool[10];
        foreach (int v in values)
        {
            Assert.IsTrue(v >= 1 && v <= 9);
            Assert.IsFalse(seen[v], "Duplicate digit found");
            seen[v] = true;
        }
    }

    private static int[] RowValues(int[,] grid, int row)
    {
        var values = new int[9];
        for (int c = 0; c < 9; c++) values[c] = grid[row, c];
        return values;
    }

    private static int[] ColValues(int[,] grid, int col)
    {
        var values = new int[9];
        for (int r = 0; r < 9; r++) values[r] = grid[r, col];
        return values;
    }

    private static int[] BoxValues(int[,] grid, int boxRow, int boxCol)
    {
        var values = new int[9];
        int index = 0;
        for (int r = boxRow * 3; r < boxRow * 3 + 3; r++)
            for (int c = boxCol * 3; c < boxCol * 3 + 3; c++)
                values[index++] = grid[r, c];
        return values;
    }
}
