# Contributing

Thanks for your interest in improving the FLFloppa Save System! We welcome bug reports, feature requests, documentation updates, and pull requests.

## Code of conduct

By participating you agree to abide by the [Code of Conduct](CODE_OF_CONDUCT.md). Please make sure you read it before contributing.

## Getting started

1. Fork the repository and create a branch from `main`.
2. Install the package into a Unity 2022.3+ project for local testing.
3. If you add new runtime or editor features, include or update documentation inside `Documentation~/` and provide sample coverage when appropriate.
4. Run the edit-mode test suite under `Packages/FLFloppa Save System/Tests/` before opening a pull request.

## Pull request checklist

- [ ] Target the `main` branch.
- [ ] Include a descriptive title and summary.
- [ ] Update CHANGELOG.md with user-facing changes.
- [ ] Add or update tests for bug fixes and new features.
- [ ] Update documentation and samples as necessary.

## Bug reports

Open an issue and include:

- Expected behavior
- Actual behavior
- Steps to reproduce
- Unity version and platform

Screenshots or save files demonstrating the issue are encouraged.

## Feature requests

Share the problem you are trying to solve and describe the desired workflow. If you already have an implementation plan, feel free to outline it in the issue to speed up the review process.

## Development conventions

- Follow the existing folder layout under `Runtime/`, `Editor/`, and `Documentation~/`.
- Prefer composition and interfaces over singletons to maintain modularity.
- Keep public APIs documented with XML comments.
- For UI Toolkit inspectors, store UXML/USS within the package and avoid referencing project-level assets.

## Releasing

This repository adheres to Semantic Versioning. When preparing a release:

1. Update `CHANGELOG.md`.
2. Bump the version in `package.json`.
3. Tag the release in Git as `vX.Y.Z`.
4. Publish release notes summarising major changes and migration steps.
