using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MissingReferencesFinder : MonoBehaviour {
    [MenuItem("Tools/Find Missing References/In scene", false, 50)]
    public static void FindMissingReferencesInCurrentScene() {
        var objects = GetSceneObjects();
        FindMissingReferences(EditorSceneManager.GetActiveScene().path, objects);
    }

    [MenuItem("Tools/Find Missing References/In all scenes", false, 51)]
    public static void MissingSpritesInAllScenes() {
        foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled)) {
            EditorSceneManager.OpenScene(scene.path);
            FindMissingReferences(scene.path, GetSceneObjects());
        }
    }

    [MenuItem("Tools/Find Missing References/In assets", false, 52)]
    public static void MissingSpritesInAssets() {
        var allAssetPaths = AssetDatabase.GetAllAssetPaths();
        var objs = allAssetPaths
                   .Where(path => !path.StartsWith("/"))
                   .Select(a => AssetDatabase.LoadAssetAtPath(a, typeof(GameObject)) as GameObject)
                   .Where(a => a != null)
                   .ToArray();

        FindMissingReferences("Project", objs);
    }

    private static void FindMissingReferences(string context, GameObject[] objects) {
        foreach (var go in objects) {
            var components = go.GetComponents<Component>();

            foreach (var c in components) {
                if (!c) {
                    Debug.LogError("Missing Component in GO: " + FullPath(go), go);
                    continue;
                }

                var so = new SerializedObject(c);
                var              sp = so.GetIterator();

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

        Debug.Log("Finished finding missing references.");
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

    private static string FullPath(GameObject go) => go.transform.parent == null ? go.name : FullPath(go.transform.parent.gameObject) + "/" + go.name;
}