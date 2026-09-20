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
}
