using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Bomberman : MonoBehaviour 
{
	[System.Serializable]
	public enum ActionButton
	{
		Up,
		Down,
		Left,
		Right,
		Action
	}

	[System.Serializable]
	public enum KeyState
	{
		Up,
		Down,
		Press
	}

	[System.Serializable]
	public class KeyInput
	{
		public KeyCode Key;
		public KeyState State;
		public ActionButton Action;
	}

	[System.Serializable]
	public class Player
	{
		public KeyInput[] Inputs;
		public float Speed;
		public Transform SpawnPoint;
		public GameObject Object;
		public CharacterController Controller;

		[System.NonSerialized]
		public int Row;
		[System.NonSerialized]
		public int Column;

		[System.NonSerialized]
		public Transform PlayerT;

		[System.NonSerialized]
		public int ID;
	}

	[System.Serializable]
	public class Bomb
	{
		public enum State
		{
			Dropped,
			Exploded,
			Destroyed
		}

		public Transform transform;
		//Player ID who spawned
		public int PlayerID;
		public int Index;

		public int Strength;
		public float Timer;
		public State CurrentState; 
	}

	public GameObject BombPrefab;
	public GameObject ExplosionPrefab;

	public Player[] Players;

	public float BombAnimSpeed = 5.0f;
	public float BombExplodeTime = 4.0f; 
	
	private int row = 15;
	private int column = 15;

	private float startX = 0.0f;
	private float startZ = 0.0f;

	private int[] LevelData = null;

	private List<Bomb> newlyAddedBombs = new List<Bomb>();
	private List<Bomb> activeBombs = new List<Bomb>();

	private const int BOMB_TYPE = 10;
	private const int EXPLOSION_TYPE = 11;

	void Awake()
	{
		string sceneName = SceneManager.GetActiveScene().name;
		string dataPath = Path.Combine(Application.streamingAssetsPath, string.Format("{0}.data", sceneName));
		try
		{
			FileStream fileStream = new FileStream(dataPath, FileMode.Open);
			LevelData = new int[fileStream.Length];
			for (int i = 0; i < fileStream.Length; i++)
			{
				LevelData[i] = fileStream.ReadByte();
				//Todo:Fix this!
				if(LevelData[i] == 3)
				{
					LevelData[i] = 0;
				}
			}
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("Error while reading file : {0}, Error : {1}", dataPath, e.Message);
		}
	}

	void Start()
	{
		startX = ((row/2) - 0.5f);
		startZ = ((column/2) - 0.5f);

		for(int i = 0; i < Players.Length; i++)
		{
			Players[i].PlayerT = Players[i].Object.transform;
			Players[i].PlayerT.position = Players[i].SpawnPoint.position;
			Players[i].ID = i;
			UpdateIndex(Players[i]);
		}
	}

	int GetIndex(int r, int c)
	{
		int idx = (r * column) + c;
		return idx;
	}

	void SpawnBomb(Player player, int index)
	{
		//Install bomb	
		float x = -startX + player.Column;		
		float z = -player.Row + startZ;	

		Vector3 pos = new Vector3(x, 0, z);
		GameObject bombObj = Instantiate(BombPrefab, pos, Quaternion.identity) as GameObject;
		LevelData[index] = BOMB_TYPE;

		Bomb bomb = new Bomb();
		bomb.transform = bombObj.transform;
		bomb.PlayerID = player.ID;
		bomb.Index = index;
		bomb.Strength = 1;
		newlyAddedBombs.Add(bomb);
		activeBombs.Add(bomb);
	}

	void SpawnExplosion(Bomb bomb, int index)
	{
		int r = index / column;
		int c = index % column;

		//Install bomb	
		float x = -startX + c;		
		float z = -r + startZ;	

		Vector3 pos = new Vector3(x, 0, z);
		GameObject explostion = Instantiate(ExplosionPrefab, pos, Quaternion.identity) as GameObject;
		explostion.transform.SetParent(bomb.transform);
		LevelData[index] = EXPLOSION_TYPE;
	}

	void UpdateIndex(Player player)
	{
		Vector3 position = player.PlayerT.position;
		position.x += startX;
		position.z -= startZ;		

		player.Column = (int)Mathf.Floor(Mathf.Abs(position.x) + 0.5f);
		player.Row = (int)Mathf.Floor(Mathf.Abs(position.z) + 0.5f);
	}

	void UpdateAction(Player player, ActionButton action)
	{
		CharacterController controller = player.Controller;
		float speed = player.Speed * Time.deltaTime;
		switch(action)
		{
			case ActionButton.Up:
			{
				Vector3 move = new Vector3(0, 0, 1) * speed;
				controller.Move(move);
			}break;

			case ActionButton.Down:
			{
				Vector3 move = new Vector3(0, 0, -1) * speed;
				controller.Move(move);
			}break;

			case ActionButton.Left:
			{
				Vector3 move = new Vector3(-1, 0, 0) * speed;
				controller.Move(move);
			}break;

			case ActionButton.Right:
			{
				Vector3 move = new Vector3(1, 0, 0) * speed;
				controller.Move(move);
			}break;

			case ActionButton.Action:
			{
				int index = GetIndex(player.Row, player.Column);
				if(LevelData[index] == 0)
				{
					SpawnBomb(player, index);
				}				
			}break;
		}		
	}

	void UpdateBombs()
	{
		//Enable bomb colliders if player moved from the dropped place
		{
			List<Bomb> enabledBombs = new List<Bomb>();
			for(int i = 0; i < newlyAddedBombs.Count; i++)
			{
				Bomb bomb = newlyAddedBombs[i];
				Player player = Players[bomb.PlayerID];
				int playerIndex = GetIndex(player.Row, player.Column);
				float distance =  Vector3.Distance(player.PlayerT.position, bomb.transform.position);
				if(bomb.Index != playerIndex && distance > 1.5f)
				{
					Collider collider = bomb.transform.GetComponent<Collider>();
					collider.enabled = true;
					enabledBombs.Add(bomb);
				}
			}
			foreach(Bomb bomb in enabledBombs)
			{
				newlyAddedBombs.Remove(bomb);
			}
		}

		for(int i = 0; i < activeBombs.Count; i++)
		{
			Bomb bomb = activeBombs[i];
			if(bomb.CurrentState == Bomb.State.Dropped)
			{
				//PingPong scaling
				float size = 1.0f + ((1.0f + Mathf.Sin(bomb.Timer * BombAnimSpeed) / 2.0f) * 0.125f);
				Vector3 scale = Vector3.one * size;
				bomb.transform.localScale = scale;
				bomb.Timer += Time.deltaTime;
				if(bomb.Timer >= BombExplodeTime)
				{
					bomb.CurrentState = Bomb.State.Exploded;
					int[] indexsToCheck = new int[]{-1, 1, -column, column};
					//parse through all four sides
					for(int n = 0; n < indexsToCheck.Length; n++)
					{
						int incrementer = indexsToCheck[n];
						int index = bomb.Index;

						for(int j = 0; j < bomb.Strength; j++)
						{
							index += incrementer;
							if(index < 0 || index >= LevelData.Length)
							{
								break;
							}

							if(LevelData[index] == 0)
							{
								SpawnExplosion(bomb, index);
							}  
						}
					}
				}
			}
		}
	}

	void Update()
	{
		for(int i = 0; i < Players.Length; i++)
		{
			for(int j = 0; j < Players[i].Inputs.Length; j++)
			{
				bool updateAction = false;
				switch(Players[i].Inputs[j].State)
				{
					case KeyState.Up:
					{
						updateAction = Input.GetKeyUp(Players[i].Inputs[j].Key);
					}break;

					case KeyState.Down:
					{
						updateAction = Input.GetKeyDown(Players[i].Inputs[j].Key);
					}break;

					case KeyState.Press:
					{
						updateAction = Input.GetKey(Players[i].Inputs[j].Key);
					}break;
				}
				
				if(updateAction)
				{
					UpdateAction(Players[i], Players[i].Inputs[j].Action);
				}
			}

			UpdateIndex(Players[i]);
		}

		UpdateBombs();
	}
}
