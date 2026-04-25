# Contribuer à Harbor

Merci pour votre intérêt à contribuer à **Harbor** — le centre de commande unifié, local-first et open-source pour développeurs et sysadmins.

🇬🇧 [English version](CONTRIBUTING.md)

## Avant de commencer

- Lisez le [document d'architecture](harbor-architecture.md) pour comprendre la vision, les principes directeurs (local-first, privacy by design, keyboard-first) et les choix techniques.
- Consultez la [feuille de route](harbor-roadmap.md) pour voir où nous en sommes et ce qui est en cours.
- Respectez le [Code de Conduite](CODE_OF_CONDUCT.fr.md).

## Environnement de développement

**Pré-requis**

- SDK .NET 10 (10.0.2xx ou plus récent)
- Git
- Un IDE compatible : Visual Studio 2022 17.10+, JetBrains Rider 2024.3+, ou VS Code avec le C# Dev Kit

**Cloner et builder**

```bash
git clone git@github.com:TheFraTo/Harbor-Project.git
cd Harbor-Project
dotnet restore
dotnet build
dotnet test
```

Le build doit passer avec **zéro warning et zéro erreur** (le mode strict est activé).

Pour les tests d'intégration qui touchent aux vrais protocoles (SFTP, FTP, mocks S3…), vous aurez aussi besoin de **Docker** localement — ces tests utilisent [Testcontainers](https://dotnet.testcontainers.org/) pour démarrer de vrais serveurs dans des conteneurs jetables.

## Style de code

- **Langage** : C# moderne avec `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`.
- **Formatage** : défini par `.editorconfig` à la racine. Lancez `dotnet format` avant de committer.
- **Analyzers** : `Microsoft.CodeAnalysis.NetAnalyzers` (`AnalysisLevel=latest-recommended`) plus Roslynator, avec `TreatWarningsAsErrors=true`. Un build qui produit un warning ne passe pas.
- **Naming** :
  - Les interfaces sont préfixées `I` (`IRemoteFileSystem`).
  - Les paramètres de type sont préfixés `T` (`TKey`).
  - Les champs privés sont en `_camelCase`.
  - Les types et membres publics sont en `PascalCase`.
  - Les variables locales et paramètres sont en `camelCase`.
- **Tests** : tout fix de bug doit s'accompagner d'un test de non-régression. Les nouvelles features arrivent avec une couverture xUnit raisonnable. Tests de services = unitaires avec mocks ; tests de providers = intégration avec Testcontainers.

## Comment contribuer

1. **Ouvrez d'abord une issue** pour tout changement non trivial afin de discuter l'approche avant d'écrire du code.
2. **Forkez** le repo et créez une branche `feat/ma-feature`, `fix/mon-bug` ou `docs/ma-doc`.
3. **Commitez** avec des messages clairs. On préfère les [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, `chore:`, etc.).
4. **Poussez** et ouvrez une Pull Request vers `main`.
5. La CI doit être verte. Si un check échoue, regardez les logs avant de demander une review.
6. Un mainteneur relit, commente, merge.

## Règles anti-scope-creep

- Une PR = un sujet. Pas de mega-PRs mélangeant refactor + feature + docs.
- Pas de changement d'architecture sans RFC discutée d'abord en issue.
- Pas de nouvelle dépendance sans justification explicite (impact taille binaire, licence, mainteneurs actifs).

## Licence

En contribuant, vous acceptez que votre contribution soit publiée sous [licence MIT](LICENSE).
