using UnityEngine;
using Okey.Presentation.Racks;

namespace Okey.Presentation.Tables
{
    public sealed class TableSurfaceView : MonoBehaviour
    {
        [SerializeField] private GameObject[] _publicMeldVisuals;
        [SerializeField] private GameObject _classicPanel;
        [SerializeField] private GameObject _emptyMeldHint;
        [SerializeField] private DrawSourceView _drawSource;
        [SerializeField] private DiscardAreaView[] _discardAreas;
        [SerializeField] private GameObject _drawCue;
        [SerializeField] private GameObject _discardCue;
        [SerializeField] private MeldBoardView _seriesBoard;
        [SerializeField] private MeldBoardView _pairsBoard;
        [SerializeField] private Transform _indicatorRoot;
        [SerializeField] private Vector3 _indicatorScale;
        [SerializeField] private Vector3 _discardScale;
        [SerializeField, Min(0)] private float _meldDropRadius;
        [SerializeField] private Transform _winningHandRoot;
        [SerializeField] private Vector3 _winningTileScale;
        [SerializeField] private Vector2 _winningTileSpacing;
        [SerializeField, Min(1)] private int _pairGroupsPerRow;
        [SerializeField, Min(0)] private int _pairGroupGapColumns;
        public DrawSourceView DrawSource => _drawSource;
        public DiscardAreaView[] DiscardAreas => _discardAreas;
        public MeldBoardView SeriesBoard => _seriesBoard;
        public MeldBoardView PairsBoard => _pairsBoard;
        public Transform IndicatorRoot => _indicatorRoot;
        public Vector3 IndicatorScale => _indicatorScale;
        public Vector3 DiscardScale => _discardScale;
        public float MeldDropRadius => _meldDropRadius;
        public Transform WinningHandRoot => _winningHandRoot;
        public Vector3 WinningTileScale => _winningTileScale;
        public Vector3 WinningTilePosition(int group, int tile, bool pair) => pair
            ? new Vector3((group % _pairGroupsPerRow * (2 + _pairGroupGapColumns) + tile) * _winningTileSpacing.x,
                -(group / _pairGroupsPerRow) * _winningTileSpacing.y, 0)
            : new Vector3(tile * _winningTileSpacing.x, -group * _winningTileSpacing.y, 0);
        public void Render(bool publicMelds, bool emptyBoard, bool closedHandHint, bool canDraw, bool canDiscard, bool canTakeDiscard, int playerCount)
        {
            foreach (var visual in _publicMeldVisuals) visual.SetActive(publicMelds);
            _classicPanel.SetActive(closedHandHint);
            _emptyMeldHint.SetActive(publicMelds && emptyBoard);
            _drawCue.SetActive(canDraw);
            _discardCue.SetActive(canDiscard);
            _drawSource.CanDraw = canDraw;
            _discardAreas[0].SetAvailable(canDiscard);
            _discardAreas[playerCount - 1].SetAvailable(canTakeDiscard);
        }
        public void SetPlayerCount(int count)
        {
            for (var seat = 0; seat < _discardAreas.Length; seat++) _discardAreas[seat].gameObject.SetActive(seat < count);
        }
    }
}
