using Sudoku.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.UI
{
    public class DifficultyButtonView : MonoBehaviour
    {
        [SerializeField] private SudokuGameController controller;
        [SerializeField] private DifficultySettings difficulty;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => controller.NewGame(difficulty));
        }
    }
}
