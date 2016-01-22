using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Joueur : MonoBehaviour {

	new Rigidbody rigidbody;
	Vector3 deplacement;
    private int nombrePickUp=0;

    private int score = 0;
    public Text scoreText;
    public float vitesse;

    /* Initialisation */
    void Start () {

		rigidbody = GetComponent<Rigidbody> ();
        score = 0;

        /* On calcule le nombre de pick up */
        GameObject pickUps = GameObject.Find("Pick Ups");
        foreach (Transform child in pickUps.transform)
        {
            nombrePickUp++;
        }

        /* Affichage du score*/
        setScoreTexte();

    }

    /* Deplacement du joueur */
    void Update () {
		deplacement = new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical")).normalized * vitesse;
	}

	void FixedUpdate() {
		rigidbody.AddForce (deplacement);
	}
    
    /* Collision du joueur*/
	void OnTriggerEnter(Collider objet) 
	{
        /* Avec les pick ups */
        if (objet.gameObject.CompareTag("Pick Up"))
        {
            /* Disparition du pick up*/
            objet.gameObject.SetActive(false);

            /* Mise a jour du score et de l'affichage */
            score++;
            setScoreTexte();
        }
    }

    /* Affichage du score */
    public void setScoreTexte()
    {
        scoreText.text ="Cliquez pour changer de map. \n"+(nombrePickUp - score).ToString()+" cubes restants!";

        /* Tous les pick up ont été récupéré : victoire */
        if (score == nombrePickUp)
        {
            /* On passe au niveau suivant et on remet le score à 0 */
            score = 0;
            GameObject mG = GameObject.Find("Generateur de Map");
            MapGenerateur mapGen = mG.GetComponent<MapGenerateur>();
            mapGen.GenerationMap();
            setScoreTexte();
        }
    }

    /* Mutateur pour le score */ 
    public void setScore(int c)
    {
        score = c;
    }

}