using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class LevelEditor : EditorWindow 
{
	public int Row = 15;
	public int Column = 15;
	public GameObject[] _Prefabs;

	private int[] LevelMap = null;
	private Vector2 scrollPosition = Vector2.zero;
	private int state = 0;
	private GameObject[] prefabs = null;
	private int MaxItems = 9;

	public class MapObject
	{
		public GameObject obj;
		public int index;
	}

	[MenuItem("Window/LevelEditor")]
	public static void ShowWindow()
	{
		//Show existing window instance. If one doesn't exist, make one.
		EditorWindow.GetWindow(typeof(LevelEditor));
	}
	
	private List<MapObject> mapObjectList = new List<MapObject>();

	void Awake()
	{
		LevelMap = new int[Row * Column];
		for (int i = 0; i < LevelMap.Length; i++)
		{
			LevelMap[i] = 0;
		}
		prefabs = new GameObject[MaxItems];
	}

	Vector3 GetPosition(int index)
	{
		int row = index / Column;
		int column = index % Column;

		float startX = -((Row/2) - 0.5f);
		float startZ = ((Column/2) - 0.5f);

		float x = startX + column;
		float z = startZ - row;
		return new Vector3(x, 0.0f, z);
	}
	
	void UpdateMap(int previous, int current, int index)
	{
		if (current == 0 || previous != 0)
		{
			//Remove the old map element
			MapObject mapObject = null;
			foreach (MapObject obj in mapObjectList)
			{
				if (obj.index == index)
				{
					mapObject = obj;
					break;
				}
			}

			if (mapObject != null)
			{
				DestroyImmediate(mapObject.obj);
				mapObjectList.Remove(mapObject);
			}

			if (current == 0)
			{
				return;
			}
		}

		//Add the new map element
		{
			MapObject mapObject = new MapObject();
			GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[state - 1]);
			mapObject.obj = newObject;
			mapObject.obj.transform.position = GetPosition(index);
			mapObject.index = index;
			mapObjectList.Add(mapObject);
		}
	}

	void UpdateIndex(int index)
	{
		if (state == 0)
		{
			return;
		}

		int previousData = LevelMap[index];
		if (previousData == state)
		{
			//Reset if daubed again
			LevelMap[index] = 0;
		}
		else
		{
			LevelMap[index] = state;
		}

		UpdateMap(previousData, LevelMap[index], index);
	}


	void SaveScene()
	{
		try
		{
			GameObject levelDataObj = new GameObject("LevelData");
			LevelData levelData = levelDataObj.AddComponent<LevelData>();
			levelData.LevelMap = LevelMap;
			for(int i = 0; i < mapObjectList.Count; i++)
			{
				levelData.AddMapObject(mapObjectList[i].obj, mapObjectList[i].index);
			}

			//Save the .unity scen file
			string savePath = EditorUtility.SaveFilePanelInProject("Save Scene", "Level1", "unity", "saves the current changes in new scene");
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), savePath);
		}
		catch(System.Exception e)
		{
			Debug.LogErrorFormat("Error while trying to save scene, Message : {0}", e.Message);
		}
	}

	void OnGUI()
	{
		for (int i = 1; i < 10; i++)
		{
			if (Event.current != null && Event.current.isKey)
			{
				if ( (Event.current.keyCode == KeyCode.Alpha0 + i) || (Event.current.keyCode == KeyCode.Keypad0 + i))
				{
					state = i;
				}
			}
		}

		for (int i = 0; i < MaxItems; i++)
		{
			string text = string.Format("Item - {0}", i + 1);
			prefabs[i] = (GameObject)(EditorGUILayout.ObjectField(text, prefabs[i], typeof(GameObject), false));
		}


		GUILayout.Label("Configure Level");
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		GUILayout.BeginVertical();
		for (int i = 0; i < Row; i++)
		{
			GUILayout.BeginHorizontal();
			for (int j = 0; j < Column; j++)
			{
				int idx = (i * Column) + j;
				string text = (LevelMap[idx]).ToString();
				if (GUILayout.Button(text, GUILayout.Width(50.0f), GUILayout.Height(50.0f)))
				{
					UpdateIndex(idx);
				}
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
		GUILayout.EndScrollView();

		if (GUILayout.Button("Save Scene"))
		{
			SaveScene();
		}
	}

}
