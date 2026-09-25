using NUnit.Framework;
using Sudoku.Game;
using UnityEngine;

public class SudokuGameControllerTests
{
    private SudokuGameController _controller;

    [SetUp]
    public void SetUp()
    {
        var go = new GameObject("Controller");
        _controller = go.AddComponent<SudokuGameController>();
        _controller.NewGame(Difficulty.Normal);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_controller.gameObject);
    }

    [Test]
    public void SelectCell_OnGivenCell_DoesNotSelect()
    {
        FindGivenCell(out int row, out int col);

        _controller.SelectCell(row, col);

        Assert.IsFalse(_controller.HasSelection);
    }

    [Test]
    public void SelectCell_OnEmptyCell_Selects()
    {
        int row = FindEmptyRow(out int col);

        _controller.SelectCell(row, col);

        Assert.IsTrue(_controller.HasSelection);
        Assert.AreEqual(row, _controller.SelectedRow);
        Assert.AreEqual(col, _controller.SelectedCol);
    }

    [Test]
    public void SetValue_IgnoresOutOfRangeDigits()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);

        _controller.SetValue(0);
        _controller.SetValue(10);

        Assert.AreEqual(0, _controller.Board.GetValue(row, col));
    }

    [Test]
    public void SetValue_OnSelectedEmptyCell_SetsValue()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);

        _controller.SetValue(7);

        Assert.AreEqual(7, _controller.Board.GetValue(row, col));
    }

    [Test]
    public void TogglePencilMark_OnFilledCell_SwitchesToNotesMode()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);
        _controller.SetValue(7);

        _controller.TogglePencilMark(3);

        Assert.AreEqual(0, _controller.Board.GetValue(row, col));
        Assert.IsTrue(_controller.Board.GetCell(row, col).HasMark(3));
    }

    [Test]
    public void ClearValue_OnFilledNonGivenCell_ClearsIt()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);
        _controller.SetValue(6);

        _controller.ClearValue(row, col);

        Assert.AreEqual(0, _controller.Board.GetValue(row, col));
    }

    [Test]
    public void ClearValue_OnGivenCell_DoesNothing()
    {
        FindGivenCell(out int row, out int col);
        int originalValue = _controller.Board.GetValue(row, col);

        _controller.ClearValue(row, col);

        Assert.AreEqual(originalValue, _controller.Board.GetValue(row, col));
    }

    [Test]
    public void NewGame_ReplacesBoardAndClearsSelection()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);

        _controller.NewGame(Difficulty.Normal);

        Assert.IsFalse(_controller.HasSelection);
        Assert.IsNotNull(_controller.Board);
    }

    [Test]
    public void NewGame_Easy_UsesEasyEmptyCellsCount()
    {
        _controller.NewGame(Difficulty.Easy);

        Assert.AreEqual(81 - 40, CountFilledCells());
    }

    [Test]
    public void NewGame_Hard_UsesHardEmptyCellsCount()
    {
        _controller.NewGame(Difficulty.Hard);

        Assert.AreEqual(81 - 56, CountFilledCells());
    }

    [Test]
    public void Undo_AfterSetValue_RestoresPriorValue()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);
        _controller.SetValue(5);

        _controller.Undo();

        Assert.AreEqual(0, _controller.Board.GetValue(row, col));
    }

    [Test]
    public void Undo_AfterTogglePencilMark_RestoresPriorMask()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);
        _controller.TogglePencilMark(4);

        _controller.Undo();

        Assert.IsFalse(_controller.Board.GetCell(row, col).HasMark(4));
    }

    [Test]
    public void Undo_AfterClearValue_RestoresPriorValue()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);
        _controller.SetValue(8);
        _controller.ClearValue(row, col);

        _controller.Undo();

        Assert.AreEqual(8, _controller.Board.GetValue(row, col));
    }

    [Test]
    public void Undo_OnEmptyStack_DoesNothing()
    {
        Assert.DoesNotThrow(() => _controller.Undo());
    }

    [Test]
    public void SetValue_ReenteringSameDigit_DoesNotGrowUndoStack()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);
        _controller.SetValue(6);
        _controller.SetValue(6);

        Assert.AreEqual(1, _controller.UndoStackCount);
    }

    [Test]
    public void NewGame_ClearsUndoStack()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);
        _controller.SetValue(3);

        _controller.NewGame(Difficulty.Normal);

        Assert.AreEqual(0, _controller.UndoStackCount);
    }

    [Test]
    public void OnPuzzleSolved_FiresWhenBoardBecomesFullyAndCorrectlySolved()
    {
        bool fired = false;
        _controller.OnPuzzleSolved += () => fired = true;

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
        int[,] almostSolved = (int[,])solvedGrid.Clone();
        almostSolved[8, 8] = 0;
        _controller.Board.LoadGivens(almostSolved);

        _controller.SelectCell(8, 8);
        _controller.SetValue(9);

        Assert.IsTrue(fired);
    }

    [Test]
    public void OnPuzzleSolved_DoesNotFireWhenBoardStillHasEmptyCells()
    {
        bool fired = false;
        _controller.OnPuzzleSolved += () => fired = true;

        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);
        _controller.SetValue(5);

        Assert.IsFalse(fired);
    }

    private int CountFilledCells()
    {
        int count = 0;
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (_controller.Board.GetValue(r, c) != 0) count++;
        return count;
    }

    private void FindGivenCell(out int row, out int col)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (_controller.Board.IsGiven(r, c)) { row = r; col = c; return; }
        row = -1;
        col = -1;
    }

    private int FindEmptyRow(out int col)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (!_controller.Board.IsGiven(r, c)) { col = c; return r; }
        col = -1;
        return -1;
    }
}
