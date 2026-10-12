using UnityEngine;
namespace Okey.Presentation.Tables
{
    [CreateAssetMenu(menuName = "Okey/Presentation/Seat Colors")]
    public sealed class SeatColorSettings : ScriptableObject
    {
        [SerializeField] private Color[] _individual;
        [SerializeField] private Color[] _partners;
        public Color For(int identity, bool partnerships) => (partnerships ? _partners : _individual)[identity];
    }
}
