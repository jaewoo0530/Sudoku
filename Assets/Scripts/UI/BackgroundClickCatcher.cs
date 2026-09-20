using Sudoku.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sudoku.UI
{
    public class BackgroundClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        private SudokuGameController _controller;
        private BoardView _boardView;

        public void Initialize(SudokuGameController controller, BoardView boardView)
        {
            _controller = controller;
            _boardView = boardView;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _controller.ClearSelection();
            _boardView.HidePicker();
        }
    }
}
