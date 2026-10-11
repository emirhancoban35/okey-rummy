using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using Okey.Composition;
using Okey.Core.Decks;
using Okey.Core.Dealing;
using Okey.Core.Stones;
using Okey.Presentation.Stones;
using Okey.Presentation.Racks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.TestTools;
using Zenject;

namespace Okey.Tests.PlayMode
{
    public sealed class GameCompositionTests
    {
        private Scene _scene;
        private SceneContext _context;
        private Stone _stone;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
            yield return null;
            _scene = SceneManager.GetSceneByName("SampleScene");
            _context = _scene.GetRootGameObjects()
                .Select(root => root.GetComponent<SceneContext>()).Single(context => context != null);
            var definition = _context.Container.Resolve<DeckDefinition>();
            var deck = _context.Container.Resolve<DeckFactory>().Create(definition);
            _stone = _context.Container.Resolve<StoneViewFactory>().Create(deck[0], _context.transform);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(_scene);
        }

        private Stone[] HandViews => _context.Container.Resolve<OpeningHandPresenter>().Views.ToArray();
        private RackView Rack => _context.Container.Resolve<RackView>();
        private RackArrangement Arrangement => _context.Container.Resolve<RackArrangement>();
        private RackInteractionController Interaction => _context.Container.Resolve<RackInteractionController>();

        private Vector3 SlotWorldPosition(int index)
        {
            return Rack.StoneRoot.TransformPoint(Rack.GetSlotLocalPosition(index));
        }

        private void CompleteHandMovement()
        {
            foreach (var view in HandViews)
                DOTween.Complete(view.transform);
        }

        [Test]
        public void InsertingIntoOccupiedSlotShiftsAllAffectedViewsAndPreservesOrder()
        {
            CompleteHandMovement();
            var views = HandViews;
            Assert.That(Interaction.TryBeginDrag(views[0]), Is.True);
            Assert.That(Interaction.TryBeginDrag(views[1]), Is.False);
            Interaction.DragTo(views[0], SlotWorldPosition(4));
            Interaction.EndDrag(views[0], SlotWorldPosition(4));
            var tween = DOTween.TweensByTarget(views[1].transform).Single();
            tween.Goto(tween.Duration() / 2);
            Assert.That(views[1].transform.position.x,
                Is.GreaterThan(SlotWorldPosition(0).x).And.LessThan(SlotWorldPosition(1).x));
            CompleteHandMovement();

            Assert.That(Arrangement.Slots.Take(5).Select(stone => stone.Id),
                Is.EqualTo(new[] { views[1], views[2], views[3], views[4], views[0] }.Select(view => view.Instance.Id)));
            foreach (var view in views)
                Assert.That(Vector3.Distance(view.transform.position,
                    SlotWorldPosition(Arrangement.FindSlot(view.Instance.Id))), Is.LessThan(0.001f));
        }

        [Test]
        public void InsertionFromOtherRowUsesNearbyGapWithoutCollapsingExistingGroups()
        {
            var views = HandViews;
            Interaction.TryBeginDrag(views[2]);
            Interaction.EndDrag(views[2], SlotWorldPosition(27));
            Interaction.TryBeginDrag(views[20]);
            Interaction.EndDrag(views[20], SlotWorldPosition(1));
            CompleteHandMovement();

            Assert.That(Arrangement.FindSlot(views[20].Instance.Id), Is.EqualTo(1));
            Assert.That(Arrangement.FindSlot(views[1].Instance.Id), Is.EqualTo(2));
            Assert.That(Arrangement.FindSlot(views[2].Instance.Id), Is.EqualTo(27));
            Assert.That(Arrangement.Slots[20], Is.Null);
            foreach (var view in views)
                Assert.That(Vector3.Distance(view.transform.position,
                    SlotWorldPosition(Arrangement.FindSlot(view.Instance.Id))), Is.LessThan(0.001f));
        }

        [Test]
        public void EmptySlotAcceptsStoneAndOutsideDropReturnsToCurrentSlot()
        {
            var stone = HandViews[0];
            var lastSlot = Arrangement.Capacity - 1;
            Interaction.TryBeginDrag(stone);
            Interaction.EndDrag(stone, SlotWorldPosition(lastSlot));
            CompleteHandMovement();
            Assert.That(Arrangement.Slots[0], Is.Null);
            Assert.That(Arrangement.FindSlot(stone.Instance.Id), Is.EqualTo(lastSlot));

            var outside = SlotWorldPosition(lastSlot) + Vector3.up * 5;
            Interaction.TryBeginDrag(stone);
            Interaction.DragTo(stone, outside);
            Interaction.EndDrag(stone, outside);
            CompleteHandMovement();
            Assert.That(Arrangement.FindSlot(stone.Instance.Id), Is.EqualTo(lastSlot));
            Assert.That(Vector3.Distance(stone.transform.position, SlotWorldPosition(lastSlot)), Is.LessThan(0.001f));
        }

        [Test]
        public void CancellationRestoresPositionAndUnregisteredViewsCannotMoveHand()
        {
            var stone = HandViews[0];
            Assert.That(Interaction.TryBeginDrag(_stone), Is.False);
            Interaction.TryBeginDrag(stone);
            Interaction.DragTo(stone, SlotWorldPosition(1));
            Interaction.CancelDrag(stone);
            Interaction.EndDrag(stone, SlotWorldPosition(1));
            Assert.That(Arrangement.FindSlot(stone.Instance.Id), Is.Zero);
            Assert.That(Vector3.Distance(stone.transform.position, SlotWorldPosition(0)), Is.LessThan(0.001f));
            Assert.That(DOTween.IsTweening(stone.transform), Is.False);
        }

        [Test]
        public void SceneRaycastAndPointerEventsMoveStoneWithoutLosingClickOffset()
        {
            CompleteHandMovement();
            Physics2D.SyncTransforms();
            var stone = HandViews[0];
            var camera = _scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).Single(value => value != null);
            var raycaster = camera.GetComponent<Physics2DRaycaster>();
            Assert.That(raycaster, Is.Not.Null);
            var offset = new Vector3(0.05f, 0.05f, 0);
            var data = new PointerEventData(EventSystem.current)
            {
                pointerId = 7,
                button = PointerEventData.InputButton.Left,
                position = camera.WorldToScreenPoint(stone.transform.position + offset)
            };
            var results = new List<RaycastResult>();
            raycaster.Raycast(data, results);
            var hit = results.Single(result => result.gameObject == stone.gameObject);
            data.pointerPressRaycast = hit;
            var group = stone.GetComponent<SortingGroup>();
            var originalOrder = group.sortingOrder;
            ExecuteEvents.Execute(stone.gameObject, data, ExecuteEvents.beginDragHandler);
            Assert.That(group.sortingOrder, Is.GreaterThan(originalOrder));
            data.position = camera.WorldToScreenPoint(SlotWorldPosition(23) + offset);
            ExecuteEvents.Execute(stone.gameObject, data, ExecuteEvents.dragHandler);
            Assert.That(Vector3.Distance(stone.transform.position, SlotWorldPosition(23)), Is.LessThan(0.001f));
            data.pointerId = 8;
            ExecuteEvents.Execute(stone.gameObject, data, ExecuteEvents.endDragHandler);
            Assert.That(Arrangement.FindSlot(stone.Instance.Id), Is.Zero);
            data.pointerId = 7;
            ExecuteEvents.Execute(stone.gameObject, data, ExecuteEvents.endDragHandler);
            CompleteHandMovement();
            Assert.That(Arrangement.FindSlot(stone.Instance.Id), Is.EqualTo(23));
            Assert.That(group.sortingOrder, Is.EqualTo(originalOrder));
        }

        [Test]
        public void DisabledDragHandlerCancelsDragAndRestoresSorting()
        {
            CompleteHandMovement();
            var stone = HandViews[0];
            var camera = _scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).Single(value => value != null);
            var data = new PointerEventData(EventSystem.current)
            {
                pointerId = 1,
                position = camera.WorldToScreenPoint(stone.transform.position),
                pointerPressRaycast = new RaycastResult { module = camera.GetComponent<Physics2DRaycaster>() }
            };
            var group = stone.GetComponent<SortingGroup>();
            var originalOrder = group.sortingOrder;
            stone.DragHandler.OnBeginDrag(data);
            Interaction.DragTo(stone, SlotWorldPosition(1));
            stone.DragHandler.enabled = false;
            Assert.That(Arrangement.FindSlot(stone.Instance.Id), Is.Zero);
            Assert.That(Vector3.Distance(stone.transform.position, SlotWorldPosition(0)), Is.LessThan(0.001f));
            Assert.That(group.sortingOrder, Is.EqualTo(originalOrder));
            Assert.That(Interaction.TryBeginDrag(HandViews[1]), Is.True);
            Interaction.CancelDrag(HandViews[1]);
        }

        [Test]
        public void SceneInstallerResolvesConfiguredDeckAndPrefabFactory()
        {
            Assert.That(_context.Initialized, Is.True);
            Assert.That(_context.Container.Resolve<DeckDefinition>().Count, Is.EqualTo(106));
            Assert.That(_context.Container.Resolve<DeckFactory>(),
                Is.SameAs(_context.Container.Resolve<DeckFactory>()));
            Assert.That(_stone.Instance.Id, Is.Zero);
            Assert.That(_stone.Data.Value, Is.EqualTo(1));
            Assert.That(_stone.transform.parent, Is.EqualTo(_context.transform));
            Assert.That(_stone.GetComponent<StoneMotion>(), Is.Not.Null);
        }

        [Test]
        public void SceneProvidesOneOpeningDealWithConfiguredHandAndRemainingPile()
        {
            var deal = _context.Container.Resolve<OpeningDeal>();

            Assert.That(_context.Container.Resolve<OpeningDeal>(), Is.SameAs(deal));
            Assert.That(deal.InitialHand.Count, Is.EqualTo(21));
            Assert.That(deal.DrawPile.RemainingCount, Is.EqualTo(85));
            var allIds = deal.InitialHand.Concat(deal.DrawPile.Draw(85)).Select(stone => stone.Id);
            Assert.That(allIds, Is.EquivalentTo(Enumerable.Range(0, 106)));
        }

        [Test]
        public void OpeningHandDisplaysEachPhysicalStoneOnceAndInitializationIsIdempotent()
        {
            var presenter = _context.Container.Resolve<OpeningHandPresenter>();
            var deal = _context.Container.Resolve<OpeningDeal>();
            var rack = _context.Container.Resolve<RackView>();
            var views = presenter.Views.ToArray();

            presenter.Initialize();

            Assert.That(presenter.Views, Is.EqualTo(views));
            Assert.That(views.Select(view => view.Instance.Id),
                Is.EqualTo(deal.InitialHand.Select(stone => stone.Id)));
            Assert.That(views.Length, Is.EqualTo(21));
            Assert.That(views.Select(view => view.Instance.Id).Distinct().Count(), Is.EqualTo(21));
            Assert.That(rack.StoneRoot.childCount, Is.EqualTo(21));
            for (var index = 0; index < views.Length; index++)
            {
                DOTween.Complete(views[index].transform);
                Assert.That(Vector3.Distance(views[index].transform.localPosition,
                    rack.GetSlotLocalPosition(index)), Is.LessThan(0.001f));
            }
        }

        [Test]
        public void TooLargeHandFailsBeforeCreatingAnyViews()
        {
            var rack = _context.Container.Resolve<RackView>();
            var definition = _context.Container.Resolve<DeckDefinition>();
            var deal = _context.Container.Resolve<OpeningDealFactory>().Create(definition, rack.Capacity + 1, 42);
            var interaction = new RackInteractionController(new RackArrangement(deal.InitialHand, rack.Capacity + 1), rack);
            var presenter = new OpeningHandPresenter(deal, _context.Container.Resolve<StoneViewFactory>(), rack, interaction);
            var childCount = rack.StoneRoot.childCount;

            Assert.Throws<InvalidOperationException>(() => presenter.Initialize());
            Assert.That(presenter.Views, Is.Empty);
            Assert.That(rack.StoneRoot.childCount, Is.EqualTo(childCount));
            presenter.Dispose();
        }

        [UnityTest]
        public IEnumerator PresenterDisposalStopsMovementAndDestroysOwnedViews()
        {
            var presenter = _context.Container.Resolve<OpeningHandPresenter>();
            var views = presenter.Views.ToArray();

            presenter.Dispose();
            presenter.Dispose();

            Assert.That(presenter.Views, Is.Empty);
            foreach (var view in views)
            {
                Assert.That(view.gameObject.activeSelf, Is.False);
                Assert.That(DOTween.IsTweening(view.transform), Is.False);
            }
            Assert.Throws<ObjectDisposedException>(() => presenter.Initialize());
            yield return null;
            Assert.That(views.All(view => view == null), Is.True);
        }

        [Test]
        public void NegativeDelayDoesNotCancelExistingMovement()
        {
            _stone.Motion.MoveTo(Vector3.right);
            var tween = DOTween.TweensByTarget(_stone.transform).Single();

            Assert.Throws<ArgumentOutOfRangeException>(() => _stone.Motion.MoveTo(Vector3.left, -1));

            Assert.That(tween.IsActive(), Is.True);
        }

        [Test]
        public void MotionInterpolatesAndReachesLocalDestination()
        {
            var motion = _stone.GetComponent<StoneMotion>();
            motion.SnapTo(Vector3.zero);
            motion.MoveTo(Vector3.right);
            var tween = DOTween.TweensByTarget(_stone.transform).Single();

            tween.Goto(tween.Duration() / 2);

            Assert.That(_stone.transform.localPosition.x, Is.GreaterThan(0).And.LessThan(1));
            tween.Complete();
            Assert.That(Vector3.Distance(_stone.transform.localPosition, Vector3.right), Is.LessThan(0.001f));
        }

        [Test]
        public void NewMovementReplacesPreviousTween()
        {
            var motion = _stone.GetComponent<StoneMotion>();
            motion.MoveTo(Vector3.right);
            var previous = DOTween.TweensByTarget(_stone.transform).Single();

            motion.MoveTo(Vector3.left);

            Assert.That(previous.IsActive(), Is.False);
            Assert.That(DOTween.TweensByTarget(_stone.transform).Count, Is.EqualTo(1));
            DOTween.Complete(_stone.transform);
            Assert.That(Vector3.Distance(_stone.transform.localPosition, Vector3.left), Is.LessThan(0.001f));
        }

        [Test]
        public void DisabledStoneCancelsMovement()
        {
            var motion = _stone.GetComponent<StoneMotion>();
            motion.MoveTo(Vector3.right);

            _stone.gameObject.SetActive(false);

            Assert.That(DOTween.IsTweening(_stone.transform), Is.False);
            _stone.gameObject.SetActive(true);
            Assert.That(DOTween.IsTweening(_stone.transform), Is.False);
        }

        [Test]
        public void SnapCancelsTweenAndSetsExactPosition()
        {
            var motion = _stone.GetComponent<StoneMotion>();
            motion.MoveTo(Vector3.right);

            motion.SnapTo(Vector3.up);

            Assert.That(_stone.transform.localPosition, Is.EqualTo(Vector3.up));
            Assert.That(DOTween.IsTweening(_stone.transform), Is.False);
        }

        [Test]
        public void MissingStoneDataDoesNotInstantiatePrefab()
        {
            var factory = _context.Container.Resolve<StoneViewFactory>();
            var childCount = _context.transform.childCount;

            Assert.Throws<ArgumentNullException>(() => factory.Create(null, _context.transform));
            Assert.That(_context.transform.childCount, Is.EqualTo(childCount));
        }
    }
}
