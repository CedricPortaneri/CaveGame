using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshGenerateur : MonoBehaviour {

    /* Grille de la map, possede non plus de simple case mais des carre comprenant chacun 8 points */
	public Grille grille;
	public MeshFilter murs;
	public MeshFilter cave;

	/*public bool est2D;*/
	public float hauteurMur = 3;

    /* Liste des sommets et des triangles permettant la création de mesh 3D */
	List<Vector3> sommets;
	List<int> triangles;

    /* Dictionnaire des triangles, regroupant pour chaque sommet leurs triangles associés */
	Dictionary<int,List<Triangle>> triangleDictionnaire = new Dictionary<int, List<Triangle>> ();

    /* Ensemble des contours interieurs de la map */
	List<List<int>> contours = new List<List<int>> ();

    /* Ensemble des positions des objets de jeu (joueur, pick up) */
    List<List<int>> objetPosition = new List<List<int>>();

    /* Indique les sommets déjà parcourus */
    HashSet<int> sommetsVus = new HashSet<int>();

    /* Génération des meshs 3D */
    public void GenerationMesh(int[,] map, float tailleCarre) {

        /*O vide les liste lors de la génération d'une autre nouvelle map */
		triangleDictionnaire.Clear ();
		contours.Clear ();
		sommetsVus.Clear ();
        objetPosition.Clear();

        /* Represente la map avec ses pieces et murs */
        grille = new Grille(map, tailleCarre);

        /* Listes des sommets et des triangles des meshs */
		sommets = new List<Vector3>();
		triangles = new List<int>();

        /* On va creer des triangles à partir des carrés en liant les nodes actives entre elles
        *  Cela permet d'avoir une map plus réaliste, moins carré.
        */
		for (int x = 0; x < grille.carres.GetLength(0); x ++) {
			for (int y = 0; y < grille.carres.GetLength(1); y ++) {
				TriangulationCarre(grille.carres[x,y]);
			}
		}

        /* Creation d'une mesh, comprend des sommets, des triangles et des uvs */
		Mesh mesh = new Mesh();
		cave.mesh = mesh;

		mesh.vertices = sommets.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();

        /* Les uvs servent à placer les textures */
		int tileAmount = 10;
		Vector2[] uvs = new Vector2[sommets.Count];
		for (int i =0; i < sommets.Count; i ++) {
			float percentX = Mathf.InverseLerp(-map.GetLength(0)/2*tailleCarre,map.GetLength(0)/2*tailleCarre,sommets[i].x) * tileAmount;
			float percentY = Mathf.InverseLerp(-map.GetLength(0)/2*tailleCarre,map.GetLength(0)/2*tailleCarre,sommets[i].z) * tileAmount;
			uvs[i] = new Vector2(percentX,percentY);
		}
		mesh.uv = uvs;

        /* Si le jeu est en 2D */
        /*if (est2D) {
			Generate2DColliders();
		} else {
			CreateWallMesh ();
		}*/

        /* On rajoute les meshs 3D des murs */
        CreationMeshMur();

        /* On ajoute sur la map les élements de jeu */
        System.Random pseudoRandom = new System.Random();
        Transform player = GameObject.Find("Joueur").transform;
        GameObject pickUps = GameObject.Find("Pick Ups");
        /* Position aléatoire du joueur */
        aleatoireObjetSurMap(pseudoRandom, new List<Transform>() { player }, map, tailleCarre);


        foreach (Transform child in pickUps.transform)
        {
            /* Position aléatoire des pick up */
            aleatoireObjetSurMap(pseudoRandom, new List<Transform>() { child }, map, tailleCarre);
        }


	}
		
    /* Fonction qui dispose une liste d'élement à un endroit alétoire sur la map */
	void aleatoireObjetSurMap(System.Random pseudoRandom,List <Transform> objs, int[,] map, float tailleCarre){
		int randX =-1;
		int randY =-1;
        bool randsOk = false;

        while (randsOk == false)
        {
            /* Choisi un position sur la map qui est bien entouré d'espace vide */
            do
            {
                randX = pseudoRandom.Next(0, map.GetLength(0));
                randY = pseudoRandom.Next(0, map.GetLength(1));
            } while (map[randX, randY] == 1 || map[randX + 1, randY] == 1 || map[randX - 1, randY] == 1 || map[randX, randY + 1] == 1 || map[randX, randY - 1] == 1);

            /* Verifie qu'on ne met pas deux objets à la même place */
            if (objetPosition.Count != 0)
            {
               
                foreach (List<int> objCoord in objetPosition)
                {
                    if (objCoord[0] != randX || objCoord[1] != randY)
                    {
                        randsOk = true;
                    }
                    else
                    {
                        randsOk = false;
                    }
                }
            }
            else
            {
                randsOk = true;
            }   

        }

        /* Positionne la liste d'objet objs aux positions randX et randY */
        foreach (Transform obj in objs)
        {
            obj.position = new Vector3(-map.GetLength(0) * tailleCarre / 2 + randX * tailleCarre + tailleCarre / 2, -hauteurMur + 0.5f, -map.GetLength(1) * tailleCarre / 2 + randY * tailleCarre + tailleCarre / 2);

        }

        /* Ajout des coordonnées des objets à la liste des positions */
        List<int> newCoord = new List<int>() { randX, randY };
        objetPosition.Add(newCoord);

    }

    /* Creer les meshs des murs */
	void CreationMeshMur() {

        /* On determine les countour interieur de la map ou les murs 3D vont être appliqués*/
		CalculMeshContours ();

        /* Construction de la mesh des murs */
		List<Vector3> sommetsMur = new List<Vector3> ();
		List<int> triangleMur = new List<int> ();
		Mesh meshMur = new Mesh ();

        /* Pour chaque contour on ajoute les 4 sommets du mur dans sommetsMur puis les deux triangles dans triangleMur
        * Une mesh affiche les triangles en prenant dans son attribu triangles les sommets 3 par 3 dans l'ordre
        */
        foreach (List<int> contour in contours) {
			for (int i = 0; i < contour.Count -1; i ++) {
				int startIndex = sommetsMur.Count;
                /* Sommets */
				sommetsMur.Add(sommets[contour[i]]); // gauche
				sommetsMur.Add(sommets[contour[i+1]]); // droit
				sommetsMur.Add(sommets[contour[i]] - Vector3.up * hauteurMur); // bas gauche
				sommetsMur.Add(sommets[contour[i+1]] - Vector3.up * hauteurMur); // bas droit

                /* Triangle */
				triangleMur.Add(startIndex + 0);
				triangleMur.Add(startIndex + 2);
				triangleMur.Add(startIndex + 3);
				triangleMur.Add(startIndex + 3);
				triangleMur.Add(startIndex + 1);
				triangleMur.Add(startIndex + 0);
			}
		}

		meshMur.vertices = sommetsMur.ToArray ();
		meshMur.triangles = triangleMur.ToArray ();
		murs.mesh = meshMur;

        /* Ajout d'un collider pour que le joueur ne puisse pas traverser le mur */
		MeshCollider murCollider = murs.gameObject.GetComponent<MeshCollider> ();
		murCollider.sharedMesh = meshMur;

        /* On met le sol sous les murs */
		GameObject sol = GameObject.Find ("Sol");
		sol.transform.position = new Vector3 (0,-hauteurMur,0);

	}

    /* Creer les colliders pour une version 2D */
	/* void Generate2DColliders() {

		EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D> ();
		for (int i = 0; i < currentColliders.Length; i++) {
			Destroy(currentColliders[i]);
		}

		CalculateMeshOutlines ();

		foreach (List<int> outline in contours) {
			EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
			Vector2[] edgePoints = new Vector2[outline.Count];

			for (int i =0; i < outline.Count; i ++) {
				edgePoints[i] = new Vector2(vertices[outline[i]].x,vertices[outline[i]].z);
			}
			edgeCollider.points = edgePoints;
		}

	}
    */

    /* A partir d'un carre et de ses node on construit des triangles en liants les nodes actives entre elles pour avoir une meilleur qualité de map */
	void TriangulationCarre(Carre carre) {

        /* Chaque configuration des carrés reprensente toutes les combinaisons possible avec 4 nodes de controles
        * Une node active signifira qu'il y a mur pres de celle-ci et donc on on entourera cette node on liant les nodes autours entre elles 
        * Cela va former plusieurs triangle au sein du carré et donc obtenir des formes interessantes pour une cave.
        * meshAvecNode va creer ces triangle en fonction des node selectionnés
        */

        switch (carre.configuration) {
		case 0:
			break;

			/* 1 points */
		case 1:
			mesh2DAvecNode(carre.centreGauche, carre.centreBas, carre.basGauche);
			break;
		case 2:
			mesh2DAvecNode(carre.basDroit, carre.centreBas, carre.centreDroit);
			break;
		case 4:
			mesh2DAvecNode(carre.hautDroit, carre.centreDroit, carre.centreHaut);
			break;
		case 8:
			mesh2DAvecNode(carre.hautGauche, carre.centreHaut, carre.centreGauche);
			break;

			/* 2 points */
		case 3:
			mesh2DAvecNode(carre.centreDroit, carre.basDroit, carre.basGauche, carre.centreGauche);
			break;
		case 6:
			mesh2DAvecNode(carre.centreHaut, carre.hautDroit, carre.basDroit, carre.centreBas);
			break;
		case 9:
			mesh2DAvecNode(carre.hautGauche, carre.centreHaut, carre.centreBas, carre.basGauche);
			break;
		case 12:
			mesh2DAvecNode(carre.hautGauche, carre.hautDroit, carre.centreDroit, carre.centreGauche);
			break;
		case 5:
			mesh2DAvecNode(carre.centreHaut, carre.hautDroit, carre.centreDroit, carre.centreBas, carre.basGauche, carre.centreGauche);
			break;
		case 10:
			mesh2DAvecNode(carre.hautGauche, carre.centreHaut, carre.centreDroit, carre.basDroit, carre.centreBas, carre.centreGauche);
			break;

			/* 3 points */
		case 7:
			mesh2DAvecNode(carre.centreHaut, carre.hautDroit, carre.basDroit, carre.basGauche, carre.centreGauche);
			break;
		case 11:
			mesh2DAvecNode(carre.hautGauche, carre.centreHaut, carre.centreDroit, carre.basDroit, carre.basGauche);
			break;
		case 13:
			mesh2DAvecNode(carre.hautGauche, carre.hautDroit, carre.centreDroit, carre.centreBas, carre.basGauche);
			break;
		case 14:
			mesh2DAvecNode(carre.hautGauche, carre.hautDroit, carre.basDroit, carre.centreBas, carre.centreGauche);
			break;

			/* 4 points: */
		case 15:
			mesh2DAvecNode(carre.hautGauche, carre.hautDroit, carre.basDroit, carre.basGauche);
            
            /* Comme il s'agit d'un pur carre de mur (et donc aucun contact avec une piece), ce ne peut etre un contour*/
            sommetsVus.Add(carre.hautGauche.sommetIndex);
			sommetsVus.Add(carre.hautDroit.sommetIndex);
			sommetsVus.Add(carre.basDroit.sommetIndex);
			sommetsVus.Add(carre.basGauche.sommetIndex);
			break;
		}

	}

    /* On va definir la mesh 2D composé de triangles avec les nodes pour remplacer un carre de mur */
	void mesh2DAvecNode(params Node[] points) {
        /* On recupere les sommets */
        AssigneSommets(points);

        /* On creer les differents triangles qui compose cette mesh 2D en fonction des sommets */
		if (points.Length >= 3)
			CreerTriangle(points[0], points[1], points[2]);
		if (points.Length >= 4)
			CreerTriangle(points[0], points[2], points[3]);
		if (points.Length >= 5) 
			CreerTriangle(points[0], points[3], points[4]);
		if (points.Length >= 6)
			CreerTriangle(points[0], points[4], points[5]);

	}

    /* Mise a jour des sommets traités */
	void AssigneSommets(Node[] points) {
		for (int i = 0; i < points.Length; i ++) {
			if (points[i].sommetIndex == -1)
                {
                /* Pour chaque point on va definir un index incremental */
				points[i].sommetIndex = sommets.Count;
                /* Ajout à la liste des sommets les points traité pour pouvoir ajouter les murs par la suite */
                sommets.Add(points[i].position);
			}
		}
	}

    /* Creation d'un simple triangle a partir de 3 points */
	void CreerTriangle(Node a, Node b, Node c) {
        /* Ajout dans l'ordre des sommets du triangle dans la listes triangles qui generera par la suite automatiquement les mesh*/
		triangles.Add(a.sommetIndex);
		triangles.Add(b.sommetIndex);
		triangles.Add(c.sommetIndex);

        /* Création du triangle et ajout de celui-ci dans le dictionnaire des triangles et cela pour chaque sommets
        * Le dictionnaire permet pour chaque sommet de voir les triangles qui lui sont attribués
        * Par la suite ce dictionnaire indiquera quels sommets font partir des contours interieur de la map 
        * (pas plus de deux triangles attribués pour une paire de sommet)
        */
		Triangle triangle = new Triangle (a.sommetIndex, b.sommetIndex, c.sommetIndex);
		AjouteTriangleDictionnaire (triangle.sommetIndexA, triangle);
		AjouteTriangleDictionnaire (triangle.sommetIndexB, triangle);
		AjouteTriangleDictionnaire (triangle.sommetIndexC, triangle);
	}

    /* Ajout d'un triangle au dictionnaire selon l'indice d'un sommet 
    *  Le sommet est la clé et les triangles les valeurs
    */
	void AjouteTriangleDictionnaire(int indiceSommet, Triangle triangle) {
        /* Si le dictionnaire connait deja ce sommet, on ajoute ce triangle a sa liste */
		if (triangleDictionnaire.ContainsKey (indiceSommet)) {
			triangleDictionnaire [indiceSommet].Add (triangle);
        /* Sinon on ajoute un nouveau sommet a ce dictionnaire avec le triangle */
		} else {
			List<Triangle> triangleList = new List<Triangle>();
			triangleList.Add(triangle);
			triangleDictionnaire.Add(indiceSommet, triangleList);
		}
	}

    /* Determination du contour interieur de la map */
	void CalculMeshContours() {

        /* Pour tout les sommets, on va determiner tout les contours interieurs*/
		for (int sommetIndex = 0; sommetIndex < sommets.Count; sommetIndex ++) {
			if (!sommetsVus.Contains(sommetIndex)) {

                /* Si ce sommet à un voisin qui fait parti du contour , on le recupere et il fait parti du contour interieur */
				int nouveauSommetIndex = connectionSommetInterieur(sommetIndex);
				if (nouveauSommetIndex != -1) {
					sommetsVus.Add(sommetIndex);

                    /* Creation d'un nouveau contour interieur, ajout à l'ensemble de contours defini par contours */
					List<int> nouveauContour = new List<int>();
					nouveauContour.Add(sommetIndex);
					contours.Add(nouveauContour);
                    /* On commence la recursion */
					poursuiteContourInterieur(nouveauSommetIndex, contours.Count-1);
                    /* On boucle le contour interieur*/
					contours[contours.Count-1].Add(sommetIndex);
				}
			}
		}
	}

    /* Fonction recursive qui creer la liste des sommets faisant parti du contour interieux*/
	void poursuiteContourInterieur(int sommetIndex, int countourIndex) {
        /* Le sommet fait parti du contour defini par contourIndex et on l'a vu */
		contours [countourIndex].Add (sommetIndex);
		sommetsVus.Add (sommetIndex);

        /* Recuperation du prochain sommet du contour */
		int nextVertexIndex = connectionSommetInterieur (sommetIndex);

        /* Recursion de la fonction si on trouve un suivant */
		if (nextVertexIndex != -1) {
			poursuiteContourInterieur(nextVertexIndex, countourIndex);
		}
	}

    /* A partir d'un sommet, cherche un second sommet qui fait parti du contour interieur*/
	int connectionSommetInterieur(int sommetIndex) {

		List<Triangle> trianglesPossedantSommets = triangleDictionnaire [sommetIndex];
        /* On parcours tous les triangles */
		for (int i = 0; i < trianglesPossedantSommets.Count; i ++) {
            Triangle triangle = trianglesPossedantSommets[i];
            
            /* Pour les 3 sommets du triangle */
			for (int j = 0; j < 3; j ++) {
				int vertexB = triangle[j];
                /* Si ce sommet B n'est pas A, qu'on ne la pas deja vu et qu'il fait parti du contour, on le renvoie  */
				if (vertexB != sommetIndex && !sommetsVus.Contains(vertexB)) {
					if (estContour(sommetIndex, vertexB)) {
						return vertexB;
					}
				}
			}
		}

		return -1;
	}

    /* Renvoie vrai si le trait defini par les sommets A et B fait parti du contour interieur de la map */
	bool estContour(int sommetA, int sommetB) {
		List<Triangle> trianglePossegantSommetA = triangleDictionnaire [sommetA];
		int nbTrianglePartage = 0;

        /*  On regarde si les deux sommets partage plus d'un triangle, si oui ce n'est pas un contour */
		for (int i = 0; i < trianglePossegantSommetA.Count; i ++) {
			if (trianglePossegantSommetA[i].Contiens(sommetB)) {
				nbTrianglePartage ++;
				if (nbTrianglePartage > 1) {
					break;
				}
			}
		}
		return nbTrianglePartage == 1;
	}

    /* Structure géométrique d'un triangle pour la création de mesh */
	struct Triangle {
		public int sommetIndexA;
		public int sommetIndexB;
		public int sommetIndexC;
		int[] sommets;

		public Triangle (int a, int b, int c) {
			sommetIndexA = a;
			sommetIndexB = b;
			sommetIndexC = c;

			sommets = new int[3];
			sommets[0] = a;
			sommets[1] = b;
			sommets[2] = c;
		}

        /* Implementation d'un accesseur de sommet plus pratique  */
		public int this[int i] {
			get {
				return sommets[i];
			}
		}

        /* Retourne vrai si le rectangle contient un sommet defini par sommetIndex */
		public bool Contiens(int sommetIndex) {
			return sommetIndex == sommetIndexA || sommetIndex == sommetIndexB || sommetIndex == sommetIndexC;
		}
	}

    /* Ensemble des carrés du la map */
	public class Grille {
		public Carre[,] carres;

		public Grille(int[,] map, float tailleCarre) {
			int nbNodeX = map.GetLength(0);
			int nbNodeY = map.GetLength(1);
			float largeurMap = nbNodeX * tailleCarre;
			float hauteurMap = nbNodeY * tailleCarre;

            /* On met sur la grille l'ensemble des nodes de controles*/
            ControlNode[,] controlNodes = new ControlNode[nbNodeX, nbNodeY];
			for (int x = 0; x < nbNodeX; x ++) {
				for (int y = 0; y < nbNodeY; y ++) {
					Vector3 pos = new Vector3(-largeurMap/2 + x * tailleCarre + tailleCarre/2, 0, -hauteurMap/2 + y * tailleCarre + tailleCarre/2);
					controlNodes[x,y] = new ControlNode(pos,map[x,y] == 1, tailleCarre);
				}
			}
            /* Puis les carrés contenant les nodes simple */
			carres = new Carre[nbNodeX -1,nbNodeY -1];
			for (int x = 0; x < nbNodeX-1; x ++) {
				for (int y = 0; y < nbNodeY-1; y ++) {
					carres[x,y] = new Carre(controlNodes[x,y+1], controlNodes[x+1,y+1], controlNodes[x+1,y], controlNodes[x,y]);
				}
			}

		}
	}

    /* Carré (mur ou sol) optimisé pour atteindre différentes formes et donc un réalisme plus important */
	public class Carre {
        /* 4 Node de controlle */
		public ControlNode hautGauche, hautDroit, basDroit, basGauche;

        /* 4 Node simple aux centre des cotés */
		public Node centreHaut, centreDroit, centreBas, centreGauche;

        /* Indique quelles sont les nodes active pour representer les differentes formes découpés du carrés */
		public int configuration;

		public Carre (ControlNode _hautGauche, ControlNode _hautDroit, ControlNode _basDroit, ControlNode _basGauche) {
			hautGauche = _hautGauche;
			hautDroit = _hautDroit;
			basDroit = _basDroit;
			basGauche = _basGauche;

			centreHaut = hautGauche.droite;
			centreDroit = basDroit.dessus;
			centreBas = basGauche.droite;
			centreGauche = basGauche.dessus;

            /* Code binaire pour déterminer les configurations */
			if (hautGauche.active)
				configuration += 8;
			if (hautDroit.active)
				configuration += 4;
			if (basDroit.active)
				configuration += 2;
			if (basGauche.active)
				configuration += 1;
		}

	}

    /* Node simple sur une case (sur les centres de chaque coté)  */
	public class Node {
		public Vector3 position;
		public int sommetIndex = -1;

		public Node(Vector3 _pos) {
			position = _pos;
		}
	}

    /* Node de controle sur une case qui permet de diviser un carre en plusieur partie (sur les coins) */
	public class ControlNode : Node {

		public bool active;
		public Node dessus, droite;

		public ControlNode(Vector3 _pos, bool _active, float tailleCarre) : base(_pos) {
			active = _active;
			dessus = new Node(position + Vector3.forward * tailleCarre/2f);
			droite = new Node(position + Vector3.right * tailleCarre/2f);
		}

	}
}
 