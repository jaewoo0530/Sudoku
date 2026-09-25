using System;
using System.Collections.Generic;
using Sudoku.Core;
using UnityEngine;

namespace Sudoku.Game
{
    public class SudokuGameController : MonoBehaviour
    {
        [SerializeField] private int easyEmptyCells = 40;
        [SerializeField] private int normalEmptyCells = 49;
        [SerializeField] private int hardEmptyCells = 56;

        private readonly Stack<CellEditCommand> _undoStack = new Stack<CellEditCommand>();

        public SudokuBoard Board { get; private set; }
        public int SelectedRow { get; private set; } = -1;
        public int SelectedCol { get; private set; } = -1;
        public bool HasSelection => SelectedRow >= 0;
        public int UndoStackCount => _undoStack.Count;

        public event Action OnBoardReset;
        public event Action OnSelectionChanged;
        public event Action<int, int> OnCellChanged;
        public event Action OnPuzzleSolved;

        private void Awake()
        {
            NewGame(Difficulty.Normal);
        }

        public void NewGame(Difficulty difficulty)
        {
            int emptyCells;
            if (difficulty == Difficulty.Easy) emptyCells = easyEmptyCells;
            else if (difficulty == Difficulty.Hard) emptyCells = hardEmptyCells;
            else emptyCells = normalEmptyCells;

            int targetGivens = SudokuBoard.Size * SudokuBoard.Size - emptyCells;

            var puzzle = SudokuGenerator.Generate(targetGivens);
            Board = new SudokuBoard();
            Board.LoadGivens(puzzle.Givens);
            SelectedRow = -1;
            SelectedCol = -1;
            _undoStack.Clear();
            OnBoardReset?.Invoke();
        }

        public void SelectCell(int row, int col)
        {
            if (Board.IsGiven(row, col)) return;

            SelectedRow = row;
            SelectedCol = col;
            OnSelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            SelectedRow = -1;
            SelectedCol = -1;
            OnSelectionChanged?.Invoke();
        }

        public void SetValue(int digit)
        {
            if (digit < 1 || digit > 9) return;
            if (!HasSelection) return;
            if (Board.IsGiven(SelectedRow, SelectedCol)) return;

            Cell before = Board.GetCell(SelectedRow, SelectedCol);
            Board.SetValue(SelectedRow, SelectedCol, digit);
            PushIfChanged(SelectedRow, SelectedCol, before);
            OnCellChanged?.Invoke(SelectedRow, SelectedCol);

            if (Board.IsSolved())
            {
                OnPuzzleSolved?.Invoke();
            }
        }

        public void TogglePencilMark(int digit)
        {
            if (digit < 1 || digit > 9) return;
            if (!HasSelection) return;
            if (Board.IsGiven(SelectedRow, SelectedCol)) return;

            Cell before = Board.GetCell(SelectedRow, SelectedCol);
            Board.ToggleMark(SelectedRow, SelectedCol, digit);
            PushIfChanged(SelectedRow, SelectedCol, before);
            OnCellChanged?.Invoke(SelectedRow, SelectedCol);
        }

        public void ClearValue(int row, int col)
        {
            if (Board.IsGiven(row, col)) return;

            Cell before = Board.GetCell(row, col);
            Board.ClearValue(row, col);
            PushIfChanged(row, col, before);
            OnCellChanged?.Invoke(row, col);
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            CellEditCommand command = _undoStack.Pop();
            command.Undo(Board);
            OnCellChanged?.Invoke(command.Row, command.Col);
        }

        private void PushIfChanged(int row, int col, Cell before)
        {
            Cell after = Board.GetCell(row, col);
            if (before.Value != after.Value || before.PencilMask != after.PencilMask)
            {
                _undoStack.Push(new CellEditCommand(row, col, before, after));
            }
        }
    }
}
