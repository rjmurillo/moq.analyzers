# Open Work Roadmap

44 open issues as of 2026-03-14. Organized by priority and effort.

## Large Efforts (Epics)

### Sequence Patterns Analyzer (3 epics, unstarted)

- #615: Epic 1 - InSequence/MockSequence Coordination Validation
- #616: Epic 2 - Incomplete Sequence Configuration Detection
- #617: Epic 3 - Mixed Returns/Throws Sequence Validation
- #614: Gap analysis for all sequence patterns
- Biggest unstarted feature area in the project.

### Returns() Delegate Mismatch on Async (#777)

- Task breakdown exists
- Detect Returns() with non-task delegate on async methods
- Related to Moq1208

## Medium Efforts

### CRAP Score Reduction (5 issues)

- #627: Explainer of CRAP methodology
- #628: Reduce CRAP in Callback/Returns Analysis
- #629: Reduce CRAP in Setup/Verify Extensions
- #630: Reduce CRAP in Method Overloads/Invocation Analysis
- #631: Reduce CRAP in Diagnostic/Edit Properties

### PerfDiff Tech Debt (5 issues)

- #609: Resource management improvements
- #610: Reduce complexity
- #611: Add documentation
- #612: Enable static analysis
- #613: General tech debt

### Protected Member Setup Validation (#579)

- New analyzer for protected member setup patterns
- Unstarted

## Small/Documentation Items

### Documentation Gaps

- #944: AnalyzerReleases.Unshipped.md categories all say 'Usage', contradicting README
- #885: Contributor Guide for Roslyn Analyzer Development
- #774: Investigate Verify.SourceGenerators for test infrastructure (blocked)

### Testing Improvements

- #904: Adopt Stryker.NET mutation testing
- #1067: Improve invocation chain analysis for wrapped setup configurations

## Activity Patterns

- July 2025: 81 issues created from systematic audit (bulk creation)
- Feb-Mar 2026: 45 issues in current active development cycle
- Most issues carry the "triage" label, indicating backlog grooming needed
