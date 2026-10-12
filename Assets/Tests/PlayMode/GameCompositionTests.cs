using Okey.Presentation.Tables;
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
            var existing = UnityEngine.Object.FindFirstObjectByType<GameFlowState>();
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
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
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowState>();
            if (flow != null) UnityEngine.Object.DestroyImmediate(flow.gameObject);
            SetSetting("_randomStartingSeat", _randomStart);
            SetSetting("_humanlikeBots", _humanlike);
            if (_scene.IsValid() && _scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(_scene);
        }

        private Stone[] HandViews => _context.Container.Resolve<HandPresenter>().Views.ToArray();
        private RackView Rack => _context.Container.Resolve<RackView>();
        private IRackLayout Arrangement => _context.Container.Resolve<IRackLayout>();
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

        private TableView Controls => _context.Container.Resolve<TableView>();
        [UnityTest]
        public IEnumerator TableStartsWithDiscardAndDraggingToRightAreaAdvancesTheBots()
        {
            var session = _context.Container.Resolve<TableSession>();
            Assert.That(Controls.Surface.DrawSource.CanDraw, Is.False);
            var stone = HandViews[0];
            var id = stone.Instance.Id;
            Interaction.TryBeginDrag(stone);
            Interaction.EndDrag(stone, Controls.Surface.DiscardAreas[0].StoneRoot.position);
            Assert.That(session.ActiveSeat, Is.EqualTo(1));
            Assert.That(session.Players[0].Hand.Count, Is.EqualTo(21));
            Assert.That(HandViews.Any(view => view.Instance.Id == id), Is.False);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            Assert.That(session.ActiveSeat, Is.Zero);
            Assert.That(Controls.Surface.DrawSource.CanDraw, Is.True);
            var stockBefore = session.RemainingCount;
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            Controls.Surface.DrawSource.OnPointerClick(data);
            Controls.Surface.DrawSource.OnPointerClick(data);
            Assert.That(session.Players[0].Hand.Count, Is.EqualTo(22));
            Assert.That(session.RemainingCount, Is.EqualTo(stockBefore - 1));
            for (var seat = 0; seat < session.Players.Count; seat++)
                Assert.That(Controls.Surface.DiscardAreas[seat].StoneRoot.GetComponentInChildren<Stone>() != null,
                    Is.EqualTo(session.Players[seat].Discards.Count > 0));
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
            var serial = new SerializedObject(Controls.Clock);
            var sliders = serial.FindProperty("_turnSliders");
            var texts = serial.FindProperty("_opponentClockTexts");
            Assert.That(sliders.arraySize, Is.EqualTo(4));
            _context.Container.Resolve<TurnClockController>().RenderClock(0, 22.5, false, 0, 45);
            Assert.That(((Slider)sliders.GetArrayElementAtIndex(0).objectReferenceValue).value, Is.EqualTo(.5f));
            _context.Container.Resolve<TurnClockController>().RenderClock(2, 10, false, 0, 20);
            for (var seat = 0; seat < 4; seat++)
            {
                var slider = (Slider)sliders.GetArrayElementAtIndex(seat).objectReferenceValue;
                Assert.That(slider.value, Is.EqualTo(seat == 2 ? .5f : 0));
                Assert.That(slider.interactable, Is.False);
                Assert.That(slider.fillRect.sizeDelta.x, Is.Zero);
            }
            Assert.That(((TMP_Text)texts.GetArrayElementAtIndex(1).objectReferenceValue).text, Is.EqualTo("10s"));
            _context.Container.Resolve<TurnClockController>().RenderClock(2, 0, false, 0, 20, true);
            Assert.That(((Slider)sliders.GetArrayElementAtIndex(2).objectReferenceValue).value, Is.Zero);
            Assert.That(((TMP_Text)texts.GetArrayElementAtIndex(1).objectReferenceValue).text, Is.Empty);
        }

        [Test]
        public void EscalatingToggleAppliesToTheNextTable()
        {
            var settings = _context.Container.Resolve<GameSetup>();
            var table = _context.Container.Resolve<TableSession>();
            var original = settings.EscalatingOpenings;
            var button = (Button)new SerializedObject(Controls.Options).FindProperty("_escalatingButton").objectReferenceValue;
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
        public void BotControllerExecutesInjectedDecisionsAndLeavesTheLocalSeatAlone()
        {
            var deck = _context.Container.Resolve<DeckFactory>().Create(_context.Container.Resolve<DeckDefinition>());
            var session = new TableSession(_context.Container.Resolve<OkeyRules>(), deck, Rack.Capacity, game: new Okey.Core.Games.Okey101Game());
            var decisions = new RecordingDecisions();
            var controller = new BotTurnController(session, decisions,
                new BotPreferences(false, 0, 0, 0, 0, 0, 2), new SeededRandomSource(42), new LocalPlayerContext(2, 4));
            controller.Act();
            controller.Act();
            controller.Act();
            Assert.That(session.ActiveSeat, Is.EqualTo(2));
            Assert.That(decisions.Count, Is.EqualTo(3));
            controller.Act();
            Assert.That(decisions.Count, Is.EqualTo(3));
            Assert.That(session.Players[0].Discards.Count, Is.EqualTo(1));
            Assert.That(session.Players[1].Discards.Count, Is.EqualTo(1));
            Assert.That(session.Players[2].Discards, Is.Empty);
        }

        private sealed class RecordingDecisions : IBotDecisionPolicy
        {
            public int Count { get; private set; }
            public bool ShouldHoldOpening(BotPreferences preferences) => false;
            public PlayerAction Decide(PlayerTurnObservation turn, BotPreferences preferences, bool holdOpening)
            {
                Count++;
                return turn.Phase == TurnPhase.AwaitingDraw ? PlayerAction.Draw() : PlayerAction.Discard(turn.Player.Hand[0].Id);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void LocalPlayerCanUseAnySeatWithoutChangingControllers(int localSeat)
        {
            _context.Container.Resolve<TurnController>().Dispose();
            _context.Container.Resolve<TableHudPresenter>().Dispose();
            _context.Container.Resolve<HandPresenter>().Dispose();
            _context.Container.Resolve<TablePresenter>().Dispose();
            var deck = _context.Container.Resolve<DeckFactory>().Create(_context.Container.Resolve<DeckDefinition>());
            var session = new TableSession(_context.Container.Resolve<OkeyRules>(), deck, Rack.Capacity, startingSeat: localSeat, game: new Okey.Core.Games.Okey101Game());
            var local = new LocalPlayerContext(localSeat, session.Players.Count);
            var interaction = new RackInteractionController(session.Players[localSeat].Layout, Rack);
            var factory = _context.Container.Resolve<StoneViewFactory>();
            var hand = new HandPresenter(session.Players[localSeat].Hand, factory, Rack, interaction);
            var names = new TablePlayerNames(Controls.Texts, _context.Container.Resolve<BotPreferences>(), local);
            var table = new TablePresenter(session, Controls, factory, local, names);
            var controller = new TurnController(session, hand, interaction, Controls, table, Rack, local);
            var hud = new TableHudPresenter(session, Controls, hand, interaction, table, Rack, local, names, new RackMeldEvaluator());
            try
            {
                hand.Initialize(); table.Initialize(); hud.Initialize(); controller.Initialize();
                Assert.That(hand.Views.Select(view => view.Instance.Id), Is.EquivalentTo(
                    session.Players[localSeat].Hand.Slots.Where(tile => tile != null).Select(tile => tile.Id)));
                var clock = new TurnClockController(session, _context.Container.Resolve<TimeoutDiscardPolicy>(), Controls, interaction, local, names);
                clock.RenderClock(localSeat, 20, false, 0, 40);
                var sliders = new SerializedObject(Controls.Clock).FindProperty("_turnSliders");
                Assert.That(((Slider)sliders.GetArrayElementAtIndex(0).objectReferenceValue).value, Is.EqualTo(.5f));
                var stone = hand.Views[0];
                stone.Selection.OnPointerClick(new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left, clickCount = 2
                });
                Assert.That(session.Players[localSeat].Discards.Single().Id, Is.EqualTo(stone.Instance.Id));
                Assert.That(session.Players[0].Discards, Is.Empty);
                var shown = Controls.Surface.DiscardAreas[0].StoneRoot.GetComponentInChildren<Stone>();
                Assert.That(shown.Instance.Id, Is.EqualTo(stone.Instance.Id));
                Assert.That(local.ViewSeat(localSeat), Is.Zero);
                Assert.That(local.TableSeat(session.Players.Count - 1), Is.EqualTo((localSeat + session.Players.Count - 1) % session.Players.Count));
            }
            finally { controller.Dispose(); hud.Dispose(); table.Dispose(); hand.Dispose(); }
        }

        [Test]
        public void MovingGroupsUpdatesTheDisplayedPointsAndOpeningUsesThatSameArrangement()
        {
            _context.Container.Resolve<TurnController>().Dispose();
            _context.Container.Resolve<TableHudPresenter>().Dispose();
            _context.Container.Resolve<HandPresenter>().Dispose();
            _context.Container.Resolve<TablePresenter>().Dispose();
            var deck = _context.Container.Resolve<DeckFactory>().Create(_context.Container.Resolve<DeckDefinition>()).ToArray();
            StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
                new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
            deck[0] = Tile(0,13,StoneColor.Blue);
            for (var index = 1; index <= 6; index++) deck[index] = Tile(index,10+(index-1)%3,index<=3?StoneColor.Red:StoneColor.Black);
            deck[7] = Tile(7,13);
            deck[8] = Tile(8,5,StoneColor.Blue);
            var session = new TableSession(new OkeyRules(4,7,1,13,66,5,101,202,101,-101,101,2,2),deck,28, game: new Okey.Core.Games.Okey101Game());
            var interaction = new RackInteractionController(session.Players[0].Layout,Rack);
            var factory = _context.Container.Resolve<StoneViewFactory>();
            var hand = new HandPresenter(session.Players[0].Hand,factory,Rack,interaction);
            var table = new TablePresenter(session,Controls,factory,new LocalPlayerContext(0,session.Players.Count), _context.Container.Resolve<TablePlayerNames>());
            var controller = new TurnController(session,hand,interaction,Controls,table,Rack,new LocalPlayerContext(0,session.Players.Count));
            var hud = new TableHudPresenter(session,Controls,hand,interaction,table,Rack,new LocalPlayerContext(0,session.Players.Count), _context.Container.Resolve<TablePlayerNames>(), new RackMeldEvaluator());
            try
            {
                hand.Initialize();table.Initialize();hud.Initialize();controller.Initialize();
                var serialized = new SerializedObject(Controls.Actions[0]);
                var points = (TMPro.TMP_Text)serialized.FindProperty("_progressText").objectReferenceValue;
                Assert.That(points.text, Is.EqualTo("66 / 66"));
                var moved = hand.Views.Single(view=>view.Instance.Id==2);
                interaction.TryBeginDrag(moved);
                interaction.EndDrag(moved,SlotWorldPosition(20));
                Assert.That(points.text, Is.EqualTo("33 / 66"));
                interaction.TryBeginDrag(moved);
                interaction.EndDrag(moved,SlotWorldPosition(1));
                Assert.That(points.text, Is.EqualTo("66 / 66"));
                ((Button)serialized.FindProperty("_button").objectReferenceValue).onClick.Invoke();
                Assert.That(session.Melds.Count, Is.EqualTo(2));
                Assert.That(session.Players[0].Hand.Count, Is.EqualTo(2));
                Assert.That(hand.Views.Count, Is.EqualTo(2));
                DOTween.CompleteAll();
                var layOff = hand.Views.Single(view => view.Instance.Id == 7);
                var endpoint = Controls.Surface.SeriesBoard.transform.GetComponentsInChildren<Stone>().Single(view => view.Instance.Id == 3).transform.position + Vector3.right * Controls.Surface.SeriesBoard.TileScale.x;
                var rackScale = layOff.transform.localScale;
                interaction.TryBeginDrag(layOff);
                interaction.DragTo(layOff, endpoint);
                Assert.That(Vector3.Distance(layOff.transform.lossyScale, Controls.Surface.SeriesBoard.TileScale), Is.LessThan(.001f));
                interaction.CancelDrag(layOff);
                Assert.That(layOff.transform.localScale, Is.EqualTo(rackScale));
                interaction.TryBeginDrag(layOff);
                interaction.DragTo(layOff, endpoint);
                interaction.EndDrag(layOff, endpoint);
                Assert.That(session.Melds[0].Tiles.Count, Is.EqualTo(4));
                Assert.That(session.Players[0].Hand.Count, Is.EqualTo(1));
                Assert.That(hand.Views.Count, Is.EqualTo(1));
                var publicTile = Controls.Surface.SeriesBoard.transform.GetComponentsInChildren<Stone>().Single(view => view.Instance.Id == 7);
                Assert.That(Vector3.Distance(publicTile.transform.position, endpoint), Is.LessThan(.001f));
            }
            finally {controller.Dispose();hud.Dispose();table.Dispose();hand.Dispose();}
        }

        [UnityTest]
        public IEnumerator HelpAndSettingsStayHiddenUntilRequestedAndNeverOverlap()
        {
            var serialized = new SerializedObject(Controls.Panels);
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
                {position=camera.WorldToScreenPoint(Controls.Surface.DrawSource.transform.position)},hits);
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
            var controlsFields = new SerializedObject(Controls.Surface);
            Assert.That(((GameObject)controlsFields.FindProperty("_discardCue").objectReferenceValue).activeSelf, Is.True);
            Interaction.EndDrag(stone, Controls.Surface.DiscardAreas[1].StoneRoot.position);
            Assert.That(session.Players[0].Hand.Count, Is.EqualTo(22));
            Assert.That(session.ActiveSeat, Is.Zero);
            Interaction.TryBeginDrag(stone);
            Interaction.EndDrag(stone, Controls.Surface.DiscardAreas[0].StoneRoot.position);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            Interaction.TryBeginDrag(HandViews[0]);
            _context.Container.Resolve<TurnController>().Draw();
            Assert.That(Controls.Surface.DrawSource.CanDraw, Is.False);
            Assert.That(session.Players[0].Hand.Count, Is.EqualTo(21));
            Interaction.CancelDrag(HandViews[0]);
            Assert.That(Controls.Surface.DrawSource.CanDraw, Is.True);
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
            var table=new TableSession(new OkeyRules(4,16,1,13,1,5,101,202,101,-101,101,2,2),deck,28, game: new Okey.Core.Games.Okey101Game());
            var presenter=new TablePresenter(table,Controls,_context.Container.Resolve<StoneViewFactory>(),new LocalPlayerContext(0,table.Players.Count), _context.Container.Resolve<TablePlayerNames>());
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
                Assert.That(Controls.Surface.SeriesBoard.PageCount,Is.EqualTo(2));
                Assert.That(Controls.Surface.SeriesBoard.transform.GetComponentsInChildren<Stone>(true).Length,Is.EqualTo(60));
                Assert.That(Controls.Surface.SeriesBoard.transform.GetComponentsInChildren<Stone>().Length,Is.EqualTo(48));
                var original=Controls.Surface.SeriesBoard.transform.GetComponentsInChildren<Stone>().Single(view=>view.Instance.Id==1);
                DOTween.CompleteAll();var origin=original.transform.position;
                Controls.Surface.SeriesBoard.ChangePage(1);
                Assert.That(Controls.Surface.SeriesBoard.transform.GetComponentsInChildren<Stone>().Length,Is.EqualTo(12));
                Assert.That(original.gameObject.activeSelf,Is.False);
                table.TryDraw(0,out var drawn);
                Assert.That(drawn.Id,Is.EqualTo(69));
                var target=Controls.Surface.SeriesBoard.transform.TransformPoint(Controls.Surface.SeriesBoard.GetPosition(new MeldBoardCell(1,0,3,5)));
                Assert.That(presenter.TryFindMeld(drawn,target,out var index),Is.True);
                Assert.That(index,Is.EqualTo(19));
                Assert.That(table.TryLayOff(0,drawn.Id,index),Is.True);
                Assert.That(table.Melds[19].Tiles.Count,Is.EqualTo(4));
                Assert.That(table.Melds[3].Tiles.Count,Is.EqualTo(3));
                Controls.Surface.SeriesBoard.ChangePage(-1);
                Assert.That(original.transform.position,Is.EqualTo(origin));
            }
            finally {presenter.Dispose();}
        }

        [Test]
        public void HumanlikeOpponentsShowOnlyTheirPlayerNamesAndRefreshOnModeChange()
        {
            var preferences = _context.Container.Resolve<BotPreferences>();
            var names = _context.Container.Resolve<TablePlayerNames>();
            preferences.ToggleMode();
            var labels = Controls.Players.GetComponentsInChildren<TMP_Text>()
                .Where(text => text.name == "OpponentStatus").Select(text => text.text).ToArray();
            Assert.That(labels, Is.EquivalentTo(new[] { names.ForSeat(1), names.ForSeat(2), names.ForSeat(3) }));
            Assert.That(labels.All(label => label.StartsWith("Player")), Is.True);
            preferences.ToggleMode();
            labels = Controls.Players.GetComponentsInChildren<TMP_Text>()
                .Where(text => text.name == "OpponentStatus").Select(text => text.text).ToArray();
            Assert.That(labels, Is.EquivalentTo(new[] { "East", "North", "West" }));
        }

        [Test]
        public void PileDragRendersAboveTheTableUiAndRestoresItsOrderOnCancel()
        {
            CompleteHandMovement();
            var stone = HandViews[0];
            var group = stone.GetComponent<SortingGroup>();
            var originalOrder = group.sortingOrder;
            Assert.That(Controls.Canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
            Assert.That(Controls.Canvas.worldCamera, Is.Not.Null);
            Assert.That(Interaction.TryBeginDrag(stone), Is.True);
            Assert.That(group.sortingOrder, Is.GreaterThan(Controls.Canvas.sortingOrder));
            Interaction.CancelDrag(stone);
            Assert.That(group.sortingOrder, Is.EqualTo(originalOrder));
        }

        [Test]
        public void PublicViewsUseAnchorsAndNeverExposeOpponentHands()
        {
            var table = _context.Container.Resolve<TableSession>();
            var indicator = Controls.Surface.IndicatorRoot.GetComponentInChildren<Stone>();
            Assert.That(indicator.Instance, Is.SameAs(table.Indicator));
            Assert.That(indicator.IsFaceUp, Is.True);
            Assert.That(indicator.transform.localPosition, Is.EqualTo(Vector3.zero));
            var opponentIds = table.Players.Skip(1).SelectMany(player => player.Hand.Slots)
                .Where(stone => stone != null).Select(stone => stone.Id).ToHashSet();
            var visible = Controls.GetComponentsInChildren<Stone>().Concat(HandViews);
            Assert.That(visible.Any(view => view.Instance != null && opponentIds.Contains(view.Instance.Id)), Is.False);
            Assert.That(visible.Count(view => view.Instance == null && !view.IsFaceUp), Is.EqualTo(3));
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
            var table = new TableSession(new OkeyRules(4, 7, 1, 13, 66, 5, 101, 202, 101, -101, 101, 2, 2), deck, 28, game: new Okey.Core.Games.Okey101Game());
            var presenter = new TablePresenter(table, Controls, _context.Container.Resolve<StoneViewFactory>(),new LocalPlayerContext(0,table.Players.Count), _context.Container.Resolve<TablePlayerNames>());
            presenter.Initialize();
            table.MeldRules.TryCreate(deck.Skip(1).Take(3), MeldKind.Run, out var first);
            table.MeldRules.TryCreate(deck.Skip(4).Take(3), MeldKind.Run, out var second);
            Assert.That(table.TryOpen(0, OpeningMode.Series, new[] {first, second}), Is.True);
            var original = Controls.Surface.SeriesBoard.transform.GetComponentsInChildren<Stone>();
            Assert.That(original.Length, Is.EqualTo(6));
            DOTween.CompleteAll();
            var endpoint = original.Single(view => view.Instance.Id == 3).transform.position + Vector3.right * Controls.Surface.SeriesBoard.TileScale.x;
            Assert.That(presenter.TryFindMeld(deck[7], endpoint, out var target), Is.True);
            Assert.That(target, Is.Zero);
            Assert.That(presenter.TryFindMeld(deck[8], endpoint, out _), Is.False);
            presenter.PreviewLayOff(deck[7], endpoint);
            var highlight = (GameObject)new SerializedObject(original[0].Selection).FindProperty("_highlight").objectReferenceValue;
            Assert.That(highlight.activeSelf, Is.True);
            presenter.ClearPreview();
            Assert.That(highlight.activeSelf, Is.False);
            Assert.That(table.TryLayOff(0, 7, 0), Is.True);
            var extended = Controls.Surface.SeriesBoard.transform.GetComponentsInChildren<Stone>();
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
            Interaction.EndDrag(stone, Controls.Surface.DiscardAreas[0].StoneRoot.position);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            Physics2D.SyncTransforms();
            var camera = _scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).Single(value => value != null);
            var data = new PointerEventData(EventSystem.current)
            {
                position = camera.WorldToScreenPoint(Controls.Surface.DrawSource.transform.position),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            camera.GetComponent<Physics2DRaycaster>().Raycast(data, hits);
            var hit = hits.First(result => result.gameObject.transform.IsChildOf(Controls.Surface.DrawSource.transform));
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
            var table = new TableSession(rules,deck,28,startingSeat:1, game: new Okey.Core.Games.Okey101Game());
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
                new BotTurnController(table,new BotDecisionPolicy(strategy,new FixedRandom(.1)),new BotPreferences(humanlike,.01,.35,1.2,3.8,.8,2),new FixedRandom(.1),new LocalPlayerContext(0,table.Players.Count)).Act();
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
            var table = new TableSession(new OkeyRules(4,21,1,13,101,1,101,202,101,-101,101,2,2),deck,28, game: new Okey.Core.Games.Okey101Game());
            var pair = table.Players[0].Hand.Slots.Where(tile => tile != null && !table.MeldRules.IsWild(tile))
                .GroupBy(tile => tile.Data.Value).First(group => group.Count() == 2).ToArray();
            table.MeldRules.TryCreate(pair, MeldKind.Pair, out var meld);
            var presenter = new TablePresenter(table,Controls,_context.Container.Resolve<StoneViewFactory>(),new LocalPlayerContext(0,table.Players.Count), _context.Container.Resolve<TablePlayerNames>());
            presenter.Initialize();
            Assert.That(table.TryOpen(0,OpeningMode.Pairs,new[] {meld}),Is.True);
            Assert.That(Controls.Surface.PairsBoard.transform.GetComponentsInChildren<Stone>().Length,Is.EqualTo(2));
            Assert.That(Controls.Surface.SeriesBoard.transform.GetComponentsInChildren<Stone>().Length,Is.Zero);
            presenter.Dispose();
            yield return null;
            Assert.That(Controls.Surface.PairsBoard.transform.GetComponentsInChildren<Stone>().Length,Is.Zero);
        }

        [UnityTest]
        public IEnumerator MousePressAndReleaseDrawFromTheWholePileSurface()
        {
            var stone = HandViews[0];
            Interaction.TryBeginDrag(stone);
            Interaction.EndDrag(stone, Controls.Surface.DiscardAreas[0].StoneRoot.position);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            yield return null;
            Physics2D.SyncTransforms();
            var camera = _scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).Single(value => value != null);
            var position = camera.WorldToScreenPoint(Controls.Surface.DrawSource.transform.position + Vector3.down * .45f);
            CompleteHandMovement();
            Physics2D.SyncTransforms();
            Assert.That(Controls.Surface.DrawSource.CanDraw, Is.True);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),
                Is.EqualTo(Controls.Surface.DrawSource.gameObject), string.Join(" / ", hits.Select(hit => hit.gameObject.name)));
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
                Assert.That(Controls.Surface.DrawSource.CanDraw, Is.False);
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
            Interaction.EndDrag(stone, Controls.Surface.DiscardAreas[0].StoneRoot.position);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var action = 0; action < 100 && _context.Container.Resolve<TableSession>().ActiveSeat != 0; action++) bot.Act();
            var table = _context.Container.Resolve<TableSession>();
            var camera = _scene.GetRootGameObjects().Select(root => root.GetComponent<Camera>()).Single(value => value != null);
            var data = new PointerEventData(EventSystem.current)
            {
                pointerId = 29,
                position = camera.WorldToScreenPoint(Controls.Surface.DrawSource.transform.position),
                button = PointerEventData.InputButton.Left,
                pointerPressRaycast = new RaycastResult { module = camera.GetComponent<Physics2DRaycaster>() }
            };
            var stockBefore = table.RemainingCount;
            var before = HandViews.Select(view => view.Instance.Id).ToHashSet();
            Controls.Surface.DrawSource.OnPointerDown(data);
            Controls.Surface.DrawSource.OnBeginDrag(data);
            Controls.Surface.DrawSource.OnBeginDrag(data);
            var drawn = HandViews.Single(view => !before.Contains(view.Instance.Id));
            Assert.That(Interaction.IsDragging, Is.True);
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(22));
            data.position = camera.WorldToScreenPoint(SlotWorldPosition(26));
            Controls.Surface.DrawSource.OnDrag(data);
            Controls.Surface.DrawSource.OnEndDrag(data);
            Controls.Surface.DrawSource.OnPointerClick(data);
            Assert.That(Interaction.IsDragging, Is.False);
            Assert.That(table.Players[0].Hand.FindSlot(drawn.Instance.Id), Is.EqualTo(26));
            Assert.That(table.RemainingCount, Is.EqualTo(stockBefore - 1));
            DOTween.Complete(drawn.transform);
            Assert.That(Vector3.Distance(drawn.transform.position, SlotWorldPosition(26)), Is.LessThan(.001f));
        }

        [Test]
        public void RefreshPreservesExistingViewsAndReusesDiscardedSlot()
        {
            var original = HandViews;
            Interaction.TryBeginDrag(original[3]);
            Interaction.EndDrag(original[3], Controls.Surface.DiscardAreas[0].StoneRoot.position);
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
            var deal = new OpeningDealFactory(new DeckFactory(),new SeededStoneShuffler()).Create(definition, rack.Capacity + 1, 42);
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
        [Test]
        public void WholeMeldDragPreservesSpacingAndCannotTriggerADiscard()
        {
            var tiles = Enumerable.Range(0, 3).Select(index => new StoneInstance(5000 + index,
                new StoneData(StoneType.Regular, StoneColor.Red, index + 3))).ToArray();
            var hand = new RackArrangement(tiles, Rack.Capacity);
            var interaction = new RackInteractionController(hand, Rack);
            var presenter = new HandPresenter(hand, _context.Container.Resolve<StoneViewFactory>(), Rack, interaction);
            presenter.Initialize();
            try
            {
                var rules = new MeldRules(_context.Container.Resolve<OkeyRules>(),
                    new StoneInstance(6000, new StoneData(StoneType.Regular, StoneColor.Blue, 13)));
                Assert.That(rules.TryCreate(tiles, MeldKind.Run, out var meld), Is.True);
                interaction.SetGroups(new[] { meld });
                var views = presenter.Views.ToArray();
                foreach (var view in views) DOTween.Complete(view.transform);
                var handle = views[2];
                Assert.That(handle.Selection.IsGroupHandle(handle.transform.TransformPoint(new Vector3(.35f, .35f))), Is.True);
                var offset = views[0].transform.position - handle.transform.position;
                var dropped = false;
                interaction.DropRequested += (stone, position) => dropped = true;
                Assert.That(interaction.TryBeginDrag(handle, true), Is.True);
                interaction.DragTo(handle, SlotWorldPosition(10));
                Assert.That(Vector3.Distance(views[0].transform.position - handle.transform.position, offset), Is.LessThan(.001f));
                interaction.EndDrag(handle, SlotWorldPosition(10));
                Assert.That(tiles.Select(tile => hand.FindSlot(tile.Id)), Is.EqualTo(new[] { 8, 9, 10 }));
                Assert.That(dropped, Is.False);
                Assert.That(interaction.IsDragging, Is.False);
            }
            finally { presenter.Dispose(); }
        }

        [Test]
        public void LocalHandCountIsHiddenAndScoreSheetCanBeOpenedDuringPlay()
        {
            var actions = Controls.Canvas.transform.Find("OpeningActions");
            Assert.That(actions.Find("HandCount").gameObject.activeSelf, Is.False);
            Assert.That(actions.Find("HandIcon").gameObject.activeSelf, Is.False);
            Assert.That(Controls.Hud.StatusText.text, Is.EqualTo(Controls.Texts.YourTurnNotice));
            var view = _context.Container.Resolve<ScoreboardView>();
            view.Show();
            DOTween.CompleteAll();
            Assert.That(view.transform.Find("ScorePanel").gameObject.activeSelf, Is.True);
            Assert.That(view.GetComponentsInChildren<TMP_Text>().Any(text => text.text.Contains("101 OKEY")), Is.True);
            Assert.That(view.GetComponentsInChildren<TMP_Text>().Any(text => text.text.Contains("Hand 1 / 5")), Is.True);
        }

        [Test]
        public void MatchHistoriesAndPartnerColorsStaySeparateAcrossModes()
        {
            var setup = _settings.CreateSetup();
            var matches = new MatchHistory();
            var individual = matches.For(setup);
            Assert.That(matches.For(setup), Is.SameAs(individual));
            setup.TogglePartnerships();
            var partners = matches.For(setup);
            Assert.That(partners, Is.Not.SameAs(individual));
            setup.NextRules();
            Assert.That(matches.For(setup), Is.Not.SameAs(partners));
            var colors = _context.Container.Resolve<SeatColorSettings>();
            Assert.That(colors.For(0, true), Is.Not.EqualTo(colors.For(1, true)));
            Assert.That(new TableOptions(partnerships: true).Partnerships, Is.True);
        }

        [Test]
        public void TimeoutDrawsOnceAndDiscardsTheLowestSafeTile()
        {
            var table = _context.Container.Resolve<TableSession>();
            table.TryDiscard(0, HandViews[0].Instance.Id);
            var bot = _context.Container.Resolve<BotTurnController>();
            for (var step = 0; table.ActiveSeat != 0 && step < 100; step++) bot.Act();
            Assert.That(table.Phase, Is.EqualTo(TurnPhase.AwaitingDraw));
            var stock = table.RemainingCount;
            var count = table.Players[0].Hand.Count;
            _context.Container.Resolve<TurnClockController>().Advance(46);
            Assert.That(table.RemainingCount, Is.EqualTo(stock - 1));
            Assert.That(table.Players[0].Hand.Count, Is.EqualTo(count));
            var discarded = table.Players[0].Discards.Last();
            var options = table.Players[0].Hand.Slots.Where(tile => tile != null).Concat(new[] { discarded });
            var observation = new PlayerObservation(options, table.Melds, table.Players[0].Opening, table.Rules, table.MeldRules);
            Assert.That(discarded.Id, Is.EqualTo(new TimeoutDiscardPolicy().ChooseDiscard(observation)));
            Assert.That(table.ActiveSeat, Is.EqualTo(1));
        }

        [Test]
        public void FinishedHandOpensTheScoreSheetAndRecordsItsWinnerExactlyOnce()
        {
            _context.Container.Resolve<ScoreboardPresenter>().Dispose();
            var deck = Enumerable.Range(0, 30).Select(id => new StoneInstance(id,
                new StoneData(StoneType.Regular, StoneColor.Black, 8))).ToArray();
            deck[0] = new StoneInstance(0, new StoneData(StoneType.Regular, StoneColor.Blue, 13));
            for (var value = 1; value <= 3; value++)
                deck[value] = new StoneInstance(value, new StoneData(StoneType.Regular, StoneColor.Red, value));
            var table = new TableSession(new OkeyRules(4, 3, 1, 13, 1, 1, 101, 202, 101, -101, 101, 2, 2),
                deck, 8, new Okey.Core.Games.Okey101Game());
            var ledger = new MatchScoreLedger(4, 1);
            var view = _context.Container.Resolve<ScoreboardView>();
            var presenter = new ScoreboardPresenter(table, ledger, view, _context.Container.Resolve<TablePlayerNames>(),
                new LocalPlayerContext(0, 4), _context.Container.Resolve<SeatColorSettings>(), _context.Container.Resolve<ITableNavigation>());
            presenter.Initialize();
            try
            {
                table.MeldRules.TryCreate(deck.Skip(1).Take(3), MeldKind.Run, out var meld);
                Assert.That(table.TryOpen(0, OpeningMode.Series, new[] { meld }), Is.True);
                Assert.That(table.TryDiscard(0, 4), Is.True);
                Assert.That(view.transform.Find("ScorePanel").gameObject.activeSelf, Is.True);
                Assert.That(view.GetComponentsInChildren<TMP_Text>().Any(text => text.text == "-101"), Is.True);
                Assert.That(view.GetComponentsInChildren<TMP_Text>().Any(text => text.text.Contains("Match complete")), Is.True);
                Assert.That(ledger.Rounds.Count, Is.EqualTo(1));
                Assert.That(ledger.Record(table), Is.False);
            }
            finally { presenter.Dispose(); }
        }

        [Test]
        public void FeedbackQueuesNoticesInsteadOfOverwritingPenalties()
        {
            var feedback = Controls.Feedback;
            var field = typeof(TableFeedbackView).GetField("_feedback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            for (var index = 0; index < 20 && field.GetValue(feedback) is Tween; index++) ((Tween)field.GetValue(feedback)).Complete();
            var text = (TMP_Text)new SerializedObject(feedback).FindProperty("_feedbackText").objectReferenceValue;
            feedback.Show("Playable discard +101", false);
            feedback.Show("Your turn", true);
            Assert.That(text.text, Is.EqualTo("Playable discard +101"));
            var panel = (CanvasGroup)new SerializedObject(feedback).FindProperty("_panel").objectReferenceValue;
            ((Tween)field.GetValue(feedback)).Goto(.3f, false);
            Assert.That(panel.alpha, Is.EqualTo(1));
            Assert.That(panel.blocksRaycasts, Is.False);
            ((Tween)field.GetValue(feedback)).Complete();
            Assert.That(text.text, Is.EqualTo("Your turn"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TrialReturnsBeforeDrawingOrTimeout(bool timeout)
        {
            var table = _context.Container.Resolve<TableSession>();
            table.TryDiscard(0, HandViews[0].Instance.Id);
            var bots = _context.Container.Resolve<BotTurnController>();
            for (var step = 0; table.ActiveSeat != 0 && step < 100; step++) bots.Act();
            var borrowed = table.Players[3].Discards.Last(); var stock = table.RemainingCount;
            Controls.Surface.DiscardAreas[3].OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            Assert.That(table.PendingDiscard.Id, Is.EqualTo(borrowed.Id));
            Assert.That(Controls.Surface.DrawSource.CanDraw, Is.True);
            Assert.That(Controls.Hud.StatusText.text, Is.EqualTo(Controls.Texts.DiscardTrialStatus));
            if (timeout) _context.Container.Resolve<TurnClockController>().Advance(46);
            else _context.Container.Resolve<TurnController>().Draw();
            Assert.That(table.PendingDiscard, Is.Null);
            Assert.That(table.Players[3].Discards.Last().Id, Is.EqualTo(borrowed.Id));
            Assert.That(table.RemainingCount, Is.EqualTo(stock - 1));
            Assert.That(table.ActiveSeat, Is.EqualTo(timeout ? 1 : 0));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OpenPlayerCanDragAPairOrReplaceAPublicJoker(bool reclaim)
        {
            var container = _context.Container;
            container.Resolve<TurnController>().Dispose();
            container.Resolve<TableHudPresenter>().Dispose();
            container.Resolve<HandPresenter>().Dispose();
            container.Resolve<TablePresenter>().Dispose();
            container.Resolve<TableExperiencePresenter>().Dispose();
            StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
                new StoneInstance(id, new StoneData(StoneType.Regular, color, value));
            var deck = Enumerable.Range(0, 50).Select(id => Tile(id, 6, StoneColor.Black)).ToArray();
            deck[0] = Tile(0, 13, StoneColor.Blue);
            deck[1] = Tile(1, 3); deck[2] = Tile(2, 4); deck[3] = Tile(3, 5);
            deck[4] = Tile(4, 7); deck[5] = Tile(5, 7); deck[6] = Tile(6, 9);
            deck[8] = Tile(8, 8); deck[9] = Tile(9, 1, StoneColor.Blue); deck[10] = Tile(10, 10);
            deck[11] = Tile(11, 11, StoneColor.Yellow); deck[12] = Tile(12, 11, StoneColor.Yellow);
            var session = new TableSession(new OkeyRules(2, 7, 1, 13, 1, 1, 101, 202, 101, -101, 101, 2, 2),
                deck, Rack.Capacity, new Okey.Core.Games.Okey101Game(), startingSeat: 1,
                options: new TableOptions(escalatingOpenings: !reclaim));
            var local = new LocalPlayerContext(0, 2);
            var interaction = new RackInteractionController(session.Players[0].Layout, Rack);
            var factory = container.Resolve<StoneViewFactory>();
            var hand = new HandPresenter(session.Players[0].Hand, factory, Rack, interaction);
            var table = new TablePresenter(session, Controls, factory, local, container.Resolve<TablePlayerNames>());
            var controller = new TurnController(session, hand, interaction, Controls, table, Rack, local);
            var hud = new TableHudPresenter(session, Controls, hand, interaction, table, Rack, local,
                container.Resolve<TablePlayerNames>(), new RackMeldEvaluator());
            var experience = new TableExperiencePresenter(session, Controls, container.Resolve<TablePlayerNames>(), local,
                container.Resolve<SeatColorSettings>());
            try
            {
                hand.Initialize(); table.Initialize(); controller.Initialize(); hud.Initialize(); experience.Initialize();
                session.MeldRules.TryCreate(reclaim ? new[] { deck[8], deck[9], deck[10] } : new[] { deck[11], deck[12] },
                    reclaim ? MeldKind.Run : MeldKind.Pair, out var opponentMeld);
                Assert.That(session.TryOpen(1, reclaim ? OpeningMode.Series : OpeningMode.Pairs, new[] { opponentMeld }), Is.True);
                session.TryDiscard(1, 13); session.TryDraw(0);
                session.MeldRules.TryCreate(new[] { deck[1], deck[2], deck[3] }, MeldKind.Run, out var localMeld);
                Assert.That(session.TryOpen(0, OpeningMode.Series, new[] { localMeld }), Is.True);
                DOTween.CompleteAll();
                Assert.That(Controls.Actions[0].Button.interactable, Is.False);
                Controls.Actions[0].Button.onClick.Invoke();
                Assert.That(session.Players[0].Penalty, Is.Zero);
                if (!reclaim)
                {
                    Assert.That(Controls.Surface.SeriesBoard.ThresholdText.text, Is.EqualTo("13"));
                    Assert.That(Controls.Surface.PairsBoard.ThresholdText.text, Is.EqualTo("2"));
                }
                var handCount = session.Players[0].Hand.Count;
                var dragged = hand.Views.Single(view => view.Instance.Id == (reclaim ? 6 : 5));
                var position = reclaim ? Controls.Surface.SeriesBoard.GetComponentsInChildren<Stone>()
                    .Single(view => view.Instance.Id == 9).transform.position :
                    Controls.Surface.PairsBoard.transform.TransformPoint(new Vector3(.5f, -.5f));
                Assert.That(interaction.TryBeginDrag(dragged, !reclaim), Is.True);
                interaction.DragTo(dragged, position);
                interaction.EndDrag(dragged, position);
                Assert.That(interaction.IsDragging, Is.False);
                Assert.That(session.Players[0].Opening, Is.EqualTo(OpeningMode.Series));
                if (reclaim)
                {
                    Assert.That(session.Players[1].Penalty, Is.EqualTo(101));
                    Assert.That(hand.Views.Any(view => view.Instance.Id == 9), Is.True);
                    Assert.That(Controls.Surface.SeriesBoard.GetComponentsInChildren<Stone>().Any(view => view.Instance.Id == 9), Is.False);
                    Assert.That(session.Melds[0].Tiles[1].Stone.Id, Is.EqualTo(6));
                    Assert.That(session.Players[0].Hand.Count, Is.EqualTo(handCount));
                }
                else
                {
                    Assert.That(session.Melds.Count(meld => meld.Kind == MeldKind.Pair), Is.EqualTo(2));
                    Assert.That(session.Players[0].Hand.Count, Is.EqualTo(handCount - 2));
                    Assert.That(Controls.Surface.PairsBoard.GetComponentsInChildren<Stone>().Length, Is.EqualTo(4));
                }
            }
            finally { experience.Dispose(); controller.Dispose(); hud.Dispose(); table.Dispose(); hand.Dispose(); }
        }

        [Test]
        public void PreviousDiscardCanBeDraggedIntoTheRackWithoutDrawingOrReturningItOnRelease()
        {
            var table = _context.Container.Resolve<TableSession>();
            table.TryDiscard(0, HandViews[0].Instance.Id);
            var bots = _context.Container.Resolve<BotTurnController>();
            for (var step = 0; table.ActiveSeat != 0 && step < 100; step++) bots.Act();
            var previous = table.Players[3].Discards.Last(); var stock = table.RemainingCount;
            var source = Controls.Surface.DiscardAreas[3];
            var camera = _context.Container.Resolve<Camera>();
            var data = new PointerEventData(EventSystem.current) { pointerId = 13,
                button = PointerEventData.InputButton.Left, position = camera.WorldToScreenPoint(source.StoneRoot.position),
                pointerPressRaycast = new RaycastResult { module = camera.GetComponent<Physics2DRaycaster>() } };
            source.OnPointerDown(data); source.OnBeginDrag(data); source.OnBeginDrag(data);
            Assert.That(Interaction.IsDragging, Is.True);
            Assert.That(table.PendingDiscard.Id, Is.EqualTo(previous.Id));
            data.position = camera.WorldToScreenPoint(SlotWorldPosition(28));
            source.OnDrag(data); source.OnEndDrag(data); source.OnPointerClick(data);
            Assert.That(Interaction.IsDragging, Is.False);
            Assert.That(table.Players[0].Hand.FindSlot(previous.Id), Is.EqualTo(28));
            Assert.That(table.PendingDiscard.Id, Is.EqualTo(previous.Id));
            Assert.That(table.RemainingCount, Is.EqualTo(stock));
            Assert.That(table.TryDiscard(0, previous.Id), Is.False);
        }

    }
}
