using Sudoku.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.UI
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private SudokuGameController controller;
        [SerializeField] private float boardSize = 540f;
        [SerializeField] private float boxSpacing = 6f;
        [SerializeField] private float cellSpacing = 1f;

        private readonly CellView[,] _cells = new CellView[9, 9];
        private NumberPickerView _picker;

        private void Awake()
        {
            BuildBoard();
            BuildPicker();
        }

        private void OnEnable()
        {
            controller.OnBoardReset += HandleBoardReset;
            controller.OnSelectionChanged += HandleSelectionChanged;
            controller.OnCellChanged += HandleCellChanged;
        }

        private void OnDisable()
        {
            controller.OnBoardReset -= HandleBoardReset;
            controller.OnSelectionChanged -= HandleSelectionChanged;
            controller.OnCellChanged -= HandleCellChanged;
        }

        private void Start()
        {
            RefreshAll();
        }

        private void HandleBoardReset()
        {
            _picker.Hide();
            RefreshAll();
        }

        private void HandleSelectionChanged()
        {
            RefreshAll();
        }

        private void HandleCellChanged(int row, int col)
        {
            _cells[row, col].Refresh(IsSelected(row, col));
        }

        private bool IsSelected(int row, int col)
        {
            return controller.HasSelection && controller.SelectedRow == row && controller.SelectedCol == col;
        }

        private void RefreshAll()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    _cells[r, c].Refresh(IsSelected(r, c));
        }

        public void ShowPickerFor(CellView cell)
        {
            var canvasRect = (RectTransform)transform;
            var cellRect = (RectTransform)cell.transform;
            Vector3 worldCenter = cellRect.TransformPoint(cellRect.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldCenter);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);
            _picker.transform.SetParent(canvasRect, false);
            _picker.ShowAt(localPoint);
            _picker.transform.SetAsLastSibling();
        }

        public void HidePicker()
        {
            _picker.Hide();
        }

        private void BuildBoard()
        {
            var canvasTransform = GetComponentInParent<Canvas>().transform;

            var boardRoot = CreateUIObject("BoardRoot", canvasTransform);
            boardRoot.sizeDelta = new Vector2(boardSize, boardSize);
            boardRoot.anchorMin = boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            boardRoot.anchoredPosition = Vector2.zero;

            float boxSize = (boardSize - boxSpacing * 4f) / 3f;
            var outerGrid = boardRoot.gameObject.AddComponent<GridLayoutGroup>();
            outerGrid.cellSize = new Vector2(boxSize, boxSize);
            outerGrid.spacing = new Vector2(boxSpacing, boxSpacing);
            outerGrid.padding = new RectOffset((int)boxSpacing, (int)boxSpacing, (int)boxSpacing, (int)boxSpacing);

            for (int boxRow = 0; boxRow < 3; boxRow++)
            {
                for (int boxCol = 0; boxCol < 3; boxCol++)
                {
                    var boxRect = CreateUIObject($"Box_{boxRow}_{boxCol}", boardRoot);
                    var boxImage = boxRect.gameObject.AddComponent<Image>();
                    boxImage.color = new Color(0.6f, 0.6f, 0.6f);
                    boxImage.raycastTarget = false;

                    float cellSize = (boxSize - cellSpacing * 4f) / 3f;
                    var innerGrid = boxRect.gameObject.AddComponent<GridLayoutGroup>();
                    innerGrid.cellSize = new Vector2(cellSize, cellSize);
                    innerGrid.spacing = new Vector2(cellSpacing, cellSpacing);
                    innerGrid.padding = new RectOffset((int)cellSpacing, (int)cellSpacing, (int)cellSpacing, (int)cellSpacing);

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int row = boxRow * 3 + r;
                            int col = boxCol * 3 + c;
                            _cells[row, col] = BuildCell(boxRect, row, col, cellSize);
                        }
                    }
                }
            }

            BuildBackgroundCatcher(canvasTransform);
        }

        private CellView BuildCell(Transform parent, int row, int col, float cellSizePx)
        {
            var cellRect = CreateUIObject($"Cell_{row}_{col}", parent);
            var background = cellRect.gameObject.AddComponent<Image>();
            background.color = Color.white;

            var valueRect = CreateUIObject("Value", cellRect);
            StretchFull(valueRect);
            var valueText = valueRect.gameObject.AddComponent<Text>();
            ConfigureText(valueText, Mathf.RoundToInt(cellSizePx * 0.55f));

            var marksRect = CreateUIObject("Marks", cellRect);
            StretchFull(marksRect);
            var marksGrid = marksRect.gameObject.AddComponent<GridLayoutGroup>();
            float markCellSize = cellSizePx / 3f;
            marksGrid.cellSize = new Vector2(markCellSize, markCellSize);
            marksGrid.spacing = Vector2.zero;

            var markTexts = new Text[9];
            for (int i = 0; i < 9; i++)
            {
                var markCellRect = CreateUIObject($"Mark_{i + 1}", marksRect);
                var markText = markCellRect.gameObject.AddComponent<Text>();
                ConfigureText(markText, Mathf.RoundToInt(markCellSize * 0.6f));
                markText.text = string.Empty;
                markTexts[i] = markText;
            }

            var cellView = cellRect.gameObject.AddComponent<CellView>();
            cellView.Initialize(controller, this, row, col, background, valueText, markTexts);
            return cellView;
        }

        private void BuildBackgroundCatcher(Transform canvasTransform)
        {
            var catcherRect = CreateUIObject("BackgroundCatcher", canvasTransform);
            StretchFull(catcherRect);
            var image = catcherRect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            var catcher = catcherRect.gameObject.AddComponent<BackgroundClickCatcher>();
            catcher.Initialize(controller, this);
            catcherRect.SetAsFirstSibling();
        }

        private void BuildPicker()
        {
            var pickerRect = CreateUIObject("NumberPicker", transform);
            pickerRect.sizeDelta = new Vector2(180, 180);
            pickerRect.anchorMin = pickerRect.anchorMax = new Vector2(0.5f, 0.5f);
            pickerRect.pivot = new Vector2(0.5f, 0.5f);

            var pickerBackground = pickerRect.gameObject.AddComponent<Image>();
            pickerBackground.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var grid = pickerRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(56, 56);
            grid.spacing = new Vector2(4, 4);
            grid.padding = new RectOffset(4, 4, 4, 4);

            var buttons = new PickerButton[9];
            for (int i = 0; i < 9; i++)
            {
                var buttonRect = CreateUIObject($"Digit_{i + 1}", pickerRect);
                var buttonImage = buttonRect.gameObject.AddComponent<Image>();
                buttonImage.color = Color.white;

                var textRect = CreateUIObject("Label", buttonRect);
                StretchFull(textRect);
                var text = textRect.gameObject.AddComponent<Text>();
                ConfigureText(text, 24);
                text.text = (i + 1).ToString();

                buttons[i] = buttonRect.gameObject.AddComponent<PickerButton>();
            }

            _picker = pickerRect.gameObject.AddComponent<NumberPickerView>();
            _picker.Initialize(controller, buttons);
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ConfigureText(Text text, int fontSize)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.raycastTarget = false;
        }
    }
}
