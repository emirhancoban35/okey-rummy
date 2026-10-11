using DG.Tweening;
using UnityEngine;

namespace Okey.Presentation.Stones
{
    [CreateAssetMenu(menuName = "Okey/Stone Motion Settings")]
    public sealed class StoneMotionSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private float _moveDuration;
        [SerializeField] private Ease _moveEase;
        [SerializeField] private bool _useUnscaledTime;

        public float MoveDuration => _moveDuration;
        public Ease MoveEase => _moveEase;
        public bool UseUnscaledTime => _useUnscaledTime;
    }
}
