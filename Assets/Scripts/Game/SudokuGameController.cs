using System;
using Sudoku.Core;
using UnityEngine;

namespace Sudoku.Game
{
    public class SudokuGameController : MonoBehaviour
    {
        [SerializeField] private int targetGivens = 32;

        public SudokuBoard Board { get; private set; }
        public int SelectedRow { get; private set; } = -1;
        public int SelectedCol { get; private set; } = -1;
        public bool HasSelection => SelectedRow >= 0;

        public event Action OnBoardReset;
        public event Action OnSelectionChanged;
        public event Action<int, int> OnCellChanged;

        private void Awake()
        {
            NewGame();
        }

        public void NewGame()
        {
            var puzzle = SudokuGenerator.Generate(targetGivens);
            Board = new SudokuBoard();
            Board.LoadGivens(puzzle.Givens);
            SelectedRow = -1;
            SelectedCol = -1;
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

            Board.SetValue(SelectedRow, SelectedCol, digit);
            OnCellChanged?.Invoke(SelectedRow, SelectedCol);
        }

        public void TogglePencilMark(int digit)
        {
            if (digit < 1 || digit > 9) return;
            if (!HasSelection) return;
            if (Board.IsGiven(SelectedRow, SelectedCol)) return;

            Board.ToggleMark(SelectedRow, SelectedCol, digit);
            OnCellChanged?.Invoke(SelectedRow, SelectedCol);
        }

        public void ClearValue(int row, int col)
        {
            if (Board.IsGiven(row, col)) return;

            Board.ClearValue(row, col);
            OnCellChanged?.Invoke(row, col);
        }
    }
}
