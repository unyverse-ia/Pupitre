# Pupitre — organiseur de bureau, tâches et notes (Windows 11)

Pupitre pose sur le bureau des **cadres** en verre dépoli qui regroupent fichiers et raccourcis
(comme Fences), un **panneau Tâches** façon liste de projets (projets → sous-tâches, service,
priorité, échéance) et un **panneau Notes** (bibliothèque de notes réordonnables). Un seul fichier,
`dist\Pupitre.exe`, **100 % hors ligne**, sans droits administrateur.

## Les cadres

| Action | Comment |
|---|---|
| Ce qu'affiche un cadre | Un dossier. Le cadre **Bureau** montre les icônes du bureau Windows (et du bureau public), précédées de la **Corbeille** et des autres icônes système cochées dans les paramètres du bureau Windows (Ce PC, Réseau…) : double-clic pour ouvrir, clic droit → « Vider la Corbeille », et glisser des fichiers sur la Corbeille pour les y mettre. Les autres cadres sont par défaut des dossiers cachés du bureau (`Bureau\NomDuCadre`), ou n'importe quel dossier existant |
| Ranger un fichier | Glisser-déposer depuis le cadre Bureau, un autre cadre ou l'Explorateur (Ctrl = copier). Clic droit → « Déplacer vers » |
| Ouvrir | Double-clic, ou Entrée sur la sélection. Clic droit : ouvrir avec, afficher dans l'Explorateur, copier, renommer (F2), supprimer (Suppr, vers la Corbeille), propriétés |
| Nouveaux éléments | Tout fichier ou dossier créé sur le bureau (téléchargement, installation d'un logiciel…) apparaît dans le cadre Bureau en moins d'une seconde avec un point bleu « nouveau » et un compteur dans le titre, pendant 48 h ou jusqu'au premier clic. Si une règle de tri automatique correspond, il est rangé dans le bon cadre et une notification le signale |
| Tri automatique | Dans les paramètres d'un cadre, des règles (`*.pdf; *.docx; facture*`) rangent automatiquement les nouveaux fichiers du bureau |
| Déplacer / redimensionner | Glisser le titre ; bords et coins de la fenêtre (la barre de défilement est en retrait du bord pour rester cliquable). Aimantation aux bords de l'écran et aux autres panneaux (désactivable). Largeur minimale : une icône ; quand le titre et les boutons ne tiennent plus sur une ligne, les boutons passent sur une seconde ligne sous le titre. Verrou par panneau dans ses paramètres (un cadenas s'affiche dans le titre) ; « Verrouiller tous les panneaux » dans les paramètres généraux |
| Enrouler | Double-clic sur le titre, ou le chevron |
| Fusionner des cadres | Sur le bureau : glisser un cadre par son titre et le déposer contre le bas d'une autre fenêtre (ou contre le haut d'une tête de colonne) ; le bord d'accrochage se surligne pendant le glisser. Glisser un cadre collé l'écarte de sa colonne. Option « Fusionner un cadre déposé contre un autre » dans les paramètres généraux. Vue d'ensemble : Paramètres du cadre → onglet **Fusion** (ou Paramètres généraux → « Fusion des cadres… », ou clic droit sur le titre → Fusionner) : un tableau des colonnes, fenêtre principale en tête et fenêtres empilées dessous, réorganisable par glisser-déposer (sur une colonne : collée en bas ; sur une ligne : insérée à cette place ; zone « indépendante » : libérée). Les cadres d'une colonne ont la même largeur, se déplacent ensemble et chacun se replie sur son titre. Tâches, Notes et Cartes mentales restent indépendants |
| Barre de titre | Au survol : crayon (renommer en place), **Paramètres du cadre** (couleur parmi 10 teintes ou **couleur personnalisée** via la palette Windows, **opacité de 0 à 100 %** au curseur avec aperçu en direct, verrou, dossier, tri, taille des icônes, règles), enrouler, « + » (nouvelle tâche, nouveau cadre, nouvelle note). Les paramètres généraux s'ouvrent depuis le menu de l'icône de la zone de notification. Les panneaux Tâches et Notes ont la même boîte réduite (couleur, opacité, verrou) |
| Clic droit sur le fond | Nouveau dossier, coller, actualiser, ouvrir dans l'Explorateur, et les trois créations (tâche, cadre, note). Clic droit sur un fichier : ouvrir, déplacer vers, renommer, supprimer, propriétés |
| Toujours visibles | Les panneaux restent sous toutes les fenêtres et ne sont pas masqués par Win+D ; Alt+F4 ne les ferme pas |
| Icônes Windows | Option « Masquer les icônes du bureau » : le cadre Bureau les affiche à la place. Réversible ; elles reviennent quand Pupitre est fermé |
| Plusieurs écrans | **Une disposition par configuration d'écrans** : Pupitre reconnaît l'ensemble d'écrans branchés (bureau à trois écrans, portable seul…) et mémorise pour chacun la disposition de tous les panneaux, mise à jour en continu. Quand les écrans changent, la disposition correspondante est appliquée automatiquement (notification) ; une configuration inconnue est créée et mémorisée. Vous pouvez aussi enregistrer des **instantanés nommés** et les restaurer : Paramètres → « Dispositions par configuration d'écrans », ou menu de la zone de notification → « Dispositions ». Tout est écrit à l'arrêt de Windows |

Un panneau ne peut plus devenir plus grand que l'écran : les poignées (quatre bords et quatre coins) s'arrêtent à
l'espace de travail. Le menu d'un panneau (clic droit sur sa barre de titre) et ses paramètres proposent **Taille par
défaut** et **Ramener à l'écran** ; la zone de notification propose **Ramener tous les panneaux à l'écran**, et les paramètres généraux listent toutes les
fenêtres (cadres et panneaux, avec position et taille) avec ces deux actions, pour retrouver une fenêtre perdue.

## Les tâches : suivi de projets

Le panneau Tâches reprend l'organisation d'un suivi de projets : **catégories** → **tâches** → **sous-tâches**.

| Élément | Comment |
|---|---|
| Catégorie | Carte colorée (barre et titre teintés). « + Catégorie » dans la barre d'outils, ou saisie rapide. Chevron ou clic sur l'en-tête pour replier ; double-clic sur le nom pour renommer ; « … » : couleur, renommer, supprimer. Le compteur indique « n en cours · n terminées » |
| Tâche | Sujet suivi dans une catégorie : « + Tâche » dans l'en-tête (saisie en ligne). Sous le titre, trois badges cliquables : **service** (couleur du service), **statut** (Non commencé · En cours · Terminé) et **échéance** (si définie). Drapeau = priorité importante. « … » ou clic droit : statut, service, priorité, échéance, commentaires, renommer, déplacer vers une autre catégorie, supprimer |
| Commentaires | Dans la fiche de la tâche (icône œil, qui porte le nombre de commentaires) ou « Commentaires et historique… » dans son menu : historique de la tâche, commentaires libres et événements journalisés (« Statut → En cours »). Un commentaire se modifie en place (clic sur son texte ou crayon au survol ; Entrée enregistre, Échap annule) et se supprime par la corbeille ; les événements journalisés sont en lecture seule. Le dernier commentaire s'affiche en italique sous les badges |
| Sous-tâches | Dépliez une tâche : cases à cocher, « Ajouter une sous-tâche… » en bas, croix au survol pour supprimer. Cocher une première sous-tâche passe la tâche « En cours ». Les sous-tâches réalisées se replient sous « n sous-tâches réalisées » avec leur date |
| Fiche | Icône œil de la tâche, double-clic sur une tâche ou une sous-tâche, ou « Ouvrir dans une fenêtre » dans son menu : fenêtre flottante redimensionnable avec la catégorie, le titre, les badges, les sous-tâches et tout l'historique, pour tout lire et modifier sans agrandir le panneau ni faire défiler. Échap ferme |
| Terminés | Une tâche « Terminé » rejoint « Tâches terminées · n » en bas de sa catégorie (barrée, date). « Archiver les terminés » les déplace dans `archive.json` |
| Filtre et recherche | « Tous les services ▾ » : filtre multi-services avec compteurs. Loupe de la barre de titre : recherche dans les titres, sous-tâches et commentaires. Les catégories sans tâche correspondante sont masquées (le pied du panneau en donne le nombre) ; créer une catégorie efface filtre et recherche pour la montrer |
| Étapes | Paramètres du panneau → Étapes : la première est l'état initial, la dernière vaut « terminé » (barré, archivable). Ajoutez, renommez, colorez et réordonnez les étapes intermédiaires (En attente, En revue…) ; supprimer une étape renvoie ses tâches à la précédente. La pastille de statut d'une tâche propose toutes les étapes |
| Services et catégories | Paramètres du panneau (engrenage de la barre de titre, ou menu « … »), sous l'apparence : noms, couleurs (huit pastilles ou, par la dernière pastille « palette », n'importe quelle couleur), usage, suppression, ajout. Le menu « … » d'une catégorie propose aussi « Couleur → Personnalisée… » |
| Ordre | Poignée à gauche d'une catégorie, d'une tâche ou d'une sous-tâche : l'élément se **détache** et suit la souris (carte fantôme), sa place reste marquée en transparence et se décale **en direct** dans la liste ; relâchez pour le rattacher |
| Capturer une demande | **Ctrl + Alt + N** depuis n'importe quelle application : nouvelle tâche dans la catégorie choisie (avec service), ou sous-tâche dans une tâche existante |
| Syntaxe rapide | `Indicateur surcharges ! @demain` → `!` priorité importante · `@demain`, `@lundi`, `@15/11`, `@+3`, `@semaine` échéance |
| Export | Barre de titre → « Copier le suivi (texte) » : catégories, tâches (service, statut, échéance), sous-tâches et dernier commentaire |
| Rappel | Au lancement et chaque matin : notification des tâches à échéance (désactivable). Le badge de la barre de titre compte les retards |

Les données de l'ancien format (projets → sous-tâches) sont reprises automatiquement dans une catégorie « Général », avec les services retrouvés.

## Les notes

Toutes les notes vivent dans le panneau **Notes** (une bibliothèque, pas une fenêtre par note). Le
bouton **+ Nouvelle note** en tête du panneau, le « + » d'un panneau ou le menu de la zone de
notification créent une carte : titre, texte libre, couleur, date de dernière modification,
corbeille. Hors saisie, le texte est rendu : les adresses web, chemins de fichiers (`C:\…`, `\\serveur\…`,
entre guillemets s'ils contiennent des espaces) sont cliquables, et les lignes commençant par `[ ]` ou `[x]`
deviennent des cases à cocher. Un clic sur le texte (ou Tab depuis le titre) passe en saisie ; Échap ou un clic
ailleurs revient au rendu. Le chevron replie la carte sur son seul titre. La poignée à gauche du titre permet de
glisser une note : elle se détache sous la souris et sa place se décale en direct dans la liste. L'icône œil de la carte (ou un double-clic sur la
carte) ouvre la note en grand dans une fenêtre flottante redimensionnable (titre, couleur, texte complet), pour la lire
ou la rédiger sans agrandir le panneau. Enregistrement automatique. Le panneau se masque depuis les paramètres ou la
zone de notification.

## Les cartes mentales

Le panneau **Cartes mentales** reprend le fonctionnement de XMind, hors ligne et dans un panneau de bureau. Une
**feuille** (bouton « Nom ▾ » : liste défilante avec filtre, clic pour changer, puis sur chaque ligne monter, descendre,
renommer, dupliquer, supprimer, et « Nouvelle feuille… ») part d'un sujet central ;
une nouvelle feuille se crée à partir d'un **modèle**, chacun illustré par une vignette (fond, couleurs et structure) : carte mentale, schéma logique (droite ou gauche), organigramme
(bas ou haut), arborescence, chronologie (horizontale ou verticale), arête de poisson, remue-méninges, plan de projet,
compte rendu de réunion, SWOT, semaine. La barre d'outils se limite à trois libellés, Feuille ▾, Insérer ▾ et Exporter ▾ ; la barre de titre ajoute ajuster,
volet Style et plein écran ; tout le reste se fait par le **volet Style** à gauche (vignettes : structure, thème, palette des
branches, forme des sujets, lignes, espacement, numérotation, puis couleur, forme et texte du sujet sélectionné), par
le clic droit, ou au clavier. Le volet s'affiche ou se masque avec l'icône liste de la barre de titre et se
resserre dans un panneau étroit au lieu de disparaître ; l'icône flèche de la barre de titre passe le panneau en plein
écran et en revient.

| Élément | Comment |
|---|---|
| Sujets | Clic sur le sujet central : un **+** à droite et un **+** à gauche (ou au-dessus / en dessous sur une chronologie ou une arête) créent le sujet du côté choisi ; Tab prend le côté le moins chargé, et les côtés restent ensuite stables. Clic sur un autre sujet : un **+** sur son côté extérieur (sous-sujet) et un autre en dessous (sujet frère), et une **barre flottante** au-dessus donne couleur, gras, marqueur, note, lien, relation, menu complet (…) et suppression. Le nouveau sujet se saisit directement dans sa forme (comme dans MindNode ou Whimsical) ; laissé vide, il disparaît. Taper sur un sujet sélectionné remplace son texte ; double-clic, Espace ou F2 pour le modifier (Maj+Entrée : saut de ligne). Clavier : **Tab** sous-sujet, **Entrée** sujet frère, **Ctrl+Entrée** sujet parent intercalé, **Suppr** demande confirmation, **Ctrl+Suppr** supprime le sujet seul en gardant ses sous-sujets. Clic droit : menu court (Insérer, Style du sujet, Organiser, copier / coller, supprimer) |
| Déplacer | Glisser un sujet : la carte se réorganise **en direct** pour montrer où il atterrira (sous le sujet survolé, au rang correspondant ; un sujet principal lâché de l'autre côté du centre change de côté ; un sous-sujet lâché sur le fond devient flottant). Échap pendant le glisser annule, Ctrl+Z ensuite |
| Sujets flottants | Double-clic sur le fond (ou clic droit → « Sujet flottant ici ») : sujet indépendant, avec ses propres sous-sujets |
| Relations | « Insérer → Relation… » ou **Ctrl+Maj+R**, puis clic sur le sujet cible : flèche courbe en pointillés avec un texte (double-clic sur le texte pour le modifier, clic droit : inverser, supprimer) |
| Limites et résumés | **Ctrl+B** encadre un sujet et sa branche (texte de la limite par le menu). « Résumé des sous-sujets… » trace une accolade qui regroupe les sous-sujets avec un intitulé |
| Légendes | « Légende… » : bulle jaune au-dessus du sujet (double-clic pour la modifier) |
| Marqueurs | Groupes XMind : priorité 1 à 7, avancement (0 à 100 %), drapeaux, étoiles, flèches, symboles, mois, jours. Un marqueur par groupe ; choisir le même le retire |
| Étiquettes, notes, liens, cases | Étiquettes (puces sous le texte), note affichée au survol (icône ≡, **Ctrl+Maj+N**), lien web / fichier / dossier (icône 🔗, **Ctrl+K**), case à faire (☐ / ☑, cliquer pour basculer ; fait = texte barré) |
| Style d'un sujet | Volet Style (section « Sujet sélectionné ») ou clic droit : style rapide (très important, important, barré, par défaut), couleur de branche (teintes ou couleur libre, héritée par les sous-sujets), forme (celle de la carte, arrondi, rectangle, pilule, texte seul, ellipse, souligné), taille, gras, italique, barré, côté ou position par rapport à l'axe |
| Style de la feuille | Volet Style (ou clic droit sur le fond) : thème (arc-en-ciel XMind par défaut : fond blanc, centre bleu nuit, sujets principaux en aplats, sous-sujets en pastel ; classique XMind ; sombre ; clair ; coloré), palette des branches (arc-en-ciel, Pupitre, océan, forêt, crépuscule, pastel, ardoise), branches colorées ou unies, lignes effilées, forme des sujets, lignes courbes / droites / coudées, épaisseur, espacement, numérotation (chiffres, lettres, romains, hiérarchique) |
| Pliage et navigation | Pastille « +n » sur un sujet replié ; **Ctrl+/** replie ou déplie, **Ctrl+Alt+/** au premier niveau ; **Ctrl+;** n'affiche qu'une branche (bouton « ⤴ Toute la carte » pour revenir) ; **Ctrl+R** revient au sujet central ; flèches pour se déplacer ; **Alt+↑/↓** réordonnent ; molette ou **Ctrl + / −** pour zoomer, **Ctrl+0** ajuste, glisser le fond déplace la vue |
| Recherche | **Ctrl+F** (ou clic droit sur le fond → Rechercher) : sujets, étiquettes, notes et légendes ; **F3** résultat suivant, Échap efface |
| Annuler | **Ctrl+Z** annule, **Ctrl+Y** rétablit ; **Ctrl+D** duplique une branche, **Ctrl+C** la copie en plan texte, **Ctrl+V** colle un plan ou du Markdown comme sous-sujets |
| Export, import | « Exporter ▾ » : image PNG sur le disque ou dans le presse-papiers, plan texte indenté, Markdown (titres puis listes) ; import d'un plan indenté ou d'un document Markdown (#, listes, cases `[ ]`). « Nouvelle feuille à partir de ce sujet » isole une branche dans sa propre feuille |

Non repris de XMind : sélection multiple, matrice et tableau arborescent, structures différentes par branche, images et
pièces jointes, mode plan éditable, présentation, Gantt. Les feuilles sont enregistrées dans `cartes.json` ; le panneau
se masque depuis les paramètres ou la zone de notification.

## Raccourcis globaux

- **Ctrl + Alt + N** : nouvelle tâche.
- **Ctrl + Alt + B** : afficher / masquer tous les panneaux.
- Icône de la zone de notification : clic gauche = afficher / masquer, double-clic = nouvelle tâche, clic droit = menu (dont les paramètres).
- Les combinaisons sont fixes ; pour en changer, modifier `QuickMods`/`QuickVk` et `ToggleMods`/`ToggleVk` dans `settings.json` (Pupitre fermé).
- `Pupitre.exe --parametres` ouvre directement la fenêtre des paramètres (utile pour un raccourci) ; `Pupitre.exe --quitter` demande à l'instance en cours d'enregistrer et de se fermer (mise à jour).

## Compilation

Aucun outil à installer : `build.cmd` utilise le compilateur C# fourni avec le .NET Framework 4.8
(inclus dans Windows 10/11) et produit `dist\Pupitre.exe`. `build.cmd icon` régénère l'icône
depuis `assets\pupitre-icone.png` (source PNG du logo).

**Smart App Control** : si cette protection de Windows 11 est activée, elle peut refuser de lancer
un exécutable fraîchement compilé (« Une stratégie de contrôle d'application a bloqué ce fichier »).
Le verdict dépend de l'empreinte du fichier : relancer `build.cmd` produit un nouveau fichier,
généralement accepté. L'exécutable livré dans `dist\` a été vérifié sur ce PC.

## Signature de l'exécutable

`dist\Pupitre.exe` n'est pas signé. Pour le diffuser à des collègues sans avertissement SmartScreen, et pour
que Smart App Control l'accepte, il faut le signer avec un **certificat de signature de code délivré par une
autorité reconnue** (certificat OV ou EV, payant, à acheter auprès d'une autorité comme DigiCert, Sectigo ou
GlobalSign ; l'EV donne la réputation SmartScreen immédiatement). Une fois le certificat installé dans le
magasin personnel :

```
.\sign.ps1 -Thumbprint <empreinte du certificat>
```

Le script utilise `Set-AuthenticodeSignature` de PowerShell (aucun outil à installer) avec horodatage, pour que
la signature reste valable après l'expiration du certificat. Variante : `-Pfx fichier.pfx -PfxPassword …`.

Pour un usage **interne sans achat**, `.\sign.ps1 -MakeTestCert` crée un certificat auto-signé et exporte
`dist\Pupitre-test.cer`, à importer sur chaque poste dans « Autorités de certification racines de confiance »
et « Éditeurs approuvés » (`certutil -addstore Root` et `certutil -addstore TrustedPublisher`, ou stratégie de
groupe). Cela supprime l'« éditeur inconnu » sur ces postes, mais ni SmartScreen ni Smart App Control ne
considèrent un certificat auto-signé comme de confiance : seul un certificat d'autorité reconnue le permet.

## Mise à jour

Les données vivent dans `%APPDATA%\Pupitre` (section suivante), jamais à côté de l'exécutable : remplacer `Pupitre.exe`
ne touche ni tâches, ni notes, ni cartes, ni disposition. Les fichiers d'une version antérieure sont lus tels quels
(migrations automatiques ; les anciens champs restent écrits, un retour en arrière reste possible).

- Automatique : `dist\mettre-a-jour.cmd` (double-clic, à côté de `Pupitre.exe`) copie d'abord les données dans
  `%APPDATA%\Pupitre\sauvegardes\<date_heure>`, télécharge la dernière version depuis GitHub, ferme proprement Pupitre
  (`Pupitre.exe --quitter`, qui enregistre tout), remplace l'exécutable (l'ancien reste en `Pupitre.precedent.exe`) et le relance.
  Si la version installée est antérieure à cette commande de fermeture, le script demande de quitter Pupitre à la main
  (zone de notification → Quitter), puis continue.
- À la main : quitter Pupitre (icône de la zone de notification → Quitter), remplacer `Pupitre.exe` par celui du dépôt, relancer.
  Un fichier téléchargé peut déclencher l'avertissement SmartScreen (« Informations complémentaires » → « Exécuter quand même »).

## Fichiers créés à l'exécution

- `%APPDATA%\Pupitre\settings.json`, `layout.json` (dispositions par configuration d'écrans comprises), `tasks.json` (services, catégories, tâches, sous-tâches, commentaires), `cartes.json` (cartes mentales), `archive.json` — réglages,
  disposition (cadres, notes, panneaux Tâches et Notes, écran et position par panneau),
  tâches, tâches archivées. Modifiables à la main
  (Pupitre fermé) ; un fichier illisible est mis de côté en `.corrompu-*` et remplacé par défaut.
- `%APPDATA%\Pupitre\erreurs.log` — uniquement en cas d'erreur inattendue.
- `Bureau\<Nom du cadre>` (dossier caché) — pour chaque cadre créé « sur le bureau ». Supprimer un
  cadre ne supprime jamais son dossier ni les fichiers.
- Clé `HKCU\...\Run\Pupitre` — seulement si « Lancer avec Windows » est coché.

Si Pupitre est lancé par une application empaquetée (MSIX, par exemple depuis le terminal d'un assistant), Windows
redirige `%APPDATA%` vers `AppData\Local\Packages\<paquet>\LocalCache\Roaming` : cette instance lit et écrit alors ses
propres fichiers, différents de ceux de l'instance lancée au démarrage de la session. En cas de « données disparues »,
comparer les deux dossiers ; l'outil `tools\dev\sonde.cmd` (à lancer par un double-clic dans l'Explorateur) liste le vrai
dossier.

## Fonctionnement et sécurité

- Chaque panneau est une fenêtre WPF à transparence par pixel, appartenant à la fenêtre du bureau
  (Progman / WorkerW) et maintenue en bas de l'ordre d'affichage : au-dessus du fond d'écran, sous
  tout le reste. Couleur, opacité et coins arrondis sont dessinés par Pupitre, donc identiques que le
  panneau soit actif ou non (le verre acrylique de Windows 11 n'est rendu que pour la fenêtre active,
  ce qui faisait « disparaître » la couleur : abandonné).
- Les déplacements de fichiers passent par le shell Windows (`SHFileOperation`) : conflits gérés
  par Windows, suppression vers la Corbeille, annulation possible dans l'Explorateur.
- Aucune API réseau, aucun hook clavier : les raccourcis utilisent `RegisterHotKey`.
- Exécutable non signé, manifeste `asInvoker` : jamais de demande d'élévation.

## Si les icônes du bureau ont disparu

Quand « Masquer les icônes du bureau » est coché, Pupitre les masque à son lancement et les réaffiche quand il
se ferme, quand Windows ferme la session ou s'il plante. Si malgré tout le bureau est vide alors que Pupitre ne
tourne pas (processus tué, coupure de courant) : clic droit sur le bureau → **Affichage** → **Afficher les éléments
du bureau**, ou double-clic sur `dist\restaurer-icones.cmd`. Relancer Pupitre les masque de nouveau et affiche le
cadre « Bureau » à la place.

## Démarrage avec Windows et Smart App Control

« Lancer avec Windows » inscrit Pupitre dans la clé `Run` de votre profil. Sur un PC où **Smart App Control** est
actif, un exécutable non signé peut être bloqué sans message à l'ouverture de session (le journal
*Microsoft-Windows-CodeIntegrity/Operational* enregistre alors un événement 3077). Si Pupitre n'apparaît pas au
démarrage, ouvrez-le à la main depuis `dist\` ; si Windows le refuse, recompilez (`build.cmd`) ou signez-le
(voir plus haut). Signer l'exécutable est la seule solution durable sur ces postes.

## Limites connues

- Les panneaux restent tous au même niveau (sous les fenêtres) : l'aimantation est faite pour les
  juxtaposer plutôt que les superposer.
- Le tri automatique attend que le fichier soit stable (~1 s) ; les téléchargements en cours
  (`.crdownload`, `.part`, `.tmp`) sont ignorés jusqu'à leur fin.
- Écrans multiples à échelles différentes : positions mémorisées en unités de l'écran principal
  (l'écran d'appartenance et la position relative sont toutefois conservés).
