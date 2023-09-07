using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

internal class ComponentProperty
{
    public Component Component;
    public string PropertyName;

    public ComponentProperty(Component component, SerializedProperty serializedProperty)
    {
        Component = component;
        PropertyName = ObjectNames.NicifyVariableName(serializedProperty.name);
    }
}

public class MissingReferencesResultsWindow : EditorWindow
{
    private static MissingReferencesResultsWindow instance;

    private readonly Dictionary<GameObject, int> missingComponents = new Dictionary<GameObject, int>();
    private readonly Dictionary<GameObject, IList<ComponentProperty>> missingReferences = new Dictionary<GameObject, IList<ComponentProperty>>();
    private bool isSearching = false;

    private GameObject selectedGameObject;
    private GameObject SelectedGameObject
    {
        get => selectedGameObject;
        set
        {
            var changed = selectedGameObject != value;
            selectedGameObject = value;
            if (!changed) return;

            if (!selectedGameObject)
            {
                // Remove results?
                return;
            }
            refreshMissingReferences();
        }
    }

    private Vector2 componentsScrollPos = Vector2.zero;
    private Vector2 referencesScrollPos = Vector2.zero;

    private bool showMissingComponents = false;
    private bool showMissingReferences = false;

    [MenuItem("GameObject/Find Missing References")]
    public static void ShowWindow()
    {
        if (!instance)
        {
            instance = GetWindow<MissingReferencesResultsWindow>("Find Missing References");
        }

        var selected = Selection.transforms.FirstOrDefault()?.gameObject;
        if (instance.SelectedGameObject)
        {
            instance.refreshMissingReferences();
        }
        else
        {
            instance.SelectedGameObject = selected;
        }
        instance.Show();
    }

    [MenuItem("GameObject/Find Missing References", true)]
    private static bool ValidateOneGameObjectSelected() => (Selection.transforms?.Length ?? 0) == 1;

    private void OnGUI()
    {

        SelectedGameObject = (GameObject)EditorGUILayout.ObjectField("Game Object: ", SelectedGameObject, typeof(GameObject), true);
		if(GUILayout.Button("Refresh")) refreshMissingReferences();

        EditorGUILayout.Space();

        if (isSearching)
        {
            showLoadingSpinner($"Finding missing references in {SelectedGameObject.name}");
            return;
        }

        EditorGUILayout.BeginVertical();

        showMissingComponents = drawCollapsibleSection("Missing Components", missingComponents.Count, showMissingComponents);
        if (showMissingComponents)
        {
            componentsScrollPos = EditorGUILayout.BeginScrollView(componentsScrollPos);
            drawMissingComponentsTable();
            EditorGUILayout.EndScrollView();
        }

        showMissingReferences = drawCollapsibleSection("Missing References", missingReferences.Values.Sum(list => list.Count), showMissingReferences);
        if (showMissingReferences)
        {
            referencesScrollPos = EditorGUILayout.BeginScrollView(referencesScrollPos);
            drawMissingReferencesTable();
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    private bool drawCollapsibleSection(string sectionTitle, int resultCount, bool isExpanded)
    {
        EditorGUILayout.BeginHorizontal();
		if (resultCount > 0) {
			isExpanded = EditorGUILayout.Foldout(isExpanded, $"{sectionTitle} ({resultCount} results)", true);
		} else {
			EditorGUILayout.LabelField($"{sectionTitle} ({resultCount} results)");
		}
        EditorGUILayout.EndHorizontal();
        return isExpanded;
    }

    private void drawMissingComponentsTable()
    {
        foreach (var kvp in missingComponents)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(kvp.Key, typeof(GameObject), true, GUILayout.MinWidth(150f));
            EditorGUILayout.LabelField(kvp.Value.ToString(), GUILayout.MinWidth(150f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }

    private void drawMissingReferencesTable()
    {
        foreach (var kvp in missingReferences)
        {
            foreach (var property in kvp.Value)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(kvp.Key, typeof(GameObject), true, GUILayout.MinWidth(150f));
                EditorGUILayout.LabelField(property.Component.GetType().Name, GUILayout.MinWidth(150f));
                EditorGUILayout.LabelField(property.PropertyName, GUILayout.MinWidth(150f));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }
    }

    private void refreshMissingReferences()
    {
        isSearching = true;
        missingComponents.Clear();
        missingReferences.Clear();

        if (SelectedGameObject)
        {
            MissingReferencesFinder.FindMissingReferences(SelectedGameObject, true, missingComponents, missingReferences);
        }

        isSearching = false;
        Repaint();
    }

    private void showLoadingSpinner(string label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        var spinnerRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight);
        EditorGUI.ProgressBar(spinnerRect, -1f, string.Empty);
        Repaint();
    }
}
