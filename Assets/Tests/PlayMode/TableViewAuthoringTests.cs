using System.Collections;
using System.Linq;
using NUnit.Framework;
using Okey.Composition;
using Okey.Presentation.Tables;
using Okey.Presentation.Text;
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
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
            yield return null;
            _scene = SceneManager.GetSceneByName("SampleScene");
            _context = _scene.GetRootGameObjects().Select(root => root.GetComponent<SceneContext>()).Single(value => value != null);
            _view = _context.Container.Resolve<TableView>();
            _context.Container.Resolve<TickableManager>().Remove(_context.Container.Resolve<BotTurnController>());
            _context.Container.Resolve<TickableManager>().Remove(_context.Container.Resolve<TurnClockController>());
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var flow = Object.FindFirstObjectByType<GameFlowState>();
            if (flow != null) Object.DestroyImmediate(flow.gameObject);
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
        public void ReplacingThePrefabTextCatalogUpdatesDynamicLabelsWithoutCodeChanges()
        {
            var original = _view.Texts;
            var copy = Object.Instantiate(original);
            var fields = new SerializedObject(copy);
            fields.FindProperty("_openLabel").stringValue = "CUSTOM OPEN";
            fields.FindProperty("_finishLabel").stringValue = "CUSTOM FINISH";
            fields.FindProperty("_progressFormat").stringValue = "[{0} of {1}]";
            fields.FindProperty("_additionalGroupFormat").stringValue = "[+{0}]";
            fields.ApplyModifiedPropertiesWithoutUndo();
            var viewFields = new SerializedObject(_view);
            viewFields.FindProperty("_texts").objectReferenceValue = copy;
            viewFields.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                _context.Container.Resolve<TableHudPresenter>().Refresh();
                Assert.That(_view.Actions[0].Label.text, Is.EqualTo(_context.Container.Resolve<Okey.Core.Tables.TableSession>().Game.AllowsPublicMelds ? "CUSTOM OPEN" : "CUSTOM FINISH"));
                Assert.That(_view.Actions[0].ProgressText.text, Does.StartWith("["));
            }
            finally
            {
                viewFields.FindProperty("_texts").objectReferenceValue = original;
                viewFields.ApplyModifiedPropertiesWithoutUndo();
                Object.Destroy(copy);
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
