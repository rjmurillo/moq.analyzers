This is a C# based repository of Roslyn analyzers for the Moq library. It is primarily responsible for identifying issues with the usage of Moq that are permitted through compilation, but fail at runtime. 

Please follow these guidelines when contributing:

## Communication

Err on the side of over communicating. Explain the problem being solved, the solution, and any updates or considerations required for maintainers. In code, write annontations as necessary to inform future contributors or maintainers to the "why" something is the way it is. These comments are not necessary on every change, only on those that are not immediately obvious. 

### Comments from reviewers

PRs may be reviewed by one or more tools, maintainers, bots, and/or external processes. As changes occur on your PR, read all comments and requests for feedback. If you disagree with the change being requested, articulate why. If the maintainer insists on the change, perform the change.

## Design

- Use tests as leverage
- Keep in mind concepts such as SOLID, KISS, DRY, and YAGNI
- Start with *patterns* in the *problem*, then relate them in *context*
- Be intentional about changes

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
- Run `dotnet format whitespace`, `dotnet format style`, and `dotnet format analyzers` before committing any changes to ensure proper code formatting.
- The CI runs with warnings elevated to errors, so any issues will fail a build and your changes will be rejected by the maintainers.
- This will run format on all .NET files to maintain consistent style.

### Development Flow
- Lint: `dotnet format whitespace`, `dotnet format style`, and `dotnet format analyzers` to ensure any changes meet code formatting standards
- Build: `dotnet build /p:PedanticMode=true /p:SquiggleCop_AutoBaseline=true` to ensure everything passes and analyzer warnings and suppressions are kept up to date
- Test: `dotnet test --settings ./build/targets/tests/test.runsettings` to use the same settings as CI; ensure all tests pass

Run this Development Flow after making each and every change to avoid accumulating errors.

#### Troubleshooting Development Flow

If you encounter:

1. **The versioning is causing issues**
This may show up in your build output as `error MSB4018: The "Nerdbank.GitVersioning.Tasks.GetBuildVersion" task failed unexpectedly`. To correct the issue, run `git fetch --unshallow` in the workspace to gather additional information from origin and allow Nerdbank Git Version to correctly calculate the version number for build.

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
