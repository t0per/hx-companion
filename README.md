# HxCompanion

Petit outil Windows qui ajoute un **réglage de volume du sidetone** (retour voix du micro) pour le
casque **HyperX Cloud III S Wireless**. L'application officielle NGENUITY ne propose qu'un interrupteur
activé/coupé pour ce casque.

Projet non officiel, sans lien avec HyperX ni HP.

## Fonctionnement

HxCompanion ne parle pas directement au casque. Il envoie ses commandes au service local de NGENUITY
(`hxn-srv-communication-hub`), de la même façon que l'application officielle : connexion ZeroMQ sur
`localhost`, messages FlatBuffers. NGENUITY doit donc être installé et lancé.

- Case « Sidetone activé » : active ou coupe le retour voix.
- Curseur : niveau de -6 à +6 dB (plage constatée sur un Cloud III S Wireless).
- La ligne de statut affiche la valeur demandée et celle que le casque renvoie.

L'outil ne modifie aucun fichier de NGENUITY.

## Installation

1. Téléchargez `HxCompanion-win-x64.zip` depuis la page
   [Releases](https://github.com/t0per/hx-companion/releases) et décompressez-le.
2. Lancez NGENUITY et allumez le casque.
3. Lancez `HxCompanion.exe`.

L'archive ne contient que HxCompanion. Les bibliothèques de NGENUITY dont il a besoin sont chargées
depuis votre installation de NGENUITY au démarrage.

## Prérequis

- Windows 10 ou 11, 64 bits
- HyperX NGENUITY 3.1.0 installé et lancé (testé avec cette version uniquement)
- Runtime .NET 10 Desktop (déjà installé par NGENUITY)
- Pour compiler : SDK .NET 10

## Compilation

Les bibliothèques de NGENUITY (`NGENUITY.Common.dll`, `NetMQ.dll`, `Google.FlatBuffers.dll`,
`AsyncIO.dll`, `NaCl.dll`) sont nécessaires à la compilation. Elles sont propriétaires ou tierces et ne
sont **pas** fournies dans ce dépôt, ni copiées dans le dossier de sortie : elles sont prises dans votre
installation de NGENUITY.

```
dotnet publish -c Release -o publish
```

Par défaut, le projet cherche NGENUITY dans `%LOCALAPPDATA%\NGENUITY\current`. Si votre installation est
ailleurs :

```
dotnet publish -c Release -o publish -p:NgenuityDir="C:\chemin\vers\NGENUITY"
```

Lancez ensuite `publish\HxCompanion.exe`.

## Limites

- Seuls les modèles Cloud III S Wireless et Cloud III S Wireless DTS sont reconnus.
- Le protocole du service NGENUITY n'est pas documenté et peut changer avec une mise à jour de
  NGENUITY ; il faut alors recompiler avec les nouvelles bibliothèques.
