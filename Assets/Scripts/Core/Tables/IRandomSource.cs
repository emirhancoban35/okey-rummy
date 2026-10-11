namespace Okey.Core.Tables
{
    public interface IRandomSource
    {
        double NextDouble();
        int Next(int maximum);
    }
}
