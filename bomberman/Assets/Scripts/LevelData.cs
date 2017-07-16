using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelData : MonoBehaviour 
{
	public enum ElementType
	{
		None,
		Wall,
		Brick,
		SpawnPoint,
		Bomb = 10,
		BombExplosion = 11,	
		Pickups = 100,
	}

	public int[] LevelMap;

	[System.Serializable]
	public class MapObject
	{
		public GameObject obj;
		public int index;
	}

	public class RemoveMapObject
	{
		public GameObject obj;
		public Transform transform;
		public float timer;
		
		public RemoveMapObject(GameObject toRemove)
		{
			obj = toRemove;
			transform = obj.transform;
			timer = 0.0f;
		}
	}

	public List<MapObject> MapObjectList = new List<MapObject>();
	private List<RemoveMapObject> removeObjectList = new List<RemoveMapObject>();

	public void AddMapObject(GameObject obj, int index)
	{
		MapObject element = new MapObject();
		element.obj = obj;
		element.index = index;
		MapObjectList.Add(element);
	}

	public void AddMapObject(GameObject obj, int index, ElementType type)
	{
		LevelMap[index] = (int)type;
		AddMapObject(obj, index);
	}

	public void RemoveMapObjectAt(int index)
	{
		for(int i = 0; i < MapObjectList.Count; i++)
		{
			if(MapObjectList[i].index == index)
			{
				removeObjectList.Add(new RemoveMapObject(MapObjectList[i].obj));
				LevelMap[index] = 0;
				MapObjectList.RemoveAt(i);
				return;
			}
		}
	}

	void Update()
	{
		List<RemoveMapObject> cleanupList = new List<RemoveMapObject>();
		for(int i = 0; i < removeObjectList.Count; i++)		
		{
			RemoveMapObject removeObject = removeObjectList[i];
			//shrink size
			float size = 1.0f - ((1.0f + Mathf.Sin(removeObject.timer * 4.0f)) * 0.5f);
			Vector3 scale =  Vector3.one * size;
			removeObject.transform.localScale = scale;

			removeObject.timer += Time.deltaTime;
			if(removeObject.timer >= 0.5f)
			{
				Destroy(removeObject.obj);
				cleanupList.Add(removeObject);
			}
		}

		for(int i = 0; i < cleanupList.Count; i++)
		{
			removeObjectList.Remove(cleanupList[i]);
		}
	}
}
