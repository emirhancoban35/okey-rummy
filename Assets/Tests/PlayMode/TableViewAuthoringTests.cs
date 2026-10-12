using System.Collections;
using System.Linq;
using NUnit.Framework;
using Okey.Composition;
using Okey.Presentation.Tables;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
        public void AvailabilityAndHoverPreserveNativeRendererColors()
        {
            var custom = new Color(.24f, .63f, .82f, .9f);
            foreach (var area in _view.Surface.DiscardAreas)
            {
                var highlight = (GameObject)new SerializedObject(area).FindProperty("_availableHighlight").objectReferenceValue;
                var renderer = highlight.GetComponent<SpriteRenderer>();
                renderer.color = custom;
                area.SetAvailable(true);
                Assert.That(highlight.activeSelf, Is.True);
                Assert.That(renderer.color, Is.EqualTo(custom));
                area.SetAvailable(false);
                Assert.That(highlight.activeSelf, Is.False);
                Assert.That(renderer.color, Is.EqualTo(custom));
            }
            var source = _view.Surface.DrawSource;
            var fields = new SerializedObject(source);
            var ready = (GameObject)fields.FindProperty("_readyHighlight").objectReferenceValue;
            var hover = (GameObject)fields.FindProperty("_hoverHighlight").objectReferenceValue;
            ready.GetComponent<SpriteRenderer>().color = custom;
            hover.GetComponent<SpriteRenderer>().color = custom;
            source.CanDraw = true;
            source.OnPointerEnter(new PointerEventData(EventSystem.current));
            Assert.That(hover.activeSelf, Is.True);
            Assert.That(ready.activeSelf, Is.False);
            source.CanDraw = false;
            Assert.That(hover.activeSelf, Is.False);
            source.CanDraw = true;
            Assert.That(hover.activeSelf, Is.True);
            source.OnPointerExit(new PointerEventData(EventSystem.current));
            Assert.That(ready.activeSelf, Is.True);
            Assert.That(hover.activeSelf, Is.False);
            Assert.That(ready.GetComponent<SpriteRenderer>().color, Is.EqualTo(custom));
            Assert.That(hover.GetComponent<SpriteRenderer>().color, Is.EqualTo(custom));
        }

        [Test]
        public void OpponentAndNotificationStatesPreserveNativeColors()
        {
            var custom = new Color(.24f, .63f, .82f, .9f);
            foreach (var seat in _view.GetComponentsInChildren<OpponentSeatView>(true))
            {
                var highlight = (GameObject)new SerializedObject(seat).FindProperty("_activeHighlight").objectReferenceValue;
                highlight.GetComponent<SpriteRenderer>().color = custom;
                seat.Render("Player", true);
                Assert.That(highlight.activeSelf, Is.True);
                seat.Render("Player", false);
                Assert.That(highlight.activeSelf, Is.False);
                Assert.That(highlight.GetComponent<SpriteRenderer>().color, Is.EqualTo(custom));
            }
            var feedback = _view.Feedback;
            feedback.gameObject.SetActive(false);
            feedback.gameObject.SetActive(true);
            var failure = (GameObject)new SerializedObject(feedback).FindProperty("_failureAccent").objectReferenceValue;
            var accent = failure.GetComponent<Image>();
            accent.color = custom;
            feedback.Show("Penalty", false);
            Assert.That(failure.activeSelf, Is.True);
            Assert.That(accent.color, Is.EqualTo(custom));
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
