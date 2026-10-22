# Livraison des vignettes

## Fichiers produits

- `mind.png`
- `logic.png`
- `org.png`
- `tree.png`
- `timeline.png`
- `vtimeline.png`
- `fishbone.png`
- `brainstorm.png`
- `project.png`
- `meeting.png`
- `swot.png`
- `week.png`
- `orgup.png`

## Outils

- Génération : ImageGen intégré, une génération distincte par modèle.
- Normalisation : `assets-codex/tools/Resize-Templates.ps1` avec `System.Drawing`.
- Import reproductible des sources générées : `assets-codex/tools/Import-Generated.ps1`.

## Vérifications

- Les 13 fichiers sont des PNG opaques en RGB 24 bits.
- Dimensions vérifiées : 476 × 236 px pour chaque fichier.
- Aucun texte, chiffre ou logo n'est présent.
- Les structures, couleurs et fonds suivent le brief ; `brainstorm.png` utilise le fond sombre et `meeting.png` les blocs blancs bordés.

## Remarques

- `fishbone.png` a été régénéré une fois afin d'obtenir exactement quatre arêtes.
- ImageGen a introduit sur certains aplats une très légère variation de luminosité malgré la consigne d'aplat strict. Aucun dégradé lourd, ombre portée marquée ou texture n'a été conservé.
- `tree.png` a été refaite avec ImageGen, recadrée à 476 × 236 px et contrôlée en PNG RGB opaque.
