using UnityEngine;
using System.Collections;

public class CameraControlleur : MonoBehaviour {

	public GameObject joueur;

	public Vector3 distance;

   
	void Start ()
	{
        /* Initialisation de l'ecart entre la camera et le joueur */
        distance = transform.position;
	}

	void LateUpdate ()
	{
        /* Mise à jour de la position de la camera selon la position du joueur */
        transform.position = joueur.transform.position + distance;
	}
}
