# Brief graphiste — vignettes des modèles de cartes mentales (Pupitre)

Pupitre est un organiseur de bureau Windows (WPF, sombre, moderne). Son panneau « Cartes mentales » propose une
galerie de **13 modèles** quand on crée une feuille. Chaque modèle doit être illustré par une vignette **belle,
propre, moderne**, dans l'esprit des miniatures de modèles de XMind (aplats nets, coins arrondis, lignes fines,
aucune texture, aucun dégradé lourd, aucune ombre portée marquée, **aucun texte** : les sujets sont des blocs vides).

## Format de livraison

- Un PNG par modèle, **476 × 236 px** (affiché à 238 × 118), fond **opaque** (pas de transparence), coins droits
  (Pupitre arrondit lui-même).
- Dossier : `assets-codex/templates/`, noms de fichiers exacts : `mind.png`, `logic.png`, `org.png`, `tree.png`,
  `timeline.png`, `vtimeline.png`, `fishbone.png`, `brainstorm.png`, `project.png`, `meeting.png`, `swot.png`,
  `week.png`, `orgup.png`.
- Même famille visuelle pour les 13 : mêmes épaisseurs de lignes, mêmes rayons d'arrondi, même échelle.

## Palette (thème « Arc-en-ciel », relevé sur XMind)

- Fond clair : `#F6F7F9` (ou blanc `#FFFFFF`).
- Sujet central : bleu nuit `#0F1A3C`, bloc arrondi, un peu plus grand que les autres.
- Sujets principaux (aplats) : rouge `#F0463C`, orange `#F5A33F`, jaune `#F3CC2E`, vert `#1FB573`, bleu `#3A6BF2`,
  violet `#4E3FB5`, rose `#E84393`, turquoise `#15B5C8` — dans cet ordre.
- Sous-sujets : la couleur de leur branche éclaircie (pastel, ~80 % vers le blanc).
- Lignes de branche : couleur de la branche, 2 à 3 px, bouts arrondis.
- Exceptions : `brainstorm` sur fond sombre `#24272F` avec sujets pleins très colorés en pilules ; `meeting` sur fond
  clair avec sujets principaux blancs à **bordure** colorée (thème « Classique »).

## Les 13 modèles (structure à représenter)

| Fichier | Structure | Détails |
|---|---|---|
| `mind.png` | Carte mentale bilatérale | centre au milieu, 2 branches à droite, 2 à gauche, lignes **courbes**, chaque branche avec 1–2 sous-sujets pastel |
| `logic.png` | Schéma logique vers la droite | centre à gauche, 3 sujets principaux empilés à droite, lignes **coudées** (angles droits), un ou deux sous-sujets |
| `org.png` | Organigramme vers le bas | centre en haut, 3 sujets en ligne dessous, lignes coudées, blocs **rectangulaires** peu arrondis |
| `tree.png` | Arborescence (liste indentée) | centre en haut à gauche, sujets empilés dessous avec retrait croissant, lignes coudées, sujets en **texte seul** (traits fins représentant du texte, sans bloc) |
| `timeline.png` | Chronologie horizontale | centre à gauche, **axe horizontal** épais bleu nuit, 4 étapes en pilules alternées au-dessus / en dessous reliées par de courtes tiges |
| `vtimeline.png` | Chronologie verticale | centre en haut, axe vertical, étapes alternées à droite / à gauche, lignes coudées |
| `fishbone.png` | Arête de poisson (Ishikawa) | tête (centre) à droite, colonne vertébrale horizontale, 4 arêtes diagonales alternées haut / bas avec un sujet au bout de chacune |
| `brainstorm.png` | Remue-méninges | bilatéral, fond sombre, sujets en **pilules** très colorées, 6 branches, lignes courbes |
| `project.png` | Plan de projet | schéma logique à droite avec 5 sujets principaux (objectifs, équipe, jalons, risques, budget), petites icônes stylisées facultatives mais **sans texte** |
| `meeting.png` | Compte rendu de réunion | schéma logique à droite, 4 sujets blancs à bordure colorée, sous-sujets soulignés d'un trait coloré |
| `swot.png` | Forces / faiblesses (SWOT) | bilatéral : 2 sujets à droite (vert, bleu), 2 à gauche (rouge, orange), blocs rectangulaires, lignes droites |
| `week.png` | Semaine | chronologie horizontale avec 5 étapes régulières (lundi à vendredi), pilules de couleurs successives |
| `orgup.png` | Organigramme vers le haut | centre en bas, 3 sujets en ligne au-dessus, lignes coudées |

## Contraintes

- Aucun texte, aucun chiffre, aucun logo. Pas de perspective, pas de 3D, pas de flou.
- Marges : rien ne touche les bords ; la structure occupe ~80 % de la largeur.
- Vérifier chaque fichier : dimensions exactes 476 × 236, PNG RGB opaque.
- Si l'outil d'image ne permet pas une taille exacte, générer plus grand puis redimensionner / recadrer avec un script
  (Node ou Python) placé dans `assets-codex/tools/`.
- À la fin, écrire `assets-codex/templates/LIVRAISON.md` : liste des fichiers produits, outil utilisé, remarques.
