using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MissingPrefabsFinder { // Based on this post: https://forum.unity.com/threads/detecting-missing-nested-prefab.697562/
	[MenuItem("Tools/Find Missing Prefabs", false, 50)]
	static void Init() {
		var allPrefabs = GetAllPrefabs();

		var count = 0;
		EditorUtility.DisplayProgressBar("Processing...", "Begin Job", 0);

		foreach (var prefab in allPrefabs) {
			var o = AssetDatabase.LoadMainAssetAtPath(prefab);

			if (o == null) {
				Debug.Log("prefab " + prefab + " null?");
				continue;
			}

			GameObject go;
			try {
				go = (GameObject)PrefabUtility.InstantiatePrefab(o);
				EditorUtility.DisplayProgressBar("Processing...", go.name, ++count / (float)allPrefabs.Length);
				FindMissingPrefabInGO(go, prefab, true);

				GameObject.DestroyImmediate(go);

			} catch {
				Debug.Log("For some reason, prefab " + prefab + " won't cast to GameObject");

			}
		}

		EditorUtility.ClearProgressBar();
	}


	static void FindMissingPrefabInGO(GameObject g, string prefabName, bool isRoot) {
		if (g.name.Contains("Missing Prefab")) {
			Debug.LogError($"{prefabName} has missing prefab {g.name}");
			return;

		}

		if (PrefabUtility.IsPrefabAssetMissing(g)) {
			Debug.LogError($"{prefabName} has missing prefab {g.name}");
			return;
		}

		if (PrefabUtility.IsDisconnectedFromPrefabAsset(g)) {
			Debug.LogError($"{prefabName} has missing prefab {g.name}");
			return;
		}

		if (!isRoot) {
			if (PrefabUtility.IsAnyPrefabInstanceRoot(g)) {
				return;
			}

			GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(g);
			if (root == g) {
				return;
			}
		}


		// Now recurse through each child GO (if there are any):
		foreach (Transform childT in g.transform) {
			//Debug.Log("Searching " + childT.name  + " " );
			FindMissingPrefabInGO(childT.gameObject, prefabName, false);
		}
	}

	public static string[] GetAllPrefabs() {
		string[] temp = AssetDatabase.GetAllAssetPaths();
		List<string> result = new List<string>();
		foreach (string s in temp) {
			if (s.Contains(".prefab")) result.Add(s);
		}

		return result.ToArray();
	}
}