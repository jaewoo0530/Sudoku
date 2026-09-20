namespace Sudoku.Core
{
    public class SudokuBoard
    {
        public const int Size = 9;
        public const int BoxSize = 3;

        private readonly Cell[,] _cells = new Cell[Size, Size];

        public Cell GetCell(int row, int col) => _cells[row, col];

        public int GetValue(int row, int col) => _cells[row, col].Value;

        public bool IsGiven(int row, int col) => _cells[row, col].IsGiven;

        public void LoadGivens(int[,] givens)
        {
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    int v = givens[r, c];
                    _cells[r, c] = new Cell
                    {
                        Value = v,
                        IsGiven = v != 0,
                        PencilMask = 0
                    };
                }
            }
        }

        public void SetValue(int row, int col, int digit)
        {
            Cell cell = _cells[row, col];
            cell.Value = digit;
            cell.PencilMask = 0;
            _cells[row, col] = cell;
        }

        // If the cell currently holds a value, clears it first so value and
        // marks stay mutually exclusive, then toggles the requested mark.
        public void ToggleMark(int row, int col, int digit)
        {
            Cell cell = _cells[row, col];
            if (cell.Value != 0)
            {
                cell.Value = 0;
            }
            cell.ToggleMark(digit);
            _cells[row, col] = cell;
        }

        public void ClearValue(int row, int col)
        {
            Cell cell = _cells[row, col];
            cell.Value = 0;
            cell.PencilMask = 0;
            _cells[row, col] = cell;
        }
    }
}
