using Okey.Presentation.Tables;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Okey.Composition;
using Okey.Configuration;
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
    public sealed class NewGameMenuTests
    {
        private GameSetup _settings;
        private bool _partnerships;
        private bool _humanlike;
        private bool _escalating;
        private NewGameMenuView _view;
        private SceneContext _context;
        private static SceneContext Context(Scene scene) => scene.GetRootGameObjects()
            .Select(root => root.GetComponent<SceneContext>()).Single(value => value != null);
        private Button Button(string field) => (Button)new SerializedObject(_view).FindProperty(field).objectReferenceValue;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var existing = Object.FindFirstObjectByType<GameFlowState>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
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
            var flow = Object.FindFirstObjectByType<GameFlowState>();
            if (flow != null) Object.DestroyImmediate(flow.gameObject);
            if (_settings.Partnerships != _partnerships) _settings.TogglePartnerships();
            if (_settings.HumanlikeBots != _humanlike) _settings.ToggleBotMode();
            if (_settings.EscalatingOpenings != _escalating) _settings.ToggleEscalatingOpenings();
            foreach (var name in new[] { "MainMenu", "SampleScene" })
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
        public IEnumerator PlayStartsTheConfiguredTableAndNewGameReturnsToTheMenu()
        {
            Button("_partnershipButton").onClick.Invoke();
            Button("_escalatingButton").onClick.Invoke();
            var partners = _settings.Partnerships;
            var escalating = _settings.EscalatingOpenings;
            Button("_playButton").onClick.Invoke();
            var deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "SampleScene" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            yield return null;
            var tableContext = Context(SceneManager.GetSceneByName("SampleScene"));
            var table = tableContext.Container.Resolve<TableSession>();
            Assert.That(table.Options.Partnerships, Is.EqualTo(partners));
            Assert.That(table.Options.EscalatingOpenings, Is.EqualTo(escalating));
            var controls = tableContext.Container.Resolve<TableView>();
            ((Button)new SerializedObject(controls.Options).FindProperty("_newHandButton").objectReferenceValue).onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            yield return null;
            Assert.That(_settings.Partnerships, Is.EqualTo(partners));
            Assert.That(_settings.EscalatingOpenings, Is.EqualTo(escalating));
        }
    }
}
