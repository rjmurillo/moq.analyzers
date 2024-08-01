# Contributing

We welcome contributions. If you want to contribute to existing issues, check the
[help wanted](https://github.com/rjmurillo/moq.analyzers/labels/help%20wanted) or
[good first issue](https://github.com/rjmurillo/moq.analyzers/labels/good%20first%20issue) items in the backlog.

If you have new ideas or want to complain about bugs, feel free to [create a new issue](https://github.com/rjmurillo/moq.analyzers/issues/new).

## Updating SquiggleCop baselines

To update SquiggleCop baselines, run this command and review and commit the results:

```powershell
dotnet clean && dotnet build /p:PedanticMode=true /p:SquiggleCop_AutoBaseline=true
```

`$(PedanticMode)` turns on the CI configuration (e.g. `TreatWarningsAsErrors`) and `$(SquiggleCop_AutoBaseline)`
automatically accepts the new baseline.
