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

	private int[] LevelData = null;
	private Vector2 scrollPosition = Vector2.zero;
	private int state = 0;
	private GameObject[] prefabs = null;
	private int MaxItems = 9;

	[MenuItem("Window/LevelEditor")]
	public static void ShowWindow()
	{
		//Show existing window instance. If one doesn't exist, make one.
		EditorWindow.GetWindow(typeof(LevelEditor));
	}

	public class MapObject
	{
		public GameObject obj;
		public int index;
	}

	private List<MapObject> mapObjectList = new List<MapObject>();

	void Awake()
	{
		LevelData = new int[Row * Column];
		string filePath = Path.Combine(Application.streamingAssetsPath, EditorSceneManager.GetActiveScene().name);
		string dataPath = string.Format("{0}.data", filePath);
		if (File.Exists(dataPath))
		{
			try
			{
				FileStream fileStream = new FileStream(dataPath, FileMode.Open);
				for (int i = 0; i < fileStream.Length; i++)
				{
					LevelData[i] = fileStream.ReadByte();
				}
			}
			catch (System.Exception e)
			{
				Debug.LogErrorFormat("Error while reading file : {0}, Error : {1}", dataPath, e.Message);
			}
		}
		else
		{
			for (int i = 0; i < LevelData.Length; i++)
			{
				LevelData[i] = 0;
			}
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

		int previousData = LevelData[index];
		if (previousData == state)
		{
			//Reset if daubed again
			LevelData[index] = 0;
		}
		else
		{
			LevelData[index] = state;
		}

		UpdateMap(previousData, LevelData[index], index);
	}


	void SaveScene()
	{
		string savePath = EditorUtility.SaveFilePanelInProject("Save Scene", "Level1", "unity", "saves the current changes in new scene");
		try
		{
			//Save the .unity scen file
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), savePath);

			//Save LevelData file
			savePath = savePath.Replace(".unity", ".data");
			FileStream fileStream = new FileStream(savePath, FileMode.OpenOrCreate);
			byte[] bytes = new byte[LevelData.Length];
			for (int i = 0; i < LevelData.Length; i++)
			{
				bytes[i] = (byte)LevelData[i];
			}
			fileStream.Write(bytes, 0, bytes.Length);
			fileStream.Close();
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
				string text = (LevelData[idx]).ToString();
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
