using Sudoku.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sudoku.UI
{
    public class NumberPickerView : MonoBehaviour
    {
        private SudokuGameController _controller;

        public void Initialize(SudokuGameController controller, PickerButton[] buttons)
        {
            _controller = controller;
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
                Hide();
            }
            else if (button == PointerEventData.InputButton.Right)
            {
                _controller.TogglePencilMark(digit);
            }
        }

        public void ShowAt(Vector2 anchoredPosition)
        {
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
