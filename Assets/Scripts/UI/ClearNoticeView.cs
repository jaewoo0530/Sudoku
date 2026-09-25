using System.Collections;
using UnityEngine;

namespace Sudoku.UI
{
    public class ClearNoticeView : MonoBehaviour
    {
        private const float DisplaySeconds = 1.75f;

        private Coroutine _hideRoutine;

        public void Show()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }
            gameObject.SetActive(true);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(DisplaySeconds);
            gameObject.SetActive(false);
            _hideRoutine = null;
        }
    }
}
