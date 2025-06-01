#!/bin/bash

# This script iterates through all .csproj files in the repository,
# cleans and builds each project with specific properties to set the SquiggleCop baseline.
find ./ -name "*.csproj" -type f | while read -r project; do
    dotnet clean "$project"
    dotnet build "$project" /p:PedanticMode=true /p:SquiggleCop_AutoBaseline=true
done
