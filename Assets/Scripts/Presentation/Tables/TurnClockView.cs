using System;
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
        public void RenderProgress(int seat, float fill, bool visible)
        {
            for (var index = 0; index < _turnSliders.Length; index++)
                _turnSliders[index].value = visible && seat == index ? Mathf.Clamp01(fill) : 0;
        }
        public void RenderText(int seat, string text, bool visible)
        {
            _clockText.text = visible && seat == 0 ? text : string.Empty;
            for (var index = 0; index < _opponentClockTexts.Length; index++)
                _opponentClockTexts[index].text = visible && seat == index + 1 ? text : string.Empty;
        }
    }
}
