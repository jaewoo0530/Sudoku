using Sudoku.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sudoku.UI
{
    public class CellView : MonoBehaviour, IPointerClickHandler
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        private SudokuGameController _controller;
        private BoardView _boardView;
        private Image _background;
        private Text _valueText;
        private Text[] _markTexts;

        private static readonly Color GivenColor = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color NormalColor = Color.white;
        private static readonly Color SelectedColor = new Color(0.75f, 0.88f, 1f);
        private static readonly Color ConflictColor = new Color(1f, 0.7f, 0.7f);

        public void Initialize(SudokuGameController controller, BoardView boardView, int row, int col,
            Image background, Text valueText, Text[] markTexts)
        {
            _controller = controller;
            _boardView = boardView;
            Row = row;
            Col = col;
            _background = background;
            _valueText = valueText;
            _markTexts = markTexts;
        }

        public void Refresh(bool isSelected)
        {
            var cell = _controller.Board.GetCell(Row, Col);
            bool hasConflict = _controller.Board.HasConflictAt(Row, Col);

            _background.raycastTarget = !cell.IsGiven;
            _background.color = hasConflict
                ? ConflictColor
                : (cell.IsGiven ? GivenColor : (isSelected ? SelectedColor : NormalColor));

            if (cell.Value != 0)
            {
                _valueText.text = cell.Value.ToString();
                _valueText.gameObject.SetActive(true);
                foreach (var t in _markTexts) t.text = string.Empty;
            }
            else
            {
                _valueText.gameObject.SetActive(false);
                for (int digit = 1; digit <= 9; digit++)
                {
                    var t = _markTexts[digit - 1];
                    t.text = cell.HasMark(digit) ? digit.ToString() : string.Empty;
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_controller.Board.IsGiven(Row, Col)) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _controller.SelectCell(Row, Col);
                _boardView.ShowPickerFor(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (_controller.Board.GetValue(Row, Col) != 0)
                {
                    _controller.ClearValue(Row, Col);
                }
            }
        }
    }
}
