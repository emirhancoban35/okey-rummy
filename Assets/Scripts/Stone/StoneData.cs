using UnityEngine;

public class StoneData
{
    public StoneType Type;
    public StoneColor Color;
    public int Value;
    public bool IsFaceUp = true;

    public StoneData(StoneType type, StoneColor color, int value, bool isFaceUp)
    {
        Type = type;
        Color = color;
        Value = value;
        IsFaceUp = isFaceUp;
    }
}
