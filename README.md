# Harbor

> The modern, open-source, privacy-first command center for developers and sysadmins.

**Harbor** unifie SSH, SFTP, FTP/FTPS, stockages cloud (S3 et compatibles, Azure Blob, GCS), Docker, Kubernetes, terminal intégré, gestion de clés SSH et de secrets — dans une seule application desktop cross-platform (Windows, macOS, Linux), **gratuite**, **open source** (MIT) et **local-first**.

L'objectif : remplacer l'empilement PuTTY + WinSCP + FileZilla + Cyberduck + Docker Desktop + gestionnaire de clés par un seul outil moderne, sans télémétrie et sans cloud forcé.

---

## Statut

Projet en cours de démarrage. Aucune version stable publiée pour le moment.

- Document d'architecture : [`harbor-architecture.md`](harbor-architecture.md)
- Feuille de route : [`harbor-roadmap.md`](harbor-roadmap.md)

## Stack technique

- **.NET 8** + **Avalonia UI 11** (MVVM via CommunityToolkit.Mvvm)
- **SQLite** chiffré (SQLCipher) pour la persistance
- **SSH.NET**, **FluentFTP**, **AWSSDK.S3**, **Docker.DotNet**, **KubernetesClient** pour les protocoles
- **MoonSharp** (Lua) pour les plugins
- **Serilog** pour le logging

## Plateformes cibles

Windows 10+, macOS 12+, Linux (x64 et ARM64).

## Licence

[MIT](LICENSE)
