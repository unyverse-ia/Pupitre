# Brief graphiste — logo de Pupitre, 4 concepts uniques (v2)

La première livraison (monogramme « Pu » sur carré sombre) a été refusée : c'est une copie des icônes Adobe. Ce que
l'on garde d'Adobe, c'est **le niveau de finition** : icône à plat, géométrie impeccable, deux ou trois couleurs,
lisible de 16 px à 256 px, aucun effet. Ce que l'on veut maintenant : **un symbole original qui représente le concept
de Pupitre**, et **aucune lettre, aucun texte**.

## Le concept à représenter

Pupitre est un organiseur de bureau Windows : il range les icônes du bureau dans des **cadres** (zones rectangulaires
arrondies posées sur le fond d'écran), avec des panneaux **Tâches**, **Notes** et **Cartes mentales**. Le mot
« pupitre » évoque aussi le pupitre d'écolier, le pupitre de chef d'orchestre, le pupitre de commande : une surface
inclinée où tout est à sa place, sous les yeux, bien ordonné. Idées fortes : **ordre, cadres, surface de travail,
vue d'ensemble, calme**.

## Quatre concepts à livrer (un fichier chacun, tous différents)

| Fichier | Concept | Ce qu'on voit |
|---|---|---|
| `logo-a.png` | **Le pupitre** | la silhouette géométrique d'un pupitre vu de côté ou de trois quarts très stylisé : un plateau légèrement incliné posé sur un pied, avec une ou deux petites feuilles / cartes posées dessus ; formes pleines, angles arrondis |
| `logo-b.png` | **Les cadres** | trois ou quatre rectangles arrondis de tailles différentes, parfaitement alignés sur une grille, comme des cadres rangés sur un bureau ; un seul d'entre eux porte une couleur d'accent, les autres sont dans une teinte plus douce |
| `logo-c.png` | **La surface ordonnée** | un plateau (grand rectangle arrondi vu de dessus) sur lequel sont rangés quelques éléments simplifiés : une pile de deux feuilles, une coche de tâche, un petit nœud de carte mentale (un point relié à deux points) ; composition équilibrée, très épurée |
| `logo-d.png` | **Le symbole abstrait** | une forme unique et mémorable dérivée de la lettre P sans être une lettre : par exemple un cadre arrondi dont un côté se prolonge en pied de pupitre, ou un plateau incliné dont l'ombre portée plate forme un second cadre ; doit rester lisible en 16 px |

## Style commun (obligatoire)

- **À plat, sans 3D** : aucune perspective, aucun volume, aucune ombre portée, aucun dégradé, aucun reflet, aucune
  texture, aucun contour noir.
- Palette de Pupitre : fond **bleu nuit** `#1B2440` ou **bleu Pupitre** `#5B8DEF` ; symbole en **blanc** `#FFFFFF`,
  **bleu clair** `#8FB4FF` et, pour l'accent, **corail** `#F0463C` ou **ambre** `#F5A33F` (une seule couleur
  d'accent par logo, utilisée avec parcimonie).
- Le symbole est posé dans un **carré aux coins arrondis** (rayon ≈ 20 % du côté) qui occupe toute l'image ; le
  symbole occupe environ 60 % du carré, centré.
- Traits et formes pleines, épaisseurs généreuses (lisibilité à 16 px) ; pas de détails fins.

## Format de livraison

- **1024 × 1024 px, PNG 32 bits**, extérieur du carré arrondi **transparent** (alpha 0), intérieur opaque.
- Dossier `assets-codex/logo2/`, noms exacts `logo-a.png` à `logo-d.png`.
- Appliquer soi-même le masque de carré arrondi (rayon 190 px) avec le script de `assets-codex/tools/` et vérifier :
  pixel (2,2) alpha 0, pixel (512,512) alpha 255.
- Écrire `assets-codex/logo2/LIVRAISON.md` : pour chaque fichier, une phrase sur le concept retenu, l'outil,
  les vérifications, les écarts éventuels.
