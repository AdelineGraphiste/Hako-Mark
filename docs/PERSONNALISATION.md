# Personnaliser Hako Mark pour son propre usage

La licence permet d’adapter une copie privée à ta marque pour l’utiliser dans ton activité. L’application n’a pas encore de réglage de thème : il faut modifier le code et la recompiler sur Windows avec le SDK .NET 10.

Les principaux points de personnalisation sont :

- `src/HakoMark.UI/ToolbarWindow.cs` : couleurs de l’interface, titres et textes d’aide ;
- `src/HakoMark.App/App.xaml` : ressources visuelles ;
- `src/HakoMark.Core/Models.cs` : couleur initiale des annotations ;
- `assets/HakoMark.ico` : icône de l’application et de l’installeur ;
- `installer/Setup.cs` : textes affichés pendant l’installation et nom du raccourci ;
- `installer/Aide-Hako-Mark.txt` : aide livrée avec l’installeur.

Pour créer ton paquet, lance `./installer/Build-Installer.ps1` à la racine du projet. Vérifie ensuite l’interface, le raccourci du Bureau et la désinstallation sur une machine de test.

La personnalisation et l’usage dans ton activité sont autorisés ; la publication, la distribution ou la vente d’une version personnalisée demandent l’accord écrit d’Adeline Courtois. Les droits sur la police ABeeZee et sur les autres éléments tiers restent distincts.
