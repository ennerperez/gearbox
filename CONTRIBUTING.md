# Contributing to Spark
We are excited that you are interested in contributing to Spark!
To ensure that the project remains easy to navigate and maintain, we ask that you follow these guidelines.

## Contribution Workflow
1. Fork the repository and create a new branch from `develop` for your changes.
2. Make your changes. Ensure that you adhere to the coding style of the project.
3. Submit a pull request with a clear description of your changes.

## Contribution Standards
To maintain a high quality of code and a clear history of changes, we adhere to the following standards:

### Conventional Commits
All commit messages must follow the [Conventional Commits specification](https://www.conventionalcommits.org/en/v1.0/). This convention provides a clear and descriptive history of changes, and it allows for the automation of versioning and changelog generation.
A commit message should be structured as follows:

```
<type>[optional scope]: <description>
[optional body]
[optional footer(s)]
```

Common types:

- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
- `refactor`: A code change that neither fixes a bug nor adds a feature
- `perf`: A code change that improves performance
- `test`: Adding missing tests or correcting existing tests
- `chore`: Changes to the build process or auxiliary tools and libraries such as documentation generation

### Semantic Versioning (SemVer)
This project follows [Semantic Versioning 2.0.0](https://semver.org/).
The version number is determined by the types of commits that are merged into the

- `main` branch: `fix` commits will result in a `PATCH` version bump (e.g.,`1.0.0` ->`1.0.1`).
- `feat` commits will result in a `MINOR` version bump (e.g., `1.0.1` -> `1.1.0`).

Commits with a
`BREAKING CHANGE:` footer will result in a
`MAJOR` version bump (e.g.,`1.1.0` ->`2.0.0`).

### Keep a Changelog
We automatically generate and maintain the`CHANGELOG.md`file based on the Conventional Commits standard.
Please ensure your commit messages are descriptive and accurate, as they will be directly reflected in the project's changelog.
