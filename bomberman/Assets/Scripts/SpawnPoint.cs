using UnityEngine;
using System.Collections;

public class SpawnPoint : MonoBehaviour 
{
	
	void Awake () 
	{
		Destroy(GetComponent<Renderer>());		
	}

}
