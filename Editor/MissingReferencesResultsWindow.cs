using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal class ComponentProperty {
	public Component Component;
	public string PropertyName;

	public ComponentProperty(Component component, SerializedProperty serializedProperty) {
		Component = component;
		PropertyName = ObjectNames.NicifyVariableName(serializedProperty.name);
	}
}

public class MissingReferencesResultsWindow : EditorWindow {
	private static MissingReferencesResultsWindow instance;

	private readonly Dictionary<GameObject, int> missingComponents = new Dictionary<GameObject, int>();
	private readonly Dictionary<GameObject, IList<ComponentProperty>> missingReferences = new Dictionary<GameObject, IList<ComponentProperty>>();

	private GameObject selectedGameObject;
	private GameObject SelectedGameObject {
		get => selectedGameObject;
		set {
			var changed = selectedGameObject != value;
			selectedGameObject = value;
			if(!changed) return;

			if (!selectedGameObject) {
				// Remove results?
				return;
			}
			refreshMissingReferences();
		}
	}

	[MenuItem("GameObject/Find Missing References")]
	public static void ShowWindow() {
		if (!instance) {
			instance              = GetWindow<MissingReferencesResultsWindow>("Find Missing References");
		}

		var selected = Selection.transforms.FirstOrDefault()?.gameObject;
		if (instance.SelectedGameObject) {
			instance.refreshMissingReferences();
		} else {
			instance.SelectedGameObject = selected;
		}
		instance.Show();
	}

	[MenuItem("GameObject/Find Missing References", true)]
	private static bool ValidateOneGameObjectSelected() => (Selection.transforms?.Length ?? 0) == 1;

	private void OnGUI()
	{
		SelectedGameObject = (GameObject)EditorGUILayout.ObjectField("Finding missing references in: ", SelectedGameObject, typeof(GameObject), true);

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("GameObjects with Missing Components:");
		foreach (var kvp in missingComponents)
		{
			EditorGUILayout.ObjectField(kvp.Key, typeof(GameObject), true);
			EditorGUILayout.LabelField($"Missing Components: {kvp.Value}");
			EditorGUILayout.Space();
		}

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("GameObjects with Missing Component Properties:");
		foreach (var kvp in missingReferences)
		{
			EditorGUILayout.ObjectField(kvp.Key, typeof(GameObject), true);
			foreach (var property in kvp.Value)
			{
				EditorGUILayout.LabelField($"Component: {property.Component.GetType().Name}, Property: {property.PropertyName}");
			}
			EditorGUILayout.Space();
		}
	}

	private void refreshMissingReferences()
	{
		missingComponents.Clear();
		missingReferences.Clear();

		if (SelectedGameObject != null)
		{
			EditorUtility.DisplayProgressBar("Missing References Finder", $"Searching in {SelectedGameObject.name}", 0f);

			MissingReferencesFinder.FindMissingReferences(SelectedGameObject, true, missingComponents, missingReferences);

			EditorUtility.ClearProgressBar();
		}

		Repaint();
	}
}
