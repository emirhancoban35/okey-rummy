using UnityEngine;

namespace Okey.Presentation.Stones
{
    public interface IStoneDragTarget
    {
        bool TryBeginDrag(Stone stone);
        bool TryBeginDrag(Stone stone, bool group);
        void DragTo(Stone stone, Vector3 worldPosition);
        void EndDrag(Stone stone, Vector3 worldPosition);
        void CancelDrag(Stone stone);
    }
}
