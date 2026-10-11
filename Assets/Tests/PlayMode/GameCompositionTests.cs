using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using Okey.Composition;
using Okey.Core.Decks;
using Okey.Core.Dealing;
using Okey.Presentation.Stones;
using Okey.Presentation.Racks;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            var presenter = new OpeningHandPresenter(deal, _context.Container.Resolve<StoneViewFactory>(), rack);
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
