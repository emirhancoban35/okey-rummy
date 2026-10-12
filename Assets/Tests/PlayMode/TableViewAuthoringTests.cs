using System.Collections;
using System.Linq;
using NUnit.Framework;
using Okey.Composition;
using Okey.Presentation.Tables;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;

namespace Okey.Tests.PlayMode
{
    public sealed class TableViewAuthoringTests
    {
        private Scene _scene;
        private SceneContext _context;
        private TableView _view;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("GameTable", LoadSceneMode.Additive);
            yield return null;
            _scene = SceneManager.GetSceneByName("GameTable");
            _context = _scene.GetRootGameObjects().Select(root => root.GetComponent<SceneContext>()).Single(value => value != null);
            _view = _context.Container.Resolve<TableView>();
            _context.Container.Resolve<TickableManager>().Remove(_context.Container.Resolve<BotTurnController>());
            _context.Container.Resolve<TickableManager>().Remove(_context.Container.Resolve<TurnClockController>());
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance) UnityEngine.Object.DestroyImmediate(ProjectContext.Instance.gameObject);
            if (SceneManager.sceneCount == 1) SceneManager.CreateScene("ViewTestCleanup");
            yield return SceneManager.UnloadSceneAsync(_scene);
        }
        [Test]
        public void RenderingPreservesPrefabTypographyAndRectTransformEdits()
        {
            var label = _view.Actions[0].ProgressText;
            var originalSize = label.fontSize;
            var originalPosition = label.rectTransform.anchoredPosition;
            var originalDimensions = label.rectTransform.sizeDelta;
            label.fontSize = 31;
            label.rectTransform.anchoredPosition = new Vector2(17, -9);
            label.rectTransform.sizeDelta = new Vector2(211, 54);
            try
            {
                _context.Container.Resolve<TableHudPresenter>().Refresh();
                Assert.That(label.fontSize, Is.EqualTo(31));
                Assert.That(label.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(17, -9)));
                Assert.That(label.rectTransform.sizeDelta, Is.EqualTo(new Vector2(211, 54)));
            }
            finally
            {
                label.fontSize = originalSize;
                label.rectTransform.anchoredPosition = originalPosition;
                label.rectTransform.sizeDelta = originalDimensions;
            }
        }
        [Test]
        public void RenderingPreservesTheAuthoredGameTitle()
        {
            var title = _view.Canvas.transform.Find("Brand/GameTitle").GetComponent<TMP_Text>();
            title.text = "MY TABLE";
            title.fontSize = 27;
            _context.Container.Resolve<TableHudPresenter>().Refresh();
            Assert.That(title.text, Is.EqualTo("MY TABLE"));
            Assert.That(title.fontSize, Is.EqualTo(27));
        }

        [Test]
        public void NumberTemplatesComeFromThePrefabTextComponents()
        {
            var prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/GameControls.prefab");
            try
            {
                var hud = prefab.GetComponent<TableView>().Hud;
                var fields = new SerializedObject(hud);
                ((TMP_Text)fields.FindProperty("_handText").objectReferenceValue).text = "Remaining: {0}";
                typeof(TableHudView).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(hud, null);
                hud.RenderValues(31, 20, 0);
                Assert.That(((TMP_Text)fields.FindProperty("_handText").objectReferenceValue).text, Is.EqualTo("Remaining: 31"));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        [Test]
        public void OpeningThresholdHeadersAreVisibleAndRemainEditable()
        {
            foreach (var board in new[] { _view.Surface.SeriesBoard, _view.Surface.PairsBoard })
            {
                var text = board.ThresholdText;
                Assert.That(text.isActiveAndEnabled, Is.True);
                Assert.That(text.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(text.raycastTarget, Is.False);
                var original = text.rectTransform.localPosition;
                text.rectTransform.localPosition = original + Vector3.right * .1f;
                board.RenderThreshold("137");
                Assert.That(text.text, Is.EqualTo("137"));
                Assert.That(text.rectTransform.localPosition, Is.EqualTo(original + Vector3.right * .1f));
                text.rectTransform.localPosition = original;
            }
        }

    }
}
