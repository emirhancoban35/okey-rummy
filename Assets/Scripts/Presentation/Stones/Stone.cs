using System;
using Okey.Core.Stones;
using UnityEngine;

namespace Okey.Presentation.Stones
{
    [DisallowMultipleComponent]
    public sealed class Stone : MonoBehaviour
    {
        [SerializeField] private StoneVisual _visual;
        [SerializeField] private StoneMotion _motion;
        [SerializeField] private StoneDragHandler _dragHandler;
        [SerializeField] private StoneSelectionView _selection;
        [SerializeField] private bool _isFaceUp = true;

        public StoneInstance Instance { get; private set; }
        public StoneData Data => Instance?.Data;
        public bool IsFaceUp => _isFaceUp;
        public StoneMotion Motion => _motion;
        public StoneDragHandler DragHandler => _dragHandler;
        public StoneSelectionView Selection => _selection;

        public void Initialize(StoneInstance stone)
        {
            if (stone == null)
                throw new ArgumentNullException(nameof(stone));

            _visual.ConfigureInspection(false);
            _visual.SetVisual(stone.Data);
            _visual.SetFace(_isFaceUp);
            Instance = stone;
        }

        public void ConfigureInspection(bool isWild) => _visual.ConfigureInspection(isWild);

        public void SetFace(bool isFaceUp)
        {
            _visual.SetFace(isFaceUp);
            _isFaceUp = isFaceUp;
        }
    }
}
