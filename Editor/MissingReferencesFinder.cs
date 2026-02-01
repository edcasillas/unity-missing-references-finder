using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissingReferencesFinder : MonoBehaviour {
    private class ObjectData {
        public float ExpectedProgress;
        public GameObject GameObject;
    }

    [MenuItem("Tools/Find Missing References/In current scene", false, 50)]
    public static void FindMissingReferencesInCurrentScene() {
        var scene = SceneManager.GetActiveScene();
        var window = MissingReferencesResultsWindow.ShowSearchWindow();
        window.StartSearch(FindInSceneCoroutine(scene, window));
    }

    private static IEnumerator FindInSceneCoroutine(Scene scene, MissingReferencesResultsWindow window) {
        var rootObjects = scene.GetRootGameObjects();
        var total = rootObjects.Length;
        var processed = 0;

        foreach (var rootObject in rootObjects) {
            window.UpdateProgress(processed / (float)total, $"Searching in {scene.path}: {rootObject.name}");
            findMissingReferences(scene.path, rootObject, true, window);
            processed++;
            yield return null;
        }
    }

	[MenuItem("Tools/Find Missing References/In current prefab", false, 51)]
	public static void FindMissingReferencesInCurrentPrefab() {
		var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

#if UNITY_2020_1_OR_NEWER
        var assetPath = prefabStage.assetPath;
#else
        var assetPath = prefabStage.prefabAssetPath;
#endif
        var window = MissingReferencesResultsWindow.ShowSearchWindow();
        window.StartSearch(FindInPrefabCoroutine(assetPath, prefabStage.prefabContentsRoot, window));
	}

    private static IEnumerator FindInPrefabCoroutine(string assetPath, GameObject root, MissingReferencesResultsWindow window) {
        window.UpdateProgress(0f, $"Searching in {assetPath}");
        findMissingReferences(assetPath, root, true, window);
        yield return null;
    }

	[MenuItem("Tools/Find Missing References/In current prefab", true, 51)]
	public static bool FindMissingReferencesInCurrentPrefabValidate() => PrefabStageUtility.GetCurrentPrefabStage() != null;

	[MenuItem("Tools/Find Missing References/In all scenes in build", false, 52)]
    public static void FindMissingReferencesInAllScenesInBuild() {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
        var window = MissingReferencesResultsWindow.ShowSearchWindow();
        window.StartSearch(FindInScenesCoroutine(scenes, window));
    }

    private static IEnumerator FindInScenesCoroutine(List<EditorBuildSettingsScene> scenes, MissingReferencesResultsWindow window) {
        var sceneIndex = 0;
        foreach (var scene in scenes) {
            Scene openScene;
            try {
                openScene = EditorSceneManager.OpenScene(scene.path);
            } catch (Exception ex) {
                Debug.LogError($"Could not open scene at path \"{scene.path}\". Error: {ex.Message}");
                continue;
            }

            var rootObjects = openScene.GetRootGameObjects();
            var objIndex = 0;
            foreach (var rootObject in rootObjects) {
                var progress = (sceneIndex + objIndex / (float)rootObjects.Length) / scenes.Count;
                window.UpdateProgress(progress, $"Searching in {scene.path}: {rootObject.name}");
                findMissingReferences(scene.path, rootObject, true, window);
                objIndex++;
                yield return null;
            }
            sceneIndex++;
        }
    }

    /*[MenuItem("Tools/Find Missing References/In all scenes in project", false, 52)]
    public static void FindMissingReferencesInAllScenes() {
        var scenes = EditorBuildSettings.scenes;

        var finished = true;
        foreach (var scene in scenes) {
            var s = EditorSceneManager.OpenScene(scene.path);
            finished = findMissingReferencesInScene(s, 1 /(float)scenes.Count());
            if (!finished) break;
        }
        showFinishDialog(!finished);
    }*/

    [MenuItem("Tools/Find Missing References/In assets", false, 52)]
    public static void FindMissingReferencesInAssets() {
        var window = MissingReferencesResultsWindow.ShowSearchWindow();
        var allAssetPaths = AssetDatabase.GetAllAssetPaths()
                   .Where(isProjectAsset)
                   .ToArray();
        window.StartSearch(FindInAssetsCoroutine(allAssetPaths, window));
    }

    private static IEnumerator FindInAssetsCoroutine(string[] paths, MissingReferencesResultsWindow window) {
        for (var i = 0; i < paths.Length; i++) {
            var obj = AssetDatabase.LoadAssetAtPath(paths[i], typeof(GameObject)) as GameObject;
            if (obj == null || !obj) continue;

            window.UpdateProgress(i / (float)paths.Length, $"Searching in assets: {paths[i]}");
            findMissingReferences("Project", obj, true, window);
            
            if (i % 10 == 0) yield return null;
        }
    }

    [MenuItem("Tools/Find Missing References/Everywhere", false, 53)]
    public static void FindMissingReferencesEverywhere() {
        var currentScenePath = SceneManager.GetActiveScene().path;

        if (string.IsNullOrWhiteSpace(currentScenePath)) {
            if (!EditorUtility.DisplayDialog("Missing References Finder",
                "You must save the current scene before starting to find missing references in the project.", "Save",
                "Cancel")) return;
            if (EditorSceneManager.SaveOpenScenes()) {
                currentScenePath = SceneManager.GetActiveScene().path;
            }
            else {
                EditorUtility.DisplayDialog("Missing References Finder",
                    "Could not start finding missing references in the project because the current scene is not saved.",
                    "Ok");
                return;
            }
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            return;
        }

        var window = MissingReferencesResultsWindow.ShowSearchWindow();
        var scenes = EditorBuildSettings.scenes;
        var allAssetPaths = AssetDatabase.GetAllAssetPaths()
            .Where(isProjectAsset)
            .ToArray();
        
        window.StartSearch(FindEverywhereCoroutine(scenes, allAssetPaths, currentScenePath, window));
    }

    private static IEnumerator FindEverywhereCoroutine(EditorBuildSettingsScene[] scenes, string[] assetPaths, string originalScenePath, MissingReferencesResultsWindow window) {
        var totalWork = scenes.Length + assetPaths.Length;
        var currentWork = 0;

        foreach (var scene in scenes) {
            Scene openScene;
            try {
                openScene = EditorSceneManager.OpenScene(scene.path);
            }
            catch (Exception ex) {
                Debug.LogError($"Could not open scene at path \"{scene.path}\". Error: {ex.Message}");
                continue;
            }

            var rootObjects = openScene.GetRootGameObjects();
            foreach (var rootObject in rootObjects) {
                window.UpdateProgress(currentWork / (float)totalWork, $"Searching in {scene.path}: {rootObject.name}");
                findMissingReferences(scene.path, rootObject, true, window);
                yield return null;
            }
            currentWork++;
        }

        for (var i = 0; i < assetPaths.Length; i++) {
            var obj = AssetDatabase.LoadAssetAtPath(assetPaths[i], typeof(GameObject)) as GameObject;
            if (obj == null || !obj) continue;

            window.UpdateProgress((currentWork + i) / (float)totalWork, $"Searching in assets: {assetPaths[i]}");
            findMissingReferences("Project", obj, true, window);
            
            if (i % 10 == 0) yield return null;
        }

        if (!string.IsNullOrEmpty(originalScenePath)) EditorSceneManager.OpenScene(originalScenePath);
    }
    
    [MenuItem("Tools/Find Missing References/In selected gameObjects", true, 54)]
    public static bool FindMissingReferencesInSelectedGameObjectsValidate() => Selection.gameObjects.Length != 0;
    
    [MenuItem("Tools/Find Missing References/In selected gameObjects", false, 54)]
    public static void FindMissingReferencesInSelectedGameObjects()
    {
        var selectedGameObjects = Selection.gameObjects;
        
        showInitialProgressBar($"{selectedGameObjects.Length} assets");
        
        clearConsole();
        
        int count = 0;
        
        foreach (var selectedGameObject in selectedGameObjects)
        {
            count += findMissingReferences("selected", selectedGameObject, true);
        }
        
        showFinishDialog(false, count);
    }

    private static bool isProjectAsset(string path) {
#if UNITY_EDITOR_OSX
        return !path.StartsWith("/");
#else
        return path.Substring(1, 2) != ":/";
#endif
    }

    private static int findMissingReferences(string context, GameObject go, bool findInChildren, MissingReferencesResultsWindow window) {
        var count = 0;
        var components = go.GetComponents<Component>();
        var isInScene = go.scene.IsValid();

        for (var j = 0; j < components.Length; j++) {
            var c = components[j];
            if (!c) {
                if (window != null) {
                    window.AddResult(new MissingReferenceResult {
                        Context = context,
                        GameObject = go,
                        IsMissingComponent = true,
                        IsInScene = isInScene,
                        ScenePath = isInScene ? go.scene.path : null
                    });
                }
                count++;
                continue;
            }

            var so = new SerializedObject(c);
            var sp = so.GetIterator();

            while (sp.NextVisible(true)) {
                if (sp.propertyType == SerializedPropertyType.ObjectReference) {
                    if (sp.objectReferenceValue           == null
                     && sp.objectReferenceInstanceIDValue != 0) {
                        if (window != null) {
                            window.AddResult(new MissingReferenceResult {
                                Context = context,
                                GameObject = go,
                                ComponentName = c.GetType().Name,
                                PropertyName = ObjectNames.NicifyVariableName(sp.name),
                                IsMissingComponent = false,
                                IsInScene = isInScene,
                                ScenePath = isInScene ? go.scene.path : null
                            });
                        }
                        count++;
                    }
                }
            }
        }

        if (findInChildren) {
            foreach (Transform child in go.transform) {
               count += findMissingReferences(context, child.gameObject, true, window);
            }
        }

        return count;
    }

	internal static void FindMissingReferences(GameObject go, bool findInChildren,
		IDictionary<GameObject, int> missingComponents,
		IDictionary<GameObject, IList<ComponentProperty>> missingReferences) {
		var components = go.GetComponents<Component>();

		for (var j = 0; j < components.Length; j++) {
			var c = components[j];
			if (!c) {
				if (!missingComponents.TryAdd(go, 1)) missingComponents[go]++;
				continue;
			}

			var so = new SerializedObject(c);
			var sp = so.GetIterator();

			while (sp.NextVisible(true)) {
				if (sp.propertyType == SerializedPropertyType.ObjectReference) {
					if (sp.objectReferenceValue           == null
					 && sp.objectReferenceInstanceIDValue != 0) {
						var missingRef = new ComponentProperty(c, sp);
						missingReferences.TryAdd(go, new List<ComponentProperty>());
						missingReferences[go].Add(missingRef);
					}
				}
			}
		}

		if (findInChildren) {
			foreach (Transform child in go.transform) {
				FindMissingReferences(child.gameObject, true, missingComponents, missingReferences);
			}
		}
	}
}

#if !UNITY_2021_3_OR_NEWER
internal static class CollectionExtensionsLegacy {
	public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value) {
		if (dict.ContainsKey(key)) return false;
		dict.Add(key, value);
		return true;
	}
}
#endif