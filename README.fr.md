# Harbor

> **Le centre de commande moderne, open-source et privacy-first pour développeurs et sysadmins.**

[![Build](https://github.com/TheFraTo/Harbor-Project/actions/workflows/build.yml/badge.svg)](https://github.com/TheFraTo/Harbor-Project/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Status: pre-alpha](https://img.shields.io/badge/status-pre--alpha-orange)]()
[![.NET](https://img.shields.io/badge/.NET-10-512BD4)]()

🇬🇧 [English version](README.md)

---

Harbor unifie SSH, SFTP, FTP/FTPS, le stockage objet cloud (S3 et compatibles, Azure Blob, GCS), Docker, Kubernetes, un terminal intégré, la gestion des clés SSH et un coffre-fort de credentials — dans **une seule application desktop cross-platform** (Windows, macOS, Linux), **gratuite**, **open source** (MIT) et **local-first**.

Aujourd'hui, un opérateur qui gère des serveurs et du stockage jongle avec une demi-douzaine d'outils : PuTTY, WinSCP, FileZilla, Cyberduck, Docker Desktop, un gestionnaire de mots de passe, un fichier de notes "quelque part"… Les tout-en-un existants sont soit **payants et fermés** (Termius, Royal TS), soit **datés et limités** (FileZilla, WinSCP). Harbor est l'alternative moderne, gratuite et respectueuse de la vie privée.

## Pourquoi Harbor

| Principe | En pratique |
|---|---|
| **Local-first** | Aucune télémétrie. Aucune donnée envoyée dans le cloud sauf si vous l'activez. |
| **Privacy by design** | Secrets chiffrés en AES-256-GCM, master key dérivée via Argon2id. |
| **Keyboard-first** | Toute action accessible sans souris. Command palette à la VS Code. |
| **Cross-platform** | Windows, macOS et Linux au même niveau de qualité — même UX partout. |
| **Extensible** | API plugins (Lua via MoonSharp) dès la v1. |
| **Vraiment open source** | Licence MIT, gouvernance transparente, communauté ouverte. |

## Statut

Harbor est en **développement actif précoce** (pré-alpha). Aucune release publique pour le moment.

Ce qui est déjà construit (couches fondation) :
- ✅ **Harbor.Core** — modèle métier : 6 enums, 13 records (11 variantes de connexion, 9 d'auth), 6 interfaces d'abstraction, 3 EventArgs.
- ✅ **Harbor.Data** — persistance SQLite + SQLCipher chiffrée : `HarborDbContext`, migrations SQL embarquées, 6 repositories avec sérialisation JSON polymorphique.
- ✅ **Harbor.Security** — `KeyDerivation` (Argon2id m=64MiB), `SymmetricCrypto` (AES-256-GCM), `Keystore` avec timeout d'inactivité, `AuditLogger`.
- ✅ **CI** — matrice GitHub Actions (Windows / macOS / Linux), mode strict (warnings as errors), 90 tests qui passent.

Ce qui arrive : provider SSH/SFTP, file browser UI, émulateur de terminal, packaging.

L'avancement détaillé est tracké en live dans la [feuille de route](harbor-roadmap.md).

## Fonctionnalités prévues

Vue condensée — voir [`harbor-architecture.md`](harbor-architecture.md) §4 pour la liste complète.

**Protocoles**
- Shell : SSH, Mosh, Telnet, port série
- Transfert de fichiers : SFTP, SCP, FTP, FTPS, WebDAV, rsync
- Stockage cloud : AWS S3 (et MinIO, Backblaze B2, Wasabi, R2, Scaleway, Hetzner), Azure Blob, Google Cloud Storage
- Conteneurs : Docker, Docker Compose, Kubernetes, Podman

**Gestion des connexions**
- Profils nommés, tagués, en dossiers avec recherche fuzzy (`Ctrl+K`)
- Import depuis `~/.ssh/config`, registre PuTTY, WinSCP, FileZilla
- Workspaces regroupant les profils par projet / client
- Notes Markdown par profil, recherchables

**Terminal**
- Émulateur xterm-256color avec true color, ligatures, hyperliens OSC 8, images sixel
- Onglets multiples, split-panes libres, broadcast input vers N sessions
- Recherche dans le scrollback, enregistrement de session (asciicast v2), export HTML/SVG

**Gestion de fichiers**
- Vues dual-pane et quad-pane, drag & drop entre tous les panneaux
- Aperçu intégré : images, PDF, Markdown, JSON, CSV, audio, vidéo, hex
- Édition à distance via éditeur externe avec auto-upload au save
- Diff visuel local ↔ distant avant upload, sync bidirectionnelle optionnelle

**Sécurité**
- Coffre-fort local chiffré avec master password
- Gestion des clés SSH (Ed25519 / RSA / ECDSA), import OpenSSH et PuTTY PPK
- Clés matérielles FIDO2 / Yubikey, 2FA TOTP
- Intégration avec Bitwarden, 1Password, KeePassXC
- Journal d'audit local de toutes les actions sensibles

**Power features**
- Port forwarding visuel (local, remote, SOCKS dynamique)
- Jump hosts / bastions chaînés
- Dashboard de monitoring léger (CPU, RAM, disque, uptime)
- Webhooks, scripts post-connexion, watchers de fichiers

## Stack technique

- **Langage** — C# 13 sur .NET 10
- **UI** — Avalonia UI 12 avec MVVM (CommunityToolkit.Mvvm)
- **SSH/SFTP** — SSH.NET (Renci.SshNet)
- **FTP/FTPS** — FluentFTP
- **S3 et compatibles** — AWSSDK.S3
- **Azure Blob / GCS** — Azure.Storage.Blobs, Google.Cloud.Storage.V1
- **Docker / Kubernetes** — Docker.DotNet, KubernetesClient
- **Base de données** — SQLite via Microsoft.Data.Sqlite, chiffré avec SQLCipher (`bundle_e_sqlcipher`)
- **Cryptographie** — Konscious.Security.Cryptography (Argon2id), `System.Security.Cryptography.AesGcm` (BCL)
- **Plugins** — MoonSharp (Lua), Jint (JavaScript, optionnel)
- **Tests** — xUnit, Testcontainers .NET

## Build depuis les sources

Pré-requis : **SDK .NET 10** (10.0.2xx ou plus récent) et Git. N'importe quel IDE convient : Visual Studio 2022 17.10+, JetBrains Rider 2024.3+, ou VS Code avec le C# Dev Kit.

```bash
git clone git@github.com:TheFraTo/Harbor-Project.git
cd Harbor-Project
dotnet restore
dotnet build
dotnet test
```

Le build est configuré pour échouer sur **n'importe quel** warning (mode strict). Si vous en voyez un, c'est un vrai problème à corriger — n'hésitez pas à ouvrir une PR.

## Arborescence du projet

```
Harbor/
├── src/
│   ├── Harbor.App/                  # Point d'entrée Avalonia
│   ├── Harbor.UI/                   # Views, ViewModels, contrôles
│   ├── Harbor.Core/                 # Domaine : enums, records, interfaces
│   ├── Harbor.Services/             # ConnectionManager, TransferEngine, …
│   ├── Harbor.Security/             # Keystore, crypto, audit
│   ├── Harbor.Protocols.Ssh/        # Provider SSH + SFTP
│   ├── Harbor.Protocols.Ftp/        # FTP, FTPS
│   ├── Harbor.Protocols.S3/         # S3 et compatibles
│   ├── Harbor.Protocols.Azure/      # Azure Blob
│   ├── Harbor.Protocols.Gcs/        # Google Cloud Storage
│   ├── Harbor.Protocols.WebDav/     # WebDAV
│   ├── Harbor.Protocols.Docker/     # Docker
│   ├── Harbor.Protocols.Kubernetes/ # Kubernetes
│   ├── Harbor.Terminal/             # PTY + parser VT + renderer
│   ├── Harbor.Plugins/              # Runtime plugins Lua/JS
│   ├── Harbor.Data/                 # SQLite, SQLCipher, repos, migrations
│   ├── Harbor.Cli/                  # Compagnon CLI `harbor`
│   └── Harbor.Ipc/                  # IPC named pipes / Unix sockets
├── tests/                           # Tests xUnit unitaires + intégration
├── docs/                            # Documentation utilisateur et dev
└── .github/workflows/               # CI/CD
```

## Documentation

- [`harbor-architecture.md`](harbor-architecture.md) — document d'architecture technique complet (24 sections)
- [`harbor-roadmap.md`](harbor-roadmap.md) — suivi en direct de ce qui est fait et de ce qui arrive
- [`CONTRIBUTING.fr.md`](CONTRIBUTING.fr.md) — comment contribuer
- [`CODE_OF_CONDUCT.fr.md`](CODE_OF_CONDUCT.fr.md) — notre code de conduite
- [`SECURITY.fr.md`](SECURITY.fr.md) — politique de sécurité et reporting de vulnérabilités

## Contribuer

Les contributions sont bienvenues. Lisez [`CONTRIBUTING.fr.md`](CONTRIBUTING.fr.md) avant d'ouvrir une PR. En contribuant, vous acceptez de respecter le [`CODE_OF_CONDUCT.fr.md`](CODE_OF_CONDUCT.fr.md).

Pour tout changement non trivial, ouvrez d'abord une discussion ou une issue afin qu'on s'aligne sur l'approche.

## Sécurité

Si vous trouvez une vulnérabilité, **n'ouvrez pas d'issue publique**. Utilisez le [private vulnerability reporting](https://github.com/TheFraTo/Harbor-Project/security) de GitHub. Voir [`SECURITY.fr.md`](SECURITY.fr.md) pour la politique complète.

## Licence

Harbor est publié sous [licence MIT](LICENSE). Vous pouvez l'utiliser, le modifier et le redistribuer librement, y compris à des fins commerciales, tant que la mention de copyright d'origine est conservée.

## Remerciements

Harbor s'appuie sur de magnifiques projets :
- L'équipe [Avalonia](https://avaloniaui.net/) pour un excellent framework UI cross-platform
- Les mainteneurs de [SSH.NET](https://github.com/sshnet/SSH.NET), [FluentFTP](https://github.com/robinrodricks/FluentFTP) et de l'AWS SDK
- [SQLCipher](https://www.zetetic.net/sqlcipher/) pour le chiffrement SQLite transparent
- Les équipes [Roslyn](https://github.com/dotnet/roslyn) et [Roslynator](https://github.com/dotnet/roslynator) qui gardent notre code honnête

---

*Construit avec soin, à découvert. Stars et feedback bienvenus.*
