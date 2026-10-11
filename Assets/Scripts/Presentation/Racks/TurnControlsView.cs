using System;
using Okey.Core.Dealing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Racks
{
    public sealed class TurnControlsView : MonoBehaviour
    {
        [SerializeField] private Button _drawButton;
        [SerializeField] private Button _discardButton;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _handText;
        [SerializeField] private TMP_Text _pileText;
        [SerializeField] private string _drawStatus;
        [SerializeField] private string _discardStatus;
        [SerializeField] private string _finishedStatus;
        [SerializeField] private string _handFormat;
        [SerializeField] private string _pileFormat;
        [SerializeField] private Transform _discardRoot;
        [SerializeField] private Vector3 _discardScale;

        public Transform DiscardRoot => _discardRoot;
        public Vector3 DiscardScale => _discardScale;
        public event Action DrawRequested;
        public event Action DiscardRequested;

        public void Render(TurnPhase phase, int handCount, int pileCount, bool canDraw, bool canDiscard)
        {
            _drawButton.interactable = canDraw;
            _discardButton.interactable = canDiscard;
            _statusText.text = phase switch
            {
                TurnPhase.AwaitingDraw => _drawStatus,
                TurnPhase.AwaitingDiscard => _discardStatus,
                _ => _finishedStatus
            };
            _handText.text = string.Format(_handFormat, handCount);
            _pileText.text = string.Format(_pileFormat, pileCount);
        }

        private void OnEnable()
        {
            _drawButton.onClick.AddListener(RequestDraw);
            _discardButton.onClick.AddListener(RequestDiscard);
        }

        private void OnDisable()
        {
            _drawButton.onClick.RemoveListener(RequestDraw);
            _discardButton.onClick.RemoveListener(RequestDiscard);
        }

        private void RequestDraw() => DrawRequested?.Invoke();
        private void RequestDiscard() => DiscardRequested?.Invoke();
    }
}
