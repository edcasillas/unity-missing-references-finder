using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MissingReferencesFinder : MonoBehaviour {
    [MenuItem("Tools/Find Missing References/In scene", false, 50)]
    public static void FindMissingReferencesInCurrentScene() {
        var scene = EditorSceneManager.GetActiveScene().path;
        showInitialProgressBar(scene);
        var objects = GetSceneObjects();
        var finished =FindMissingReferences(scene, objects);
        showFinishDialog(!finished);
    }

    [MenuItem("Tools/Find Missing References/In all scenes", false, 51)]
    public static void MissingSpritesInAllScenes() {
        // TODO Need to adjust the progress bar progress for this case.

        var finished = true;
        foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled)) {
            EditorUtility.DisplayProgressBar("Missing References Finder", $"Opening {scene.path}", 0f);
            EditorSceneManager.OpenScene(scene.path);
            showInitialProgressBar(scene.path, false);
            finished = FindMissingReferences(scene.path, GetSceneObjects());
            if (!finished) break;
        }
        showFinishDialog(!finished);
    }

    [MenuItem("Tools/Find Missing References/In assets", false, 52)]
    public static void MissingSpritesInAssets() {
        showInitialProgressBar("all assets");
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();
        var objs = allAssetPaths
                   .Where(isProjectAsset)
                   .Select(a => AssetDatabase.LoadAssetAtPath(a, typeof(GameObject)) as GameObject)
                   .Where(a => a != null)
                   .ToArray();

        var finished = FindMissingReferences("Project", objs);
        showFinishDialog(!finished);
    }

    private static bool isProjectAsset(string path) {
#if UNITY_EDITOR_OSX
        return !path.StartsWith("/");
#else
        return path.Substring(1, 2) != ":\\";
#endif
    }

    private static bool FindMissingReferences(string context, GameObject[] objects) {
        var wasCancelled = false;
        for (var i = 0; i < objects.Length; i++) {
            if (wasCancelled || EditorUtility.DisplayCancelableProgressBar("Missing References Finder",
                                                                           $"Looking for missing references in {context}. Inspecting {objects[i].name}",
                                                                           i / (float) objects.Length)) {
                return false;
            }

            var go         = objects[i];
            var components = go.GetComponents<Component>();

            for (var j = 0; j < components.Length; j++) {
                var c = components[j];
                if (!c) {
                    Debug.LogError("Missing Component in GO: " + FullPath(go), go);
                    continue;
                }

                /*if (wasCancelled || EditorUtility.DisplayCancelableProgressBar("Missing References Finder",
                                                               "Looking for missing references",
                                                               (i / (float)objects.Length) + ((i / (float)objects.Length) / (float)components.Length) * j)) {
                    wasCancelled = true;
                    break;
                }*/

                var so = new SerializedObject(c);
                var sp = so.GetIterator();

                while (sp.NextVisible(true)) {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference) {
                        if (sp.objectReferenceValue           == null
                         && sp.objectReferenceInstanceIDValue != 0) {
                            ShowError(context, go, c.GetType().Name, ObjectNames.NicifyVariableName(sp.name));
                        }
                    }
                }
            }
        }

        return true;
    }

    private static void showInitialProgressBar(string searchContext, bool clearConsole = true) {
        if (clearConsole) {
            Debug.ClearDeveloperConsole();
            Debug.Log($"Console has been cleared by the Missing References Finder.");   
        }
        EditorUtility.DisplayProgressBar("Missing References Finder", $"Preparing search in {searchContext}", 0f);
    }
    
    private static void showFinishDialog(bool wasCancelled) {
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Missing References Finder",
                                    wasCancelled ?
                                        "Process cancelled. Current results are shown as errors in the console." :
                                        "Finished finding missing references. Results are shown as errors in the console.",
                                    "Ok");
    }

    private static GameObject[] GetSceneObjects() {
        return Resources.FindObjectsOfTypeAll<GameObject>()
                        .Where(go => string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go))
                                  && go.hideFlags == HideFlags.None)
                        .ToArray();
    }

    private const string err = "Missing Ref in: [{3}]{0}. Component: {1}, Property: {2}";

    private static void ShowError(string context, GameObject go, string c, string property) {
        Debug.LogError(string.Format(err, FullPath(go), c, property, context), go);
    }

    private static string FullPath(GameObject go) {
        var parent = go.transform.parent; 
        return parent == null ? go.name : FullPath(parent.gameObject) + "/" + go.name;
    }
}