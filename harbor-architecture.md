# Harbor — Document d'Architecture Technique

> **Harbor** — *The modern, open-source, privacy-first command center for developers and sysadmins.*

Version du document : 1.0
Dernière révision : 2026

---

## Table des matières

1. [Vision et objectifs](#1-vision-et-objectifs)
2. [Utilisateurs cibles](#2-utilisateurs-cibles)
3. [Positionnement concurrentiel](#3-positionnement-concurrentiel)
4. [Fonctionnalités complètes](#4-fonctionnalités-complètes)
5. [Choix de la stack technique](#5-choix-de-la-stack-technique)
6. [Architecture globale](#6-architecture-globale)
7. [Structure du projet](#7-structure-du-projet)
8. [Couche d'abstraction des protocoles](#8-couche-dabstraction-des-protocoles)
9. [Modèles de données](#9-modèles-de-données)
10. [Architecture de l'UI](#10-architecture-de-lui)
11. [Terminal intégré](#11-terminal-intégré)
12. [Moteur de transferts](#12-moteur-de-transferts)
13. [Architecture de sécurité](#13-architecture-de-sécurité)
14. [Système de plugins](#14-système-de-plugins)
15. [Persistance et stockage local](#15-persistance-et-stockage-local)
16. [Synchronisation optionnelle entre machines](#16-synchronisation-optionnelle-entre-machines)
17. [Observabilité et monitoring](#17-observabilité-et-monitoring)
18. [API locale et CLI compagnon](#18-api-locale-et-cli-compagnon)
19. [Tests et qualité](#19-tests-et-qualité)
20. [CI/CD et distribution](#20-cicd-et-distribution)
21. [Roadmap détaillée](#21-roadmap-détaillée)
22. [Licence et modèle économique](#22-licence-et-modèle-économique)
23. [Documentation et communauté](#23-documentation-et-communauté)
24. [Liste des dépendances](#24-liste-des-dépendances)

---

## 1. Vision et objectifs

### 1.1 Le problème résolu

Aujourd'hui, un développeur ou sysadmin qui gère plusieurs serveurs doit jongler entre :

- **PuTTY / Windows Terminal** pour SSH
- **WinSCP / FileZilla / Cyberduck** pour les transferts
- **Un gestionnaire de mots de passe** pour les credentials
- **Un gestionnaire de clés SSH** à part
- **Des clients S3 séparés** (S3 Browser, etc.)
- **kubectl / Lens / k9s** pour Kubernetes
- **Docker Desktop** pour les conteneurs
- **Un fichier texte quelque part** avec les notes sur les serveurs

Aucun outil ne les unifie réellement. Les tentatives existantes (Termius, Royal TS) sont **payantes et fermées**. Les outils open-source sont **datés** (FileZilla, WinSCP) ou **minimalistes** (lazyssh, lssh).

### 1.2 La vision

**Harbor est un centre de commande unifié, local-first, gratuit et open source**, qui permet à un opérateur d'infrastructure de :

- Accéder à **tous ses serveurs et stockages** depuis une seule interface
- Utiliser une **UX moderne** (command palette, navigable clavier, thèmes)
- Garder ses **données et secrets 100% locaux** (sauf sync cloud chiffrée optionnelle)
- Étendre l'outil via un **système de plugins** communautaire

### 1.3 Les principes directeurs

| Principe | Application concrète |
|---|---|
| **Local-first** | Aucune télémétrie, aucune donnée envoyée dans le cloud par défaut |
| **Keyboard-first** | Tout doit être accessible sans souris |
| **Privacy by design** | Secrets chiffrés AES-256, jamais de stockage en clair |
| **Cross-platform** | Windows, macOS, Linux au même niveau de qualité |
| **Extensible** | Plugin API dès la v1 |
| **Open source réel** | Licence MIT, gouvernance claire, ouvert aux contributions |

### 1.4 Objectifs mesurables

À 12 mois :

- Une v1.0 stable, signée, packagée pour les 3 OS majeurs
- Une documentation complète (getting started, user guide, plugin API)
- Un site web dédié
- Une communauté active (Discord/Matrix, GitHub Discussions)

---

## 2. Utilisateurs cibles

### 2.1 Personas

**Persona 1 — Le sysadmin freelance (persona principal)**
- Gère 10 à 50 VPS clients
- Utilise quotidiennement SSH et SFTP
- Veut un outil fiable, pas cher, qu'il contrôle
- Sensible à la sécurité (il est responsable de l'infra de ses clients)

**Persona 2 — Le dev full-stack**
- Déploie sur 2-5 environnements (dev, staging, prod)
- Utilise Docker, parfois Kubernetes
- Veut éditer des fichiers à distance rapidement
- A déjà un workflow dans VSCode

**Persona 3 — Le power-user/sysadmin d'entreprise**
- Gère des flottes importantes (100+ serveurs)
- Travaille en équipe, a besoin de partager des configs
- Veut du scripting et de l'automatisation
- Utilise 2FA, bastions, clés matérielles

**Persona 4 — L'admin de serveurs de jeux**
- Gère des serveurs Minecraft, GMod, FiveM, s&box, etc.
- Besoins spécifiques : monitoring live, console, redéploiement rapide
- Souvent moins technique qu'un sysadmin classique

### 2.2 Hors scope

- Utilisateurs non-techniques (grand public)
- Environnements Windows Server GUI-only (Harbor est orienté Unix/Linux)
- Besoins de sessions collaboratives live (screen sharing)

---

## 3. Positionnement concurrentiel

| Outil | Points forts | Points faibles | Harbor se différencie par |
|---|---|---|---|
| **PuTTY** | Léger, stable | Windows only, daté | UI moderne, cross-platform |
| **WinSCP** | Puissant | Windows only, UI datée | Cross-platform, UI moderne |
| **FileZilla** | Gratuit, connu | UI datée, réputation sécu | Chiffrement solide, UX moderne |
| **Termius** | Beau, complet | Payant, closed, cloud forcé | Gratuit, open, local-first |
| **Cyberduck** | Bon pour S3 | SFTP basique | Tout-en-un équilibré |
| **Royal TS** | Très pro | Cher (~100€), closed | Gratuit, open |
| **MobaXterm** | Tout-en-un | Windows only, freemium | Cross-platform, pas de "pro only" |
| **Tabby** | Terminal moderne | Pas un vrai client SFTP | SFTP de vraie qualité |
| **lazyssh/lssh** | Minimaliste, rapide | Features limitées | Scope bien plus large, GUI + TUI |

**L'argument de vente unique (USP) de Harbor :**
> *"Le seul outil qui remplace PuTTY + WinSCP + Cyberduck + Docker Desktop + ton gestionnaire de clés — gratuit, open source, et qui ne t'espionne pas."*

---

## 4. Fonctionnalités complètes

### 4.1 Protocoles supportés

**Shell / exécution distante**
- SSH (v2) avec auth : password, clé (RSA/Ed25519/ECDSA), certificats OpenSSH, agent, FIDO2/Yubikey, 2FA TOTP
- Mosh (SSH résilient)
- Telnet (legacy, pour matériel réseau)
- Port série (USB serial, pour IoT/routeurs)

**Transfert de fichiers**
- SFTP (via SSH)
- SCP
- FTP (plain)
- FTPS (explicite et implicite)
- WebDAV / WebDAV over HTTPS
- Rsync (via SSH)

**Stockage cloud (via SDK officiels)**
- AWS S3
- S3-compatibles (MinIO, Backblaze B2, Wasabi, Cloudflare R2, Scaleway Object Storage, Hetzner Object Storage)
- Azure Blob Storage
- Google Cloud Storage
- Google Drive, OneDrive, Dropbox (optionnel, pour cas personnel)

**Conteneurs et orchestration**
- Docker (via Docker Engine API, local et distant)
- Docker Compose
- Kubernetes (kubectl exec, file transfer dans pods)
- Podman (compatible API Docker)

**Bases de données (utilitaire)**
- Ouverture de tunnels SSH vers MySQL, PostgreSQL, MongoDB, Redis
- Test de connexion intégré (pas un client DB complet)

### 4.2 Gestion des connexions

- Profils nommés, tagués, dans des dossiers hiérarchiques
- Recherche fuzzy sur les profils (Ctrl+K)
- Import depuis `~/.ssh/config`, PuTTY registry, WinSCP INI, FileZilla XML
- Export au format Harbor (chiffré) ou OpenSSH
- Duplication rapide avec modifications
- Templates de profils (ex. "VPS Hetzner standard")
- Variables d'environnement par profil
- Notes Markdown par profil (recherchables)
- Historique des dernières connexions

### 4.3 Terminal intégré

- Émulateur xterm-256color + true color (24 bits)
- Support ligatures et polices Nerd Fonts
- Onglets multiples
- Split panes (horizontal et vertical, imbrication libre)
- Broadcast input (écrire dans N sessions simultanément)
- Recherche dans le scrollback (Ctrl+F)
- Liens cliquables (URLs, chemins avec `file:line:col`)
- Copy on select, paste bracketed
- Enregistrement de session (format asciicast v2)
- Export session en HTML/SVG
- Hyperliens OSC 8
- Support sixel et Kitty image protocol (images dans le terminal)
- Notifications système à la fin de commandes longues (`BEL` après X secondes)
- Snippets de commandes avec variables (`${server}`, `${cwd}`, custom)
- Autocomplétion des commandes fréquentes

### 4.4 Gestion des fichiers

- Vue dual-pane (local + distant)
- Vue quad-pane (deux serveurs + local + cloud)
- Vue arborescente optionnelle
- Drag & drop entre panneaux
- Multi-sélection, actions en batch
- Preview intégrée : images, PDF, Markdown rendu, JSON, CSV, audio, vidéo, hex pour binaires
- Édition à distance : ouverture dans éditeur externe (VSCode, Sublime, Neovim, autre), upload auto au save
- Diff visuel local ↔ distant avant upload (avec option merge)
- Synchronisation bidirectionnelle (modèle Unison)
- Dry-run des transferts (voir ce qui va se passer sans l'exécuter)
- Recherche dans les fichiers distants (via grep SSH)
- Recherche par nom, par date, par taille
- Corbeille locale pour les fichiers supprimés à distance (30 jours de rétention paramétrables)
- Historique complet des actions (qui, quoi, quand)
- Modification des permissions Unix (chmod graphique)
- Changement de propriétaire (chown si droits)
- Création de liens symboliques

### 4.5 Moteur de transferts

- File d'attente persistante (survit à un crash)
- Parallélisation configurable (N transferts simultanés)
- Reprise sur erreur (retry avec backoff exponentiel)
- Reprise de transferts interrompus (range requests pour HTTP, `REST` pour FTP, offset pour SFTP)
- Limitation de bande passante paramétrable
- Priorités par transfert
- Barre de progression par fichier + globale
- Notifications à la fin
- Log détaillé par transfert
- Vérification d'intégrité (checksum SHA-256 quand possible)

### 4.6 Sécurité et secrets

- Keystore local chiffré (AES-256-GCM, clé dérivée via Argon2id depuis master password)
- Intégration password managers : Bitwarden, 1Password, KeePassXC (via leurs CLI/API officielles)
- Gestionnaire de clés SSH intégré :
  - Génération (Ed25519 par défaut, RSA 4096 option)
  - Import (OpenSSH, PuTTY PPK)
  - Export
  - Audit d'âge des clés
  - Rotation assistée
- Support FIDO2/Yubikey (SSH-hardware)
- 2FA TOTP intégré (pour les serveurs qui l'exigent)
- Vérification des credentials contre HaveIBeenPwned (opt-in)
- Mode "session sensitive" : masque automatiquement les tokens/clés dans les outputs
- Journal d'audit local (toutes les connexions et actions sensibles)
- Verrouillage automatique de l'app après inactivité
- Support agent SSH (ssh-agent, pageant, Windows OpenSSH agent)

### 4.7 Réseau avancé

- Port forwarding visuel :
  - Local (`-L`)
  - Remote (`-R`)
  - Dynamic/SOCKS (`-D`)
- UI pour gérer les tunnels actifs, démarrer/arrêter
- Jump hosts / bastions en chaîne
- Proxy chains multi-niveaux (SSH → SOCKS5 → HTTP)
- Port knocking automatique avant connexion
- Keep-alive configurables
- Test de connectivité (ping, traceroute, test de latence SSH, test de bande passante)
- Support IPv6 complet

### 4.8 Workspaces et organisation

- Concept de "workspace" : regroupe serveurs + buckets + snippets + notes
- Un workspace = un projet/client
- Icône et couleur custom par workspace
- Ouverture rapide (Ctrl+O)
- Sync optionnelle entre tes propres machines

### 4.9 Automatisations

- Scripts de post-connexion (exécution auto de commandes)
- Watchers de fichiers : "si ce fichier local change, upload-le ici"
- Tâches planifiées (cron-like, UI graphique)
- Webhooks sortants (notifier Slack/Discord à certains événements)

### 4.10 Monitoring léger

- Collecte de métriques par SSH (CPU, RAM, disque, load average, uptime, dernier login)
- Dashboard avec statut de tous les serveurs
- Alertes configurables (seuils)
- Historique des métriques (local, 30 jours)
- Support des commandes custom pour métriques maison

### 4.11 UX générale

- Command palette (Ctrl+Shift+P) pour toutes les actions
- Thèmes : sombre, clair, système, + custom (format JSON)
- Support high DPI
- Traductions i18n (fr, en en priorité ; autres par contributions)
- Raccourcis claviers configurables
- Mode zen (plein écran minimaliste)
- Onboarding guidé au premier démarrage

### 4.12 Extensibilité

- Plugin API en Lua (MoonSharp côté C#) et/ou JavaScript (Jint)
- Marketplace de plugins (GitHub repo officiel au début, UI intégrée plus tard)
- CLI compagnon (`harbor connect prod-1`, `harbor upload ...`)
- API locale HTTP (REST) pour intégrations externes
- Hooks sur les événements (connexion, déconnexion, transfert terminé, etc.)

---

## 5. Choix de la stack technique

### 5.1 Comparaison des options

| Option | Pour | Contre | Verdict |
|---|---|---|---|
| **.NET 8 + Avalonia** | Cross-platform vrai, perf native, C# connu, binaires ~40 MB | Moins de packages natifs que Electron | **Retenu** |
| **Electron + TS/React** | Écosystème web énorme, dev rapide | Binaires >100 MB, RAM élevée, perf UI | Rejeté (trop lourd) |
| **Tauri + Rust/React** | Binaires 3-5 MB, perf excellente | Courbe d'apprentissage Rust, moins de libs | Alternative sérieuse |
| **Qt / C++** | Natif partout, perf | Verbeux, licence Qt complexe | Rejeté |
| **Flutter Desktop** | UI unifiée | Encore jeune sur desktop, Dart | Rejeté |

### 5.2 Choix final : .NET 8 + Avalonia

**Rationale :**
- Cross-platform mature (Windows, macOS, Linux, y compris ARM)
- XAML + MVVM = séparation propre UI/logique
- Perf native (pas de webview)
- L'écosystème .NET a toutes les libs nécessaires (SSH.NET, FluentFTP, AWS SDK, Docker.DotNet)
- MoonSharp permet Lua natif, cohérent avec l'expertise développeur
- Hot reload en dev
- AOT compilation disponible pour binaires compacts

### 5.3 Stack complète retenue

```
Langage principal     : C# (.NET 8)
Framework UI          : Avalonia UI 11.x
Pattern UI            : MVVM (CommunityToolkit.Mvvm)
SSH/SFTP              : SSH.NET (Renci.SshNet)
FTP/FTPS              : FluentFTP
S3 et compat          : AWSSDK.S3
Azure Blob            : Azure.Storage.Blobs
GCS                   : Google.Cloud.Storage.V1
Docker                : Docker.DotNet
Kubernetes            : KubernetesClient (officiel)
Terminal              : Wrapper custom autour de VT100Control (Avalonia) + ConPTY/PTY
                        Alternative : AvaloniaEdit.Terminal (quand dispo)
Crypto                : System.Security.Cryptography (natif .NET) + Konscious.Security.Cryptography (Argon2)
Base de données       : SQLite + Microsoft.Data.Sqlite + SQLCipher
ORM                   : Dapper (léger) ou EF Core (si complexité augmente)
Plugin runtime        : MoonSharp (Lua) + Jint (JavaScript) optionnel
Logging               : Serilog
DI                    : Microsoft.Extensions.DependencyInjection
Config                : Microsoft.Extensions.Configuration
Markdown              : Markdig (pour previews et notes)
Tests                 : xUnit + FluentAssertions + Moq
CLI compagnon         : System.CommandLine
Installer             : Velopack (ex-Squirrel.Windows), ou Inno Setup + brew + AppImage/deb/rpm
```

---

## 6. Architecture globale

### 6.1 Vue en couches

```
┌─────────────────────────────────────────────────────────────┐
│                       UI (Avalonia)                          │
│   Views (XAML) — ViewModels (MVVM) — Styles & Thèmes         │
├─────────────────────────────────────────────────────────────┤
│                    Services applicatifs                       │
│  ConnectionManager — TransferEngine — TerminalService —      │
│  SecretStore — WorkspaceService — PluginHost — SyncService   │
├─────────────────────────────────────────────────────────────┤
│                 Couche d'abstraction protocoles               │
│          IRemoteFileSystem — IRemoteShell — ICloudStorage    │
├─────────────────────────────────────────────────────────────┤
│              Implémentations concrètes des protocoles         │
│  SftpProvider — FtpProvider — S3Provider — DockerProvider... │
├─────────────────────────────────────────────────────────────┤
│                    Infrastructure de base                     │
│  SQLite — Keystore — FileSystem (local) — Logger — IPC       │
└─────────────────────────────────────────────────────────────┘
```

### 6.2 Principe clé : abstraction des "remote locations"

Toute source distante (SFTP, FTP, S3, Docker volume, etc.) implémente `IRemoteFileSystem`. Tout shell distant implémente `IRemoteShell`. L'UI ne connaît que ces interfaces : elle ne sait pas si tu manipules un bucket S3 ou un serveur SFTP. C'est ce qui permet à Harbor d'être cohérent entre protocoles.

### 6.3 Processus et threading

- **Thread UI** : Avalonia, ne doit jamais être bloqué
- **Thread pool** pour les opérations I/O asynchrones (tout via `async`/`await`)
- **Workers dédiés** pour : moteur de transferts (un par transfert), sessions SSH interactives (un par session), plugin host
- **IPC** entre app et CLI compagnon via named pipes (Windows) / Unix sockets (Linux/macOS)

---

## 7. Structure du projet

### 7.1 Arborescence des projets .NET

```
Harbor/
├── src/
│   ├── Harbor.App/                          # Point d'entrée Avalonia
│   │   ├── App.axaml
│   │   ├── Program.cs
│   │   └── Harbor.App.csproj
│   │
│   ├── Harbor.UI/                           # Views, ViewModels, Controls
│   │   ├── Views/
│   │   │   ├── MainWindow.axaml
│   │   │   ├── ConnectionPanel.axaml
│   │   │   ├── TerminalView.axaml
│   │   │   ├── FileBrowserView.axaml
│   │   │   └── ...
│   │   ├── ViewModels/
│   │   ├── Controls/
│   │   ├── Converters/
│   │   ├── Styles/
│   │   └── Themes/
│   │
│   ├── Harbor.Core/                         # Domaine, interfaces, modèles
│   │   ├── Abstractions/
│   │   │   ├── IRemoteFileSystem.cs
│   │   │   ├── IRemoteShell.cs
│   │   │   ├── ICloudStorage.cs
│   │   │   └── ITransferEngine.cs
│   │   ├── Models/
│   │   │   ├── Profile.cs
│   │   │   ├── Workspace.cs
│   │   │   ├── RemoteFile.cs
│   │   │   ├── Transfer.cs
│   │   │   └── ...
│   │   └── Events/
│   │
│   ├── Harbor.Services/                     # Services applicatifs
│   │   ├── Connections/
│   │   ├── Transfers/
│   │   ├── Terminals/
│   │   ├── Workspaces/
│   │   └── Automation/
│   │
│   ├── Harbor.Security/                     # Keystore, crypto, audit
│   │   ├── Keystore/
│   │   ├── Crypto/
│   │   ├── Audit/
│   │   └── Integrations/                    # Bitwarden, 1Password, KeePass
│   │
│   ├── Harbor.Protocols.Ssh/                # SSH + SFTP
│   ├── Harbor.Protocols.Ftp/                # FTP, FTPS
│   ├── Harbor.Protocols.S3/                 # S3 et compatibles
│   ├── Harbor.Protocols.Azure/
│   ├── Harbor.Protocols.Gcs/
│   ├── Harbor.Protocols.WebDav/
│   ├── Harbor.Protocols.Docker/
│   ├── Harbor.Protocols.Kubernetes/
│   │
│   ├── Harbor.Terminal/                     # Émulateur terminal
│   │   ├── PtyHost/
│   │   ├── VtParser/
│   │   └── Rendering/
│   │
│   ├── Harbor.Plugins/                      # Runtime plugins
│   │   ├── Lua/
│   │   ├── Js/
│   │   └── Api/
│   │
│   ├── Harbor.Data/                         # Persistance, SQLite
│   │   ├── Migrations/
│   │   ├── Repositories/
│   │   └── HarborDbContext.cs
│   │
│   ├── Harbor.Cli/                          # CLI compagnon
│   │   └── Program.cs
│   │
│   └── Harbor.Ipc/                          # Communication app <-> CLI <-> plugins
│
├── tests/
│   ├── Harbor.Core.Tests/
│   ├── Harbor.Services.Tests/
│   ├── Harbor.Protocols.Ssh.Tests/
│   ├── Harbor.Security.Tests/
│   └── Harbor.E2E.Tests/
│
├── docs/                                    # Documentation (Docusaurus/Astro)
├── scripts/                                 # Build scripts, installers
├── .github/
│   └── workflows/                           # CI/CD
├── Harbor.sln
├── README.md
├── LICENSE (MIT)
├── CONTRIBUTING.md
├── CODE_OF_CONDUCT.md
└── SECURITY.md
```

### 7.2 Règles d'isolation

- `Harbor.Core` ne dépend de **rien** (pur domaine)
- `Harbor.UI` ne dépend **jamais** des `Harbor.Protocols.*` directement (seulement via interfaces de Core)
- Chaque `Harbor.Protocols.X` est indépendant des autres
- Tout passe par injection de dépendances (DI container)

---

## 8. Couche d'abstraction des protocoles

### 8.1 Interface IRemoteFileSystem

```csharp
public interface IRemoteFileSystem : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();
    bool IsConnected { get; }

    Task<IReadOnlyList<RemoteFile>> ListAsync(string path, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string path, long offset = 0, CancellationToken ct = default);
    Task<Stream> OpenWriteAsync(string path, bool append = false, CancellationToken ct = default);
    Task<RemoteFile> StatAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task CreateDirectoryAsync(string path, CancellationToken ct = default);
    Task RenameAsync(string oldPath, string newPath, CancellationToken ct = default);
    Task SetPermissionsAsync(string path, UnixFileMode mode, CancellationToken ct = default);

    IAsyncEnumerable<RemoteFile> WatchAsync(string path, CancellationToken ct = default);

    RemoteFileSystemCapabilities Capabilities { get; }

    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
}
```

### 8.2 Interface IRemoteShell

```csharp
public interface IRemoteShell : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken ct = default);
    Task<int> ExecuteAsync(string command, Stream? stdout = null, Stream? stderr = null, CancellationToken ct = default);

    // Pour sessions interactives (terminal)
    Task<IInteractiveSession> StartInteractiveSessionAsync(TerminalSize size, CancellationToken ct = default);

    // Pour port forwarding
    Task<IPortForward> CreateLocalForwardAsync(int localPort, string remoteHost, int remotePort);
    Task<IPortForward> CreateRemoteForwardAsync(int remotePort, string localHost, int localPort);
    Task<IPortForward> CreateDynamicForwardAsync(int localPort);
}
```

### 8.3 Capabilities (certaines features n'existent pas partout)

```csharp
[Flags]
public enum RemoteFileSystemCapabilities
{
    None = 0,
    UnixPermissions = 1 << 0,
    Symlinks = 1 << 1,
    HardLinks = 1 << 2,
    ExtendedAttributes = 1 << 3,
    AtomicRename = 1 << 4,
    Watch = 1 << 5,
    PartialReads = 1 << 6,
    PartialWrites = 1 << 7,
    // Par exemple, S3 ne supporte pas UnixPermissions ni vraiment les Symlinks
}
```

L'UI adapte ce qu'elle affiche selon les capabilities (ex: cache le menu "chmod" si `UnixPermissions` absent).

---

## 9. Modèles de données

### 9.1 Entités principales

```csharp
public record Workspace(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<Guid> ProfileIds,
    string? Notes);

public record Profile(
    Guid Id,
    string Name,
    ProtocolKind Protocol,
    ConnectionDetails Connection,
    AuthenticationMethod Auth,
    IReadOnlyList<string> Tags,
    string? ParentFolderId,
    Dictionary<string, string> EnvVars,
    string? PostConnectScript,
    string? Notes);

public abstract record ConnectionDetails;
public record SshConnectionDetails(string Host, int Port, string Username, JumpHost? Jump) : ConnectionDetails;
public record S3ConnectionDetails(string Endpoint, string Region, string BucketName) : ConnectionDetails;
// ... etc

public abstract record AuthenticationMethod;
public record PasswordAuth(EncryptedString Password) : AuthenticationMethod;
public record KeyAuth(Guid KeyId, EncryptedString? Passphrase) : AuthenticationMethod;
public record AgentAuth() : AuthenticationMethod;
public record FidoAuth() : AuthenticationMethod;

public record SshKey(
    Guid Id,
    string Name,
    KeyAlgorithm Algorithm,
    EncryptedBytes PrivateKey,
    byte[] PublicKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt);

public record Transfer(
    Guid Id,
    TransferDirection Direction,  // Upload, Download, ServerToServer
    string SourcePath,
    string DestPath,
    Guid? SourceProfileId,
    Guid? DestProfileId,
    long TotalBytes,
    long TransferredBytes,
    TransferStatus Status,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);

public record AuditLogEntry(
    Guid Id,
    DateTimeOffset Timestamp,
    AuditEventType Type,
    Guid? ProfileId,
    string Description,
    string? Metadata);
```

### 9.2 Schéma SQLite (simplifié)

```sql
CREATE TABLE workspaces (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    icon TEXT,
    color TEXT,
    notes TEXT,
    created_at INTEGER NOT NULL,
    updated_at INTEGER NOT NULL
);

CREATE TABLE profiles (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    protocol TEXT NOT NULL,
    workspace_id TEXT REFERENCES workspaces(id) ON DELETE SET NULL,
    parent_folder_id TEXT,
    connection_json TEXT NOT NULL,    -- JSON sérialisé
    auth_json TEXT NOT NULL,           -- JSON, avec secrets chiffrés
    tags TEXT,                         -- CSV
    env_vars_json TEXT,
    post_connect_script TEXT,
    notes TEXT,
    created_at INTEGER NOT NULL,
    updated_at INTEGER NOT NULL,
    last_used_at INTEGER
);

CREATE TABLE ssh_keys (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    algorithm TEXT NOT NULL,
    private_key_encrypted BLOB NOT NULL,
    public_key BLOB NOT NULL,
    created_at INTEGER NOT NULL,
    last_used_at INTEGER
);

CREATE TABLE transfers (
    id TEXT PRIMARY KEY,
    direction TEXT NOT NULL,
    source_path TEXT NOT NULL,
    dest_path TEXT NOT NULL,
    source_profile_id TEXT,
    dest_profile_id TEXT,
    total_bytes INTEGER NOT NULL,
    transferred_bytes INTEGER NOT NULL,
    status TEXT NOT NULL,
    error_message TEXT,
    created_at INTEGER NOT NULL,
    completed_at INTEGER
);

CREATE TABLE audit_log (
    id TEXT PRIMARY KEY,
    timestamp INTEGER NOT NULL,
    type TEXT NOT NULL,
    profile_id TEXT,
    description TEXT NOT NULL,
    metadata_json TEXT
);

CREATE TABLE snippets (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    content TEXT NOT NULL,
    variables_json TEXT,
    tags TEXT,
    created_at INTEGER NOT NULL
);

-- Index pour recherche fuzzy
CREATE INDEX idx_profiles_name ON profiles(name);
CREATE INDEX idx_profiles_tags ON profiles(tags);
CREATE INDEX idx_audit_log_timestamp ON audit_log(timestamp DESC);
```

Toute la base est **chiffrée via SQLCipher** avec une clé dérivée du master password.

---

## 10. Architecture de l'UI

### 10.1 Pattern MVVM avec CommunityToolkit.Mvvm

- **View (XAML)** : purement déclaratif
- **ViewModel** : logique de présentation, expose des `ObservableProperty` et `RelayCommand`
- **Model** : entités de domaine (immuables quand possible)

Exemple :

```csharp
public partial class ConnectionPanelViewModel : ObservableObject
{
    private readonly IConnectionManager _connections;
    private readonly INavigationService _nav;

    [ObservableProperty]
    private ObservableCollection<ProfileViewModel> profiles = new();

    [ObservableProperty]
    private string searchQuery = "";

    [RelayCommand]
    private async Task ConnectAsync(ProfileViewModel profile)
    {
        await _connections.OpenAsync(profile.Id);
        _nav.NavigateToSession(profile.Id);
    }
}
```

### 10.2 Layout général

```
┌──────────────────────────────────────────────────────────────┐
│  [Harbor]  ⌘K Command palette  [profile: Prod-1]   [⚙] [?]   │
├─────────┬────────────────────────────────────────────────────┤
│         │                                                     │
│  Side-  │               Onglets : [Term prod-1] [SFTP s3-bak] │
│  bar    │  ┌─────────────────────────────────────────────────┐│
│         │  │                                                 ││
│ - Work- │  │            Zone principale                      ││
│  spaces │  │   (Terminal, File browser, Dashboard...)        ││
│ - Conn. │  │                                                 ││
│ - Keys  │  │                                                 ││
│ - Trans-│  │                                                 ││
│  fers   │  │                                                 ││
│ - Notes │  └─────────────────────────────────────────────────┘│
│         │  ┌─────────────────────────────────────────────────┐│
│         │  │  Bottom panel : queue, logs, tunnels            ││
│         │  └─────────────────────────────────────────────────┘│
└─────────┴────────────────────────────────────────────────────┘
```

### 10.3 Command palette

Accessible via `Ctrl+Shift+P` (ou `Cmd+Shift+P` sur mac). Fonctionne comme VSCode :

- Recherche fuzzy sur toutes les actions
- Recherche de profils, workspaces, snippets
- Actions rapides : `> Connect to...`, `> Upload file`, `> Open SSH key...`
- Historique des commandes récentes

### 10.4 Thèmes

- **Dark (default)** — inspiré de Catppuccin Mocha / Tokyo Night
- **Light** — sobre, neutre
- **System** — suit le thème OS
- **Custom** — fichier JSON, rechargement à chaud

Format thème (exemple partiel) :

```json
{
  "name": "My Theme",
  "colors": {
    "background.primary": "#1a1b26",
    "background.secondary": "#24283b",
    "foreground.primary": "#c0caf5",
    "accent.primary": "#7aa2f7",
    "status.success": "#9ece6a",
    "status.error": "#f7768e",
    "terminal.ansi.red": "#f7768e"
  },
  "fonts": {
    "ui": "Inter",
    "mono": "JetBrains Mono"
  }
}
```

---

## 11. Terminal intégré

### 11.1 Composants

Trois couches :

1. **PTY Host** — processus qui communique avec le serveur SSH (via SSH.NET) ou localement (via ConPTY sur Windows, pty sur Unix)
2. **VT Parser** — parse les séquences d'échappement xterm/VT100
3. **Renderer** — dessine le terminal dans un contrôle Avalonia custom (avec accélération GPU si possible)

### 11.2 Technologies

- **ConPTY** sur Windows (Windows 10+)
- **PseudoTerminal** POSIX sur Linux/macOS
- Pour SSH distant : `ShellStream` de SSH.NET, branché directement dans le VT parser

### 11.3 Features avancées

- **Ligatures** : support des polices comme Fira Code, JetBrains Mono
- **True color** : support RGB complet (ANSI 38;2;r;g;b)
- **OSC 8 hyperlinks** : liens cliquables dans la sortie
- **Sixel / Kitty image protocol** : images inline dans le terminal (utile pour `timg`, `img2sixel`)
- **Search** : recherche dans le scrollback avec surlignage
- **Broadcast** : une commande → N terminaux (groupe de sessions)

### 11.4 Snippets de commandes

Structure :

```yaml
name: "Restart nginx"
description: "Gracefully restart nginx service"
command: "sudo systemctl reload nginx && sudo systemctl status nginx"
variables: []
tags: ["nginx", "web"]
```

Avec variables :

```yaml
name: "Tail log"
command: "tail -f /var/log/${service}/${file}"
variables:
  - name: service
    default: "nginx"
  - name: file
    default: "access.log"
```

---

## 12. Moteur de transferts

### 12.1 Architecture du TransferEngine

```
┌──────────────────────────────────────────────────────┐
│                  TransferEngine                       │
│                                                       │
│  Queue (persistante SQLite) ──> Scheduler ──> Workers│
│                                     │                 │
│          ┌──────────────────────────┼──────────────┐ │
│          ▼                          ▼              ▼ │
│      Worker 1                   Worker 2        Worker N │
│   (Transfer A)                 (Transfer B)     (...)   │
└──────────────────────────────────────────────────────┘
```

### 12.2 Stratégies par protocole

| Protocole | Reprise partielle | Parallélisme par fichier | Checksum |
|---|---|---|---|
| SFTP | Oui (offset) | Non (séquentiel) | Oui (sha256sum côté serveur) |
| FTP | Oui (REST) | Non | Non (pas natif) |
| S3 | Oui (multipart) | Oui (multipart upload/download) | Oui (ETag, SHA256) |
| WebDAV | Partiel (Range) | Non | Parfois |
| Docker | Non (tar stream) | Non | Non |

### 12.3 Gestion d'erreurs

- Retry automatique 3 fois avec backoff exponentiel (1s, 2s, 4s)
- Si échec persistant : transfert marqué `Failed` mais reste dans la queue pour retry manuel
- L'utilisateur peut repartir de zéro, reprendre, skip, ou abandonner

### 12.4 Reprise après crash

La queue est en SQLite, donc elle survit à un crash de l'app. Au redémarrage, les transferts `InProgress` sont marqués `Interrupted` et proposés en reprise.

---

## 13. Architecture de sécurité

### 13.1 Modèle de menace

**Attaquants considérés :**
- Utilisateur local non autorisé ayant accès à la machine (ex: collègue curieux)
- Malware basique qui lit les fichiers de config
- Backup non chiffré qui fuite

**Hors scope :**
- Attaquant avec privilèges root/admin (non défendable au niveau applicatif)
- Attaques supply chain (NuGet compromis) — mitigation : SBOM, signatures

### 13.2 Chiffrement du keystore

```
Master password (saisi par l'utilisateur)
       │
       ▼  Argon2id (m=64MB, t=3, p=4, salt=16 bytes random stocké)
       │
       ▼
  Master Key (32 bytes)
       │
       ├──> Chiffrement base SQLite (SQLCipher, AES-256)
       │
       └──> Chiffrement secrets individuels (AES-256-GCM avec nonce aléatoire)
```

- Le master password **n'est jamais stocké**
- Il est gardé en RAM uniquement pendant la session, dans un `SecureString` zéroïsé au logout
- Timeout d'inactivité configurable (défaut : 30 min) → re-demande du master password

### 13.3 Intégration password managers

**Bitwarden :**
- Via Bitwarden CLI (`bw`) installé sur le système
- L'utilisateur s'authentifie via `bw unlock`, Harbor récupère le session key
- Harbor liste les items, permet de sélectionner, récupère le secret à la demande
- Jamais stocké en cache > 1 minute

**1Password :**
- Via `op` CLI (1Password CLI 2+)
- Même principe

**KeePassXC :**
- Via son API native (socket de KeePassXC Browser)
- Protocol officiel

### 13.4 Vérification de l'intégrité de l'application

- Binaires signés (code signing)
- SBOM (Software Bill of Materials) publié avec chaque release
- Builds reproductibles si possible (via `-ContinuousIntegrationBuild`)

### 13.5 Audit log

Toute action sensible est loggée :
- Connexion / déconnexion
- Lecture/écriture de secrets
- Modification de profils
- Import/export

Accessible depuis l'UI (`Settings → Audit log`).

---

## 14. Système de plugins

### 14.1 Choix du runtime

**Lua via MoonSharp** comme choix principal :
- Sandbox facile à définir
- Léger (~500 KB)
- Syntaxe accessible
- Cohérent avec la communauté sysadmin

**JavaScript via Jint** en option :
- Pour attirer les devs web
- Sandbox plus complexe à sécuriser

### 14.2 API exposée aux plugins

```lua
-- Exemple de plugin Lua
plugin = {
    name = "My Plugin",
    version = "1.0.0",
    author = "Fralawks"
}

function onConnect(profile)
    harbor.log.info("Connected to " .. profile.name)
    harbor.terminal.send(profile.id, "echo 'hello from plugin'\n")
end

function registerCommands()
    harbor.commands.register({
        id = "my-plugin.cleanup",
        title = "Cleanup temp files",
        handler = function(ctx)
            ctx.shell:exec("rm -rf /tmp/myapp/*")
        end
    })
end

harbor.events.on("connect", onConnect)
registerCommands()
```

### 14.3 Manifest de plugin

```json
{
  "id": "harbor-plugin-nginx-tools",
  "name": "Nginx Tools",
  "version": "1.2.3",
  "author": "...",
  "description": "Useful nginx commands and snippets",
  "main": "main.lua",
  "runtime": "lua",
  "permissions": [
    "shell.exec",
    "file.read",
    "file.write"
  ],
  "events": ["connect", "disconnect"],
  "commands": ["nginx.reload", "nginx.test"]
}
```

### 14.4 Sandbox

- Pas d'accès direct au filesystem local hors du dossier de plugin
- Pas d'accès réseau direct (tout passe par l'API Harbor)
- Pas de `require` vers modules système non autorisés
- Limite CPU/RAM par plugin

---

## 15. Persistance et stockage local

### 15.1 Emplacements des données

| OS | Emplacement |
|---|---|
| Windows | `%APPDATA%\Harbor\` |
| macOS | `~/Library/Application Support/Harbor/` |
| Linux | `~/.config/harbor/` (suit XDG Base Directory) |

Structure :

```
Harbor/
├── harbor.db            # SQLite chiffré (SQLCipher)
├── config.json          # Préférences non sensibles (thème, raccourcis)
├── themes/              # Thèmes custom
├── plugins/             # Plugins installés
│   └── <plugin-id>/
├── logs/                # Logs rotation journalière, 7 jours de rétention
└── backups/             # Backups automatiques de harbor.db (cryptés)
```

### 15.2 Backups

- Backup auto du `harbor.db` au démarrage si > 24h depuis le dernier
- Rétention : 7 derniers backups
- Export manuel possible (chiffré, exportable)

---

## 16. Synchronisation optionnelle entre machines

### 16.1 Principe

**Opt-in uniquement.** Par défaut, tout est local.

Si activé, synchronise les profils (sans secrets) + workspaces + snippets + thèmes entre les machines de l'utilisateur.

### 16.2 Options de backend

- **GitHub Gist privé** (simple, utilise le compte GitHub de l'utilisateur)
- **Git repo custom** (l'utilisateur configure son propre repo)
- **WebDAV** (Nextcloud, ownCloud)
- **S3** (bucket personnel)

### 16.3 Chiffrement

Toujours chiffré côté client avec une clé dérivée d'un mot de passe de sync **séparé** du master password. Zero-knowledge vis-à-vis du backend.

### 16.4 Secrets

Les secrets (mots de passe, clés privées) ne sont **jamais** synchronisés par défaut. Option avancée pour les inclure, avec avertissement et double chiffrement.

---

## 17. Observabilité et monitoring

### 17.1 Collecte de métriques

Pour chaque profil SSH, une commande "probe" est exécutée à intervalle configurable (défaut : 1 min) :

```bash
uptime && free -b && df -P / && nproc
```

Parser les sorties, stocker dans SQLite, afficher dans le dashboard.

### 17.2 Dashboard

- Grille ou liste de tous les serveurs
- Statut : 🟢 en ligne, 🟡 lent, 🔴 hors ligne
- Mini-graphes sparkline des métriques clés
- Alertes si seuils dépassés

### 17.3 Alertes

Configurables :
- "CPU > 90% pendant 5 min"
- "Disque < 10% libre"
- "Serveur injoignable > 2 min"

Notifications natives OS + optionnel webhook vers Discord/Slack.

---

## 18. API locale et CLI compagnon

### 18.1 CLI

```bash
harbor list                        # Liste les profils
harbor connect prod-web-1          # Ouvre une session SSH dans le terminal de l'OS
harbor upload ./dist prod-web-1:/var/www/
harbor download prod-db-1:/backups/dump.sql ./
harbor tunnel prod-db-1 3306:localhost:3306
harbor exec prod-web-1 "systemctl restart nginx"
harbor workspace open "Client X"
```

### 18.2 API locale HTTP

Serveur local (localhost only, token d'auth) écoutant sur port random, découvrable via `~/.config/harbor/api.json`.

```
GET  /api/v1/profiles
POST /api/v1/profiles
POST /api/v1/connections/open
POST /api/v1/transfers
GET  /api/v1/transfers/{id}
```

Utile pour intégrations avec d'autres outils, automations externes.

---

## 19. Tests et qualité

### 19.1 Pyramide de tests

- **Tests unitaires** (~70% du code couvert) : logique pure, parsers, services
- **Tests d'intégration** : chaque implémentation de protocole testée contre un serveur réel (conteneur Docker : OpenSSH, vsftpd, MinIO, etc.)
- **Tests E2E UI** : scénarios critiques via Avalonia.Headless + FluentAssertions.Avalonia

### 19.2 Outils

- **xUnit** pour les tests
- **FluentAssertions** pour les assertions lisibles
- **Moq** pour les mocks
- **Testcontainers .NET** pour spinner des conteneurs (SSH server, FTP, MinIO) pendant les tests

### 19.3 Qualité de code

- **.editorconfig** strict
- **Analyzers** activés (Microsoft, Roslynator)
- **Nullable reference types** partout (`#nullable enable`)
- **Warnings as errors** sur la CI
- Format check via `dotnet format`

---

## 20. CI/CD et distribution

### 20.1 GitHub Actions

Workflows :
- **build.yml** : build + tests sur push/PR (matrix Windows/macOS/Linux)
- **release.yml** : déclenché sur tag `v*`, produit binaires signés pour les 3 OS
- **security.yml** : scan dépendances (Dependabot), SAST (CodeQL)

### 20.2 Packaging

- **Windows** : installer MSI via WiX ou Velopack, signé (Authenticode)
- **macOS** : `.dmg` signé + notarisé (Apple Developer ID)
- **Linux** : `.AppImage`, `.deb`, `.rpm`, Flatpak à moyen terme
- **Binaires portables** pour chaque OS aussi

### 20.3 Distribution

- Site web avec bouton de téléchargement qui détecte l'OS
- GitHub Releases
- Package managers :
  - Windows : `winget install harbor` + Chocolatey
  - macOS : `brew install --cask harbor`
  - Linux : dépôt APT + AUR + Flathub

### 20.4 Auto-update

- Vérification au démarrage (opt-out possible)
- Téléchargement et install en arrière-plan via Velopack
- Signature vérifiée avant install

---

## 21. Roadmap détaillée

### V0.1 — "Foundation" (mois 1-2)

- [ ] Structure du projet, DI, logging
- [ ] SSH + SFTP basique (connexion, list/upload/download)
- [ ] Fenêtre principale + panneau de profils
- [ ] Terminal intégré (mono-onglet, pas de split)
- [ ] Keystore chiffré basique
- [ ] Thème sombre par défaut
- [ ] Packaging Windows (MSI non signé)

**Livrable :** binaire testable. On peut se connecter à un serveur, transférer un fichier, ouvrir un terminal.

### V0.2 — "Polished MVP" (mois 2-3)

- [ ] FTP / FTPS
- [ ] Command palette
- [ ] Multi-onglets
- [ ] Drag & drop entre panneaux
- [ ] Queue de transferts avec pause/resume
- [ ] Import depuis `~/.ssh/config`
- [ ] Thèmes (dark + light)

### V0.3 — "Cloud + Ergonomics" (mois 3-5)

- [ ] S3 et compatibles
- [ ] Workspaces
- [ ] Édition à distance (watch + upload auto)
- [ ] Diff visuel avant upload
- [ ] Preview images/PDF/Markdown
- [ ] Split panes terminal

### V0.4 — "Power Features" (mois 5-7)

- [ ] Port forwarding (local, remote, dynamic) avec UI
- [ ] Jump hosts / bastions chaînés
- [ ] Gestionnaire de clés SSH
- [ ] Intégration Bitwarden + 1Password + KeePassXC
- [ ] 2FA TOTP
- [ ] Packaging macOS signé + Linux AppImage

### V0.5 — "Extensibility" (mois 7-9)

- [ ] Système de plugins Lua
- [ ] CLI compagnon
- [ ] API locale HTTP
- [ ] Automatisations basiques (post-connect script, watchers)
- [ ] Documentation complète (Docusaurus)
- [ ] Site web lancé

### V0.9 — "Observability" (mois 9-11)

- [ ] Dashboard de monitoring
- [ ] Alertes
- [ ] Docker + Kubernetes providers
- [ ] Sync optionnelle entre machines
- [ ] Snippets avec variables
- [ ] Broadcast terminal

### V1.0 — "Ready for prime time" (mois 12)

- [ ] Polish UI complet
- [ ] Installers signés et notarisés
- [ ] Auto-update
- [ ] Traductions (EN + FR au minimum)
- [ ] Onboarding guidé
- [ ] Tous les bugs critiques corrigés
- [ ] Annonce publique : HN, Reddit (r/selfhosted, r/dotnet, r/sysadmin), Twitter/Mastodon

### Post-v1.0

- Marketplace de plugins
- Version "Team" (sync multi-utilisateur chiffrée)
- Mobile (consultation read-only sur iOS/Android via .NET MAUI)
- Mode collaboratif live (screen sharing intégré)

---

## 22. Licence et modèle économique

### 22.1 Licence

**MIT License** pour l'app principale.

Pourquoi MIT et pas GPL :
- Adoption en entreprise facilitée
- Compatible avec la plupart des dépendances
- Pas de friction pour les contributeurs

### 22.2 Modèle économique (optionnel, post-v1)

**Open-core sain** :
- **Harbor (Community)** : tout ce qui est décrit ici, gratuit, MIT
- **Harbor Team** (payant) : sync multi-utilisateur, SSO, audit logs entreprise, support — payant, closed-source additionnel

Ce modèle est éprouvé (GitLab, Mattermost, Supabase). La Community Edition reste pleinement utilisable, personne ne se sent lésé.

---

## 23. Documentation et communauté

### 23.1 Documentation

Site Docusaurus avec :
- **Getting Started** (installation, première connexion)
- **User Guide** (toutes les features)
- **Plugin API** (référence complète)
- **CLI Reference**
- **FAQ**
- **Troubleshooting**
- **Contributing**

### 23.2 Communauté

- **GitHub Discussions** pour Q&A
- **Discord** ou **Matrix** pour le chat
- **Blog** (dev log mensuel)
- **Twitter/Mastodon/Bluesky** officiels

### 23.3 Gouvernance

- `BDFL` (Benevolent Dictator For Life) : toi, initialement
- Core contributors ajoutés selon la contribution
- Décisions majeures via RFC (Request For Comments) dans le repo

---

## 24. Liste des dépendances

### 24.1 Runtime (toutes MIT ou compatibles)

| Paquet | Version | Licence | Rôle |
|---|---|---|---|
| Avalonia | 11.x | MIT | UI framework |
| CommunityToolkit.Mvvm | 8.x | MIT | MVVM helpers |
| SSH.NET | 2024.x | MIT | SSH/SFTP client |
| FluentFTP | 49.x | MIT | FTP/FTPS client |
| AWSSDK.S3 | 3.x | Apache 2.0 | S3 |
| Azure.Storage.Blobs | 12.x | MIT | Azure Blob |
| Google.Cloud.Storage.V1 | 4.x | Apache 2.0 | GCS |
| Docker.DotNet | 3.x | MIT | Docker API |
| KubernetesClient | 14.x | Apache 2.0 | Kubernetes |
| Microsoft.Data.Sqlite | 8.x | MIT | SQLite |
| SQLitePCLRaw.bundle_e_sqlcipher | 2.x | Apache 2.0 | SQLCipher |
| Konscious.Security.Cryptography.Argon2 | 1.x | MIT | Argon2 |
| MoonSharp | 2.x | BSD 3-Clause | Lua runtime |
| Serilog | 4.x | Apache 2.0 | Logging |
| Markdig | 0.x | BSD 2-Clause | Markdown |
| System.CommandLine | 2.x | MIT | CLI parsing |

### 24.2 Dev dependencies

| Paquet | Rôle |
|---|---|
| xUnit | Tests unitaires |
| FluentAssertions | Assertions |
| Moq | Mocking |
| Testcontainers | Conteneurs pour tests |
| coverlet.collector | Couverture de code |

---

## Annexe A — Glossaire

- **Profil** : une configuration de connexion nommée (ex: "Prod Web 1")
- **Workspace** : un groupement logique de profils et ressources (un projet/client)
- **Keystore** : le coffre-fort local des secrets
- **Jump host (bastion)** : serveur intermédiaire pour atteindre un serveur interne
- **PTY** : pseudo-terminal, interface pour programmes interactifs en CLI
- **ConPTY** : l'API Windows équivalente à PTY (Windows 10+)
- **Argon2id** : algorithme moderne de dérivation de mot de passe (résistant GPU/ASIC)

---

## Annexe B — Checklist de démarrage

Pour commencer à coder dès demain :

1. [ ] Créer le repo GitHub (public, MIT license, README initial)
2. [ ] Initialiser la solution : `dotnet new sln -n Harbor`
3. [ ] Créer les projets minimum : `Harbor.App`, `Harbor.Core`, `Harbor.UI`, `Harbor.Protocols.Ssh`, `Harbor.Core.Tests`
4. [ ] Ajouter Avalonia : `dotnet new avalonia.app`
5. [ ] Configurer `.editorconfig`, `Directory.Build.props`, `.gitignore`
6. [ ] Mettre en place GitHub Actions (build + tests sur push)
7. [ ] Coder une première connexion SSH minimale : se connecter, lister `/`, afficher dans la console
8. [ ] Coder la première fenêtre Avalonia avec bouton "Connect" qui appelle le code précédent
9. [ ] Commit, push, tag `v0.0.1`

À partir de là, chaque feature de la roadmap V0.1 devient une issue GitHub, un PR, une release.

---

*Document vivant — à faire évoluer au fur et à mesure des décisions techniques.*
