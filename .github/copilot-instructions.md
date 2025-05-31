This is a C# based repository of Roslyn analyzers for the Moq library. It is primarily responsible for identifying issues with the usage of Moq that are permitted through compilation, but fail at runtime. 

Please follow these guidelines when contributing:

## Performance

The analyzers can run on large code bases, so we need implementations to be fast and efficient. When reviewing or adding code, keep this goal in mind.

When looking for optimization opportunities, consider:
- Algorithm complexity and big O analysis 
- Expensive operations
- Unnecessary iterations or computations
- Repeated calculations of the same value 
- Inefficient data structures or data types
- Opportunities to cache or memoize results
- Parallelization with threads/async 
- More efficient built-in functions or libraries
- Query or code paths that can be short-circuited
- Reducing memory allocations and copying
- Compiler or interpreter optimizations to leverage

Add benchmarks if possible to quantify the performance improvements. Document any new usage constraints (e.g. increased memory requirements).

Try to prioritize the changes that will have the largest impact on typical usage scenarios based on your understanding of the codebase.

## Code Standards

### Required Before Each Commit
- Run `dotnet format whitespace`, `dotnet format style`, and `dotnet format analyzers` before committing any changes to ensure proper code formatting
- This will run format on all .NET files to maintain consistent style

### Development Flow
- Lint: `dotnet format whitespace`, `dotnet format style`, and `dotnet format analyzers` to ensure any changes meet code formatting standards
- Build: `dotnet build /p:PedanticMode=true /p:SquiggleCop_AutoBaseline=true` to ensure everything passes and analyzer warnings and suppressions are kept up to date
- Test: `dotnet test` to ensure all tests pass

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
