using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickupManager : MonoBehaviour 
{
	public enum PickupType
	{
		None = -1,
		ExtraBomb,
		LongerBlast,
		BootsOfSpeed,
		RemoteBomb,
		
		MaxPickup
	}

	[System.Serializable]
	public class PickupData
	{
		public PickupType Type;
		public GameObject Prefab;		
	}

	public class PickupObject
	{
		public GameObject gameObject;
		public Transform transform;
		public int index;
		public PickupType pickupType;
		public float timer;

		public Vector3 initPosition;

		public PickupObject(GameObject obj, int index, PickupType pickupType)
		{
			this.gameObject = obj;
			this.transform = gameObject.transform;
			this.index = index;
			this.pickupType = pickupType;
			this.timer = 0.0f;

			this.initPosition = transform.position;
		}
	}

	public PickupData[] PickupDatas;
	public float AnimSpeed = 10.0f;

	private List<PickupObject> activePickups = new List<PickupObject>();

	GameObject GetPrefabForType(PickupType type)
	{
		for(int i = 0; i < PickupDatas.Length; i++)
		{
			if(PickupDatas[i].Type == type)
			{
				return PickupDatas[i].Prefab;
			}
		}

		return null;
	}

	public void SpawnRandomPickup(int index, Vector3 spawnPos)
	{
		PickupType pickupType = (PickupType)Random.Range((int)PickupType.None + 1, (int)PickupType.MaxPickup);
		GameObject obj = Instantiate(GetPrefabForType(pickupType), spawnPos, Quaternion.identity) as GameObject;
		activePickups.Add(new PickupObject(obj, index, pickupType));
	}

	public PickupType RemoveAndGetPickupType(int index)
	{
		PickupType pickupType = PickupType.None;
		for(int i = 0; i < activePickups.Count; i++)
		{
			if(index == activePickups[i].index)
			{
				pickupType = activePickups[i].pickupType;
				Destroy(activePickups[i].gameObject);
				activePickups.RemoveAt(i);
				break;
			}
		}
		return pickupType;
	}

	void Update()
	{
		for(int i = 0; i < activePickups.Count; i++)
		{
			PickupObject pickup = activePickups[i];
			Vector3 pos = pickup.initPosition;
			pos.y += ((1.0f + Mathf.Sin(pickup.timer * AnimSpeed)) * 0.5f) * 0.5f;
			pickup.transform.position = pos;
			pickup.timer += Time.deltaTime;
		}
	}
}
