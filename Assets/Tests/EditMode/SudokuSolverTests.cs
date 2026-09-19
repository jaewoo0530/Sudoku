using NUnit.Framework;
using Sudoku.Core;

public class SudokuSolverTests
{
    private static readonly int[,] SolvedGrid =
    {
        {5,3,4,6,7,8,9,1,2},
        {6,7,2,1,9,5,3,4,8},
        {1,9,8,3,4,2,5,6,7},
        {8,5,9,7,6,1,4,2,3},
        {4,2,6,8,5,3,7,9,1},
        {7,1,3,9,2,4,8,5,6},
        {9,6,1,5,3,7,2,8,4},
        {2,8,7,4,1,9,6,3,5},
        {3,4,5,2,8,6,1,7,9}
    };

    [Test]
    public void IsSafe_RejectsDigitAlreadyInRow()
    {
        var grid = (int[,])SolvedGrid.Clone();
        grid[0, 0] = 0;

        Assert.IsFalse(SudokuSolver.IsSafe(grid, 0, 0, 3)); // 3 already in row 0
    }

    [Test]
    public void IsSafe_AllowsValidPlacement()
    {
        var grid = (int[,])SolvedGrid.Clone();
        grid[0, 0] = 0;

        Assert.IsTrue(SudokuSolver.IsSafe(grid, 0, 0, 5)); // matches the known solution
    }

    [Test]
    public void CountSolutions_OnCompleteGrid_ReturnsOne()
    {
        Assert.AreEqual(1, SudokuSolver.CountSolutions(SolvedGrid, 2));
    }

    [Test]
    public void CountSolutions_OnEmptyGrid_ReachesLimit()
    {
        var grid = new int[9, 9];

        Assert.AreEqual(2, SudokuSolver.CountSolutions(grid, 2));
    }

    [Test]
    public void TrySolve_SolvesAPuzzleWithOneCellRemoved()
    {
        var puzzle = (int[,])SolvedGrid.Clone();
        puzzle[0, 0] = 0;

        bool solved = SudokuSolver.TrySolve(puzzle, out int[,] solution);

        Assert.IsTrue(solved);
        Assert.AreEqual(5, solution[0, 0]);
    }
}