using System;
using DG.Tweening;
using UnityEngine;

namespace Okey.Presentation.Stones
{
    [DisallowMultipleComponent]
    public sealed class StoneMotion : MonoBehaviour
    {
        [SerializeField] private StoneMotionSettings _settings;

        private Tween _moveTween;

        public void MoveTo(Vector3 localPosition, float delay = 0)
        {
            if (delay < 0)
                throw new ArgumentOutOfRangeException(nameof(delay));

            StopMovement();
            var from = transform.localPosition;
            var movement = _settings.ArcHeight > 0 && Vector3.Distance(from, localPosition) >= _settings.ArcMinimumDistance
                ? (Tween)transform.DOLocalPath(new[] { (from + localPosition) * .5f + Vector3.up * _settings.ArcHeight, localPosition },
                    _settings.MoveDuration, PathType.CatmullRom)
                : transform.DOLocalMove(localPosition, _settings.MoveDuration);
            _moveTween = movement
                .SetEase(_settings.MoveEase)
                .SetDelay(delay)
                .SetUpdate(_settings.UseUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void SnapTo(Vector3 localPosition)
        {
            StopMovement();
            transform.localPosition = localPosition;
        }

        private void OnDisable()
        {
            StopMovement();
        }

        private void StopMovement()
        {
            _moveTween?.Kill();
            _moveTween = null;
        }
    }
}
