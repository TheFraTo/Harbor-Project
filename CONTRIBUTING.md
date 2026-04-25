# Contributing to Harbor

Thanks for your interest in contributing to **Harbor** — the unified, local-first, open-source command center for developers and sysadmins.

🇫🇷 [Version française](CONTRIBUTING.fr.md)

## Before you start

- Read the [architecture document](harbor-architecture.md) to understand the vision, the guiding principles (local-first, privacy by design, keyboard-first) and the technical choices.
- Check the [roadmap](harbor-roadmap.md) to see where we are and what is in flight.
- Respect the [Code of Conduct](CODE_OF_CONDUCT.md).

## Development environment

**Requirements**

- .NET 10 SDK (10.0.2xx or newer)
- Git
- A compatible IDE: Visual Studio 2022 17.10+, JetBrains Rider 2024.3+, or VS Code with the C# Dev Kit

**Clone and build**

```bash
git clone git@github.com:TheFraTo/Harbor-Project.git
cd Harbor-Project
dotnet restore
dotnet build
dotnet test
```

The build must pass with **zero warnings and zero errors** (strict mode is enabled).

For integration tests that involve real protocols (SFTP, FTP, S3 mocks…), you will also need **Docker** running locally — these tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up real servers in throwaway containers.

## Code style

- **Language**: modern C# with `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`.
- **Formatting**: defined by `.editorconfig` at the repo root. Run `dotnet format` before committing.
- **Analyzers**: `Microsoft.CodeAnalysis.NetAnalyzers` (`AnalysisLevel=latest-recommended`) plus Roslynator, with `TreatWarningsAsErrors=true`. A build that produces a warning will not pass.
- **Naming**:
  - Interfaces are prefixed `I` (`IRemoteFileSystem`).
  - Type parameters are prefixed `T` (`TKey`).
  - Private fields are `_camelCase`.
  - Types and public members are `PascalCase`.
  - Locals and parameters are `camelCase`.
- **Tests**: every bug fix should ship with a regression test. New features should come with reasonable xUnit coverage. Service tests are unit tests with mocks; provider tests are integration tests using Testcontainers.

## How to contribute

1. **Open an issue first** for any non-trivial change so the approach can be discussed before code is written.
2. **Fork** the repo and create a branch named `feat/my-feature`, `fix/my-bug` or `docs/my-doc`.
3. **Commit** with clear messages. We prefer [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, `chore:`, etc.).
4. **Push** and open a Pull Request targeting `main`.
5. CI must be green. If a check fails, look at the logs before requesting review.
6. A maintainer reviews, comments, and merges.

## Anti scope-creep rules

- One PR equals one topic. No mega-PRs mixing refactor + feature + docs.
- No architectural changes without an RFC discussed first in an issue.
- No new dependency without explicit justification (binary size impact, license, active maintainers).

## License

By contributing, you agree that your contribution is licensed under the [MIT License](LICENSE).
