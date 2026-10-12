using Okey.Presentation.Tables;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Okey.Composition;
using Okey.Configuration;
using Okey.Core.Dealing;
using Okey.Core.Games;
using Okey.Core.Rules;
using Okey.Core.Stones;
using Okey.Core.Tables;
using Okey.Presentation.Menus;
using Okey.Presentation.Racks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;

namespace Okey.Tests.PlayMode
{
    public sealed class ClassicOkeyCompositionTests
    {
        private GameSetup _settings;
        private TableSettings _defaults;
        private int _originalIndex;
        private bool _originalEscalating;
        private bool _randomStart;
        private static SceneContext Context(string name) => SceneManager.GetSceneByName(name).GetRootGameObjects()
            .Select(root => root.GetComponent<SceneContext>()).Single(value => value != null);
        private void Set(string field, bool value)
        {
            var serial = new SerializedObject(_defaults);
            serial.FindProperty(field).boolValue = value;
            serial.ApplyModifiedPropertiesWithoutUndo();
        }
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var existing = Object.FindFirstObjectByType<GameFlowState>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            _defaults = AssetDatabase.LoadAssetAtPath<TableSettings>("Assets/Settings/Dealing/Standard101Table.asset");
            _randomStart = _defaults.RandomStartingSeat;
            Set("_randomStartingSeat",false);
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Additive);
            yield return null;
            _settings = Context("MainMenu").Container.Resolve<GameSetup>();
            _originalIndex = _settings.RuleIndex;
            _originalEscalating = _settings.EscalatingOpenings;
            while (_settings.RuleIndex != 0) _settings.NextRules();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var flow = Object.FindFirstObjectByType<GameFlowState>();
            if (flow != null) Object.DestroyImmediate(flow.gameObject);
            while (_settings.RuleIndex != _originalIndex) _settings.NextRules();
            if (_settings.EscalatingOpenings != _originalEscalating) _settings.ToggleEscalatingOpenings();
            Set("_randomStartingSeat",_randomStart);
            foreach(var name in new[] {"MainMenu","SampleScene"})
            {
                var scene=SceneManager.GetSceneByName(name);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (SceneManager.sceneCount == 1) SceneManager.CreateScene("ClassicTestCleanup");
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
        [UnityTest]
        public IEnumerator MenuSelectsClassicRulesAndComposesAClosedTable()
        {
            var view=Context("MainMenu").Container.Resolve<NewGameMenuView>();
            var menu=new SerializedObject(view);
            ((Button)menu.FindProperty("_ruleButton").objectReferenceValue).onClick.Invoke();
            Assert.That(_settings.ActiveProfile,Is.TypeOf<ClassicOkeyRuleProfile>());
            Assert.That(((TMP_Text)menu.FindProperty("_ruleText").objectReferenceValue).text,Is.EqualTo("CLASSIC OKEY"));
            Assert.That(((Button)menu.FindProperty("_escalatingButton").objectReferenceValue).interactable,Is.False);
            if (!_settings.EscalatingOpenings) _settings.ToggleEscalatingOpenings();
            ((Button)menu.FindProperty("_playButton").objectReferenceValue).onClick.Invoke();
            var deadline=Time.realtimeSinceStartup+15;
            while(SceneManager.GetActiveScene().name!="SampleScene" && Time.realtimeSinceStartup<deadline) yield return null;
            yield return null;
            var context=Context("SampleScene");
            var table=context.Container.Resolve<TableSession>();
            var scores = context.Container.Resolve<ScoreboardView>();
            scores.Show();
            Assert.That(scores.GetComponentsInChildren<TMP_Text>().Any(text => text.text.Contains("CLASSIC OKEY")), Is.True);
            Assert.That(scores.GetComponentsInChildren<TMP_Text>().Any(text => text.text == "PENALTY" || text.text == "OPENED"), Is.False);
            scores.transform.Find("ScorePanel").gameObject.SetActive(false);
            Assert.That(table.Game,Is.TypeOf<ClassicOkeyGame>());
            Assert.That(context.Container.Resolve<IPlayerStrategy>(),Is.TypeOf<ClassicOkeyStrategy>());
            Assert.That(table.Rules.HandSize,Is.EqualTo(14));
            Assert.That(table.Players.Select(player=>player.Hand.Count),Is.EqualTo(new[] {15,14,14,14}));
            Assert.That(table.RemainingCount,Is.EqualTo(48));
            Assert.That(table.Options.EscalatingOpenings,Is.False);
            var controls=context.Container.Resolve<TableView>();
            var fields=new SerializedObject(controls.Surface);
            Assert.That(((GameObject)fields.FindProperty("_classicPanel").objectReferenceValue).activeSelf,Is.True);
            Assert.That(controls.Actions[0].Label.text,Is.EqualTo("FINISH"));
            Assert.That(controls.Actions[1].Label.text,Is.EqualTo("PAIRS"));
            var visuals=fields.FindProperty("_publicMeldVisuals");
            Assert.That(visuals.arraySize,Is.GreaterThanOrEqualTo(4));
            for(var index=0;index<visuals.arraySize;index++)
                Assert.That(((GameObject)visuals.GetArrayElementAtIndex(index).objectReferenceValue).activeSelf,Is.False);
            var hand=context.Container.Resolve<HandPresenter>();
            var first=hand.Views.First();
            table.TryDiscard(0,first.Instance.Id);
            Assert.That(table.Melds,Is.Empty);
            Assert.That(table.Players[0].Penalty,Is.Zero);
            var previous=table.Players[0].Discards.Last();
            Assert.That(table.TryTakeDiscard(1),Is.True);
            Assert.That(table.Players[1].Hand.FindSlot(previous.Id),Is.GreaterThanOrEqualTo(0));
            Assert.That(table.Players[1].Hand.Count,Is.EqualTo(15));
        }
        [UnityTest]
        public IEnumerator BotFinishesAClosedHandAndExposesAllFourteenTiles()
        {
            _settings.NextRules();
            yield return SceneManager.LoadSceneAsync("SampleScene",LoadSceneMode.Additive);
            yield return null;
            var container = Context("SampleScene").Container;
            container.Resolve<TickableManager>().Remove(container.Resolve<BotTurnController>());
            container.Resolve<TickableManager>().Remove(container.Resolve<TurnClockController>());
            container.Resolve<TurnController>().Dispose();
            container.Resolve<TableHudPresenter>().Dispose();
            container.Resolve<HandPresenter>().Dispose();
            container.Resolve<TablePresenter>().Dispose();
            StoneInstance Tile(int id, int value, StoneColor color = StoneColor.Red) =>
                new StoneInstance(id,new StoneData(StoneType.Regular,color,value));
            var winning = Enumerable.Range(1,14).Select(id=>Tile(id,(id+1)/2)).Append(Tile(15,13,StoneColor.Black));
            var deck = new[] {Tile(1000,12,StoneColor.Black)}
                .Concat(Enumerable.Range(50,14).Select(id=>Tile(id,id%13+1,StoneColor.Blue)))
                .Concat(winning)
                .Concat(Enumerable.Range(100,76).Select(id=>Tile(id,id%13+1,(StoneColor)(id%4)))).ToArray();
            var table = new TableSession(_settings.CreateRules(),deck,28,startingSeat:1,game:new ClassicOkeyGame(new MeldPlanner()));
            var controls = container.Resolve<TableView>();
            var presenter = new TablePresenter(table,controls,container.Resolve<StoneViewFactory>(),new LocalPlayerContext(0,table.Players.Count), container.Resolve<TablePlayerNames>());
            presenter.Initialize();
            Assert.That(controls.Surface.WinningHandRoot.childCount,Is.Zero);
            var bot = new BotTurnController(table,new BotDecisionPolicy(new ClassicOkeyStrategy(new MeldPlanner()),new SeededRandomSource(7)),_settings.CreateBotPreferences(),new SeededRandomSource(7),new LocalPlayerContext(0,table.Players.Count));
            bot.Act();
            Assert.That(table.Winner,Is.EqualTo(1));
            Assert.That(table.Phase,Is.EqualTo(TurnPhase.Finished));
            Assert.That(table.Players[1].Hand.Count,Is.EqualTo(14));
            Assert.That(table.Melds,Is.Empty);
            Assert.That(table.WinningHand.TileCount,Is.EqualTo(14));
            Assert.That(controls.Surface.WinningHandRoot.childCount,Is.EqualTo(14));
            Assert.That(table.Players[1].Opening,Is.EqualTo(OpeningMode.Pairs));
            presenter.Dispose();
        }
        [Test]
        public void ReturningTo101RestoresItsProfileWithoutLosingRisingOpeningChoice()
        {
            var view=Context("MainMenu").Container.Resolve<NewGameMenuView>();
            var menu=new SerializedObject(view);
            var button=(Button)menu.FindProperty("_ruleButton").objectReferenceValue;
            if (!_settings.EscalatingOpenings) _settings.ToggleEscalatingOpenings();
            button.onClick.Invoke();
            Assert.That(_settings.CreateRules().HandSize,Is.EqualTo(14));
            Assert.That(_settings.CreateOptions().EscalatingOpenings,Is.False);
            button.onClick.Invoke();
            Assert.That(_settings.CreateRules().HandSize,Is.EqualTo(21));
            Assert.That(_settings.CreateRules().SeriesThreshold,Is.EqualTo(101));
            Assert.That(_settings.CreateOptions().EscalatingOpenings,Is.True);
            Assert.That(((Button)menu.FindProperty("_escalatingButton").objectReferenceValue).interactable,Is.True);
        }
    }
}
