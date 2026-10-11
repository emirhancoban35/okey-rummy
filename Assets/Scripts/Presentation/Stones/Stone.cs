using System;
using Okey.Core.Stones;
using UnityEngine;

namespace Okey.Presentation.Stones
{
    [DisallowMultipleComponent]
    public sealed class Stone : MonoBehaviour
    {
        [SerializeField] private StoneVisual _visual;
        [SerializeField] private bool _isFaceUp = true;

        public StoneData Data { get; private set; }
        public bool IsFaceUp => _isFaceUp;

        public void Initialize(StoneData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _visual.SetVisual(data);
            _visual.SetFace(_isFaceUp);
            Data = data;
        }

        public void SetFace(bool isFaceUp)
        {
            _visual.SetFace(isFaceUp);
            _isFaceUp = isFaceUp;
        }
    }
}
