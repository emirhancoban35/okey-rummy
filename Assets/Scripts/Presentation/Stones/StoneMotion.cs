using DG.Tweening;
using UnityEngine;

namespace Okey.Presentation.Stones
{
    [DisallowMultipleComponent]
    public sealed class StoneMotion : MonoBehaviour
    {
        [SerializeField] private StoneMotionSettings _settings;

        private Tween _moveTween;

        public void MoveTo(Vector3 localPosition)
        {
            StopMovement();
            _moveTween = transform.DOLocalMove(localPosition, _settings.MoveDuration)
                .SetEase(_settings.MoveEase)
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
