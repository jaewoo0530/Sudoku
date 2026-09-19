using UnityEngine;
using UnityEngine.InputSystem;

namespace Sudoku.Game
{
    public class SudokuInput : MonoBehaviour
    {
        [SerializeField] private SudokuGameController controller;

        private static readonly Key[] DigitKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !controller.HasSelection) return;

            for (int i = 0; i < DigitKeys.Length; i++)
            {
                if (keyboard[DigitKeys[i]].wasPressedThisFrame)
                {
                    controller.SetValue(i + 1);
                    break;
                }
            }
        }
    }
}
