using UnityEngine;

namespace Okey.Presentation.Racks
{
    public sealed class DiscardAreaView : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D _dropArea;
        [SerializeField] private Transform _stoneRoot;
        public Transform StoneRoot => _stoneRoot;
        public bool Contains(Vector3 worldPosition) => _dropArea.OverlapPoint(worldPosition);
    }
}
