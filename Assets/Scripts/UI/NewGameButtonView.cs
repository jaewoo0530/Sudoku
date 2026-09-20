using Sudoku.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.UI
{
    public class NewGameButtonView : MonoBehaviour
    {
        [SerializeField] private SudokuGameController controller;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(controller.NewGame);
        }
    }
}
