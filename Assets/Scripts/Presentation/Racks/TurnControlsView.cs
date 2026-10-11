using System;
using DG.Tweening;
using Okey.Core.Dealing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Racks
{
    public sealed class TurnControlsView : MonoBehaviour
    {
        [SerializeField] private Button _botModeButton;
        [SerializeField] private Button _partnershipButton;
        [SerializeField] private Button _newHandButton;
        [SerializeField] private string _humanlikeBotLabel = "BOTS: HUMANLIKE";
        [SerializeField] private string _fastBotLabel = "BOTS: FAST";
        [SerializeField] private string _nextPartnerLabel = "NEXT: PARTNERS";
        [SerializeField] private string _nextIndividualLabel = "NEXT: INDIVIDUAL";
        [SerializeField] private string _individualModeLabel = "INDIVIDUAL TABLE";
        [SerializeField] private string _partnerModeFormat = "PARTNERS  ·  You + North  ·  Team {0}";
        [SerializeField] private TMP_Text _botModeLabel;
        [SerializeField] private TMP_Text _partnershipLabel;
        public event Action BotModeRequested;
        public event Action PartnershipRequested;
        public event Action NewHandRequested;
        public void RenderOptions(bool humanlike, bool partners, bool finished)
        {
            if (_botModeLabel != null) _botModeLabel.text = humanlike ? _humanlikeBotLabel : _fastBotLabel;
            if (_partnershipLabel != null) _partnershipLabel.text = partners ? _nextPartnerLabel : _nextIndividualLabel;
            if (_newHandButton != null) _newHandButton.interactable = true;
        }
        private void RequestBotMode() => BotModeRequested?.Invoke();
        private void RequestPartnership() => PartnershipRequested?.Invoke();
        private void RequestNewHand() => NewHandRequested?.Invoke();
        [SerializeField] private TMP_Text _clockText;
        [SerializeField] private string _localSeatName = "You";
        [SerializeField] private string _clockFormat = "{0}  ·  {1:0}s";
        [SerializeField] private TMP_Text _modeText;
        [SerializeField] private Transform _pairsRoot;
        [SerializeField, Min(1)] private int _pairRows = 7;
        public Transform PairsRoot => _pairsRoot != null ? _pairsRoot : _meldRoot;
        [SerializeField, Min(.1f)] private float _pairWidth = 2.4f;
        private float PairSpacing(int count) => Mathf.Min(_meldSpacing.x, _pairWidth / (Mathf.Max(1, Mathf.CeilToInt((float)count / _pairRows)) * 3));
        public Vector3 GetPairScale(int count) => new Vector3(_meldScale.x * PairSpacing(count) / _meldSpacing.x, _meldScale.y, 1);
        public Vector3 GetPairPosition(int index, int tile, int count) => new Vector3((index / _pairRows * 3 + tile) * PairSpacing(count), -(index % _pairRows) * _meldSpacing.y, 0);
        private int _lastClockSeat = -1;
        private int _lastClockSecond = -1;
        private int _lastTeamScore = int.MinValue;
        private bool _lastPartnership;
        public void RenderClock(int seat, double remaining, bool partners, int score)
        {
            var seconds = Mathf.CeilToInt((float)remaining);
            if (seat != _lastClockSeat || seconds != _lastClockSecond)
            {
                _lastClockSeat = seat;
                _lastClockSecond = seconds;
                if (_clockText != null) _clockText.text = string.Format(_clockFormat, seat == 0 ? _localSeatName : _seatNames[seat - 1], seconds);
            }
            if (score != _lastTeamScore || partners != _lastPartnership)
            {
                _lastTeamScore = score;
                _lastPartnership = partners;
                if (_modeText != null) _modeText.text = partners ? string.Format(_partnerModeFormat, score) : _individualModeLabel;
            }
        }
        [SerializeField] private DrawSourceView _drawSource;
        [SerializeField] private DiscardAreaView[] _discardAreas;
        [SerializeField] private TMP_Text[] _opponentTexts;
        [SerializeField] private string[] _seatNames;
        [SerializeField] private SpriteRenderer[] _opponentHighlights;
        [SerializeField] private Color _activeSeatColor;
        [SerializeField] private Color _idleSeatColor;
        [SerializeField] private string _activeSeatText;
        [SerializeField] private GameObject _emptyMeldHint;
        [SerializeField] private TMP_Text _feedbackText;
        [SerializeField] private string _openingSuccessText;
        [SerializeField] private string _openingFailureFormat;
        [SerializeField] private Color _successColor;
        [SerializeField] private Color _failureColor;
        [SerializeField, Min(0)] private float _feedbackHold;
        [SerializeField, Min(0)] private float _feedbackFade;
        private Tween _feedbackTween;
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
            _discardAreas[0].SetAvailable(canOpen);
            _statusText.text = phase == TurnPhase.Finished ? _finishedStatus : seat != 0 ? string.Format(_waitingFormat, _seatNames[seat - 1]) :
                phase == TurnPhase.AwaitingDraw ? _drawStatus : _discardStatus;
            _handText.text = string.Format(_handFormat, handCount);
            _pileText.text = string.Format(_pileFormat, pileCount);
            _pointsText.text = string.Format(_pointsFormat, points, threshold, pairs, pairThreshold);
            _penaltyText.text = string.Format(_penaltyFormat, penalty);
        }

        public void RenderOpponent(int seat, int handCount, int penalty, int score, bool active)
        {
            _opponentTexts[seat - 1].text = string.Format(_opponentFormat, _seatNames[seat - 1], handCount, penalty, score, active ? _activeSeatText : "");
            _opponentHighlights[seat - 1].color = active ? _activeSeatColor : _idleSeatColor;
        }

        public void SetMeldCount(int count) => _emptyMeldHint.SetActive(count == 0);

        public void ShowOpeningResult(bool success, int penalty)
        {
            _feedbackTween?.Kill();
            _feedbackText.text = success ? _openingSuccessText : string.Format(_openingFailureFormat, penalty);
            _feedbackText.color = success ? _successColor : _failureColor;
            _feedbackText.alpha = 1;
            _feedbackTween = DOTween.To(() => _feedbackText.alpha, value => _feedbackText.alpha = value, 0, _feedbackFade)
                .SetDelay(_feedbackHold).SetLink(gameObject, LinkBehaviour.KillOnDisable);
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
            if (_botModeButton != null) _botModeButton.onClick.AddListener(RequestBotMode);
            if (_partnershipButton != null) _partnershipButton.onClick.AddListener(RequestPartnership);
            if (_newHandButton != null) _newHandButton.onClick.AddListener(RequestNewHand);
            _seriesButton.onClick.AddListener(RequestSeries);
            _pairsButton.onClick.AddListener(RequestPairs);
        }

        private void OnDisable()
        {
            _feedbackTween?.Kill();
            _drawSource.DrawRequested -= RequestDraw;
            if (_botModeButton != null) _botModeButton.onClick.RemoveListener(RequestBotMode);
            if (_partnershipButton != null) _partnershipButton.onClick.RemoveListener(RequestPartnership);
            if (_newHandButton != null) _newHandButton.onClick.RemoveListener(RequestNewHand);
            _seriesButton.onClick.RemoveListener(RequestSeries);
            _pairsButton.onClick.RemoveListener(RequestPairs);
        }

        private void RequestDraw() => DrawRequested?.Invoke();
        private void RequestSeries() => SeriesRequested?.Invoke();
        private void RequestPairs() => PairsRequested?.Invoke();
    }
}
