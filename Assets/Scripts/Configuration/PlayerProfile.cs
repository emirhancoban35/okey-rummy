using Okey.Core.Players;
using UnityEngine;

namespace Okey.Configuration
{
    [CreateAssetMenu(menuName = "Okey/Player Profile")]
    public sealed class PlayerProfile : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        public PlayerIdentity CreateIdentity() => new PlayerIdentity(_id, _displayName);
    }
}
