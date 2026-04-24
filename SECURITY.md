# Security Policy

## Versions supportées

Harbor est encore en phase de développement initial (pré-v1.0). Seule la branche `main` reçoit des correctifs de sécurité pour le moment. Après la sortie de la v1.0, cette politique sera mise à jour pour couvrir les branches de release maintenues.

| Version | Supportée |
|---------|-----------|
| `main` (pre-release)   | Oui |
| Releases v0.x alpha/beta | Au cas par cas, selon la sévérité |

## Signaler une vulnérabilité

**Ne divulguez pas publiquement de vulnérabilité de sécurité via une issue ou une PR publique.**

Utilisez à la place la fonctionnalité **"Private vulnerability reporting"** de GitHub :

1. Ouvrez la page [Security du dépôt](https://github.com/TheFraTo/Harbor-Project/security).
2. Cliquez sur **Report a vulnerability**.
3. Décrivez le problème avec autant de détails que possible :
   - Type de vulnérabilité (ex. fuite de secret, injection, escalation, etc.)
   - Composant ou fichier concerné (ex. `Harbor.Security`, `Harbor.Protocols.Ssh`)
   - Étapes de reproduction, proof of concept si disponible
   - Impact estimé (confidentialité, intégrité, disponibilité)
   - Version / commit concerné
   - Mitigation éventuelle proposée

## Ce à quoi vous attendre

- **Accusé de réception** sous 72 heures ouvrées.
- **Évaluation initiale** (sévérité, scope) sous 7 jours.
- **Plan de correction** communiqué sous 14 jours, proportionné à la gravité.
- **Divulgation coordonnée** : nous publierons un avis de sécurité après le correctif, en créditant le déclarant si souhaité.

## Périmètre

**Dans le périmètre :**
- Gestion des secrets (keystore, chiffrement, dérivation de clés)
- Authentification et intégration avec SSH, SFTP, FTP(S), S3, Azure, GCS, Docker, Kubernetes
- Système de plugins (échappement de sandbox, permissions, accès à l'API Harbor)
- Sérialisation / désérialisation de profils, imports depuis formats externes
- API locale HTTP et IPC (escalation, fuite de données)
- Auto-update : vérification de signature, MITM

**Hors périmètre :**
- Attaques nécessitant un accès root/admin préalable à la machine hôte (non défendable au niveau applicatif — cf. `harbor-architecture.md` §13.1)
- Issues des dépendances tierces : signalez-les upstream et ouvrez une issue ici pour le tracking de mise à jour
- Ingénierie sociale ou phishing visant les utilisateurs
- Attaques physiques sur la machine

## Remerciements

Un hall of fame des contributeurs sécurité sera tenu dans les notes de release une fois la v1.0 publiée.

Merci de nous aider à garder Harbor sûr pour ses utilisateurs.
