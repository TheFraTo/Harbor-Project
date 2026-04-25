# Harbor

> **The modern, open-source, privacy-first command center for developers and sysadmins.**
Project paused Project paused Project paused Project paused Project paused Project paused Project paused Project paused
[![Build](https://github.com/TheFraTo/Harbor-Project/actions/workflows/build.yml/badge.svg)](https://github.com/TheFraTo/Harbor-Project/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Status: pre-alpha](https://img.shields.io/badge/status-pre--alpha-orange)]()
[![.NET](https://img.shields.io/badge/.NET-10-512BD4)]()

🇫🇷 [Version française](README.fr.md)

---

Harbor unifies SSH, SFTP, FTP/FTPS, cloud object storage (S3 and S3-compatibles, Azure Blob, GCS), Docker, Kubernetes, an integrated terminal, SSH key management and a credential keystore — into **one cross-platform desktop application** (Windows, macOS, Linux), **free**, **open source** (MIT) and **local-first**.

Today, an operator who manages servers and storage juggles half a dozen tools: PuTTY, WinSCP, FileZilla, Cyberduck, Docker Desktop, a password manager, a file with notes "somewhere"… The existing all-in-ones are either **paid and closed** (Termius, Royal TS) or **dated and limited** (FileZilla, WinSCP). Harbor is the modern, free, privacy-respecting alternative.

## Why Harbor

| Principle | What it means in practice |
|---|---|
| **Local-first** | No telemetry. No data sent to the cloud unless you opt in. |
| **Privacy by design** | Secrets encrypted with AES-256-GCM, master key derived via Argon2id. |
| **Keyboard-first** | Every action reachable without the mouse. Command palette like VS Code. |
| **Cross-platform** | Windows, macOS and Linux as first-class citizens — same UX everywhere. |
| **Extensible** | Plugin API (Lua via MoonSharp) from day one of v1. |
| **Real open source** | MIT license, transparent governance, community-driven. |

## Status

Harbor is in **active early development** (pre-alpha). No public release yet.

What's already built (foundation layers):
- ✅ **Harbor.Core** — domain model: 6 enums, 13 records (11 connection variants, 9 auth variants), 6 abstraction interfaces, 3 event args.
- ✅ **Harbor.Data** — SQLite + SQLCipher encrypted persistence: `HarborDbContext`, embedded SQL migrations, 6 repositories with polymorphic JSON serialization.
- ✅ **Harbor.Security** — `KeyDerivation` (Argon2id m=64MiB), `SymmetricCrypto` (AES-256-GCM), `Keystore` with inactivity timeout, `AuditLogger`.
- ✅ **CI** — GitHub Actions matrix build (Windows / macOS / Linux), strict mode (warnings as errors), 90 passing tests.

What's coming next: SSH/SFTP provider, file browser UI, terminal emulator, packaging.

Track progress on the live [roadmap](harbor-roadmap.md).

## Planned features

A condensed view — see [`harbor-architecture.md`](harbor-architecture.md) §4 for the full list.

**Protocols**
- Shell: SSH, Mosh, Telnet, serial port
- File transfer: SFTP, SCP, FTP, FTPS, WebDAV, rsync
- Cloud storage: AWS S3 (and MinIO, Backblaze B2, Wasabi, R2, Scaleway, Hetzner), Azure Blob, Google Cloud Storage
- Containers: Docker, Docker Compose, Kubernetes, Podman

**Connection management**
- Named, tagged, foldered profiles with fuzzy search (`Ctrl+K`)
- Import from `~/.ssh/config`, PuTTY registry, WinSCP, FileZilla
- Workspaces grouping profiles by project / client
- Markdown notes per profile, searchable

**Terminal**
- xterm-256color emulator with true color, ligatures, OSC 8 hyperlinks, sixel images
- Multiple tabs, free split-panes, broadcast input across N sessions
- Search in scrollback, session recording (asciicast v2), HTML/SVG export

**File management**
- Dual-pane and quad-pane views, drag and drop between any pane
- Built-in preview: images, PDF, Markdown, JSON, CSV, audio, video, hex
- Remote editing via external editor with auto-upload on save
- Visual local ↔ remote diff before upload, optional bidirectional sync

**Security**
- Encrypted local keystore with master password
- SSH key management (Ed25519 / RSA / ECDSA), import OpenSSH and PuTTY PPK
- FIDO2 / Yubikey hardware keys, TOTP 2FA
- Integration with Bitwarden, 1Password, KeePassXC
- Local audit log of all sensitive actions

**Power features**
- Visual port forwarding (local, remote, dynamic SOCKS)
- Chained jump hosts / bastions
- Light monitoring dashboard (CPU, RAM, disk, uptime)
- Webhooks, post-connect scripts, file watchers

## Tech stack

- **Language** — C# 13 on .NET 10
- **UI** — Avalonia UI 12 with MVVM (CommunityToolkit.Mvvm)
- **SSH/SFTP** — SSH.NET (Renci.SshNet)
- **FTP/FTPS** — FluentFTP
- **S3 and compatibles** — AWSSDK.S3
- **Azure Blob / GCS** — Azure.Storage.Blobs, Google.Cloud.Storage.V1
- **Docker / Kubernetes** — Docker.DotNet, KubernetesClient
- **Database** — SQLite via Microsoft.Data.Sqlite, encrypted with SQLCipher (`bundle_e_sqlcipher`)
- **Cryptography** — Konscious.Security.Cryptography (Argon2id), `System.Security.Cryptography.AesGcm` (built-in)
- **Plugins** — MoonSharp (Lua), Jint (JavaScript, optional)
- **Testing** — xUnit, Testcontainers .NET

## Building from source

You'll need the **.NET 10 SDK** (10.0.2xx or newer) and Git. Any IDE will do: Visual Studio 2022 17.10+, JetBrains Rider 2024.3+, or VS Code with the C# Dev Kit.

```bash
git clone git@github.com:TheFraTo/Harbor-Project.git
cd Harbor-Project
dotnet restore
dotnet build
dotnet test
```

The build is configured to fail on **any** warning (strict mode). If you see one, that's a real issue worth fixing — please file a PR.

## Project layout

```
Harbor/
├── src/
│   ├── Harbor.App/                  # Avalonia entry point
│   ├── Harbor.UI/                   # Views, ViewModels, controls
│   ├── Harbor.Core/                 # Domain: enums, records, interfaces
│   ├── Harbor.Services/             # ConnectionManager, TransferEngine, …
│   ├── Harbor.Security/             # Keystore, crypto, audit
│   ├── Harbor.Protocols.Ssh/        # SSH + SFTP provider
│   ├── Harbor.Protocols.Ftp/        # FTP, FTPS
│   ├── Harbor.Protocols.S3/         # S3 and S3-compatibles
│   ├── Harbor.Protocols.Azure/      # Azure Blob
│   ├── Harbor.Protocols.Gcs/        # Google Cloud Storage
│   ├── Harbor.Protocols.WebDav/     # WebDAV
│   ├── Harbor.Protocols.Docker/     # Docker
│   ├── Harbor.Protocols.Kubernetes/ # Kubernetes
│   ├── Harbor.Terminal/             # PTY + VT parser + renderer
│   ├── Harbor.Plugins/              # Lua/JS plugin runtime
│   ├── Harbor.Data/                 # SQLite, SQLCipher, repositories, migrations
│   ├── Harbor.Cli/                  # `harbor` command-line companion
│   └── Harbor.Ipc/                  # Named pipes / Unix sockets IPC
├── tests/                           # xUnit unit + integration tests
├── docs/                            # User and developer documentation
└── .github/workflows/               # CI/CD
```

## Documentation

- [`harbor-architecture.md`](harbor-architecture.md) — full technical architecture document (24 sections)
- [`harbor-roadmap.md`](harbor-roadmap.md) — live tracker of what's done and what's next
- [`CONTRIBUTING.md`](CONTRIBUTING.md) — how to contribute
- [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md) — our code of conduct
- [`SECURITY.md`](SECURITY.md) — security policy and how to report vulnerabilities

## Contributing

Contributions are welcome. Please read [`CONTRIBUTING.md`](CONTRIBUTING.md) before opening a PR. By contributing, you agree to abide by the [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md).

For non-trivial changes, open a discussion or an issue first so we can agree on the approach.

## Security

If you find a security vulnerability, **please do not open a public issue**. Use GitHub's [private vulnerability reporting](https://github.com/TheFraTo/Harbor-Project/security) instead. See [`SECURITY.md`](SECURITY.md) for the full policy.

## License

Harbor is released under the [MIT License](LICENSE). You can use, modify and redistribute it freely, including for commercial purposes, as long as the original copyright notice is preserved.

## Acknowledgments

Harbor stands on the shoulders of giants:
- The [Avalonia](https://avaloniaui.net/) team for an excellent cross-platform UI framework
- The [SSH.NET](https://github.com/sshnet/SSH.NET), [FluentFTP](https://github.com/robinrodricks/FluentFTP) and AWS SDK maintainers
- [SQLCipher](https://www.zetetic.net/sqlcipher/) for transparent SQLite encryption
- The [Roslyn](https://github.com/dotnet/roslyn) and [Roslynator](https://github.com/dotnet/roslynator) teams for keeping our code honest

---

*Built with care, in the open. Stars and feedback welcome.*
