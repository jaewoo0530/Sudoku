namespace Sudoku.Core
{
    public struct Cell
    {
        public int Value; // 0 = empty, 1-9 = filled
        public bool IsGiven;
        public int PencilMask; // bit (n-1) set => digit n is marked

        public bool HasMark(int digit) => (PencilMask & (1 << (digit - 1))) != 0;

        public void SetMark(int digit, bool on)
        {
            int bit = 1 << (digit - 1);
            PencilMask = on ? (PencilMask | bit) : (PencilMask & ~bit);
        }

        public void ToggleMark(int digit)
        {
            PencilMask ^= 1 << (digit - 1);
        }
    }
}
