using Okey.Presentation.Tables;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Okey.Editor
{
    [CustomEditor(typeof(TableView))]
    public sealed class TableViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var view = (TableView)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Dynamic wording: Texts asset. Typography and placement: select a text below, edit TextMeshPro and RectTransform, then save the prefab. Play Mode changes are temporary.", MessageType.Info);
            if (GUILayout.Button("Edit dynamic wording")) Selection.activeObject = view.Texts;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefab text elements", EditorStyles.boldLabel);
            foreach (var text in view.GetComponentsInChildren<TMP_Text>(true))
            {
                var path = AnimationUtility.CalculateTransformPath(text.transform, view.transform);
                if (GUILayout.Button(path, EditorStyles.miniButton)) Selection.activeGameObject = text.gameObject;
            }
        }
    }
}
