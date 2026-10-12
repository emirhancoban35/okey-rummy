using System;
namespace Okey.Core.Rules
{
    public sealed class DiscardPenalties
    {
        public int Joker { get; }
        public int Playable { get; }
        public DiscardPenalties(int joker, int playable)
        {
            if (joker < 0 || playable < 0) throw new ArgumentOutOfRangeException(nameof(joker));
            Joker = joker;
            Playable = playable;
        }
    }
}
