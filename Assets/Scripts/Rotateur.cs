using UnityEngine;
using System.Collections;

public class Rotateur : MonoBehaviour {

    /* Fait tourner les pick ups */
	void Update () 
	{
		transform.Rotate (new Vector3 (15, 30, 45) * Time.deltaTime);
	}
}
