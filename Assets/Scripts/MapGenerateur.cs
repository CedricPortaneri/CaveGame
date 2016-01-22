using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MapGenerateur : MonoBehaviour {

    /* Dimension de la map */
	public int largeur;
	public int hauteur;

    /* Seed de la carte aléatoire */
	public string seed;
	public bool utilisationSeedAleatoire = true;

    /* Pourcentage de mur sur la map */
	[Range(0,100)]
	public int pourcentageMur;

    /* Taille minimums des murs et des pieces */
    public int tailleMinimumMur = 50;
	public int tailleMinimumPiece = 50;

    /* Taille du bord de mur autour de la map */
    public int tailleBordure = 1;

    /* Taille des passages entre les pièces */
    public int taillePassage = 1;

    /* Map (1: mur, 0: pièce) */
	int[,] map;

    /* Initialisation */
	void Start() {
		GenerationMap();
	}

	void Update() {
        /* Cliquer dans le jeu changera de map */
        if (Input.GetMouseButtonDown(0)) {
            ChangeMap();
        }
		if (Input.GetKey("escape"))
			Application.Quit();
	}

    /* Changement de map */
    void ChangeMap()
    {
        GenerationMap();
        GameObject joueurObj = GameObject.Find("Joueur");
        Joueur joueur = joueurObj.GetComponent<Joueur>();
        joueur.setScore(0);
        joueur.setScoreTexte();
    }

    /* Génération d'une map */
	public void GenerationMap() {

		map = new int[largeur,hauteur];

        /* On rempli aléatoirement la map de mur et de piece selon le pourcentage de mur */
		remplissageMapAleatoire();

        /* Optimise et améliore la map */
        optimisationMap();

        /* Ajout de la bordure de mur pour ne pas sortir de la map */
		int[,] mapBordure = new int[largeur + tailleBordure * 2,hauteur + tailleBordure * 2];
		for (int x = 0; x < mapBordure.GetLength(0); x ++) {
			for (int y = 0; y < mapBordure.GetLength(1); y ++) {
				if (x >= tailleBordure && x < largeur + tailleBordure && y >= tailleBordure && y < hauteur + tailleBordure) {
					mapBordure[x,y] = map[x-tailleBordure,y-tailleBordure];
				}
				else {
					mapBordure[x,y] = 1;
				}
			}
		}

        /* Ajout des murs 3D */
		MeshGenerateur meshGen = GetComponent<MeshGenerateur>();
		meshGen.GenerationMesh(mapBordure, 1);

	}

    /* Remplissage de la map aléatoirement de mur */
    void remplissageMapAleatoire()
    {
        /* Utilisation du HashCode du temps qui s'ecoule comme seed de l'aléatoire */
        if (utilisationSeedAleatoire)
        {
            seed = Time.time.ToString();
        }
        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < largeur; x++)
        {
            for (int y = 0; y < hauteur; y++)
            {
                /* Securité autour de la bordure */
                if (x == 0 || x == largeur - 1 || y == 0 || y == hauteur - 1)
                {
                    map[x, y] = 1;
                }

                /* Choisi aleatoirement de mettre un mur ou non */
                else {
                    map[x, y] = (pseudoRandom.Next(0, 100) < pourcentageMur) ? 1 : 0;
                }
            }
        }
    }

    /* Optimise la map pour obtenir une zone de jeu adéquate
     *  - Lissage de la map
     *  - Taille minimum des murs et des pièces
     *  - Connexion des pièces
     */
    void optimisationMap() {

        /* On repete le processus de lissage de la map pour avoir une certaine coherence dans le résultat */
        for (int i = 0; i < 5; i++)
        {
            lissageMap();
        }

        /* Suppression des blocs de mur inférieur au seuil limite */
        List<List<Coord>> regionsMur = GetRegions (1);
		foreach (List<Coord> regionMur in regionsMur) {
			if (regionMur.Count < tailleMinimumMur) {
				foreach (Coord coord in regionMur) {
					map[coord.coordX,coord.coordY] = 0;
				}
			}
		}

        /* Suppression des pièces inférieur au seuil limite et conservation des pieces restantes dans piecesRestantes */
        List<List<Coord>> regionsPiece = GetRegions (0);
		List<Salle> piecesRestantes = new List<Salle> ();
		foreach (List<Coord> regionPiece in regionsPiece) {
			if (regionPiece.Count < tailleMinimumPiece) {
				foreach (Coord coord in regionPiece) {
					map[coord.coordX,coord.coordY] = 1;
				}
			}
			else {
				piecesRestantes.Add(new Salle(regionPiece, map));
			}
		}

        /* Trie les pieces restantes par taille */
		piecesRestantes.Sort ();

        /* La plus grosse pièce est la piece principale */
		piecesRestantes [0].estSallePrincipale = true;
		piecesRestantes [0].estAccessibleSallePrincipale = true;

        /* Relie les autres pieces restantes à la piece principale */
		ConnectePieces (piecesRestantes);
	}

    /* Lissage des contours des pièces la map */
    void lissageMap()
    {
        for (int x = 0; x < largeur; x++)
        {
            for (int y = 0; y < hauteur; y++)
            {
                /* On regarde pour chaque case le nombre de mur juste autour */
                int nbMurVoisin = nbMurVoisins(x, y);

                /* Si il y a plus de mur que d'espace alors cette case sera un mur et inversement */
                if (nbMurVoisin > 4)
                    map[x, y] = 1;
                else if (nbMurVoisin < 4)
                    map[x, y] = 0;
            }
        }
    }

    /* Renvoie le nombre de mur voisin d'un case */
    int nbMurVoisins(int x, int y)
    {
        int nbMur = 0;

        /* Parcours des cases autour de la cible */
        for (int voisinX = x - 1; voisinX <= x + 1; voisinX++)
        {
            for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
            {
                /* Verification que le voisin est bien dans la map */
                if (estDansMap(voisinX, neighbourY))
                {
                    /* Verification que le voisin n'est pas la cible */
                    if (voisinX != x || neighbourY != y)
                    {
                        /* On ajoute 1 si il s'agit d'un mur, 0 sinon */
                        nbMur += map[voisinX, neighbourY];
                    }
                }

                /* On considere ce qui est en dehors de la map comme des mur */
                else {
                    nbMur++;
                }
            }
        }

        return nbMur;
    }

    /* Retourne vrai si des coordonnées sont bien sur la map */
    bool estDansMap(int x, int y)
    {
        return x >= 0 && x < largeur && y >= 0 && y < hauteur;
    }

    /* Relie toutes les pièces de la map à la pièce principale
    * La fonction sera utilisé de manière récursive, commencant par lier les salles les plus proches entre elles, puis avec la salle principale
    */
    void ConnectePieces(List<Salle> toutesPieces, bool forceAccessibiliteDePiecePrincipale = false) {

		List<Salle> salleListeA = new List<Salle> ();
		List<Salle> salleListeB = new List<Salle> ();

        /* Seconde phase de la fonction */
		if (forceAccessibiliteDePiecePrincipale) {
            /* On met les salles qui ont accès à la salle pricipale dans salleListeB, sinon salleListeA */
			foreach (Salle salle in toutesPieces) {
				if (salle.estAccessibleSallePrincipale) {
					salleListeB.Add (salle);
				} else {
					salleListeA.Add (salle);
				}
			}
        /* Mais on commence par lier les salles les plus proches entre elles */
		} else {
			salleListeA = toutesPieces;
			salleListeB = toutesPieces;
		}


		int meilleurDistance = 0;
		Coord meilleurCoordA = new Coord ();
		Coord meilleurCoordB = new Coord ();
		Salle meilleurSalleA = new Salle ();
		Salle meilleurSalleB = new Salle ();
		bool possibleConnectionTrouve = false;

		foreach (Salle salleA in salleListeA) {

            /* Pour la première phase on lie qu'une fois les salle entre elles */
            if (!forceAccessibiliteDePiecePrincipale) {
				possibleConnectionTrouve = false;
				if (salleA.salleConnecte.Count > 0) {
					continue;
				}
			}

			foreach (Salle salleB in salleListeB) {

                /* On évite d'avoir deux fois la même salle ou une salle déjà connecté à A*/
				if (salleA == salleB || salleA.estConnecte(salleB)) {
					continue;
				}

                /* On récupère la distance entre les deux salles et les cases de contour les plus proches entre A et B */
				for (int caseIndexA = 0; caseIndexA < salleA.contourCase.Count; caseIndexA ++) {
					for (int caseIndexB = 0; caseIndexB < salleB.contourCase.Count; caseIndexB ++) {
						Coord caseA = salleA.contourCase[caseIndexA];
						Coord caseB = salleB.contourCase[caseIndexB];
						int distanceEntreSalles = (int)(Mathf.Pow (caseA.coordX-caseB.coordX,2) + Mathf.Pow (caseA.coordY-caseB.coordY,2));
                        /* Cherche la distance minimum */
						if (distanceEntreSalles < meilleurDistance || !possibleConnectionTrouve) {
							meilleurDistance = distanceEntreSalles;
							possibleConnectionTrouve = true;
							meilleurCoordA = caseA;
							meilleurCoordB = caseB;
							meilleurSalleA = salleA;
							meilleurSalleB = salleB;
						}
					}
				}
			}

            /* Si une connection a été trouvé on créé un passage entre les deux salles */
			if (possibleConnectionTrouve && !forceAccessibiliteDePiecePrincipale) {
				CreePassage(meilleurSalleA, meilleurSalleB, meilleurCoordA, meilleurCoordB);
			}
		}

        /* On répete la phase 2 jusqu'il n'y ait plus de connection possible*/
		if (possibleConnectionTrouve && forceAccessibiliteDePiecePrincipale) {
			CreePassage(meilleurSalleA, meilleurSalleB, meilleurCoordA, meilleurCoordB);
			ConnectePieces(toutesPieces, true);
		}

        /* On passe à la phase 2 */
		if (!forceAccessibiliteDePiecePrincipale) {
			ConnectePieces(toutesPieces, true);
		}
	}

    /* Cree un passage entre deux salles */
	void CreePassage(Salle roomA, Salle roomB, Coord tileA, Coord tileB) {
        
        /* Lie les salles entre elles */
		Salle.ConnecteSalles (roomA, roomB);

        /* Creer une liste de coordonnée qui correspond à la ligne qu'on va creuser */
		List<Coord> line = getLigne (tileA, tileB);

        /* Creuse le passage à traver le mur */
		foreach (Coord c in line) {
			dessineCercle(c,taillePassage);
		}

	}

    /* Creuse un cercle de rayon r positioné à c */
	void dessineCercle(Coord c, int r) {
		for (int x = -r; x <= r; x++) {
			for (int y = -r; y <= r; y++) {
				if (x*x + y*y <= r*r) {
					int creuseX = c.coordX + x;
					int creuseY = c.coordY + y;
					if (estDansMap(creuseX, creuseY)) {
                        /* On enleve le mur */
						map[creuseX,creuseY] = 0;
					}
				}
			}
		}
	}

    /* Fonction mathématique qui permet de retourner une liste de Coordonnées correspondant à une ligne entre A et B */
	List<Coord> getLigne(Coord A, Coord B) {
		List<Coord> ligne = new List<Coord> ();

		int x = A.coordX;
		int y = A.coordY;

		int dx = B.coordX - A.coordX;
		int dy = B.coordY - A.coordY;

		bool inverse = false;
		int etape = Math.Sign (dx);
		int gradientetape = Math.Sign (dy);

		int plusLong = Mathf.Abs (dx);
		int plusCourt = Mathf.Abs (dy);

		if (plusLong < plusCourt) {
			inverse = true;
			plusLong = Mathf.Abs(dy);
			plusCourt = Mathf.Abs(dx);

			etape = Math.Sign (dy);
			gradientetape = Math.Sign (dx);
		}

		int gradientAccumulation = plusLong / 2;
		for (int i =0; i < plusLong; i ++) {
			ligne.Add(new Coord(x,y));

			if (inverse) {
				y += etape;
			}
			else {
				x += etape;
			}

			gradientAccumulation += plusCourt;
			if (gradientAccumulation >= plusLong) {
				if (inverse) {
					x += gradientetape;
				}
				else {
					y += gradientetape;
				}
				gradientAccumulation -= plusLong;
			}
		}

		return ligne;
	}

    /* Convertie les coordonnée des case en vrai coordonnée 3D de la scene */
	Vector3 CoordVersPoint3D(Coord tile) {
		return new Vector3 (-largeur / 2 + .5f + tile.coordX, 2, -hauteur / 2 + .5f + tile.coordY);
	}

    /* Recupere toute les regions de type mur ou salle */
	List<List<Coord>> GetRegions(int typeCase) {
        List<List<Coord>> regions = new List<List<Coord>>();
        /* Flags pour savoir si on a déjà vu ces cases */
		int[,] mapFlags = new int[largeur,hauteur];

		for (int x = 0; x < largeur; x ++) {
			for (int y = 0; y < hauteur; y ++) {
                /* Si on a pas déjà vu et qu'il s'agit bien d'une région qu'on recherche */
				if (mapFlags[x,y] == 0 && map[x,y] == typeCase) {
					List<Coord> nouvelleRegion = GetRegionCases(x,y);
					regions.Add(nouvelleRegion);

                    /* Note les case déjà visité */
					foreach (Coord cases in nouvelleRegion) {
						mapFlags[cases.coordX, cases.coordY] = 1;
					}
				}
			}
		}

		return regions;
	}

    /* Recupere toute les case d'un région (mure ou salle) à partir d'une case d'origine */
	List<Coord> GetRegionCases(int debutX, int debutY) {

		List<Coord> cases = new List<Coord> ();
		int[,] mapFlags = new int[largeur,hauteur];
		int tileType = map [debutX, debutY];

        /* Utilisation de la structure queue pour l'aglorithme de "remplissage" */
		Queue<Coord> queue = new Queue<Coord> ();
		queue.Enqueue (new Coord (debutX, debutY));
		mapFlags [debutX, debutY] = 1;

        /* Jusqu'a ce que la queue est vide */
		while (queue.Count > 0) {
            /* On ajoute a la liste finale de le dernier element de la queue */
			Coord Case = queue.Dequeue();
			cases.Add(Case);

            /* On ajoute à la queue les cases autours si elles sont bien dans la map, 
            * qu'on ne les a pas déjà ajoutés et qu'elles sont du meme types que la case initiale
			*/
            for (int x = Case.coordX - 1; x <= Case.coordX + 1; x++) {
				for (int y = Case.coordY - 1; y <= Case.coordY + 1; y++) {
					if (estDansMap(x,y) && (y == Case.coordY || x == Case.coordX)) {
						if (mapFlags[x,y] == 0 && map[x,y] == tileType) {
							mapFlags[x,y] = 1;
							queue.Enqueue(new Coord(x,y));
						}
					}
				}
			}
		}

		return cases;
	}

    /* Structure de coordonnée */
	struct Coord {
		public int coordX;
		public int coordY;

		public Coord(int x, int y) {
			coordX = x;
			coordY = y;
		}
	}

    /* Structure de salle (comparable par le nombre de cases) */
	class Salle : IComparable<Salle> {

		public List<Coord> cases;
		public List<Coord> contourCase;
		public List<Salle> salleConnecte;
		public int taille;
		public bool estAccessibleSallePrincipale;
		public bool estSallePrincipale;

		public Salle() {
		}

        /* Constructeur */
		public Salle(List<Coord> casesSalle, int[,] map) {
			cases = casesSalle;
			taille = cases.Count;
			salleConnecte = new List<Salle>();

            /* Initialisation des contour de la salle*/
            contourCase = new List<Coord>();
			foreach (Coord Case in cases) {
				for (int x = Case.coordX-1; x <= Case.coordX+1; x++) {
					for (int y = Case.coordY-1; y <= Case.coordY+1; y++) {
						if (x == Case.coordX || y == Case.coordY) {
							if (map[x,y] == 1) {
								contourCase.Add(Case);
							}
						}
					}
				}
			}

		}

        /* Indique que la salle est accessible via la salle pricipale */
		public void setEstAccessibleSallePrincipale() {
			if (!estAccessibleSallePrincipale) {
				estAccessibleSallePrincipale = true;
                /* Donc que toute les salle connectées à celle ci le sont également */
				foreach (Salle salle in salleConnecte) {
					salle.setEstAccessibleSallePrincipale();
				}
			}
		}

        /* Connecte deux salle, A et B */
		public static void ConnecteSalles(Salle salleA, Salle roomB) {
			if (salleA.estAccessibleSallePrincipale) {
				roomB.setEstAccessibleSallePrincipale ();
			} else if (roomB.estAccessibleSallePrincipale) {
				salleA.setEstAccessibleSallePrincipale();
			}
			salleA.salleConnecte.Add (roomB);
			roomB.salleConnecte.Add (salleA);
		}

        /* Une salle est connecté a une autre si elle est dans sa liste salle connecté */
		public bool estConnecte(Salle autreSalle) {
			return salleConnecte.Contains(autreSalle);
		}

        /* On compare les salle par leur taille */
		public int CompareTo(Salle autreSalle) {
			return autreSalle.taille.CompareTo (taille);
		}
	}
}