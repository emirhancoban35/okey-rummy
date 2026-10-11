using System;
using DG.Tweening;
using Okey.Core.Dealing;
using Okey.Core.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Okey.Presentation.Racks
{
    public sealed class TurnControlsView : MonoBehaviour
    {
        [SerializeField] private GameObject[] _publicMeldVisuals;
        [SerializeField] private GameObject _classicPanel;
        [SerializeField] private Transform _winningHandRoot;
        [SerializeField] private Vector3 _winningTileScale = new Vector3(.5f, .6f, 1);
        [SerializeField] private Vector2 _winningTileSpacing = new Vector2(.56f, .72f);
        public Transform WinningHandRoot => _winningHandRoot;
        public Vector3 WinningTileScale => _winningTileScale;
        public Vector3 WinningTilePosition(int group, int tile, bool pair) => pair
            ? new Vector3(group % 4 * _winningTileSpacing.x * 3 + tile * _winningTileSpacing.x, -group / 4 * _winningTileSpacing.y, 0)
            : new Vector3(tile * _winningTileSpacing.x, -group * _winningTileSpacing.y, 0);
        [SerializeField] private TMP_Text _seriesActionLabel;
        [SerializeField] private TMP_Text _pairActionLabel;
        [SerializeField] private TMP_Text _helpText;
        [SerializeField] private string _openLabel = "OPEN";
        [SerializeField] private string _finishLabel = "FINISH";
        [SerializeField] private string _pairsLabel = "PAIRS";
        [SerializeField] private string _openHelp = "Drag to a matching meld. Double tap a tile to discard.";
        [SerializeField] private string _classicHelp = "Draw from the pile or the previous discard. Keep your hand closed. Complete all tiles as runs and sets, or seven pairs. Discard the spare tile to win.";
        [SerializeField] private string _emptyStockStatus = "Take discard or end.";
        [SerializeField] private string _classicDiscardStatus = "Arrange, then discard.";
        [SerializeField] private string _winnerFormat = "{0} wins";
        [SerializeField] private string _drawnRoundLabel = "Stock empty. Round drawn.";
        private bool _publicMelds = true;
        public void ConfigureGame(bool publicMelds)
        {
            _publicMelds = publicMelds;
            foreach (var visual in _publicMeldVisuals) visual.SetActive(publicMelds);
            _classicPanel.SetActive(!publicMelds);
            if (!publicMelds) _emptyMeldHint.SetActive(false);
            _seriesActionLabel.text = publicMelds ? _openLabel : _finishLabel;
            _pairActionLabel.text = publicMelds ? _openLabel : _pairsLabel;
            _helpText.text = publicMelds ? _openHelp : _classicHelp;
            _escalatingButton.gameObject.SetActive(publicMelds);
        }
        public void RenderClosedHand(MeldPlan series, MeldPlan pairs, int handSize, bool canFinish)
        {
            _seriesPointsText.text = string.Format(_progressFormat, series.TileCount, handSize);
            _pairCountText.text = string.Format(_progressFormat, pairs.Melds.Count, _pairThreshold);
            _seriesProgress.fillAmount = Mathf.Clamp01((float)series.TileCount / handSize);
            _pairProgress.fillAmount = Mathf.Clamp01((float)pairs.Melds.Count / _pairThreshold);
            _seriesButton.interactable = canFinish;
            _pairsButton.interactable = false;
        }
        public void RenderRoundResult(int winner)
        {
            if (!_publicMelds && winner >= 0) _classicPanel.SetActive(false);
            _statusText.text = winner < 0 ? _drawnRoundLabel :
                string.Format(_winnerFormat, winner == 0 ? _localSeatName : _seatNames[winner - 1]);
        }
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _helpButton;
        [SerializeField] private Button _closeSettingsButton;
        [SerializeField] private Button _closeHelpButton;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _helpPanel;
        [SerializeField] private TMP_Text _seriesPointsText;
        [SerializeField] private TMP_Text _pairCountText;
        [SerializeField] private string _progressFormat = "{0} / {1}";
        [SerializeField] private string _additionalGroupFormat = "+ {0}";
        [SerializeField] private Image _seriesProgress;
        [SerializeField] private Image _pairProgress;
        [SerializeField] private GameObject _drawCue;
        [SerializeField] private GameObject _discardCue;
        private int _seriesThreshold;
        private int _pairThreshold;
        public void RenderGroups(MeldPlan series, MeldPlan pairs, OpeningMode opening)
        {
            if (_seriesPointsText != null) _seriesPointsText.text = opening == OpeningMode.Closed
                ? string.Format(_progressFormat, series.Points, _seriesThreshold) : string.Format(_additionalGroupFormat, series.TileCount);
            if (_pairCountText != null) _pairCountText.text = opening == OpeningMode.Closed
                ? string.Format(_progressFormat, pairs.Melds.Count, _pairThreshold) : string.Format(_additionalGroupFormat, pairs.Melds.Count);
            if (_seriesProgress != null) _seriesProgress.fillAmount = opening == OpeningMode.Closed
                ? Mathf.Clamp01((float)series.Points / _seriesThreshold) : series.TileCount > 0 ? 1 : 0;
            if (_pairProgress != null) _pairProgress.fillAmount = opening == OpeningMode.Closed
                ? Mathf.Clamp01((float)pairs.Melds.Count / _pairThreshold) : pairs.Melds.Count > 0 ? 1 : 0;
            _seriesButton.interactable &= opening != OpeningMode.Pairs;
            _pairsButton.interactable &= opening != OpeningMode.Series;
        }
        private void ToggleSettings()
        {
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
            _helpPanel.SetActive(false);
        }
        private void ToggleHelp()
        {
            _helpPanel.SetActive(!_helpPanel.activeSelf);
            _settingsPanel.SetActive(false);
        }
        private void ClosePanels()
        {
            _settingsPanel.SetActive(false);
            _helpPanel.SetActive(false);
        }
        [SerializeField] private Button _botModeButton;
        [SerializeField] private Button _partnershipButton;
        [SerializeField] private Button _newHandButton;
        [SerializeField] private Button _escalatingButton;
        [SerializeField] private TMP_Text _escalatingLabel;
        [SerializeField] private string _escalatingOnLabel = "NEXT: RISING OPENINGS";
        [SerializeField] private string _escalatingOffLabel = "NEXT: FIXED OPENINGS";
        public event Action EscalatingRequested;
        private void RequestEscalating() => EscalatingRequested?.Invoke();
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
        public void RenderOptions(bool humanlike, bool partners, bool finished, bool escalating = false)
        {
            if (_escalatingLabel != null) _escalatingLabel.text = escalating ? _escalatingOnLabel : _escalatingOffLabel;
            if (_botModeLabel != null) _botModeLabel.text = humanlike ? _humanlikeBotLabel : _fastBotLabel;
            if (_partnershipLabel != null) _partnershipLabel.text = partners ? _nextPartnerLabel : _nextIndividualLabel;
            if (_newHandButton != null) _newHandButton.interactable = true;
        }
        private void RequestBotMode() => BotModeRequested?.Invoke();
        private void RequestPartnership() => PartnershipRequested?.Invoke();
        private void RequestNewHand() => NewHandRequested?.Invoke();
        [SerializeField] private TMP_Text _clockText;
        [SerializeField] private Slider[] _turnSliders = Array.Empty<Slider>();
        [SerializeField] private TMP_Text[] _opponentClockTexts = Array.Empty<TMP_Text>();
        [SerializeField] private string _secondsFormat = "{0:0}s";
        [SerializeField] private string _localSeatName = "You";
        [SerializeField] private string _clockFormat = "{0}  ·  {1:0}s";
        [SerializeField] private TMP_Text _modeText;
        [SerializeField] private MeldBoardView _seriesBoard;
        [SerializeField] private MeldBoardView _pairsBoard;
        public MeldBoardView SeriesBoard => _seriesBoard;
        public MeldBoardView PairsBoard => _pairsBoard;
        public Transform PairsRoot => _pairsBoard.transform;
        private int _lastClockSeat = -1;
        private int _lastClockSecond = -1;
        private int _lastTeamScore = int.MinValue;
        private bool _lastPartnership;
        private bool _lastClockFinished;
        public void RenderClock(int seat, double remaining, bool partners, int score, double duration = 45, bool finished = false)
        {
            for (var index = 0; index < _turnSliders.Length; index++)
            {
                if (_turnSliders[index] == null) continue;
                _turnSliders[index].value = !finished && seat == index ? Mathf.Clamp01((float)(remaining / duration)) : 0;
            }
            var seconds = Mathf.CeilToInt((float)remaining);
            if (seat != _lastClockSeat || seconds != _lastClockSecond || finished != _lastClockFinished)
            {
                _lastClockSeat = seat;
                _lastClockFinished = finished;
                _lastClockSecond = seconds;
                if (_clockText != null) _clockText.text = seat == 0 && !finished ? string.Format(_clockFormat, _localSeatName, seconds) : "";
                for (var index = 0; index < _opponentClockTexts.Length; index++)
                    if (_opponentClockTexts[index] != null) _opponentClockTexts[index].text = !finished && seat == index + 1 ? string.Format(_secondsFormat, seconds) : "";
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
        [SerializeField] private Vector3 _indicatorScale = new Vector3(.46f,.54f,1);
        public Vector3 IndicatorScale => _indicatorScale;
        [SerializeField] private Transform _indicatorRoot;
        [SerializeField, Min(0)] private float _meldDropRadius;

        public DrawSourceView DrawSource => _drawSource;
        public DiscardAreaView[] DiscardAreas => _discardAreas;
        public Transform MeldRoot => _seriesBoard.transform;
        public Transform IndicatorRoot => _indicatorRoot;
        public Vector3 DiscardScale => _discardScale;
        public Vector3 MeldScale => _seriesBoard.TileScale;
        public float MeldDropRadius => _meldDropRadius;
        public event Action DrawRequested;
        public event Action SeriesRequested;
        public event Action PairsRequested;

        public void Render(TurnPhase phase, int seat, int handCount, int pileCount, int points, int pairs,
            int threshold, int pairThreshold, int penalty, bool canDraw, bool canOpen)
        {
            _seriesThreshold = threshold;
            _pairThreshold = pairThreshold;
            if (_drawCue != null) _drawCue.SetActive(canDraw);
            var canDiscard = seat == 0 && phase == TurnPhase.AwaitingDiscard;
            if (_discardCue != null) _discardCue.SetActive(canDiscard);
            _drawSource.CanDraw = canDraw;
            _seriesButton.interactable = canOpen;
            _pairsButton.interactable = canOpen;
            _discardAreas[0].SetAvailable(canDiscard);
            _statusText.text = phase == TurnPhase.Finished ? _finishedStatus : seat != 0 ? string.Format(_waitingFormat, _seatNames[seat - 1]) :
                phase == TurnPhase.AwaitingDraw ? (!_publicMelds && pileCount == 0 ? _emptyStockStatus : _drawStatus) : _publicMelds ? _discardStatus : _classicDiscardStatus;
            _handText.text = string.Format(_handFormat, handCount);
            _pileText.text = string.Format(_pileFormat, pileCount);
            _pointsText.text = string.Format(_pointsFormat, points, threshold, pairs, pairThreshold);
            _penaltyText.text = string.Format(_penaltyFormat, penalty);
            _penaltyText.gameObject.SetActive(penalty != 0);
        }

        public void RenderOpponent(int seat, int handCount, int penalty, int score, bool active)
        {
            _opponentTexts[seat - 1].text = string.Format(_opponentFormat, _seatNames[seat - 1], handCount, penalty, score, active ? _activeSeatText : "");
            _opponentHighlights[seat - 1].color = active ? _activeSeatColor : _idleSeatColor;
        }

        public void SetMeldCount(int count) => _emptyMeldHint.SetActive(_publicMelds && count == 0);

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
            if (_settingsButton != null) _settingsButton.onClick.AddListener(ToggleSettings);
            if (_helpButton != null) _helpButton.onClick.AddListener(ToggleHelp);
            if (_closeSettingsButton != null) _closeSettingsButton.onClick.AddListener(ClosePanels);
            if (_closeHelpButton != null) _closeHelpButton.onClick.AddListener(ClosePanels);
            if (_botModeButton != null) _botModeButton.onClick.AddListener(RequestBotMode);
            if (_partnershipButton != null) _partnershipButton.onClick.AddListener(RequestPartnership);
            if (_escalatingButton != null) _escalatingButton.onClick.AddListener(RequestEscalating);
            if (_newHandButton != null) _newHandButton.onClick.AddListener(RequestNewHand);
            _seriesButton.onClick.AddListener(RequestSeries);
            _pairsButton.onClick.AddListener(RequestPairs);
        }

        private void OnDisable()
        {
            _feedbackTween?.Kill();
            _drawSource.DrawRequested -= RequestDraw;
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(ToggleSettings);
            if (_helpButton != null) _helpButton.onClick.RemoveListener(ToggleHelp);
            if (_closeSettingsButton != null) _closeSettingsButton.onClick.RemoveListener(ClosePanels);
            if (_closeHelpButton != null) _closeHelpButton.onClick.RemoveListener(ClosePanels);
            if (_botModeButton != null) _botModeButton.onClick.RemoveListener(RequestBotMode);
            if (_partnershipButton != null) _partnershipButton.onClick.RemoveListener(RequestPartnership);
            if (_escalatingButton != null) _escalatingButton.onClick.RemoveListener(RequestEscalating);
            if (_newHandButton != null) _newHandButton.onClick.RemoveListener(RequestNewHand);
            _seriesButton.onClick.RemoveListener(RequestSeries);
            _pairsButton.onClick.RemoveListener(RequestPairs);
        }

        private void RequestDraw() => DrawRequested?.Invoke();
        private void RequestSeries() => SeriesRequested?.Invoke();
        private void RequestPairs() => PairsRequested?.Invoke();
    }
}
