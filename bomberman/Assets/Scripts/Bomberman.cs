using UnityEngine;
using UnityEngine.UI;
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
		public string Name;
		public KeyInput[] Inputs;
		public float Speed;
		public Transform SpawnPoint;
		public GameObject Object;
		public CharacterController Controller;
		public Text NameText;
		public Text BombCountText;
		public Text RemoteBombText;
		public Text SpeedText;

		[System.NonSerialized]
		public int Row;

		[System.NonSerialized]
		public int Column;

		[System.NonSerialized]
		public Transform PlayerT;

		[System.NonSerialized]
		public int ID;

		[System.NonSerialized]
		public int CurrentBombCount = 1;

		[System.NonSerialized]
		public int BombStrength = 1;

		[System.NonSerialized]
		public float RemoteControlTimer = 0.0f;
	}

	public class Bomb
	{
		public enum State
		{
			Dropped,
			Exploded,
			Destroyed,
		}

		public Transform transform;
		//Player ID who spawned
		public int PlayerID;
		public int Index;

		public int Strength;
		public float Timer;
		public State CurrentState; 

		public List<int> AffectedIndexes = new List<int>();
	}

	public enum GameState
	{
		None,
		InGame,
		GameOver,
	}

	public class DeadInfo
	{
		public int DeadPlayerID;
		public int KillerID;

		public DeadInfo(int playerID, int killderID)
		{
			DeadPlayerID = playerID;
			KillerID = killderID;
		}
	}

	public int PickupRevealPct = 30;

	public GameObject BombPrefab;
	public GameObject ExplosionPrefab;

	public Player[] Players;
	public float PlayerMaxSpeed = 6.0f;
	public int MaxBombStrength = 4;

	public LevelData CurrentLevelData;
	public PickupManager PickupManager;

	public float BombAnimSpeed = 5.0f;
	public float BombExplodeTime = 4.0f; 
	public float BombCleanupTime = 1.5f;

	public Text GameTimerText;
	public int MaxGameTimeInSecs = 120;

	public GameObject GameOverPanelObj;
	public Text GameOverSummaryText;

	public string RemoteBombTextFormat = "Remote(secs) : {0}";
	public string BombCountTextFormat = "BombCount : {0}";
	public string SpeedTextFormat = "Speed : {0}";

	
	private GameState gameState = GameState.None;
	private float gameTimer = 0.0f;

	private int row = 15;
	private int column = 15;

	private float startX = 0.0f;
	private float startZ = 0.0f;

	private List<Bomb> newlyAddedBombs = new List<Bomb>();
	private List<Bomb> activeBombs = new List<Bomb>();
	private List<DeadInfo> deadList = new List<DeadInfo>();

	void Start()
	{
		startX = ((row/2) - 0.5f);
		startZ = ((column/2) - 0.5f);

		for(int i = 0; i < Players.Length; i++)
		{
			Players[i].PlayerT = Players[i].Object.transform;
			Players[i].PlayerT.position = Players[i].SpawnPoint.position;
			Players[i].ID = i;
			ProcessIndex(Players[i]);
			InitText(Players[i]);
		}

		gameTimer = MaxGameTimeInSecs;
		gameState = GameState.InGame;
	}

	void InitText(Player player)
	{
		player.NameText.text = string.Format("{0}:", player.Name);
		player.BombCountText.text = string.Format(BombCountTextFormat, player.CurrentBombCount);
		player.SpeedText.text = string.Format(SpeedTextFormat, player.Speed);
		player.RemoteBombText.text = string.Format(RemoteBombTextFormat, player.RemoteControlTimer);
	}

	int GetIndex(int r, int c)
	{
		int idx = (r * column) + c;
		return idx;
	}

	Vector3 GetSpawnPos(int index)
	{
		int r = index / column;
		int c = index % column;

		float x = -startX + c;		
		float z = -r + startZ;	

		return new Vector3(x, 0, z);
	}

	bool HasActiveBomb(int playerID)
	{
		for(int i = 0; i < activeBombs.Count; i++)
		{
			if(activeBombs[i].PlayerID == playerID)
			{
				return true;
			}
		}
		return false;
	}

	void UpdateBombCounter(Player player, int incrementer)
	{
		player.CurrentBombCount += incrementer;
		player.BombCountText.text = string.Format(BombCountTextFormat, player.CurrentBombCount);
	}

	void SpawnBomb(Player player, int index)
	{
		bool canPlaceBomb = player.CurrentBombCount > 0;
		if(player.RemoteControlTimer > 0.0f)
		{
			canPlaceBomb = !HasActiveBomb(player.ID);
		}

		if(canPlaceBomb)
		{
			//Install bomb	
			Vector3 spawnPos = GetSpawnPos(index);
			GameObject bombObj = Instantiate(BombPrefab, spawnPos, Quaternion.identity) as GameObject;
			CurrentLevelData.AddMapObject(bombObj, index, LevelData.ElementType.Bomb);

			Bomb bomb = new Bomb();
			bomb.transform = bombObj.transform;
			bomb.PlayerID = player.ID;
			bomb.Index = index;
			bomb.Strength = player.BombStrength;
			newlyAddedBombs.Add(bomb);
			activeBombs.Add(bomb);
			UpdateBombCounter(player, -1);
		}
	}

	void SpawnExplosion(Bomb bomb, int index)
	{
		//Install bomb	
		Vector3 spawnPos = GetSpawnPos(index);
		GameObject explostion = Instantiate(ExplosionPrefab, spawnPos, Quaternion.identity) as GameObject;
		explostion.transform.SetParent(bomb.transform);
	}

	void AddPickup(Player player, PickupManager.PickupType pickupType)
	{
		switch(pickupType)
		{
			case PickupManager.PickupType.ExtraBomb:
			{
				UpdateBombCounter(player, 1);
				player.BombCountText.text = string.Format("BombCount : {0}", player.CurrentBombCount);
			}break;

			case PickupManager.PickupType.LongerBlast:
			{
				player.BombStrength =  Mathf.Min(player.BombStrength + 1, MaxBombStrength);
			}break;
			
			case PickupManager.PickupType.BootsOfSpeed:
			{
				player.Speed = Mathf.Min(player.Speed + 1, PlayerMaxSpeed);
				player.SpeedText.text = string.Format("Speed : {0}", player.Speed);
			}break;

			case PickupManager.PickupType.RemoteBomb:
			{
				player.RemoteControlTimer += 10.0f;
				player.RemoteBombText.text = string.Format(RemoteBombTextFormat, player.RemoteControlTimer);
			}break;
		}
	}

	void ProcessIndex(Player player)
	{
		Vector3 position = player.PlayerT.position;
		position.x += startX;
		position.z -= startZ;		

		player.Column = (int)Mathf.Floor(Mathf.Abs(position.x) + 0.5f);
		player.Row = (int)Mathf.Floor(Mathf.Abs(position.z) + 0.5f);
		int index = GetIndex(player.Row, player.Column);
		if(index >= 0 && index < CurrentLevelData.LevelMap.Length)
		{
			int mapData = CurrentLevelData.LevelMap[index];
			if(mapData == (int)LevelData.ElementType.Pickups)
			{
				PickupManager.PickupType pickupType = PickupManager.RemoveAndGetPickupType(index);
				CurrentLevelData.LevelMap[index] = 0;
				AddPickup(player, pickupType);
			}
		}
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
				if(player.RemoteControlTimer > 0.0f && HasActiveBomb(player.ID))
				{
					for(int i = 0; i < activeBombs.Count; i++)
					{
						if(activeBombs[i].PlayerID == player.ID)
						{
							activeBombs[i].Timer = BombExplodeTime + 1.0f;
							break;
						}
					}
				}
				else
				{
					int index = GetIndex(player.Row, player.Column);
					if(CurrentLevelData.LevelMap[index] == 0)
					{
						SpawnBomb(player, index);
					}
				}				
			}break;
		}		
	}

	Bomb GetBombAt(int index)
	{
		for(int i = 0; i < activeBombs.Count; i++)
		{
			if(activeBombs[i].Index == index)
			{
				return activeBombs[i];
			}
		}
		return null;
	}

	void UpdateBombs()
	{
		//Enable bomb colliders if player moved from the dropped place
		{
			List<Bomb> enabledBombs = new List<Bomb>();
			for(int i = 0; i < newlyAddedBombs.Count; i++)
			{
				Bomb bomb = newlyAddedBombs[i];
				bool addToList = (bomb.CurrentState == Bomb.State.Exploded);

				bool enableCollider = true;
				for(int n = 0; n < Players.Length; n++)
				{
					Player player = Players[n];
					int playerIndex = GetIndex(player.Row, player.Column);
					float distance =  Vector3.Distance(player.PlayerT.position, bomb.transform.position);
					if(bomb.Index == playerIndex || distance < 1.1f)
					{
						enableCollider = false;
						break;
					}
				}
								
				if(enableCollider == true)
				{
					Collider collider = bomb.transform.GetComponent<Collider>();
					collider.enabled = true;
					addToList = true;
				}
				
				if(addToList == true)
				{
					enabledBombs.Add(bomb);	
				}
			}

			foreach(Bomb bomb in enabledBombs)
			{
				newlyAddedBombs.Remove(bomb);
			}
		}

		List<Bomb> destroyedBombs = new List<Bomb>();
		for(int i = 0; i < activeBombs.Count; i++)
		{
			Bomb bomb = activeBombs[i];
			switch(bomb.CurrentState)
			{
				case Bomb.State.Dropped:
				{
					//PingPong scaling
					float size = 1.0f - ((1.0f + Mathf.Sin(bomb.Timer * BombAnimSpeed) / 2.0f) * 0.125f);
					Vector3 scale = Vector3.one * size;
					bomb.transform.localScale = scale;
					bomb.Timer += Time.deltaTime;
					if(bomb.Timer >= BombExplodeTime)
					{
						CurrentLevelData.LevelMap[bomb.Index] = 0;
						int[] indexsToCheck = new int[]{0, -1, 1, -column, column};
						//parse through all four sides
						for(int n = 0; n < indexsToCheck.Length; n++)
						{
							int incrementer = indexsToCheck[n];
							int index = bomb.Index;

							for(int j = 0; j < bomb.Strength; j++)
							{
								index += incrementer;
								//check index bounds
								if(index < 0 || index >= CurrentLevelData.LevelMap.Length)
								{
									break;
								}

								if(CurrentLevelData.LevelMap[index] != 0)
								{
									switch((LevelData.ElementType)CurrentLevelData.LevelMap[index])
									{
										case LevelData.ElementType.Brick:
										{
											CurrentLevelData.RemoveMapObjectAt(index);
											if(Random.Range(0, 100) <= PickupRevealPct)
											{
												 PickupManager.SpawnRandomPickup(index, GetSpawnPos(index));
												 CurrentLevelData.LevelMap[index] = (int)LevelData.ElementType.Pickups;
											}
										}break;

										case LevelData.ElementType.Bomb:
										{
											//Trigger this bomb as well
											Bomb triggerBomb = GetBombAt(index);
											if(triggerBomb != null)
											{
												triggerBomb.Timer = BombExplodeTime + 1.0f;
											}
										}break;
									}
									break;
								}

								bomb.AffectedIndexes.Add(index);								
								SpawnExplosion(bomb, index);
							}
						}
						bomb.CurrentState = Bomb.State.Exploded;
						bomb.Timer = 0.0f;
					}
				}break;

				case Bomb.State.Exploded:
				{
					for(int n = 0; n < bomb.AffectedIndexes.Count; n++)
					{
						int index = bomb.AffectedIndexes[n];
						for(int p = 0; p < Players.Length; p++)
						{
							int playerIndex = GetIndex(Players[p].Row, Players[p].Column);
							if(index == playerIndex)
							{
								AddDeadPlayer(p, bomb.PlayerID);
							}
						}
					}

					bomb.Timer += Time.deltaTime;
					if(bomb.Timer >= BombCleanupTime)
					{
						bomb.CurrentState = Bomb.State.Destroyed;
						UpdateBombCounter(Players[bomb.PlayerID], 1);
						CurrentLevelData.RemoveMapObjectAt(bomb.Index);
						destroyedBombs.Add(bomb);
					}
				}break;
			}
		}

		for(int i = 0; i < destroyedBombs.Count; i++)
		{
			activeBombs.Remove(destroyedBombs[i]);
		}
	}

	void UpdatePlayer(Player player)
	{
		for(int i = 0; i < player.Inputs.Length; i++)
		{
			bool updateAction = false;
			KeyInput input = player.Inputs[i];
			switch(input.State)
			{
				case KeyState.Up:
				{
					updateAction = Input.GetKeyUp(input.Key);
				}break;

				case KeyState.Down:
				{
					updateAction = Input.GetKeyDown(input.Key);
				}break;

				case KeyState.Press:
				{
					updateAction = Input.GetKey(input.Key);
				}break;
			}
			
			if(updateAction)
			{
				UpdateAction(player, input.Action);
			}
		}
	
		if(player.RemoteControlTimer > 0.0f)
		{
			player.RemoteControlTimer -= Time.deltaTime;
			player.RemoteBombText.text = string.Format(RemoteBombTextFormat, player.RemoteControlTimer);
		}

		ProcessIndex(player);
	}

	void UpdateGameTimer()
	{
		//Update Game Timer
		gameTimer -= Time.deltaTime;
		if(gameTimer < 0)
		{
			gameTimer = 0.0f;
		}
		int minutes = (int)gameTimer/60;
		int seconds = (int)(gameTimer - (minutes * 60));
		GameTimerText.text = string.Format("Timer - {0}:{1:00}", minutes, seconds);
		if(gameTimer <= 0.0f)
		{
			SetGameOver("It's a Draw!");
		}
	}

	void Update()
	{
		switch(gameState)
		{
			case GameState.InGame:
			{
				for(int i = 0; i < Players.Length; i++)
				{
					UpdatePlayer(Players[i]);
				}

				UpdateBombs();

				if(deadList.Count > 0)
				{
					string summaryText = "";
					if(deadList.Count == Players.Length)
					{
						summaryText = "Both got killed!!!";
					}
					else
					{
						if(deadList[0].DeadPlayerID == deadList[0].KillerID)
						{
							summaryText = string.Format("OMG, {0} did a suicide!!!", Players[deadList[0].DeadPlayerID].Name);
						}
						else
						{
							summaryText = string.Format("Yay, {0} killed {1}!!!", Players[deadList[0].KillerID].Name, Players[deadList[0].DeadPlayerID].Name);
						}
					}
					
					SetGameOver(summaryText);
					return;
				}

				UpdateGameTimer();			
			}break;

			case GameState.GameOver:
			{
				if(Input.GetKeyUp(KeyCode.R))
				{
					Restart();
				}
				else if(Input.GetKeyUp(KeyCode.Q))
				{
					Quit();
				}
			}break;
		}
	}

	void SetGameOver(string summaryText)
	{
		gameState = GameState.GameOver;
		GameOverPanelObj.SetActive(true);
		GameOverSummaryText.text = summaryText;
	}

	void AddDeadPlayer(int playerID, int killerID)
	{
		deadList.Add(new DeadInfo(playerID, killerID));
	}

	public void Restart()
	{
		Scene currentScene = SceneManager.GetActiveScene();
		SceneManager.LoadScene(currentScene.name);
	}

	public void Quit()
	{
		Application.Quit();
	}
}
