using UnityEngine;
using Okey.Presentation.Text;

namespace Okey.Presentation.Tables
{
    public sealed class TableView : MonoBehaviour
    {
        [SerializeField] private TableTextCatalog _texts;
        [SerializeField] private TableSurfaceView _surface;
        [SerializeField] private TableHudView _hud;
        [SerializeField] private MeldActionView _seriesAction;
        [SerializeField] private MeldActionView _pairAction;
        [SerializeField] private TurnClockView _clock;
        [SerializeField] private TablePlayersView _players;
        [SerializeField] private OverlayPanelsView _panels;
        [SerializeField] private TableOptionsView _options;
        [SerializeField] private TableFeedbackView _feedback;
        public TableTextCatalog Texts => _texts;
        public TableSurfaceView Surface => _surface;
        public TableHudView Hud => _hud;
        public MeldActionView SeriesAction => _seriesAction;
        public MeldActionView PairAction => _pairAction;
        public TurnClockView Clock => _clock;
        public TablePlayersView Players => _players;
        public OverlayPanelsView Panels => _panels;
        public TableOptionsView Options => _options;
        public TableFeedbackView Feedback => _feedback;
    }
}
