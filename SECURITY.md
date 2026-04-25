# Security Policy

🇫🇷 [Version française](SECURITY.fr.md)

## Supported versions

Harbor is in early development (pre-v1.0). At this stage, only the `main` branch receives security fixes. After the v1.0 release, this policy will be updated to cover maintained release branches.

| Version | Supported |
|---------|-----------|
| `main` (pre-release)   | Yes |
| v0.x alpha / beta releases | Case-by-case, depending on severity |

## Reporting a vulnerability

**Please do not disclose security issues publicly through issues or pull requests.**

Use GitHub's **private vulnerability reporting** feature instead:

1. Go to the repository [Security tab](https://github.com/TheFraTo/Harbor-Project/security).
2. Click **Report a vulnerability**.
3. Describe the issue with as much detail as possible:
   - Type of issue (for example: secret leak, injection, privilege escalation, integrity flaw)
   - Component or file affected (for example: `Harbor.Security`, `Harbor.Protocols.Ssh`)
   - Steps to reproduce, proof of concept if available
   - Estimated impact (confidentiality, integrity, availability)
   - Version or commit affected
   - Suggested mitigation if any

## What to expect

- **Acknowledgement** within 72 working hours.
- **Initial assessment** (severity, scope) within 7 days.
- **Remediation plan** communicated within 14 days, proportional to severity.
- **Coordinated disclosure**: we publish a security advisory after the fix lands, crediting the reporter if they wish.

## Scope

**In scope:**
- Secret management (keystore, encryption, key derivation)
- Authentication and integrations with SSH, SFTP, FTP/FTPS, S3, Azure, GCS, Docker, Kubernetes
- Plugin system (sandbox escapes, permission bypass, Harbor API misuse)
- Profile serialization and import from external formats
- Local HTTP API and IPC (privilege escalation, data leaks)
- Auto-update: signature verification, MITM resistance

**Out of scope:**
- Attacks requiring root or admin access on the host machine before exploitation (cf. `harbor-architecture.md` §13.1 — not defendable at the application layer)
- Issues in third-party dependencies: please report them upstream and open a tracking issue here for the version bump
- Social engineering or phishing targeting users
- Physical attacks on the user's machine

## Acknowledgements

A security hall of fame will be maintained in the release notes once v1.0 ships.

Thank you for helping keep Harbor safe for its users.
