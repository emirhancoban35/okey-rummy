namespace Okey.Core.Tables
{
    public interface IBotDecisionPolicy
    {
        bool ShouldHoldOpening(BotPreferences preferences);
        PlayerAction Decide(PlayerTurnObservation turn, BotPreferences preferences, bool holdOpening);
    }
}
