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
        _controller.NewGame();
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
    public void NewGame_ReplacesBoardAndClearsSelection()
    {
        int row = FindEmptyRow(out int col);
        _controller.SelectCell(row, col);

        _controller.NewGame();

        Assert.IsFalse(_controller.HasSelection);
        Assert.IsNotNull(_controller.Board);
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
