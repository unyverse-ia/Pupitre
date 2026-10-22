# Brief graphiste — logo de Pupitre, style icônes Adobe Creative Cloud

Pupitre est un organiseur de bureau Windows professionnel (cadres de fichiers, tâches, notes, cartes mentales).
Le logo actuel (dossiers colorés + liste + note) est jugé enfantin. Il faut un logo **sobre, professionnel,
reconnaissable en 16 px comme en 256 px**, dans l'esprit des icônes des applications Adobe (Photoshop « Ps »,
Illustrator « Ai », Premiere « Pr », After Effects « Ae »).

## Codes du style Adobe à reproduire

- Un **carré aux coins arrondis** (rayon ≈ 18 à 20 % du côté), à plat, **aucun relief, aucune ombre, aucun dégradé,
  aucune texture, aucun reflet**.
- Fond : une couleur **sombre et profonde** (teinte foncée du coloris choisi).
- Un **monogramme de deux lettres « Pu »** (P majuscule, u minuscule), centré, en caractères **géométriques, gras,
  sans empattement** (esprit Adobe Clean / Helvetica Neue Bold), dans une **teinte claire et vive du même coloris**.
  Les lettres occupent environ 55 à 60 % de la hauteur du carré, bien centrées optiquement, interlettrage serré.
- Optionnel et discret : un **fin liseré intérieur** (1 à 1,5 % du côté) de la même couleur que les lettres, posé à
  2 à 3 % du bord, comme sur les icônes Adobe récentes.
- Rien d'autre : pas de dossier, pas de coche, pas de feuille, pas de symbole supplémentaire, pas de texte en plus.

## Quatre déclinaisons à livrer

| Fichier | Fond (sombre) | Lettres et liseré | Esprit |
|---|---|---|---|
| `logo-a.png` | bleu nuit `#1B2440` | bleu clair `#8FB4FF` (accent Pupitre `#5B8DEF` éclairci) | Photoshop / Premiere |
| `logo-b.png` | violet profond `#2A1E4A` | lavande `#C9B6FF` | After Effects |
| `logo-c.png` | vert sapin `#0F2E2A` | menthe `#7FE0C4` | Audition / XD |
| `logo-d.png` | brun chaud `#3A2210` | orange `#FFAA5C` | Illustrator |

## Format de livraison

- **1024 × 1024 px, PNG 32 bits avec transparence** : tout ce qui est hors du carré arrondi doit être **totalement
  transparent** (alpha 0), le carré lui-même totalement opaque. Le carré occupe 100 % de l'image (bords tangents).
- Dossier : `assets-codex/logo/`. Noms exacts ci-dessus.
- Si l'outil d'image ne garantit pas la transparence ou le rayon exact, génère le visuel puis **applique toi-même le
  masque de carré arrondi** (rayon 190 px sur 1024) avec un script PowerShell (System.Drawing) ou Node placé dans
  `assets-codex/tools/`, et vérifie : coins alpha 0, centre alpha 255.
- Mêmes proportions et même position des lettres sur les quatre versions (seules les couleurs changent).
- À la fin, écris `assets-codex/logo/LIVRAISON.md` : fichiers, outil, vérifications, écarts éventuels.
