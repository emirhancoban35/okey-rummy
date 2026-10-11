using System;
using TMPro;
using UnityEditor;
using Okey.Configuration;
using System.Collections;
using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using Okey.Composition;
using Okey.Core.Decks;
using Okey.Core.Dealing;
using Okey.Core.Tables;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Presentation.Stones;
using Okey.Presentation.Racks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
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
        private TableSettings _settings;
        private bool _randomStart;
        private bool _humanlike;
        private void SetSetting(string field, bool value) => typeof(TableSettings).GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(_settings, value);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<TableSettings>("Assets/Settings/Dealing/Standard101Table.asset");
            _randomStart = _settings.RandomStartingSeat;
            _humanlike = _settings.HumanlikeBots;
            SetSetting("_randomStartingSeat", false);
            SetSetting("_humanlikeBots", false);
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
            yield return null;
            _scene = SceneManager.GetSceneByName("SampleScene");
            _context = _scene.GetRootGameObjects()
                .Select(root => root.GetComponent<SceneContext>()).Single(context => context != null);
            var table = _context.Container.Resolve<TableSession>();
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var count = 0; table.ActiveSeat != 0 && table.Phase != TurnPhase.Finished && count < 100; count++) bot.Act();
            if (table.Phase == TurnPhase.AwaitingDraw) table.TryDraw(0);
            var definition = _context.Container.Resolve<DeckDefinition>();
            var deck = _context.Container.Resolve<DeckFactory>().Create(definition);
            _stone = _context.Container.Resolve<StoneViewFactory>().Create(deck[0], _context.transform);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SetSetting("_randomStartingSeat", _randomStart);
            SetSetting("_humanlikeBots", _humanlike);
            if (_scene.IsValid() && _scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(_scene);
        }

        private Stone[] HandViews => _context.Container.Resolve<HandPresenter>().Views.ToArray();
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

        private TurnControlsView Controls => _context.Container.Resolve<TurnControlsView>();
        [UnityTest]
        public IEnumerator TableStartsWithDiscardAndDraggingToRightAreaAdvancesTheBots()
        {
            var session = _context.Container.Resolve<TableSession>();
            Assert.That(Controls.DrawSource.CanDraw, Is.False);
            var stone = HandViews[0];
            var id = stone.Instance.Id;
            Interaction.TryBeginDrag(stone);
            Interaction.EndDrag(stone, Controls.DiscardAreas[0].StoneRoot.position);
            Assert.That(session.ActiveSeat, Is.EqualTo(1));
            Assert.That(session.Players[0].Hand.Count, Is.EqualTo(21));
            Assert.That(HandViews.Any(view => view.Instance.Id == id), Is.False);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            Assert.That(session.ActiveSeat, Is.Zero);
            Assert.That(Controls.DrawSource.CanDraw, Is.True);
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            Controls.DrawSource.OnPointerClick(data);
            Controls.DrawSource.OnPointerClick(data);
            Assert.That(session.Players[0].Hand.Count, Is.EqualTo(22));
            Assert.That(session.RemainingCount, Is.EqualTo(16));
            Assert.That(Controls.DiscardAreas.Take(4).All(area => area.StoneRoot.GetComponentInChildren<Stone>() != null), Is.True);
            yield return null;
        }

        [Test]
        public void DoubleTapDiscardsOnlyDuringTheLocalDiscardPhase()
        {
            var table = _context.Container.Resolve<TableSession>();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, clickCount = 2 };
            var view = HandViews[0];
            var id = view.Instance.Id;
            view.Selection.OnPointerClick(pointer);
            Assert.That(table.Players[0].Discards.Last().Id, Is.EqualTo(id));
            Assert.That(table.ActiveSeat, Is.EqualTo(1));
            var count = table.Players[0].Hand.Count;
            HandViews[0].Selection.OnPointerClick(pointer);
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(count));
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && table.ActiveSeat != 0; action++) bot.Act();
            Assert.That(table.Phase, Is.EqualTo(TurnPhase.AwaitingDraw));
            HandViews[0].Selection.OnPointerClick(pointer);
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(count));
        }

        [Test]
        public void TimerBarsAndNumbersFollowTheActiveSeat()
        {
            var serial = new SerializedObject(Controls);
            var sliders = serial.FindProperty("_turnSliders");
            var texts = serial.FindProperty("_opponentClockTexts");
            Assert.That(sliders.arraySize, Is.EqualTo(4));
            Controls.RenderClock(0, 22.5, false, 0, 45);
            Assert.That(((Slider)sliders.GetArrayElementAtIndex(0).objectReferenceValue).value, Is.EqualTo(.5f));
            Controls.RenderClock(2, 10, false, 0, 20);
            for (var seat = 0; seat < 4; seat++)
            {
                var slider = (Slider)sliders.GetArrayElementAtIndex(seat).objectReferenceValue;
                Assert.That(slider.value, Is.EqualTo(seat == 2 ? .5f : 0));
                Assert.That(slider.interactable, Is.False);
                Assert.That(slider.fillRect.sizeDelta.x, Is.Zero);
            }
            Assert.That(((TMP_Text)texts.GetArrayElementAtIndex(1).objectReferenceValue).text, Is.EqualTo("10s"));
            Controls.RenderClock(2, 0, false, 0, 20, true);
            Assert.That(((Slider)sliders.GetArrayElementAtIndex(2).objectReferenceValue).value, Is.Zero);
            Assert.That(((TMP_Text)texts.GetArrayElementAtIndex(1).objectReferenceValue).text, Is.Empty);
        }

        [Test]
        public void EscalatingToggleAppliesToTheNextTable()
        {
            var settings = _context.Container.Resolve<TableSettings>();
            var table = _context.Container.Resolve<TableSession>();
            var original = settings.EscalatingOpenings;
            var button = (Button)new SerializedObject(Controls).FindProperty("_escalatingButton").objectReferenceValue;
            try
            {
                button.onClick.Invoke();
                Assert.That(settings.CreateOptions().EscalatingOpenings, Is.EqualTo(!original));
                Assert.That(table.Options.EscalatingOpenings, Is.EqualTo(original));
            }
            finally { if (settings.EscalatingOpenings != original) settings.ToggleEscalatingOpenings(); }
        }

        [UnityTest]
        public IEnumerator HoldingATileFlipsItWithoutAutomaticallyRevealingAJokerOrSelectingIt()
        {
            var visual = new SerializedObject(_stone.GetComponent<StoneVisual>());
            var backs = visual.FindProperty("_faceDownVisuals");
            Assert.That(backs.arraySize, Is.Zero);
            var clicks = 0;
            _stone.Selection.Clicked += _ => clicks++;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = 10 };
            _stone.Selection.OnPointerDown(pointer);
            var deadline = Time.realtimeSinceStartup + 3;
            while (_stone.IsFaceUp && Time.realtimeSinceStartup < deadline) yield return null;
            _stone.Selection.OnPointerUp(pointer);
            _stone.Selection.OnPointerClick(pointer);
            Assert.That(_stone.IsFaceUp, Is.False);
            Assert.That(_stone.GetComponentsInChildren<TMP_Text>().All(text => !text.gameObject.activeSelf), Is.True);
            Assert.That(clicks, Is.Zero);
            _stone.Selection.OnPointerDown(pointer);
            deadline = Time.realtimeSinceStartup + 3;
            while (!_stone.IsFaceUp && Time.realtimeSinceStartup < deadline) yield return null;
            _stone.Selection.OnPointerUp(pointer);
            Assert.That(_stone.IsFaceUp, Is.True);
            Assert.That(backs.arraySize, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AShortPressAndACancelledHoldNeverFlipATile()
        {
            var clicks = 0;
            _stone.Selection.Clicked += _ => clicks++;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = 10 };
            _stone.Selection.OnPointerDown(pointer);
            _stone.Selection.OnPointerUp(pointer);
            _stone.Selection.OnPointerClick(pointer);
            Assert.That(clicks, Is.EqualTo(1));
            _stone.Selection.OnPointerDown(pointer);
            _stone.Selection.CancelPress();
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(_stone.IsFaceUp, Is.True);
        }

        [UnityTest]
        public IEnumerator AnOrdinaryTileCanBeFlippedButHasNoJokerStamp()
        {
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            _stone.Selection.OnPointerDown(pointer);
            var deadline = Time.realtimeSinceStartup + 3;
            while (_stone.IsFaceUp && Time.realtimeSinceStartup < deadline) yield return null;
            _stone.Selection.OnPointerUp(pointer);
            Assert.That(_stone.IsFaceUp, Is.False);
            var visual = new SerializedObject(_stone.GetComponent<StoneVisual>());
            Assert.That(visual.FindProperty("_faceDownVisuals").arraySize, Is.Zero);
        }

        [Test]
        public void MovingGroupsUpdatesTheDisplayedPointsAndOpeningUsesThatSameArrangement()
        {
            _context.Container.Resolve<TurnController>().Dispose();
            _context.Container.Resolve<HandPresenter>().Dispose();
            _context.Container.Resolve<TablePresenter>().Dispose();
            var deck = _context.Container.Resolve<DeckFactory>().Create(_context.Container.Resolve<DeckDefinition>()).ToArray();
            StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
                new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
            deck[0] = Tile(0,13,StoneColor.Blue);
            for (var index = 1; index <= 6; index++) deck[index] = Tile(index,10+(index-1)%3,index<=3?StoneColor.Red:StoneColor.Black);
            deck[7] = Tile(7,13);
            deck[8] = Tile(8,5,StoneColor.Blue);
            var session = new TableSession(new OkeyRules(4,7,1,13,66,5,101,202,101,-101,101,2,2),deck,28);
            var interaction = new RackInteractionController(session.Players[0].Hand,Rack);
            var factory = _context.Container.Resolve<StoneViewFactory>();
            var hand = new HandPresenter(session.Players[0].Hand,factory,Rack,interaction);
            var table = new TablePresenter(session,Controls,factory);
            var controller = new TurnController(session,hand,interaction,Controls,table,new RackMeldEvaluator(),Rack);
            try
            {
                hand.Initialize();table.Initialize();controller.Initialize();
                var serialized = new SerializedObject(Controls);
                var points = (TMPro.TMP_Text)serialized.FindProperty("_seriesPointsText").objectReferenceValue;
                Assert.That(points.text, Is.EqualTo("66 / 66"));
                var moved = hand.Views.Single(view=>view.Instance.Id==2);
                interaction.TryBeginDrag(moved);
                interaction.EndDrag(moved,SlotWorldPosition(20));
                Assert.That(points.text, Is.EqualTo("33 / 66"));
                interaction.TryBeginDrag(moved);
                interaction.EndDrag(moved,SlotWorldPosition(1));
                Assert.That(points.text, Is.EqualTo("66 / 66"));
                ((Button)serialized.FindProperty("_seriesButton").objectReferenceValue).onClick.Invoke();
                Assert.That(session.Melds.Count, Is.EqualTo(2));
                Assert.That(session.Players[0].Hand.Count, Is.EqualTo(2));
                Assert.That(hand.Views.Count, Is.EqualTo(2));
                DOTween.CompleteAll();
                var layOff = hand.Views.Single(view => view.Instance.Id == 7);
                var endpoint = Controls.MeldRoot.GetComponentsInChildren<Stone>().Single(view => view.Instance.Id == 3).transform.position + Vector3.right * Controls.MeldScale.x;
                var rackScale = layOff.transform.localScale;
                interaction.TryBeginDrag(layOff);
                interaction.DragTo(layOff, endpoint);
                Assert.That(Vector3.Distance(layOff.transform.lossyScale, Controls.MeldScale), Is.LessThan(.001f));
                interaction.CancelDrag(layOff);
                Assert.That(layOff.transform.localScale, Is.EqualTo(rackScale));
                interaction.TryBeginDrag(layOff);
                interaction.DragTo(layOff, endpoint);
                interaction.EndDrag(layOff, endpoint);
                Assert.That(session.Melds[0].Tiles.Count, Is.EqualTo(4));
                Assert.That(session.Players[0].Hand.Count, Is.EqualTo(1));
                Assert.That(hand.Views.Count, Is.EqualTo(1));
                var publicTile = Controls.MeldRoot.GetComponentsInChildren<Stone>().Single(view => view.Instance.Id == 7);
                Assert.That(Vector3.Distance(publicTile.transform.position, endpoint), Is.LessThan(.001f));
            }
            finally {controller.Dispose();table.Dispose();hand.Dispose();}
        }

        [UnityTest]
        public IEnumerator HelpAndSettingsStayHiddenUntilRequestedAndNeverOverlap()
        {
            var serialized = new SerializedObject(Controls);
            var help = (GameObject)serialized.FindProperty("_helpPanel").objectReferenceValue;
            var settings = (GameObject)serialized.FindProperty("_settingsPanel").objectReferenceValue;
            Assert.That(help.activeSelf || settings.activeSelf, Is.False);
            ((Button)serialized.FindProperty("_helpButton").objectReferenceValue).onClick.Invoke();
            Assert.That(help.activeSelf, Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var camera = _scene.GetRootGameObjects().Select(root=>root.GetComponent<Camera>()).Single(value=>value!=null);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                {position=camera.WorldToScreenPoint(Controls.DrawSource.transform.position)},hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.transform.IsChildOf(help.transform), Is.True, string.Join(", ", hits.Select(hit=>hit.gameObject.name+":"+hit.depth+":"+hit.sortingOrder)));
            ((Button)serialized.FindProperty("_settingsButton").objectReferenceValue).onClick.Invoke();
            Assert.That(help.activeSelf, Is.False);
            Assert.That(settings.activeSelf, Is.True);
            ((Button)serialized.FindProperty("_closeSettingsButton").objectReferenceValue).onClick.Invoke();
            Assert.That(settings.activeSelf, Is.False);
        }

        [Test]
        public void DraggingBlocksDrawingAndOtherDiscardAreasRejectLocalStones()
        {
            var session = _context.Container.Resolve<TableSession>();
            var stone = HandViews[0];
            Interaction.TryBeginDrag(stone);
            var controlsFields = new SerializedObject(Controls);
            Assert.That(((GameObject)controlsFields.FindProperty("_discardCue").objectReferenceValue).activeSelf, Is.True);
            Interaction.EndDrag(stone, Controls.DiscardAreas[1].StoneRoot.position);
            Assert.That(session.Players[0].Hand.Count, Is.EqualTo(22));
            Assert.That(session.ActiveSeat, Is.Zero);
            Interaction.TryBeginDrag(stone);
            Interaction.EndDrag(stone, Controls.DiscardAreas[0].StoneRoot.position);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            Interaction.TryBeginDrag(HandViews[0]);
            _context.Container.Resolve<TurnController>().Draw();
            Assert.That(Controls.DrawSource.CanDraw, Is.False);
            Assert.That(session.Players[0].Hand.Count, Is.EqualTo(21));
            Interaction.CancelDrag(HandViews[0]);
            Assert.That(Controls.DrawSource.CanDraw, Is.True);
        }

        [Test]
        public void FullBoardsKeepEveryMeldAccessibleAndOnlyAcceptTargetsOnTheVisiblePage()
        {
            _context.Container.Resolve<TablePresenter>().Dispose();
            StoneInstance Tile(int id,int value,StoneColor color=StoneColor.Red) =>
                new StoneInstance(id,new StoneData(StoneType.Regular,color,value));
            var deck=Enumerable.Range(0,100).Select(id=>Tile(id,7,StoneColor.Black)).ToArray();
            deck[0]=Tile(0,13,StoneColor.Blue);
            var starts=new[]{1,18,34,50};
            foreach(var start in starts)
                for(var tile=0;tile<15;tile++) deck[start+tile]=Tile(start+tile,3+tile%3);
            deck[69]=Tile(69,6);
            var table=new TableSession(new OkeyRules(4,16,1,13,1,5,101,202,101,-101,101,2,2),deck,28);
            var presenter=new TablePresenter(table,Controls,_context.Container.Resolve<StoneViewFactory>());
            presenter.Initialize();
            try
            {
                for(var seat=0;seat<4;seat++)
                {
                    if(seat>0) table.TryDraw(seat);
                    var melds=new List<Meld>();
                    for(var group=0;group<5;group++)
                    {
                        Assert.That(table.MeldRules.TryCreate(deck.Skip(starts[seat]+group*3).Take(3),MeldKind.Run,out var meld),Is.True);
                        melds.Add(meld);
                    }
                    Assert.That(table.TryOpen(seat,OpeningMode.Series,melds),Is.True);
                    table.TryDiscard(seat,table.Players[seat].Hand.Slots.First(tile=>tile!=null).Id);
                }
                Assert.That(table.Melds.Count,Is.EqualTo(20));
                Assert.That(Controls.SeriesBoard.PageCount,Is.EqualTo(2));
                Assert.That(Controls.MeldRoot.GetComponentsInChildren<Stone>(true).Length,Is.EqualTo(60));
                Assert.That(Controls.MeldRoot.GetComponentsInChildren<Stone>().Length,Is.EqualTo(48));
                var original=Controls.MeldRoot.GetComponentsInChildren<Stone>().Single(view=>view.Instance.Id==1);
                DOTween.CompleteAll();var origin=original.transform.position;
                Controls.SeriesBoard.ChangePage(1);
                Assert.That(Controls.MeldRoot.GetComponentsInChildren<Stone>().Length,Is.EqualTo(12));
                Assert.That(original.gameObject.activeSelf,Is.False);
                table.TryDraw(0,out var drawn);
                Assert.That(drawn.Id,Is.EqualTo(69));
                var target=Controls.SeriesBoard.transform.TransformPoint(Controls.SeriesBoard.GetPosition(new MeldBoardCell(1,0,3,5)));
                Assert.That(presenter.TryFindMeld(drawn,target,out var index),Is.True);
                Assert.That(index,Is.EqualTo(19));
                Assert.That(table.TryLayOff(0,drawn.Id,index),Is.True);
                Assert.That(table.Melds[19].Tiles.Count,Is.EqualTo(4));
                Assert.That(table.Melds[3].Tiles.Count,Is.EqualTo(3));
                Controls.SeriesBoard.ChangePage(-1);
                Assert.That(original.transform.position,Is.EqualTo(origin));
            }
            finally {presenter.Dispose();}
        }

        [Test]
        public void PublicViewsUseAnchorsAndNeverExposeOpponentHands()
        {
            var table = _context.Container.Resolve<TableSession>();
            var indicator = Controls.IndicatorRoot.GetComponentInChildren<Stone>();
            Assert.That(indicator.Instance, Is.SameAs(table.Indicator));
            Assert.That(indicator.IsFaceUp, Is.True);
            Assert.That(indicator.transform.localPosition, Is.EqualTo(Vector3.zero));
            var opponentIds = table.Players.Skip(1).SelectMany(player => player.Hand.Slots)
                .Where(stone => stone != null).Select(stone => stone.Id).ToHashSet();
            var visible = Controls.GetComponentsInChildren<Stone>().Concat(HandViews);
            Assert.That(visible.Any(view => view.Instance != null && opponentIds.Contains(view.Instance.Id)), Is.False);
            Assert.That(visible.Count(view => view.Instance == null && !view.IsFaceUp), Is.EqualTo(12));
        }

        [UnityTest]
        public IEnumerator OpenMeldPresentationExtendsExistingViewsAndCleansUp()
        {
            _context.Container.Resolve<TablePresenter>().Dispose();
            var deck = _context.Container.Resolve<DeckFactory>().Create(_context.Container.Resolve<DeckDefinition>()).ToArray();
            StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
                new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
            deck[0] = Tile(0, 13);
            deck[1] = Tile(1, 10);
            deck[2] = Tile(2, 11);
            deck[3] = Tile(3, 12);
            deck[4] = Tile(4, 10, StoneColor.Blue);
            deck[5] = Tile(5, 11, StoneColor.Blue);
            deck[6] = Tile(6, 12, StoneColor.Blue);
            deck[7] = Tile(7, 13);
            deck[8] = Tile(8, 2, StoneColor.Black);
            var table = new TableSession(new OkeyRules(4, 7, 1, 13, 66, 5, 101, 202, 101, -101, 101, 2, 2), deck, 28);
            var presenter = new TablePresenter(table, Controls, _context.Container.Resolve<StoneViewFactory>());
            presenter.Initialize();
            table.MeldRules.TryCreate(deck.Skip(1).Take(3), MeldKind.Run, out var first);
            table.MeldRules.TryCreate(deck.Skip(4).Take(3), MeldKind.Run, out var second);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] {first, second}), Is.True);
            var original = Controls.MeldRoot.GetComponentsInChildren<Stone>();
            Assert.That(original.Length, Is.EqualTo(6));
            DOTween.CompleteAll();
            var endpoint = original.Single(view => view.Instance.Id == 3).transform.position + Vector3.right * Controls.MeldScale.x;
            Assert.That(presenter.TryFindMeld(deck[7], endpoint, out var target), Is.True);
            Assert.That(target, Is.Zero);
            Assert.That(presenter.TryFindMeld(deck[8], endpoint, out _), Is.False);
            presenter.PreviewLayOff(deck[7], endpoint);
            var highlight = (GameObject)new SerializedObject(original[0].Selection).FindProperty("_highlight").objectReferenceValue;
            Assert.That(highlight.activeSelf, Is.True);
            presenter.ClearPreview();
            Assert.That(highlight.activeSelf, Is.False);
            Assert.That(table.TryLayOff(0, 7, 0), Is.True);
            var extended = Controls.MeldRoot.GetComponentsInChildren<Stone>();
            Assert.That(extended.Length, Is.EqualTo(7));
            foreach (var view in original) Assert.That(extended, Does.Contain(view));
            foreach (var view in extended)
            {
                DOTween.Complete(view.transform);
                Assert.That(view.IsFaceUp, Is.True);
                Assert.That(view.DragHandler.enabled, Is.False);
            }
            Assert.That(presenter.TryFindMeld(extended[0].transform.position, out var index), Is.True);
            Assert.That(index, Is.Zero);
            presenter.Dispose();
            Assert.That(extended.All(view => !view.gameObject.activeSelf), Is.True);
            yield return null;
            Assert.That(extended.All(view => view == null), Is.True);
        }

        [Test]
        public void DrawPileReceivesPointerClicksThroughItsFaceDownStone()
        {
            var stone = HandViews[0];
            Interaction.TryBeginDrag(stone);
            Interaction.EndDrag(stone, Controls.DiscardAreas[0].StoneRoot.position);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            Physics2D.SyncTransforms();
            var camera = _scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).Single(value => value != null);
            var data = new PointerEventData(EventSystem.current)
            {
                position = camera.WorldToScreenPoint(Controls.DrawSource.transform.position),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            camera.GetComponent<Physics2DRaycaster>().Raycast(data, hits);
            var hit = hits.First(result => result.gameObject.transform.IsChildOf(Controls.DrawSource.transform));
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerClickHandler);
            Assert.That(_context.Container.Resolve<TableSession>().Players[0].Hand.Count, Is.EqualTo(22));
        }

        private sealed class FixedRandom : IRandomSource
        {
            private readonly double _value;
            public FixedRandom(double value) => _value = value;
            public double NextDouble() => _value;
            public int Next(int maximum) => 0;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BotModeControlsWhetherAnEligibleOpeningCanBePostponed(bool humanlike)
        {
            var deck = new SeededStoneShuffler().Shuffle(_context.Container.Resolve<DeckFactory>().Create(_context.Container.Resolve<DeckDefinition>()),42);
            var rules = new OkeyRules(4,21,1,13,1,5,101,202,101,-101,101,2,2);
            var table = new TableSession(rules,deck,28,startingSeat:1);
            var planner = new MeldPlanner();
            var strategy = new MeldPlayerStrategy(planner);
            var observation = new PlayerObservation(table.Players[1].Hand.Slots.Where(tile => tile != null),table.Melds,OpeningMode.Closed,rules,table.MeldRules);
            var plan = strategy.PlanOpening(observation,out var mode);
            Assert.That(plan.TileCount,Is.LessThan(20));
            Assert.That(plan.Melds.Count,Is.GreaterThan(0));
            var settings = ScriptableObject.CreateInstance<TableSettings>();
            typeof(TableSettings).GetField("_humanlikeBots",System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(settings,humanlike);
            try
            {
                new BotTurnController(table,strategy,settings,new FixedRandom(.1)).Act();
                Assert.That(table.Players[1].Opening,Is.EqualTo(humanlike ? OpeningMode.Closed : mode));
                Assert.That(table.Players[1].Discards.Count,Is.EqualTo(humanlike ? 1 : 0));
                Assert.That(table.ActiveSeat,Is.EqualTo(humanlike ? 2 : 1));
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [Test]
        public void TimeoutCancelsDraggingAndEndsTheLocalTurn()
        {
            Interaction.TryBeginDrag(HandViews[0]);
            _context.Container.Resolve<TurnClockController>().Advance(46);
            var table = _context.Container.Resolve<TableSession>();
            Assert.That(Interaction.IsDragging, Is.False);
            Assert.That(table.ActiveSeat, Is.EqualTo(1));
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(21));
            Assert.That(table.Players[0].Discards.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PairsUseTheirOwnBoardAndCleanUpTheirViews()
        {
            var deck = _context.Container.Resolve<DeckFactory>().Create(_context.Container.Resolve<DeckDefinition>());
            var table = new TableSession(new OkeyRules(4,21,1,13,101,1,101,202,101,-101,101,2,2),deck,28);
            var pair = table.Players[0].Hand.Slots.Where(tile => tile != null && !table.MeldRules.IsWild(tile))
                .GroupBy(tile => tile.Data.Value).First(group => group.Count() == 2).ToArray();
            table.MeldRules.TryCreate(pair, MeldKind.Pair, out var meld);
            var presenter = new TablePresenter(table,Controls,_context.Container.Resolve<StoneViewFactory>());
            presenter.Initialize();
            Assert.That(table.TryOpen(0,OpeningMode.Pairs,new[] {meld}),Is.True);
            Assert.That(Controls.PairsRoot.GetComponentsInChildren<Stone>().Length,Is.EqualTo(2));
            Assert.That(Controls.MeldRoot.GetComponentsInChildren<Stone>().Length,Is.Zero);
            presenter.Dispose();
            yield return null;
            Assert.That(Controls.PairsRoot.GetComponentsInChildren<Stone>().Length,Is.Zero);
        }

        [UnityTest]
        public IEnumerator MousePressAndReleaseDrawFromTheWholePileSurface()
        {
            var stone = HandViews[0];
            Interaction.TryBeginDrag(stone);
            Interaction.EndDrag(stone, Controls.DiscardAreas[0].StoneRoot.position);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            yield return null;
            Physics2D.SyncTransforms();
            var camera = _scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).Single(value => value != null);
            var position = camera.WorldToScreenPoint(Controls.DrawSource.transform.position + Vector3.down * .45f);
            CompleteHandMovement();
            var originalMouse = Mouse.current;
            var originalBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            var originalEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
                InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                InputSystem.Update();
                yield return null;
                Assert.That(_context.Container.Resolve<TableSession>().Players[0].Hand.Count, Is.EqualTo(22));
                Assert.That(Controls.DrawSource.CanDraw, Is.False);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.settings.backgroundBehavior = originalBackgroundBehavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorBehavior;
                if (originalMouse != null) originalMouse.MakeCurrent();
            }
        }

        [Test]
        public void DraggingFromPileDrawsOnceAndInsertsTheNewTileAtTheRequestedSlot()
        {
            var stone = HandViews[0];
            Interaction.TryBeginDrag(stone);
            Interaction.EndDrag(stone, Controls.DiscardAreas[0].StoneRoot.position);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            var table = _context.Container.Resolve<TableSession>();
            var camera = _scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).Single(value => value != null);
            var data = new PointerEventData(EventSystem.current)
            {
                pointerId = 29,
                position = camera.WorldToScreenPoint(Controls.DrawSource.transform.position),
                button = PointerEventData.InputButton.Left,
                pointerPressRaycast = new RaycastResult { module = camera.GetComponent<Physics2DRaycaster>() }
            };
            var before = HandViews.Select(view => view.Instance.Id).ToHashSet();
            Controls.DrawSource.OnPointerDown(data);
            Controls.DrawSource.OnBeginDrag(data);
            Controls.DrawSource.OnBeginDrag(data);
            var drawn = HandViews.Single(view => !before.Contains(view.Instance.Id));
            Assert.That(Interaction.IsDragging, Is.True);
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(22));
            data.position = camera.WorldToScreenPoint(SlotWorldPosition(26));
            Controls.DrawSource.OnDrag(data);
            Controls.DrawSource.OnEndDrag(data);
            Controls.DrawSource.OnPointerClick(data);
            Assert.That(Interaction.IsDragging, Is.False);
            Assert.That(table.Players[0].Hand.FindSlot(drawn.Instance.Id), Is.EqualTo(26));
            Assert.That(table.RemainingCount, Is.EqualTo(16));
            DOTween.Complete(drawn.transform);
            Assert.That(Vector3.Distance(drawn.transform.position, SlotWorldPosition(26)), Is.LessThan(.001f));
        }

        [Test]
        public void RefreshPreservesExistingViewsAndReusesDiscardedSlot()
        {
            var original = HandViews;
            Interaction.TryBeginDrag(original[3]);
            Interaction.EndDrag(original[3], Controls.DiscardAreas[0].StoneRoot.position);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            _context.Container.Resolve<TurnController>().Draw();
            var current = HandViews;
            foreach (var view in original.Where(view => view != original[3]))
                Assert.That(current, Does.Contain(view));
            var newest = current.Last();
            Assert.That(Arrangement.FindSlot(newest.Instance.Id), Is.EqualTo(3));
            DOTween.Complete(newest.transform);
            Assert.That(Vector3.Distance(newest.transform.position, SlotWorldPosition(3)), Is.LessThan(0.001f));
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
        public void SceneProvidesFourUniqueHandsAndAConfiguredPile()
        {
            var table = _context.Container.Resolve<TableSession>();
            Assert.That(table.Players.Count, Is.EqualTo(4));
            Assert.That(table.RemainingCount, Is.EqualTo(20));
            Assert.That(table.Players.SelectMany(player => player.Hand.Slots).Where(stone => stone != null)
                .Select(stone => stone.Id).Distinct().Count(), Is.EqualTo(85));
        }

        [Test]
        public void OpeningHandDisplaysLocalPhysicalStonesOnceAndInitializationIsIdempotent()
        {
            var presenter = _context.Container.Resolve<HandPresenter>();
            var rack = _context.Container.Resolve<RackView>();
            var views = presenter.Views.ToArray();
            presenter.Initialize();
            Assert.That(presenter.Views, Is.EqualTo(views));
            Assert.That(views.Select(view => view.Instance.Id), Is.EqualTo(Arrangement.Slots.Where(stone => stone != null).Select(stone => stone.Id)));
            Assert.That(views.Length, Is.EqualTo(22));
            Assert.That(rack.StoneRoot.childCount, Is.EqualTo(22));
            for (var index = 0; index < views.Length; index++)
            {
                DOTween.Complete(views[index].transform);
                Assert.That(Vector3.Distance(views[index].transform.localPosition, rack.GetSlotLocalPosition(index)), Is.LessThan(0.001f));
            }
        }

        [Test]
        public void TooLargeHandFailsBeforeCreatingAnyViews()
        {
            var rack = _context.Container.Resolve<RackView>();
            var table = _context.Container.Resolve<TableSession>();
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var count = 0; table.ActiveSeat != 0 && table.Phase != TurnPhase.Finished && count < 100; count++) bot.Act();
            if (table.Phase == TurnPhase.AwaitingDraw) table.TryDraw(0);
            var definition = _context.Container.Resolve<DeckDefinition>();
            var deal = _context.Container.Resolve<OpeningDealFactory>().Create(definition, rack.Capacity + 1, 42);
            var hand = new RackArrangement(deal.InitialHand, rack.Capacity + 1);
            var interaction = new RackInteractionController(hand, rack);
            var presenter = new HandPresenter(hand, _context.Container.Resolve<StoneViewFactory>(), rack, interaction);
            var childCount = rack.StoneRoot.childCount;

            Assert.Throws<InvalidOperationException>(() => presenter.Initialize());
            Assert.That(presenter.Views, Is.Empty);
            Assert.That(rack.StoneRoot.childCount, Is.EqualTo(childCount));
            presenter.Dispose();
        }

        [UnityTest]
        public IEnumerator PresenterDisposalStopsMovementAndDestroysOwnedViews()
        {
            var presenter = _context.Container.Resolve<HandPresenter>();
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
