using System;

namespace Okey.Core.Stones
{
    public sealed class StoneInstance
    {
        public int Id { get; }
        public StoneData Data { get; }

        public StoneInstance(int id, StoneData data)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            Id = id;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }
    }
}
