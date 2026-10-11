using System;
using Okey.Core.Rules;
using Okey.Core.Tables;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Okey.Presentation.Tables
{
    public sealed class MeldBoardView : MonoBehaviour, IScrollHandler
    {
        [SerializeField, Min(1)] private int _rows = 8;
        [SerializeField, Min(1)] private int _lanes = 2;
        [SerializeField] private Vector2 _size;
        [SerializeField] private Vector2 _tileFill = new Vector2(.90f, .82f);
        [SerializeField] private GameObject _pageControls;
        [SerializeField] private Button _previousButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _pageText;
        [SerializeField] private string _pageFormat = "{0} / {1}";
        [SerializeField] private Transform _dropTarget;
        private MeldBoardLayout _layout;
        [SerializeField, Min(0)] private int _pairGapColumns = 1;
        private int _columns;
        private int _pages = 1;
        public int Page { get; private set; }
        public int PageCount => _pages;
        public Vector2 CellSize => new Vector2(_size.x / _lanes / _columns, _size.y / _rows);
        public Vector3 TileScale => new Vector3(CellSize.x * _tileFill.x, CellSize.y * _tileFill.y, 1);
        public event Action PageChanged;
        public void Configure(int minimumValue, int maximumValue, bool pairs)
        {
            _layout = new MeldBoardLayout(_rows, _lanes, minimumValue, maximumValue);
            _columns = pairs ? 2 + _pairGapColumns : _layout.SeriesColumns;
        }
        public MeldBoardCell Locate(Meld meld, int groupIndex, int tileIndex) => _layout.Locate(meld, groupIndex, tileIndex);
        public Vector3 GetPosition(MeldBoardCell cell) => new Vector3(
            cell.Lane * _size.x / _lanes + (cell.Column + .5f) * CellSize.x, -(cell.Row + .5f) * CellSize.y, 0);
        public bool ContainsTarget(Vector3 worldPosition, MeldBoardCell cell)
        {
            if (cell.Page != Page) return false;
            var delta = transform.InverseTransformPoint(worldPosition) - GetPosition(cell);
            return Mathf.Abs(delta.x) <= CellSize.x * .5f && Mathf.Abs(delta.y) <= CellSize.y * .5f;
        }
        public void ShowTarget(MeldBoardCell cell)
        {
            _dropTarget.gameObject.SetActive(true);
            _dropTarget.localPosition = GetPosition(cell);
            _dropTarget.localScale = TileScale;
        }
        public void HideTarget() => _dropTarget.gameObject.SetActive(false);
        public void SetGroupCount(int count)
        {
            _pages = _layout.PageCount(count);
            Page = Mathf.Min(Page, _pages - 1);
            _pageControls.SetActive(_pages > 1);
            _previousButton.interactable = Page > 0;
            _nextButton.interactable = Page < _pages - 1;
            _pageText.text = string.Format(_pageFormat, Page + 1, _pages);
        }
        public void ChangePage(int delta)
        {
            var page = Mathf.Clamp(Page + delta, 0, _pages - 1);
            if (page == Page) return;
            Page = page;
            PageChanged?.Invoke();
        }
        public void OnScroll(PointerEventData data)
        {
            if (data.scrollDelta.y != 0) ChangePage(data.scrollDelta.y < 0 ? 1 : -1);
        }
        private void Previous() => ChangePage(-1);
        private void Next() => ChangePage(1);
        private void OnEnable()
        {
            _previousButton.onClick.AddListener(Previous);
            _nextButton.onClick.AddListener(Next);
        }
        private void OnDisable()
        {
            _previousButton.onClick.RemoveListener(Previous);
            _nextButton.onClick.RemoveListener(Next);
        }
    }
}
