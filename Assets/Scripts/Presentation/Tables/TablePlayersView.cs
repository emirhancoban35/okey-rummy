using UnityEngine;

namespace Okey.Presentation.Tables
{
    public sealed class TablePlayersView : MonoBehaviour
    {
        [SerializeField] private OpponentSeatView[] _opponents;
        public void SetPlayerCount(int count)
        {
            for (var index = 0; index < _opponents.Length; index++) _opponents[index].SetVisible(index < count - 1);
        }
        public void SetIdentity(int seat, Color color) => _opponents[seat - 1].SetIdentity(color);
        public void RenderOpponent(int seat, string text, bool active) => _opponents[seat - 1].Render(text, active);
    }
}
