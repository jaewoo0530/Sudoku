using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sudoku.UI
{
    public class PickerButton : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color NormalColor = Color.white;
        private static readonly Color FlashColor = new Color(1f, 0.85f, 0.3f);
        private const float FlashDuration = 0.15f;

        private int _digit;
        private Action<int, PointerEventData.InputButton> _onClick;
        private Image _background;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            _background = GetComponent<Image>();
        }

        public void Bind(int digit, Action<int, PointerEventData.InputButton> onClick)
        {
            _digit = digit;
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Flash();
            _onClick?.Invoke(_digit, eventData.button);
        }

        // Called before the button is shown again, in case a previous flash's
        // revert-to-normal coroutine was interrupted by the popup hiding
        // (e.g. a left-click immediately closes the popup, killing the
        // coroutine before it can restore the color).
        public void ResetVisual()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }
            _background.color = NormalColor;
        }

        private void Flash()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _background.color = FlashColor;
            yield return new WaitForSecondsRealtime(FlashDuration);
            _background.color = NormalColor;
            _flashRoutine = null;
        }
    }
}
