# Harbor — Feuille de Route Officielle

> Document de suivi d'avancement — mis à jour brique par brique.
> Référence d'architecture : `harbor-architecture.md`.

---

## Objectif final

Construire **Harbor** : un centre de commande unifié, local-first, gratuit et open source (MIT), permettant à un opérateur d'infrastructure d'accéder à tous ses serveurs et stockages depuis une seule interface cross-platform (Windows, macOS, Linux).

**Stack technique retenue :** .NET 8 + Avalonia UI 11 + MVVM (CommunityToolkit.Mvvm) + SQLite chiffré (SQLCipher) + SSH.NET.

**Livrable V1.0 visé :** application signée, packagée pour les 3 OS, avec SSH/SFTP, FTP/FTPS, S3, terminal intégré, gestion de clés, plugins Lua, monitoring léger, documentation complète et site web.

---

## Règles de travail

1. Avancement **brique par brique**. Aucune tâche n'est entamée sans validation préalable du Chef de projet.
2. À la fin de chaque tâche : cocher la case correspondante dans ce fichier, lire la tâche suivante, puis la proposer au Chef de projet avant de coder.
3. Toute décision technique impactant l'architecture est consignée dans la section **Notes et Décisions** en bas du fichier.
4. Si une zone d'ombre apparaît, poser la question avant de coder — ne jamais improviser silencieusement.

---

## Phase 0 — Bootstrap & Outillage

Objectif : disposer d'un squelette de projet compilable et d'une CI fonctionnelle.

- [x] 0.1 — Initialiser le dépôt Git local (`git init`, `.gitignore` .NET/Avalonia/IDE, `README.md` minimal)
- [x] 0.2 — Créer la solution : `Harbor.sln` (format classique, décidé — voir Notes)
- [x] 0.3 — Créer l'arborescence de projets vides conforme à `harbor-architecture.md` §7 (`Harbor.App`, `Harbor.UI`, `Harbor.Core`, `Harbor.Services`, `Harbor.Security`, `Harbor.Protocols.Ssh`, `Harbor.Protocols.Ftp`, `Harbor.Protocols.S3`, `Harbor.Protocols.Azure`, `Harbor.Protocols.Gcs`, `Harbor.Protocols.WebDav`, `Harbor.Protocols.Docker`, `Harbor.Protocols.Kubernetes`, `Harbor.Terminal`, `Harbor.Plugins`, `Harbor.Data`, `Harbor.Cli`, `Harbor.Ipc`)
- [x] 0.4 — Créer les projets de tests correspondants (`tests/Harbor.Core.Tests`, `tests/Harbor.Services.Tests`, `tests/Harbor.Protocols.Ssh.Tests`, `tests/Harbor.Security.Tests`, `tests/Harbor.E2E.Tests`)
- [x] 0.5 — Configurer `Directory.Build.props` (target `net10.0`, nullable enable, warnings as errors, ImplicitUsings)
- [x] 0.6 — Configurer `.editorconfig` strict
- [x] 0.7 — Ajouter les analyzers (Microsoft.CodeAnalysis.NetAnalyzers, Roslynator)
- [x] 0.8 — Créer `LICENSE` (MIT), `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`
- [x] 0.9 — Créer le workflow GitHub Actions `build.yml` (matrix Windows/macOS/Linux : restore + build + test)
- [x] 0.10 — Vérifier que la solution compile à vide sans erreur ni warning
- [x] 0.11 — (bonus) Créer `.gitattributes` pour normaliser les line endings (LF par défaut, CRLF pour .sln/.bat/.ps1)

---

## Phase 1 — Noyau applicatif (Core, Data, Security)

Objectif : disposer du domaine, des interfaces, de la persistance et du keystore chiffré.

### 1.1 Harbor.Core — Domaine
- [x] 1.1.1 — Créer les enums (`ProtocolKind`, `KeyAlgorithm`, `TransferDirection`, `TransferStatus`, `AuditEventType`, `RemoteFileSystemCapabilities`)
- [ ] 1.1.2 — Créer les records de modèles (`Workspace`, `Profile`, `ConnectionDetails` + variantes, `AuthenticationMethod` + variantes, `SshKey`, `Transfer`, `AuditLogEntry`, `RemoteFile`, `Snippet`)
- [ ] 1.1.3 — Créer les types utilitaires (`EncryptedString`, `EncryptedBytes`, `TerminalSize`, `JumpHost`)
- [ ] 1.1.4 — Créer les interfaces d'abstraction (`IRemoteFileSystem`, `IRemoteShell`, `ICloudStorage`, `ITransferEngine`, `IInteractiveSession`, `IPortForward`)
- [ ] 1.1.5 — Créer les EventArgs (`ConnectionStateChangedEventArgs`, etc.)
- [ ] 1.1.6 — Tests unitaires sur les records / validations

### 1.2 Harbor.Data — Persistance SQLite
- [ ] 1.2.1 — Ajouter les paquets `Microsoft.Data.Sqlite`, `Dapper`, `SQLitePCLRaw.bundle_e_sqlcipher`
- [ ] 1.2.2 — Créer `HarborDbContext` (ouverture/fermeture, pragma SQLCipher)
- [ ] 1.2.3 — Écrire le script SQL initial conforme à `harbor-architecture.md` §9.2
- [ ] 1.2.4 — Mettre en place un système de migrations (numérotées `0001_init.sql`, etc.)
- [ ] 1.2.5 — Créer `WorkspaceRepository`, `ProfileRepository`, `SshKeyRepository`, `TransferRepository`, `AuditLogRepository`, `SnippetRepository`
- [ ] 1.2.6 — Tests d'intégration SQLite (DB en mémoire + DB fichier)

### 1.3 Harbor.Security — Keystore et crypto
- [ ] 1.3.1 — Ajouter `Konscious.Security.Cryptography.Argon2`
- [ ] 1.3.2 — Implémenter `KeyDerivation` (Argon2id m=64MB, t=3, p=4, salt 16 bytes)
- [ ] 1.3.3 — Implémenter `SymmetricCrypto` (AES-256-GCM chiffrement/déchiffrement avec nonce aléatoire)
- [ ] 1.3.4 — Implémenter `Keystore` (unlock avec master password, stockage en RAM via `SecureString`, zeroisation au logout, timeout d'inactivité)
- [ ] 1.3.5 — Implémenter `AuditLogger`
- [ ] 1.3.6 — Tests unitaires exhaustifs sur la crypto (vecteurs de test, nonce unique, intégrité)

---

## Phase 2 — Provider SSH / SFTP

Objectif : une première implémentation concrète de `IRemoteFileSystem` et `IRemoteShell` via SSH.NET.

- [ ] 2.1 — Ajouter le paquet `SSH.NET`
- [ ] 2.2 — Implémenter `SshConnection` (connexion password + clé Ed25519/RSA, passphrase, keep-alive)
- [ ] 2.3 — Implémenter `SftpProvider` (`List`, `Stat`, `OpenRead`, `OpenWrite`, `Delete`, `CreateDirectory`, `Rename`, `SetPermissions`)
- [ ] 2.4 — Implémenter les `Capabilities` SFTP
- [ ] 2.5 — Implémenter `SshShell` (`ExecuteAsync` non-interactif, capture stdout/stderr/exit code)
- [ ] 2.6 — Implémenter `SshInteractiveSession` (ShellStream + resize)
- [ ] 2.7 — Implémenter `JumpHostConnection` (connexion via bastion unique)
- [ ] 2.8 — Tests d'intégration via Testcontainers (image `linuxserver/openssh-server`)
- [ ] 2.9 — Tests : upload, download, list, erreurs réseau, reconnexion

---

## Phase 3 — Services applicatifs

Objectif : services de haut niveau exposés à l'UI.

- [ ] 3.1 — `ConnectionManager` : ouverture/fermeture/suivi de sessions, pool de connexions, événements
- [ ] 3.2 — `TransferEngine` : queue persistante SQLite, scheduler, worker unique, retry avec backoff exponentiel, reprise sur crash
- [ ] 3.3 — `TransferEngine` : parallélisation configurable (N workers) avec limite par profil
- [ ] 3.4 — `TerminalService` : gestion multi-sessions, buffering, forwarding vers renderer
- [ ] 3.5 — `WorkspaceService` : CRUD workspaces, association profils
- [ ] 3.6 — Configurer le conteneur DI (`Microsoft.Extensions.DependencyInjection`)
- [ ] 3.7 — Configurer Serilog (console + fichier rotatif)
- [ ] 3.8 — Tests unitaires avec Moq sur chaque service

---

## Phase 4 — UI minimale (objectif V0.1)

Objectif : une première fenêtre Avalonia fonctionnelle.

- [ ] 4.1 — Créer `Harbor.App` avec `App.axaml` + `Program.cs` Avalonia
- [ ] 4.2 — Mettre en place MVVM (`CommunityToolkit.Mvvm`)
- [ ] 4.3 — Créer `MainWindow` avec layout général (sidebar + zone principale + bottom panel)
- [ ] 4.4 — Créer le thème sombre par défaut (inspiration Tokyo Night / Catppuccin Mocha)
- [ ] 4.5 — Implémenter `ConnectionPanelView` (liste profils, filtre, double-click = connect)
- [ ] 4.6 — Implémenter `ProfileEditView` (formulaire ajout/édition profil SSH)
- [ ] 4.7 — Implémenter `FileBrowserView` dual-pane (local + distant, navigation basique)
- [ ] 4.8 — Implémenter `TerminalView` MVP (mono-onglet, rendu VT basique, input clavier)
  - Sous-décision à prendre : lib tierce vs émulateur custom (voir Notes)
- [ ] 4.9 — Connecter les services via DI dans les ViewModels
- [ ] 4.10 — Test manuel bout-en-bout : créer profil → se connecter → ouvrir terminal → transférer un fichier

---

## Phase 5 — Packaging V0.1

Objectif : premier binaire distribuable.

- [ ] 5.1 — Configurer la publication self-contained (`dotnet publish`)
- [ ] 5.2 — Créer un installeur Windows non signé (Velopack ou Inno Setup)
- [ ] 5.3 — Créer un binaire portable Windows (zip)
- [ ] 5.4 — Créer le workflow `release.yml` (déclenché sur tag `v*`)
- [ ] 5.5 — Tag `v0.1.0-alpha` et release GitHub

---

## Phase 6 — V0.2 "Polished MVP"

- [ ] 6.1 — Provider FTP + FTPS (FluentFTP)
- [ ] 6.2 — Command palette (`Ctrl+Shift+P`, recherche fuzzy actions + profils)
- [ ] 6.3 — Terminal multi-onglets
- [ ] 6.4 — Drag & drop entre panneaux du file browser
- [ ] 6.5 — Queue de transferts : UI avec pause / resume / cancel / retry
- [ ] 6.6 — Import depuis `~/.ssh/config`
- [ ] 6.7 — Thème clair + bascule dark/light/system
- [ ] 6.8 — Raccourcis clavier configurables
- [ ] 6.9 — Notifications OS natives
- [ ] 6.10 — Release `v0.2.0`

---

## Phase 7 — V0.3 "Cloud + Ergonomics"

- [ ] 7.1 — Provider S3 (AWSSDK.S3) + S3-compatibles (MinIO, B2, Wasabi, R2, Scaleway, Hetzner)
- [ ] 7.2 — Concept de Workspaces (UI + persistance)
- [ ] 7.3 — Édition à distance : ouverture dans éditeur externe + watcher local + upload auto
- [ ] 7.4 — Diff visuel local ↔ distant avant upload
- [ ] 7.5 — Previews intégrées : images, PDF, Markdown (Markdig), JSON, CSV, hex
- [ ] 7.6 — Split panes terminal (horizontal + vertical)
- [ ] 7.7 — Recherche dans le scrollback (Ctrl+F)
- [ ] 7.8 — Release `v0.3.0`

---

## Phase 8 — V0.4 "Power Features"

- [ ] 8.1 — Port forwarding UI (local `-L`, remote `-R`, dynamic `-D`)
- [ ] 8.2 — Gestion des tunnels actifs (list, start/stop)
- [ ] 8.3 — Jump hosts / bastions en chaîne (multi-niveaux)
- [ ] 8.4 — Gestionnaire de clés SSH intégré (génération, import OpenSSH/PPK, export, audit d'âge, rotation)
- [ ] 8.5 — Support ssh-agent / pageant / Windows OpenSSH agent
- [ ] 8.6 — Intégration Bitwarden (via `bw` CLI)
- [ ] 8.7 — Intégration 1Password (via `op` CLI)
- [ ] 8.8 — Intégration KeePassXC (via socket natif)
- [ ] 8.9 — 2FA TOTP intégré
- [ ] 8.10 — Packaging macOS signé + notarisé (nécessite Apple Developer ID — action humaine)
- [ ] 8.11 — Packaging Linux : AppImage + .deb + .rpm
- [ ] 8.12 — Release `v0.4.0`

---

## Phase 9 — V0.5 "Extensibility"

- [ ] 9.1 — Plugin runtime Lua (MoonSharp)
- [ ] 9.2 — Sandbox plugins (permissions, limite CPU/RAM, isolation filesystem)
- [ ] 9.3 — API plugins exposée (`harbor.log`, `harbor.terminal`, `harbor.commands`, `harbor.events`)
- [ ] 9.4 — Chargement dynamique de plugins depuis `plugins/`
- [ ] 9.5 — CLI compagnon `Harbor.Cli` (`connect`, `upload`, `download`, `tunnel`, `exec`, `list`, `workspace open`)
- [ ] 9.6 — IPC entre app et CLI (named pipes Windows / Unix sockets)
- [ ] 9.7 — API locale HTTP (localhost, token d'auth, port random, découverte via `api.json`)
- [ ] 9.8 — Automatisations : scripts post-connexion, watchers de fichiers
- [ ] 9.9 — Documentation Docusaurus (Getting Started, User Guide, Plugin API, CLI Reference, FAQ)
- [ ] 9.10 — Site web officiel (landing + téléchargement détection OS)
- [ ] 9.11 — Release `v0.5.0`

---

## Phase 10 — V0.9 "Observability"

- [ ] 10.1 — Probe SSH périodique (uptime, free, df, nproc)
- [ ] 10.2 — Stockage des métriques historiques (SQLite, rétention 30 jours)
- [ ] 10.3 — Dashboard : grille/liste de serveurs avec statut et sparklines
- [ ] 10.4 — Moteur d'alertes (seuils configurables, notifications OS + webhook)
- [ ] 10.5 — Provider Docker (Docker.DotNet) — local et distant
- [ ] 10.6 — Provider Kubernetes (KubernetesClient) — exec, transfert fichiers dans pods
- [ ] 10.7 — Synchronisation optionnelle entre machines (GitHub Gist / Git / WebDAV / S3) avec chiffrement client
- [ ] 10.8 — Snippets de commandes avec variables
- [ ] 10.9 — Broadcast input (écrire dans N sessions simultanément)
- [ ] 10.10 — Plugin runtime JavaScript (Jint) — optionnel
- [ ] 10.11 — Tâches planifiées (cron-like, UI graphique)
- [ ] 10.12 — Webhooks sortants (Slack/Discord)
- [ ] 10.13 — Release `v0.9.0`

---

## Phase 11 — V1.0 "Ready for prime time"

- [ ] 11.1 — Polish UI complet (audit visuel, homogénéité, accessibilité)
- [ ] 11.2 — Onboarding guidé au premier démarrage
- [ ] 11.3 — Mode zen (plein écran minimaliste)
- [ ] 11.4 — Traductions i18n (EN + FR minimum)
- [ ] 11.5 — Support high DPI
- [ ] 11.6 — Support sixel / Kitty image protocol (inline images dans terminal)
- [ ] 11.7 — OSC 8 hyperlinks
- [ ] 11.8 — Ligatures + Nerd Fonts
- [ ] 11.9 — Enregistrement session format asciicast v2 + export HTML/SVG
- [ ] 11.10 — Corbeille locale pour fichiers supprimés à distance
- [ ] 11.11 — Recherche fichiers distants (via grep SSH)
- [ ] 11.12 — Signature Windows (Authenticode — action humaine, certificat requis)
- [ ] 11.13 — Signature + notarisation macOS (Apple Developer ID — action humaine)
- [ ] 11.14 — Auto-update (Velopack) avec vérification signature
- [ ] 11.15 — Publication winget + Chocolatey + Homebrew Cask + APT + AUR + Flathub
- [ ] 11.16 — SBOM publié avec la release
- [ ] 11.17 — Audit sécurité externe (action humaine recommandée)
- [ ] 11.18 — Corrections de tous les bugs critiques remontés en bêta
- [ ] 11.19 — Annonce publique : HN, Reddit (r/selfhosted, r/dotnet, r/sysadmin), Twitter/Mastodon/Bluesky
- [ ] 11.20 — Release `v1.0.0`

---

## Phase Post-V1 (backlog)

- [ ] Marketplace de plugins (UI intégrée)
- [ ] Harbor Team (sync multi-utilisateur chiffrée, SSO, audit entreprise)
- [ ] Version mobile read-only (.NET MAUI iOS/Android)
- [ ] Support Mosh
- [ ] Support Telnet (matériel réseau legacy)
- [ ] Support port série (USB serial)
- [ ] Support FIDO2/Yubikey SSH-hardware
- [ ] Synchronisation bidirectionnelle (modèle Unison) complète
- [ ] Intégration Google Drive / OneDrive / Dropbox

---

## Notes et Décisions

> Consignation des décisions techniques, changements d'architecture, arbitrages et points de vigilance.

### Points de vigilance identifiés au démarrage

- **Terminal émulateur (§11 architecture)** : la décision n'est pas tranchée entre `AvaloniaEdit.Terminal` (quand disponible), une lib tierce, ou un émulateur custom. À trancher avant la tâche 4.8. Fallback acceptable en MVP : VT100 basique sans ligatures, sans sixel.
- **SSH.NET** : vérifier la version courante pour la couverture des certificats OpenSSH et des algorithmes modernes avant la Phase 2.
- **AOT compilation** : risque d'incompatibilité avec la reflection utilisée par Avalonia / SSH.NET. À tester tardivement, ne pas bloquer les phases amont.
- **Stratégie de migrations SQLite** : à définir dès la tâche 1.2.4 (numérotation + table de versions).
- **Concurrence d'écriture SQLite** : prévoir un `SemaphoreSlim` ou un pattern single-writer dès la Phase 1.

### Actions nécessitant une intervention humaine du Chef de projet

- Achat d'un certificat Authenticode (signature Windows) — Phase 11.12
- Inscription Apple Developer Program + notarisation (signature macOS) — Phases 8.10 / 11.13
- Création du repo GitHub public + paramétrage
- Validation finale UX / visuelle à chaque release
- Tests sur infrastructure réelle (serveurs, buckets cloud)
- Audit sécurité externe avant V1.0

### Décisions prises

**2026-04-24 — Framework cible : `net10.0`**
- SDK disponible sur la machine : **.NET 10.0.201** (LTS publié nov. 2025).
- Le document d'architecture `harbor-architecture.md` mentionne `.NET 8`. Choix arbitré : **on bascule sur `net10.0`** car c'est le LTS actuel et le SDK est déjà installé.
- *Conséquence :* tous les `<TargetFramework>` des `.csproj` seront `net10.0`. `Directory.Build.props` (tâche 0.5) fixera cette valeur.
- *Action doc :* mettre à jour `harbor-architecture.md` §5.3 en fin de Phase 0 pour refléter ce changement.

**2026-04-24 — Format solution : `.sln` classique**
- `dotnet new sln` avec .NET 10 génère par défaut `.slnx` (XML). Choix arbitré : **régénérer en `.sln` classique** pour maximiser la compatibilité tooling (Harbor est open-source, cible des contributeurs aux environnements variés).
- Commande utilisée : `dotnet new sln -n Harbor -f sln`.

**2026-04-24 — Branche Git : `main`**
- Branche par défaut renommée de `master` vers `main` via `git branch -m main`, conformément à la convention moderne.

**2026-04-24 — Templates Avalonia installés**
- Pack NuGet `Avalonia.Templates` installé (fournit `avalonia.app`, `avalonia.mvvm`, `avalonia.xplat`, etc.).
- Template utilisé pour `Harbor.App` : `avalonia.app` (minimal, `Program.cs` + `App.axaml` + `MainWindow.axaml`). Views/ViewModels seront dans `Harbor.UI` pour respecter l'architecture en couches (§7.2 "Harbor.UI ne dépend jamais des Harbor.Protocols.*").

**2026-04-24 — Tous les projets sur `net10.0`**
- Les 18 projets (`Harbor.App`, `Harbor.UI`, `Harbor.Core`, `Harbor.Services`, `Harbor.Security`, `Harbor.Protocols.*` x8, `Harbor.Terminal`, `Harbor.Plugins`, `Harbor.Data`, `Harbor.Cli`, `Harbor.Ipc`) ciblent `net10.0`. Build initial validé (0 warning, 0 erreur, ~9 s).
- Le template `avalonia.app` a accepté le défaut SDK et produit directement un csproj `net10.0` — aucune édition manuelle nécessaire.

**2026-04-24 — Clé SSH dédiée au projet GitHub**
- Clé Ed25519 générée : `~/.ssh/harbor_github_ed25519` (privée) + `~/.ssh/harbor_github_ed25519.pub` (publique).
- Pas de passphrase (décision de simplicité ; peut être ajoutée plus tard via `ssh-keygen -p -f ~/.ssh/harbor_github_ed25519`).
- Fichier `~/.ssh/config` créé avec une entrée `Host github.com` qui force l'utilisation de cette clé (`IdentitiesOnly yes`) pour tous les `git@github.com:...`. N'interfère pas avec d'éventuelles autres clés SSH de l'utilisateur.
- Fingerprint : `SHA256:WxSjKoaFBJuVRSYYbJI/B+oKoCfQXks0lCubqHzEc90`.
- Repo distant cible : `git@github.com:TheFraTo/Harbor-Project.git`.

**2026-04-24 — Problème NTFS ACL sur `~/.ssh/known_hosts`**
- Le fichier `known_hosts` préexistant (créé 2024-12-03 par une autre application) avait des ACL NTFS qui empêchaient Git/OpenSSH de le lire (exécution : `cat known_hosts` → "Permission denied" malgré `0644` apparent en POSIX).
- Contournement : ajout de `UserKnownHostsFile ~/.ssh/known_hosts_harbor` dans `~/.ssh/config` pour le Host github.com, fichier pré-rempli via `ssh-keyscan -t rsa,ecdsa,ed25519 github.com`.
- Le fichier legacy n'est pas supprimé ; le projet utilise son propre known_hosts dédié.

**2026-04-24 — Divergence Avalonia : 12.0.1 vs 11.x du doc**
- Le template `avalonia.app` avec le SDK .NET 10 installe **Avalonia 12.0.1** (pas 11.x comme écrit dans `harbor-architecture.md` §5.3 et §24.1).
- Avalonia 12 est une version majeure plus récente, disponible début 2026. Décision implicite par adhérence au template (pas de raison de downgrade).
- *Action doc :* mettre à jour `harbor-architecture.md` §5.3 et §24.1 (Avalonia 11.x → 12.x) lors du polissage des docs.
- Packages installés par le template : `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, `AvaloniaUI.DiagnosticsSupport` — tous en 12.0.1 / 2.2.1.

**2026-04-24 — Phase 0 terminée : Roslynator, fichiers communautaires, CI**
- `Roslynator.Analyzers` + `Roslynator.Formatting.Analyzers` 4.12.10 ajoutés dans `Directory.Build.props` (scope `all` projects, `PrivateAssets=all`).
- Suppression des 16 `src/**/Class1.cs` scaffold (bruit inutile). Les `UnitTest1.cs` des 5 projets de test sont conservés pour maintenir la baseline "5 tests passants".
- `LICENSE` (MIT 2026), `CONTRIBUTING.md` (règles .NET strictes, Conventional Commits), `CODE_OF_CONDUCT.md` (Contributor Covenant v2.1 en français), `SECURITY.md` (GitHub private vulnerability reporting).
- Workflow CI `.github/workflows/build.yml` : matrix Windows/macOS/Linux, .NET 10, `dotnet format --verify` (Linux uniquement), `restore` + `build Release` + `test Release`, upload des résultats TRX. Concurrency group pour annuler les builds superposés.
- Build Release final : 0 warning, 0 erreur, 5/5 tests passent (Debug + Release).

**2026-04-24 — `.editorconfig` strict + GenerateDocumentationFile**
- `.editorconfig` complet créé : UTF-8 LF, indent 4 (2 pour XML/JSON/YAML), file-scoped namespaces obligatoires, naming conventions C# complètes (interfaces `I*`, type params `T*`, private fields `_camelCase`), severity overrides (CA1014 off, CA1062 suggestion, IDE0005 error).
- `dotnet format` a auto-corrigé le scaffold : BOM → UTF-8 sans BOM, CRLF → LF sur les UnitTest1.cs, ordre des usings, retrait des `using System;` inutiles avec ImplicitUsings.
- **Piège Roslyn :** `IDE0005` (using inutile) en `error` nécessite `GenerateDocumentationFile=true` au build-time (cf. dotnet/roslyn#41640). Ajouté dans `Directory.Build.props`. Les `.xml` générés tombent dans `bin/` (gitignoré). `CS1591` (missing XML doc) reste à `none` donc pas de bruit.

**2026-04-24 — `Directory.Build.props` activé en mode strict**
- Centralisation : `TargetFramework=net10.0`, `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`.
- Rigueur : `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `AnalysisLevel=latest-recommended`.
- Métadonnées : Company/Product/Authors/Copyright/NeutralLanguage=en-US.
- Déterminisme : `Deterministic=true`, `ContinuousIntegrationBuild` auto quand `$(CI)=true`.
- Cleanup : les 23 `.csproj` ont été dépouillés de `TargetFramework`, `ImplicitUsings`, `Nullable` (via `sed` sur toute l'arbo).
- *Première erreur strict attrapée :* `CA1852` sur `Harbor.App/Program.cs` (Program doit être `sealed`). Fix appliqué. Montre bien la valeur du mode strict dès le départ.
- Build + tests : 0 warning, 0 erreur, 5/5 tests passent.

**2026-04-24 — Premier commit et push GitHub**
- Remote `origin` = `git@github.com:TheFraTo/Harbor-Project.git`.
- Commit initial `21f422f` : 46 fichiers, 2529 insertions (docs + solution + 18 projets).
- Push `main -> main` réussi.
- *Warning observé :* core.autocrlf=true provoque des warnings "LF will be replaced by CRLF". À neutraliser via un `.gitattributes` propre lors de la tâche 0.5 (ou plus tôt en polish).
- *Détail :* les templates classlib ont laissé des fichiers `Class1.cs` dans chaque projet. Ils seront supprimés quand on écrira le vrai code métier (Phase 1). Pas un blocker.

---

*Document vivant — mis à jour à chaque fin de tâche.*
