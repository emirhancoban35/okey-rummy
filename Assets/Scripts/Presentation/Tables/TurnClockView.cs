using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Tables
{
    public sealed class TurnClockView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _clockText;
        [SerializeField] private Slider[] _turnSliders;
        [SerializeField] private TMP_Text[] _opponentClockTexts;
        private string _clockFormat;
        private string[] _opponentFormats;

        private void Awake()
        {
            _clockFormat = _clockText.text;
            _opponentFormats = _opponentClockTexts.Select(text => text.text).ToArray();
        }

        public void RenderProgress(int seat, float fill, bool visible)
        {
            for (var index = 0; index < _turnSliders.Length; index++)
                _turnSliders[index].value = visible && seat == index ? Mathf.Clamp01(fill) : 0;
        }

        public void RenderText(int seat, int seconds, bool visible)
        {
            _clockText.text = visible && seat == 0 ? string.Format(_clockFormat, seconds) : string.Empty;
            for (var index = 0; index < _opponentClockTexts.Length; index++)
                _opponentClockTexts[index].text = visible && seat == index + 1 ? string.Format(_opponentFormats[index], seconds) : string.Empty;
        }
    }
}
