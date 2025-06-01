This is a C# based repository of Roslyn analyzers for the Moq library. It is primarily responsible for identifying issues with the usage of Moq that are permitted through compilation, but fail at runtime. 

Please follow these guidelines when contributing:

## Code Standards

### Required Before Each Commit
- Run `dotnet format whitespace`, `dotnet format style`, and `dotnet format analyzers` before committing any changes to ensure proper code formatting
- This will run format on all .NET files to maintain consistent style

### Development Flow
- Tool restore: `dotnet tool restore`
- Build: `dotnet build`
- Test: `dotnet test`

## Repository Structure
- `.config/`: Configuration files for .NET
- `.github/`: Logic related to interactions with GitHub
- `artifacts/`: Build outputs; only appears after running `dotnet build`
- `build/`: Files related to build, including scripts and MSBuild Targets and Properties shared between all project files
- `docs/`: Documentation
- `src/`: Source files of the analyzers, code fixes, and tools
- `tests/`: Test fixtures and benchmarks

## Key Guidelines
1. Follow .NET best practices and idiomatic patterns
2. Maintain existing code structure and organization
3. Use dependency injection patterns where appropriate
4. Write unit tests for new functionality
5. Document public APIs and complex logic
6. Suggest changes to the `docs/` folder when appropriate
