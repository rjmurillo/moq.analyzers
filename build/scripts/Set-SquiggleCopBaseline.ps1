Get-ChildItem *.csproj -recurse -File | ForEach-Object{ 
    dotnet clean $_.FullName && dotnet build $_.FullName /p:PedanticMode=true /p:SquiggleCop_AutoBaseline=true 
}