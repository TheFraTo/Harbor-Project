# Contributing to Harbor

Merci pour votre intérêt à contribuer à **Harbor** — le centre de commande unifié, local-first et open-source pour développeurs et sysadmins.

## Avant de commencer

- Lisez le [document d'architecture](harbor-architecture.md) pour comprendre la vision, les principes (local-first, privacy by design, keyboard-first) et les choix techniques.
- Consultez la [feuille de route](harbor-roadmap.md) pour voir où nous en sommes et ce qui est en cours.
- Respectez le [Code of Conduct](CODE_OF_CONDUCT.md).

## Installation du poste de développement

**Pré-requis :**
- .NET 10 SDK (10.0.2xx ou supérieur)
- Git
- Un IDE compatible : Visual Studio 2022 17.10+, JetBrains Rider 2024.3+, ou VS Code avec C# Dev Kit

**Cloner et builder :**

```bash
git clone https://github.com/TheFraTo/Harbor-Project.git
cd Harbor-Project
dotnet restore
dotnet build
dotnet test
```

Le build doit passer sans aucun warning ni erreur (mode strict activé).

## Règles de code

- **Langage :** C# moderne, `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`.
- **Style :** défini par `.editorconfig` à la racine. Lancez `dotnet format` avant de committer.
- **Analyzers :** `Microsoft.CodeAnalysis.NetAnalyzers` (`AnalysisLevel=latest-recommended`) + Roslynator, avec `TreatWarningsAsErrors=true`. Un build qui produit un warning ne passe pas.
- **Naming :** interfaces préfixées `I`, type parameters préfixés `T`, champs privés en `_camelCase`, types et membres publics en `PascalCase`.
- **Tests :** tout bug fix doit venir avec un test de non-régression. Les nouvelles features ont un coverage xUnit raisonnable (services = tests unitaires, providers = tests d'intégration avec Testcontainers).

## Processus de contribution

1. **Ouvrez une issue** avant d'attaquer un changement non-trivial, pour discuter de l'approche.
2. **Forkez** le repo et créez une branche `feat/ma-feature`, `fix/mon-bug`, ou `docs/ma-doc`.
3. **Commitez** avec des messages clairs. Privilégiez [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, `chore:`, etc.).
4. **Poussez** et ouvrez une Pull Request vers `main`.
5. La CI doit passer. Si elle échoue, regardez les logs avant de demander un review.
6. Un mainteneur relit, commente, merge.

## Règles anti-scope-creep

- Une PR = un sujet. Pas de mega-PRs mélangeant refactor + feature + docs.
- Pas de changement d'architecture sans RFC discutée au préalable en issue.
- Pas d'ajout de dépendance sans justification explicite (poids du binaire, licence, mainteneurs actifs).

## Licence

En contribuant, vous acceptez que votre contribution soit publiée sous la [licence MIT](LICENSE).
