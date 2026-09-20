using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sudoku.UI
{
    public class PickerButton : MonoBehaviour, IPointerClickHandler
    {
        private int _digit;
        private Action<int, PointerEventData.InputButton> _onClick;

        public void Bind(int digit, Action<int, PointerEventData.InputButton> onClick)
        {
            _digit = digit;
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke(_digit, eventData.button);
        }
    }
}
