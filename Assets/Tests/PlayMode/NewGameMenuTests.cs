using Okey.Presentation.Tables;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Okey.Composition;
using Okey.Configuration;
using Okey.Core.Tables;
using Okey.Core.Rooms;
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
    public sealed class NewGameMenuTests
    {
        private GameSetup _settings;
        private bool _partnerships;
        private bool _humanlike;
        private bool _escalating;
        private NewGameMenuView _view;
        private SceneContext _context;
        private RoomMenuView Rooms => _context.Container.Resolve<RoomMenuView>();
        private Button RoomButton(string field) => (Button)new SerializedObject(Rooms).FindProperty(field).objectReferenceValue;
        private TMP_InputField Input(string field) => (TMP_InputField)new SerializedObject(Rooms).FindProperty(field).objectReferenceValue;
        private static SceneContext Context(Scene scene) => scene.GetRootGameObjects()
            .Select(root => root.GetComponent<SceneContext>()).Single(value => value != null);
        private Button Button(string field) => (Button)new SerializedObject(_view).FindProperty(field).objectReferenceValue;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (ProjectContext.HasInstance) UnityEngine.Object.DestroyImmediate(ProjectContext.Instance.gameObject);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
            yield return null;
            _context = Context(SceneManager.GetSceneByName("MainMenu"));
            _settings = _context.Container.Resolve<GameSetup>();
            _partnerships = _settings.Partnerships;
            _humanlike = _settings.HumanlikeBots;
            _escalating = _settings.EscalatingOpenings;
            _view = _context.Container.Resolve<NewGameMenuView>();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance) UnityEngine.Object.DestroyImmediate(ProjectContext.Instance.gameObject);
            if (_settings.Partnerships != _partnerships) _settings.TogglePartnerships();
            if (_settings.HumanlikeBots != _humanlike) _settings.ToggleBotMode();
            if (_settings.EscalatingOpenings != _escalating) _settings.ToggleEscalatingOpenings();
            foreach (var name in new[] { "MainMenu", "GameTable" })
            {
                var scene = SceneManager.GetSceneByName(name);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (SceneManager.sceneCount == 1) SceneManager.CreateScene("MenuTestCleanup");
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
        [Test]
        public void MenuChoicesUpdateSettingsAndVisibleLabels()
        {
            Button("_partnershipButton").onClick.Invoke();
            Button("_botModeButton").onClick.Invoke();
            Button("_escalatingButton").onClick.Invoke();
            Assert.That(_settings.Partnerships, Is.EqualTo(!_partnerships));
            Assert.That(_settings.HumanlikeBots, Is.EqualTo(!_humanlike));
            Assert.That(_settings.EscalatingOpenings, Is.EqualTo(!_escalating));
            var serial = new SerializedObject(_view);
            Assert.That(((TMP_Text)serial.FindProperty("_partnershipText").objectReferenceValue).text,
                Is.EqualTo(_settings.Partnerships ? "PARTNERS" : "INDIVIDUAL"));
            Assert.That(((TMP_Text)serial.FindProperty("_botModeText").objectReferenceValue).text,
                Is.EqualTo(_settings.HumanlikeBots ? "HUMANLIKE" : "FAST"));
            Assert.That(((TMP_Text)serial.FindProperty("_escalatingText").objectReferenceValue).text,
                Is.EqualTo(_settings.EscalatingOpenings ? "RISING" : "FIXED"));
        }
        [Test]
        public void DisposingTheMenuControllerStopsSettingsChanges()
        {
            _context.Container.Resolve<NewGameMenuController>().Dispose();
            Button("_botModeButton").onClick.Invoke();
            Assert.That(_settings.HumanlikeBots, Is.EqualTo(_humanlike));
        }
        [UnityTest]
        public IEnumerator StartingTheConfiguredRoomLoadsTheTableAndNewGameReturnsToTheMenu()
        {
            Button("_partnershipButton").onClick.Invoke();
            Button("_escalatingButton").onClick.Invoke();
            var flow = _context.Container.Resolve<GameFlowState>();
            var partners = _settings.Partnerships;
            var escalating = _settings.EscalatingOpenings;
            RoomButton("_createButton").onClick.Invoke();
            Button("_handsButton").onClick.Invoke();
            var hands = _settings.MatchRounds;
            Button("_createRoomButton").onClick.Invoke();
            Assert.That(RoomButton("_start").interactable, Is.False);
            for (var seat = 1; seat < _settings.PlayerCount; seat++) RoomButton("_addBot").onClick.Invoke();
            Assert.That(RoomButton("_start").interactable, Is.True);
            RoomButton("_start").onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "GameTable" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameTable"));
            yield return null;
            var tableContext = Context(SceneManager.GetSceneByName("GameTable"));
            Assert.That(tableContext.Container.Resolve<GameFlowState>(), Is.SameAs(flow));
            var table = tableContext.Container.Resolve<TableSession>();
            Assert.That(tableContext.Container.Resolve<MatchScoreLedger>().RoundLimit, Is.EqualTo(hands));
            Assert.That(tableContext.Container.Resolve<TablePlayerNames>().ForSeat(1), Is.EqualTo(tableContext.Container.Resolve<LocalRoomDirectory>().Current.Members[1].DisplayName));
            Assert.That(table.Options.Partnerships, Is.EqualTo(partners));
            Assert.That(table.Options.EscalatingOpenings, Is.EqualTo(escalating));
            var controls = tableContext.Container.Resolve<TableView>();
            ((Button)new SerializedObject(controls.Options).FindProperty("_newHandButton").objectReferenceValue).onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            yield return null;
            Assert.That(Context(SceneManager.GetSceneByName("MainMenu")).Container.Resolve<GameFlowState>(), Is.SameAs(flow));
            Assert.That(_settings.Partnerships, Is.EqualTo(partners));
            Assert.That(_settings.EscalatingOpenings, Is.EqualTo(escalating));
        }
        [Test]
        public void PrivateRoomJoinValidatesThePasswordAndLoadsItsSettings()
        {
            RoomButton("_joinButton").onClick.Invoke();
            Input("_roomNumber").text = "102303"; Input("_joinPassword").text = "wrong";
            RoomButton("_joinSubmit").onClick.Invoke();
            Assert.That(_context.Container.Resolve<LocalRoomDirectory>().Current, Is.Null);
            Assert.That(((TMP_Text)new SerializedObject(Rooms).FindProperty("_message").objectReferenceValue).text, Does.Contain("password"));
            Input("_joinPassword").text = "coffee";
            RoomButton("_joinSubmit").onClick.Invoke();
            Assert.That(_settings.Partnerships, Is.True);
            Assert.That(_settings.EscalatingOpenings, Is.True);
            Assert.That(_context.Container.Resolve<LocalRoomDirectory>().Current.Number, Is.EqualTo(102303));
            Assert.That(RoomButton("_start").interactable, Is.False);
            RoomButton("_addBot").onClick.Invoke();
            Assert.That(RoomButton("_start").interactable, Is.True);
        }
        [Test]
        public void JoiningClassicThenReturningToCreateRefreshesTheVisibleRules()
        {
            RoomButton("_joinButton").onClick.Invoke(); Input("_roomNumber").text = "102302";
            RoomButton("_joinSubmit").onClick.Invoke();
            Assert.That(_settings.ActiveProfile.AllowsPublicMelds, Is.False);
            Assert.That(RoomButton("_start").interactable, Is.True);
            var backs = new SerializedObject(Rooms).FindProperty("_backButtons");
            ((Button)backs.GetArrayElementAtIndex(2).objectReferenceValue).onClick.Invoke();
            RoomButton("_createButton").onClick.Invoke();
            Assert.That(((TMP_Text)new SerializedObject(_view).FindProperty("_ruleText").objectReferenceValue).text, Is.EqualTo(_settings.ActiveProfile.Label));
        }
        [Test]
        public void ClickingAnOpenRoomJoinsImmediatelyAndRemovingAPlayerDisablesStart()
        {
            RoomButton("_joinButton").onClick.Invoke();
            var row = Rooms.GetComponentsInChildren<RoomListItemView>().Single(item => item.name.Contains("Clone") &&
                item.GetComponentInChildren<TMP_Text>().text.Contains("102302"));
            row.GetComponent<Button>().onClick.Invoke();
            Assert.That(_context.Container.Resolve<LocalRoomDirectory>().Current.Number, Is.EqualTo(102302));
            var fields = new SerializedObject(Rooms).FindProperty("_members");
            var local = (RoomSeatView)fields.GetArrayElementAtIndex(0).objectReferenceValue;
            var other = (RoomSeatView)fields.GetArrayElementAtIndex(1).objectReferenceValue;
            Assert.That(local.GetComponentInChildren<Button>(true).gameObject.activeSelf, Is.False);
            other.GetComponentInChildren<Button>().onClick.Invoke();
            Assert.That(RoomButton("_start").interactable, Is.False);
            Assert.That(_context.Container.Resolve<LocalRoomDirectory>().Current.Members.Count, Is.EqualTo(3));
            RoomButton("_addBot").onClick.Invoke();
            Assert.That(RoomButton("_start").interactable, Is.True);
        }
        [Test]
        public void SelectingAPrivateRoomOnlySelectsItsNumberUntilThePasswordIsSubmitted()
        {
            RoomButton("_joinButton").onClick.Invoke();
            Rooms.GetComponentsInChildren<RoomListItemView>().Single(item => item.GetComponentInChildren<TMP_Text>().text.Contains("102303"))
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(Input("_roomNumber").text, Is.EqualTo("102303"));
            Assert.That(_context.Container.Resolve<LocalRoomDirectory>().Current, Is.Null);
            Input("_joinPassword").text = "coffee";
            RoomButton("_joinSubmit").onClick.Invoke();
            Assert.That(_context.Container.Resolve<LocalRoomDirectory>().Current.Number, Is.EqualTo(102303));
        }
        [Test]
        public void JokerReclaimChoiceUpdatesTheNextTableAndIsUnavailableInClassicOkey()
        {
            var original = _settings.AllowJokerReclaim;
            Button("_jokerReclaimButton").onClick.Invoke();
            Assert.That(_settings.AllowJokerReclaim, Is.EqualTo(!original));
            Assert.That(_settings.CreateOptions().AllowJokerReclaim, Is.EqualTo(!original));
            Button("_ruleButton").onClick.Invoke();
            Assert.That(Button("_jokerReclaimButton").interactable, Is.False);
            Assert.That(_settings.CreateOptions().AllowJokerReclaim, Is.False);
            Button("_ruleButton").onClick.Invoke();
            Assert.That(Button("_jokerReclaimButton").interactable, Is.True);
        }

    }
}
