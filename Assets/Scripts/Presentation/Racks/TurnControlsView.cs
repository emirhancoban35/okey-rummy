using System;
using Okey.Core.Dealing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Racks
{
    public sealed class TurnControlsView : MonoBehaviour
    {
        [SerializeField] private DrawSourceView _drawSource;
        [SerializeField] private DiscardAreaView[] _discardAreas;
        [SerializeField] private TMP_Text[] _opponentTexts;
        [SerializeField] private GameObject[] _opponentHands;
        [SerializeField] private Button _seriesButton;
        [SerializeField] private Button _pairsButton;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _handText;
        [SerializeField] private TMP_Text _pileText;
        [SerializeField] private TMP_Text _pointsText;
        [SerializeField] private TMP_Text _penaltyText;
        [SerializeField] private string _drawStatus;
        [SerializeField] private string _discardStatus;
        [SerializeField] private string _finishedStatus;
        [SerializeField] private string _waitingFormat;
        [SerializeField] private string _handFormat;
        [SerializeField] private string _pileFormat;
        [SerializeField] private string _pointsFormat;
        [SerializeField] private string _penaltyFormat;
        [SerializeField] private string _opponentFormat;
        [SerializeField] private Vector3 _discardScale;
        [SerializeField] private Transform _meldRoot;
        [SerializeField] private Transform _indicatorRoot;
        [SerializeField] private Vector3 _meldScale;
        [SerializeField] private Vector2 _meldSpacing;
        [SerializeField, Min(1)] private int _meldColumns;
        [SerializeField, Min(0)] private float _meldDropRadius;

        public DrawSourceView DrawSource => _drawSource;
        public DiscardAreaView[] DiscardAreas => _discardAreas;
        public Transform MeldRoot => _meldRoot;
        public Transform IndicatorRoot => _indicatorRoot;
        public Vector3 DiscardScale => _discardScale;
        public Vector3 MeldScale => _meldScale;
        public float MeldDropRadius => _meldDropRadius;
        public event Action DrawRequested;
        public event Action SeriesRequested;
        public event Action PairsRequested;

        public Vector3 GetMeldPosition(int column, int row) => new Vector3(column * _meldSpacing.x, -row * _meldSpacing.y, 0);
        public int MeldColumns => _meldColumns;

        public void Render(TurnPhase phase, int seat, int handCount, int pileCount, int points, int pairs,
            int threshold, int pairThreshold, int penalty, bool canDraw, bool canOpen)
        {
            _drawSource.CanDraw = canDraw;
            _seriesButton.interactable = canOpen;
            _pairsButton.interactable = canOpen;
            _statusText.text = phase == TurnPhase.Finished ? _finishedStatus : seat != 0 ? string.Format(_waitingFormat, seat) :
                phase == TurnPhase.AwaitingDraw ? _drawStatus : _discardStatus;
            _handText.text = string.Format(_handFormat, handCount);
            _pileText.text = string.Format(_pileFormat, pileCount);
            _pointsText.text = string.Format(_pointsFormat, points, threshold, pairs, pairThreshold);
            _penaltyText.text = string.Format(_penaltyFormat, penalty);
        }

        public void RenderOpponent(int seat, int handCount, int penalty, int score, bool active)
        {
            _opponentTexts[seat - 1].text = string.Format(_opponentFormat, seat, handCount, penalty, score, active ? "●" : "");
        }

        public void SetPlayerCount(int count)
        {
            for (var seat = 0; seat < _discardAreas.Length; seat++)
                _discardAreas[seat].gameObject.SetActive(seat < count);
            for (var index = 0; index < _opponentHands.Length; index++)
            {
                _opponentHands[index].SetActive(index < count - 1);
                _opponentTexts[index].gameObject.SetActive(index < count - 1);
            }
        }

        private void OnEnable()
        {
            _drawSource.DrawRequested += RequestDraw;
            _seriesButton.onClick.AddListener(RequestSeries);
            _pairsButton.onClick.AddListener(RequestPairs);
        }

        private void OnDisable()
        {
            _drawSource.DrawRequested -= RequestDraw;
            _seriesButton.onClick.RemoveListener(RequestSeries);
            _pairsButton.onClick.RemoveListener(RequestPairs);
        }

        private void RequestDraw() => DrawRequested?.Invoke();
        private void RequestSeries() => SeriesRequested?.Invoke();
        private void RequestPairs() => PairsRequested?.Invoke();
    }
}
