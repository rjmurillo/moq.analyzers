# The CRAP Metric

CRAP stands for **Change Risk Anti-Patterns**. It is a single number that
combines how complex a method is with how well it is tested. A high CRAP score
marks code that is risky to change: complex enough to hide bugs, and not
covered enough for tests to catch them.

## How it is calculated

For a method `m`:

```text
CRAP(m) = comp(m)^2 * (1 - cov(m))^3 + comp(m)
```

- `comp(m)` is the cyclomatic complexity (the number of independent paths
  through the method).
- `cov(m)` is the code coverage of the method, from `0` (untested) to `1`
  (fully covered).

The formula rewards two things: lower complexity and higher coverage. Full
coverage (`cov = 1`) collapses the score to just `comp(m)`, so a well-tested
method can never be a hotspot no matter how complex. An untested method
(`cov = 0`) pays the full `comp^2 + comp` penalty.

Worked examples:

| Complexity | Coverage | CRAP score |
| ---------- | -------- | ---------- |
| 5          | 100%     | 5          |
| 5          | 0%       | 30         |
| 10         | 80%      | 10.8       |
| 10         | 0%       | 110        |

A common threshold is **30**: at or above it, a method is a risk hotspot.

## Where it shows up in this repository

Coverage reports are generated automatically by
[ReportGenerator](https://github.com/danielpalme/ReportGenerator) when you run
the test suite (see `build/targets/tests/Tests.targets`). The `HtmlInline`
report includes a **Risk Hotspots** section, and CRAP is one of the metrics it
ranks methods by.

Generate the report locally:

```shell
dotnet test --settings ./build/targets/tests/test.runsettings
```

Then open the HTML report under `artifacts/TestResults/` and look at the Risk
Hotspots table.

## Why it matters

- **Maintainability.** High-CRAP methods are hard to understand and modify, so
  every change costs more and carries more risk.
- **Reliability.** Complex, poorly tested code is where regressions hide.
- **Onboarding.** New contributors slow down when they hit tangled, untested
  methods.
- **Technical debt.** Lowering CRAP is a direct, measurable investment in code
  quality.

## How to reduce it

Because the score depends on both complexity and coverage, you have two levers:

1. **Add tests.** Cover the method with positive, negative, and boundary cases.
   Moving from `0%` to full coverage drops the score to the raw complexity
   value. This is usually the fastest win.
2. **Reduce complexity.** Extract intention-revealing helper methods, replace
   nested conditionals with early returns, and split a method that does several
   things into focused methods. Lower `comp(m)` shrinks the whole score.

Prefer doing both: cover the method first so a refactor is safe, then simplify
it. This matches the repository's bar of 100% block coverage with explicit
positive, negative, and boundary tests for all modified code.
