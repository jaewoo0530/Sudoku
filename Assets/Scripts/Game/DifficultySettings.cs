using UnityEngine;

namespace Sudoku.Game
{
    [CreateAssetMenu(fileName = "DifficultySettings", menuName = "Sudoku/Difficulty Settings")]
    public class DifficultySettings : ScriptableObject
    {
        public string displayName = "Normal";
        public int emptyCells = 49;
    }
}
