using Okey.Presentation.Tables;
using UnityEngine;
using Zenject;

namespace Okey.Composition
{
    public sealed class TableCanvasPresenter : IInitializable
    {
        private readonly TableView _view;
        private readonly Camera _camera;

        public TableCanvasPresenter(TableView view, Camera camera)
        {
            _view = view;
            _camera = camera;
        }

        public void Initialize() => _view.Canvas.worldCamera = _camera;
    }
}
