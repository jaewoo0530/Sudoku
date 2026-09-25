using Sudoku.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sudoku.UI
{
    public class NumberPickerView : MonoBehaviour
    {
        private SudokuGameController _controller;
        private PickerButton[] _buttons;

        public void Initialize(SudokuGameController controller, PickerButton[] buttons)
        {
            _controller = controller;
            _buttons = buttons;
            for (int i = 0; i < buttons.Length; i++)
            {
                int digit = i + 1;
                buttons[i].Bind(digit, OnDigitClicked);
            }
            gameObject.SetActive(false);
        }

        private void OnDigitClicked(int digit, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Left)
            {
                _controller.SetValue(digit);
            }
            else if (button == PointerEventData.InputButton.Right)
            {
                _controller.TogglePencilMark(digit);
            }
        }

        public void ShowAt(Vector2 anchoredPosition)
        {
            foreach (var button in _buttons) button.ResetVisual();
            var rect = (RectTransform)transform;
            rect.anchoredPosition = anchoredPosition;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
