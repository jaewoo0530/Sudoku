using NUnit.Framework;
using Sudoku.Core;

public class SudokuBoardTests
{
    [Test]
    public void LoadGivens_MarksNonZeroCellsAsGiven()
    {
        var board = new SudokuBoard();
        var grid = new int[9, 9];
        grid[0, 0] = 5;

        board.LoadGivens(grid);

        Assert.IsTrue(board.IsGiven(0, 0));
        Assert.AreEqual(5, board.GetValue(0, 0));
        Assert.IsFalse(board.IsGiven(0, 1));
        Assert.AreEqual(0, board.GetValue(0, 1));
    }

    [Test]
    public void SetValue_OverwritesExistingValueAndClearsPencilMask()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.ToggleMark(2, 2, 4);
        board.ToggleMark(2, 2, 7);

        board.SetValue(2, 2, 3);

        Assert.AreEqual(3, board.GetValue(2, 2));
        Assert.IsFalse(board.GetCell(2, 2).HasMark(4));
        Assert.IsFalse(board.GetCell(2, 2).HasMark(7));
    }

    [Test]
    public void ToggleMark_OnFilledCell_ClearsValueAndStartsNotesMode()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.SetValue(4, 4, 9);

        board.ToggleMark(4, 4, 2);

        Assert.AreEqual(0, board.GetValue(4, 4));
        Assert.IsTrue(board.GetCell(4, 4).HasMark(2));
    }

    [Test]
    public void ToggleMark_TwiceWithSameDigit_RemovesMark()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);

        board.ToggleMark(0, 0, 6);
        board.ToggleMark(0, 0, 6);

        Assert.IsFalse(board.GetCell(0, 0).HasMark(6));
    }

    [Test]
    public void ClearValue_OnFilledCell_SetsValueToZero()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.SetValue(3, 3, 8);

        board.ClearValue(3, 3);

        Assert.AreEqual(0, board.GetValue(3, 3));
    }

    [Test]
    public void ClearValue_AlsoClearsAnyPencilMask()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.SetValue(1, 1, 4);

        board.ClearValue(1, 1);

        Assert.IsFalse(board.GetCell(1, 1).HasMark(4));
    }

    [Test]
    public void HasConflictAt_SameRowDuplicate_ReturnsTrue()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.SetValue(0, 0, 5);
        board.SetValue(0, 3, 5);

        Assert.IsTrue(board.HasConflictAt(0, 0));
        Assert.IsTrue(board.HasConflictAt(0, 3));
    }

    [Test]
    public void HasConflictAt_SameColumnDuplicate_ReturnsTrue()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.SetValue(1, 2, 7);
        board.SetValue(5, 2, 7);

        Assert.IsTrue(board.HasConflictAt(1, 2));
        Assert.IsTrue(board.HasConflictAt(5, 2));
    }

    [Test]
    public void HasConflictAt_SameBoxDuplicate_ReturnsTrue()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.SetValue(0, 0, 9);
        board.SetValue(2, 2, 9);

        Assert.IsTrue(board.HasConflictAt(0, 0));
        Assert.IsTrue(board.HasConflictAt(2, 2));
    }

    [Test]
    public void HasConflictAt_NoSharedRowColumnOrBox_ReturnsFalse()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.SetValue(0, 0, 4);
        board.SetValue(4, 4, 4);

        Assert.IsFalse(board.HasConflictAt(0, 0));
        Assert.IsFalse(board.HasConflictAt(4, 4));
    }

    [Test]
    public void HasConflictAt_EmptyCell_ReturnsFalse()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);

        Assert.IsFalse(board.HasConflictAt(0, 0));
    }

    [Test]
    public void IsSolved_WithEmptyCell_ReturnsFalse()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        board.SetValue(0, 0, 1);

        Assert.IsFalse(board.IsSolved());
    }

    [Test]
    public void IsSolved_FullButConflicted_ReturnsFalse()
    {
        var board = new SudokuBoard();
        var grid = new int[9, 9];
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                grid[r, c] = 1;
        board.LoadGivens(grid);

        Assert.IsFalse(board.IsSolved());
    }

    [Test]
    public void IsSolved_FullAndConflictFree_ReturnsTrue()
    {
        var board = new SudokuBoard();
        int[,] solvedGrid =
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
        board.LoadGivens(solvedGrid);

        Assert.IsTrue(board.IsSolved());
    }

    [Test]
    public void SetCell_WritesExactCellState()
    {
        var board = new SudokuBoard();
        board.LoadGivens(new int[9, 9]);
        var cell = new Cell { Value = 6, IsGiven = false, PencilMask = 0 };

        board.SetCell(3, 3, cell);

        Assert.AreEqual(6, board.GetValue(3, 3));
        Assert.IsFalse(board.GetCell(3, 3).IsGiven);
    }
}
