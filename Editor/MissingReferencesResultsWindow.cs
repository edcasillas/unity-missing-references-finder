using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

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

public class MissingReferenceResult
{
    public string Context;
    public GameObject GameObject;
    public string ComponentName;
    public string PropertyName;
    public bool IsMissingComponent;
    public bool IsInScene;
    public string ScenePath;
}

public class MissingReferencesResultsWindow : EditorWindow
{
    private static MissingReferencesResultsWindow instance;

    private readonly Dictionary<GameObject, int> missingComponents = new Dictionary<GameObject, int>();
    private readonly Dictionary<GameObject, IList<ComponentProperty>> missingReferences = new Dictionary<GameObject, IList<ComponentProperty>>();
    private readonly List<MissingReferenceResult> searchResults = new List<MissingReferenceResult>();
    private bool isSearching = false;
    private float searchProgress = 0f;
    private string searchStatus = "";

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
                return;
            }
            refreshMissingReferences();
        }
    }

    private Vector2 componentsScrollPos = Vector2.zero;
    private Vector2 referencesScrollPos = Vector2.zero;
    private Vector2 searchResultsScrollPos = Vector2.zero;

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

    public static MissingReferencesResultsWindow ShowSearchWindow()
    {
        if (!instance)
        {
            instance = GetWindow<MissingReferencesResultsWindow>("Missing References Finder");
        }
        instance.Show();
        return instance;
    }

    public void StartSearch(IEnumerator searchCoroutine)
    {
        isSearching = true;
        searchProgress = 0f;
        searchStatus = "";
        searchResults.Clear();
        EditorApplication.update += ProcessSearch;
        currentSearchCoroutine = searchCoroutine;
        Repaint();
    }

    private IEnumerator currentSearchCoroutine;

    private void ProcessSearch()
    {
        if (currentSearchCoroutine == null || !currentSearchCoroutine.MoveNext())
        {
            EditorApplication.update -= ProcessSearch;
            currentSearchCoroutine = null;
            isSearching = false;
            Repaint();
        }
        else
        {
            Repaint();
        }
    }

    public void UpdateProgress(float progress, string status)
    {
        searchProgress = progress;
        searchStatus = status;
    }

    public void AddResult(MissingReferenceResult result) => searchResults.Add(result);

    private void OnGUI()
    {
        if (searchResults.Count > 0 || isSearching)
        {
            DrawSearchResults();
            return;
        }

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

    private void DrawSearchResults()
    {
        if (isSearching)
        {
            EditorGUILayout.LabelField(searchStatus, EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 20, EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(rect, searchProgress, $"{(searchProgress * 100):F0}%");
            EditorGUILayout.Space();
        }
        else
        {
            if (GUILayout.Button("Clear Results"))
            {
                searchResults.Clear();
                return;
            }
            EditorGUILayout.Space();
        }

        var missingComponentResults = searchResults.Where(r => r.IsMissingComponent).ToList();
        var missingReferenceResults = searchResults.Where(r => !r.IsMissingComponent).ToList();

        showMissingComponents = drawCollapsibleSection("Missing Components", missingComponentResults.Count, showMissingComponents);
        if (showMissingComponents)
        {
            componentsScrollPos = EditorGUILayout.BeginScrollView(componentsScrollPos);
            foreach (var result in missingComponentResults)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(result.GameObject, typeof(GameObject), true, GUILayout.MinWidth(150f));
                EditorGUILayout.LabelField($"[{result.Context}]", GUILayout.MinWidth(150f));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
        }

        showMissingReferences = drawCollapsibleSection("Missing References", missingReferenceResults.Count, showMissingReferences);
        if (showMissingReferences)
        {
            referencesScrollPos = EditorGUILayout.BeginScrollView(referencesScrollPos);
            foreach (var result in missingReferenceResults)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(result.GameObject, typeof(GameObject), true, GUILayout.MinWidth(150f));
                EditorGUILayout.LabelField(result.ComponentName, GUILayout.MinWidth(150f));
                EditorGUILayout.LabelField(result.PropertyName, GUILayout.MinWidth(150f));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void OnResultClicked(MissingReferenceResult result)
    {
        if (result.IsInScene)
        {
            if (SceneManager.GetActiveScene().path != result.ScenePath)
            {
                EditorSceneManager.OpenScene(result.ScenePath);
            }
            Selection.activeGameObject = result.GameObject;
            EditorGUIUtility.PingObject(result.GameObject);
        }
        else
        {
            Selection.activeObject = result.GameObject;
            EditorGUIUtility.PingObject(result.GameObject);
        }
    }

    private string GetGameObjectPath(GameObject go)
    {
        if (!go) return "<null>";
        var parent = go.transform.parent;
        return parent == null ? go.name : GetGameObjectPath(parent.gameObject) + "/" + go.name;
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
