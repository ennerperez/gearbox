# Stack

## Runtime

- .NET 10 (`net10.0`)
- Avalonia desktop UI
- Windows, macOS, and Linux native browser registration backends
- LiteDB persistence
- Microsoft Extensions configuration, hosting, logging, and dependency injection

## Build and Tooling

- NUKE build automation
- Visual Studio solution file format: `Gearbox.slnx`
- Central Package Management via `Directory.Packages.props`
- GitVersion for semantic version calculation
- SonarAnalyzer.CSharp for static analysis
- dotnet-coverage and ReportGenerator for coverage output

## Testing

- xUnit v3 unit tests
- Shouldly assertions
- NSubstitute test doubles
- AutoFixture test data
- coverlet collector for test coverage

## Project Layout

- `src/Core`: domain and platform services
- `src/Host`: background host process
- `src/Runner`: command forwarding entry point
- `src/Shell`: Avalonia shell application
- `tests/UnitTest`: unit test suite
