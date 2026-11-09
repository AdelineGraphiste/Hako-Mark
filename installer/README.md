# Fabrication du paquet Windows

Depuis la racine du projet, sur Windows avec le SDK .NET 10 :

```powershell
./installer/Build-Installer.ps1
```

Le script prépare `release/Hako Mark Installer` avec quatre fichiers :

- `Installer Hako Mark.exe` : installation en double-cliquant.
- `Desinstaller Hako Mark.exe` : désinstallation en double-cliquant.
- `Aide Hako Mark.txt` : aide rapide.
- `Conditions utilisation Hako Mark.md` : conditions d’utilisation.

Pour GitHub Releases, compresser **ces quatre fichiers** dans `Hako-Mark-v0.3.3-Windows.zip`. Ne pas placer l’archive dans le dépôt source : elle est destinée à une Release GitHub.

Le workflow `.github/workflows/publier-windows.yml` construit et publie cette archive quand un tag `v*` est envoyé sur GitHub.

L’icône source se trouve dans `assets/HakoMark.ico`. Les anciens scripts de compatibilité présents localement dans `installer` ne sont pas utilisés pour cette livraison et sont exclus du dépôt Git.
