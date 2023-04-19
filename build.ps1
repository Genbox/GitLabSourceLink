#!/usr/bin/env powershell
$errorAction = "Ignore"

function exec($_cmd) {
    write-host " > $_cmd $args" -ForegroundColor cyan
    & $_cmd @args
    if ($LASTEXITCODE -ne 0) {
        throw 'Command failed'
    }
}

New-Item -ItemType "directory" -Path "nuget-packages" -Force -ErrorAction SilentlyContinue
Remove-Item "nuget-packages/*" -Recurse -ErrorAction $errorAction

Remove-Item ./src/GitLabSourceLink/bin/ -Recurse -ErrorAction $errorAction
Remove-Item ./src/GitLabSourceLink/obj/ -Recurse -ErrorAction $errorAction

Remove-Item ./src/GitLabSourceLink.ExampleLibrary/bin/ -Recurse -ErrorAction $errorAction
Remove-Item ./src/GitLabSourceLink.ExampleLibrary/obj/ -Recurse -ErrorAction $errorAction

Remove-Item ./src/GitLabSourceLink.ExampleConsumer/bin/ -Recurse -ErrorAction $errorAction
Remove-Item ./src/GitLabSourceLink.ExampleConsumer/obj/ -Recurse -ErrorAction $errorAction

exec dotnet restore -f ./src/GitLabSourceLink/
exec dotnet pack -c Debug ./src/GitLabSourceLink/

exec dotnet restore -f ./src/GitLabSourceLink.ExampleLibrary/
exec dotnet pack -c Debug ./src/GitLabSourceLink.ExampleLibrary/
exec dotnet build -c Debug ./src/GitLabSourceLink.ExampleConsumer/
