![Hako Mark : accueil et barre d’annotation / home screen and annotation toolbar](assets/github/hako-mark-readme-hero.png)

# Hako Mark

[Français](#français) · [English](#english)

## Français

Hako Mark est une application Windows pour annoter directement son écran pendant une présentation, une réunion ou une vidéo. Flèches, cercles, cadres, lignes, stylo et gomme sont accessibles depuis une barre flottante.

Créée par Adeline Courtois, graphiste, dans l’univers HakoTools. Version actuelle : **0.3.3**.

### Télécharger Hako Mark

**[Télécharger Hako Mark pour Windows (ZIP)](https://github.com/AdelineGraphiste/Hako-Mark/releases/download/v0.3.3/Hako-Mark-v0.3.3-Windows.zip)**

Décompresse le ZIP, puis double-clique sur `Installer Hako Mark.exe` et sur **INSTALLER HAKO MARK**. Un raccourci apparaît sur le Bureau. Le dossier contient également `Desinstaller Hako Mark.exe`, `Aide Hako Mark.txt` et les conditions d’utilisation : garde-le pour retrouver facilement le bouton de désinstallation. L’application est destinée à Windows 10/11 en 64 bits ; aucun SDK .NET n’est nécessaire pour l’utiliser.

Le bouton GitHub **Code > Download ZIP** télécharge le code source. Pour installer l’application, utilise le lien de téléchargement ci-dessus.

### Les deux interfaces

L’accueil permet de commencer, de personnaliser les outils et d’ouvrir l’aide. La barre flottante regroupe les outils de dessin et les commandes de correction.

![Accueil Hako Mark à gauche et barre d’annotation à droite / Hako Mark home screen and toolbar](assets/github/hako-mark-interface-screenshots.png)

### Utilisation

Choisis un outil, puis clique et glisse sur l’écran. Par défaut, la souris redevient disponible après chaque tracé pour défiler, cliquer ou changer d’onglet. Les annotations restent affichées selon la durée choisie ; elles sont fixées à l’écran et ne suivent pas le texte quand une page défile.

**F8** réactive le dernier outil et un nouvel appui arrête l’annotation. Cette touche est personnalisable dans **Réglages > Application**, par exemple avec X. Pour dessiner plusieurs éléments à la suite, décoche **« Une annotation, puis retour automatique à la navigation »**. Le bouton **? Aide** détaille les outils et raccourcis.

### Licence et propriété

Hako Mark est **gratuit pour un usage personnel et professionnel**, y compris pendant des prestations rémunérées. Tu peux adapter le branding d’une copie utilisée pour tes propres besoins. **La redistribution, la publication d’une version modifiée et la revente exigent l’autorisation écrite préalable d’Adeline Courtois.** Les conditions complètes figurent dans [LICENSE.md](LICENSE.md).

Le code source est consultable. Hako Mark **n’est pas un logiciel open source** : sa licence particulière ne permet pas la libre redistribution ou la vente. La police ABeeZee conserve sa [licence distincte](src/HakoMark.UI/Fonts/OFL.txt).

### Construire depuis le code source

Sur Windows, avec le SDK .NET 10 :

```powershell
dotnet build HakoMark.sln -c Release
dotnet run --project tests/HakoMark.Tests -c Release
./installer/Build-Installer.ps1
```

Le paquet est créé dans `release/Hako Mark Installer`. La personnalisation du branding nécessite actuellement une modification du code et une recompilation ; voir le [guide de personnalisation](docs/PERSONNALISATION.md).

## English

Hako Mark is a Windows app for annotating your screen during presentations, meetings and videos. A floating toolbar provides arrows, circles, rectangles, lines, freehand drawing and an eraser.

Created by graphic designer Adeline Courtois as part of HakoTools. Current version: **0.3.3**.

### Download Hako Mark

**[Download Hako Mark for Windows (ZIP)](https://github.com/AdelineGraphiste/Hako-Mark/releases/download/v0.3.3/Hako-Mark-v0.3.3-Windows.zip)**

Extract the ZIP, double-click `Installer Hako Mark.exe`, then click **INSTALLER HAKO MARK**. A desktop shortcut is created. Keep the extracted folder: it also contains `Desinstaller Hako Mark.exe`, a French quick-start guide and the usage terms. The app targets 64-bit Windows 10/11 and does not require the .NET SDK to run. GitHub’s **Code > Download ZIP** button downloads source code, not the installer.

### The two interfaces

The home screen starts the app and opens tool settings or help. The floating toolbar provides drawing tools and correction commands. The image above shows both interfaces.

### How it works

Choose a tool, then click and drag on screen. By default, normal mouse navigation returns after each drawing gesture. Annotations stay visible for the duration selected in the app; they are fixed to the screen and do not move with scrolling page content.

Press **F8** to reactivate the last tool, and press it again to stop annotating. You can change this key in **Réglages > Application**. For uninterrupted drawing, turn off **« Une annotation, puis retour automatique à la navigation »** in the same settings tab. The **? Aide** button opens the in-app help, currently in French.

### License and ownership

Hako Mark is **free to use personally and professionally**, including in paid work. You may change the branding of a private copy for your own use. **Redistribution, publication of a modified version, or resale require Adeline Courtois’s prior written permission.** The full terms are in [LICENSE.md](LICENSE.md), currently written in French.

The source code is available to read. Hako Mark is **not open-source software** under an OSI-style license, because the custom terms restrict redistribution and sale. The included ABeeZee font has a [separate license](src/HakoMark.UI/Fonts/OFL.txt).

### Build from source

On Windows with the .NET 10 SDK, run the commands shown in the French section above. The package is generated in `release/Hako Mark Installer`. Branding changes currently require editing and rebuilding the app; see the [customization guide](docs/PERSONNALISATION.md) (French).
