using System;

namespace Okey.Core.Players
{
    public sealed class PlayerIdentity
    {
        public string Id { get; }
        public string DisplayName { get; }

        public PlayerIdentity(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A player needs a stable identity and display name.");
            Id = id;
            DisplayName = displayName;
        }
    }
}
