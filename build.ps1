param([switch]$Portable)
$ErrorActionPreference = 'Stop'
dotnet run --project "$PSScriptRoot/tests/SableKit.Tests" -c Release
if ($LASTEXITCODE -ne 0) { throw 'Core checks failed.' }
dotnet build "$PSScriptRoot/src/SableKit.Desktop" -c Release
if ($LASTEXITCODE -ne 0) { throw 'Desktop build failed.' }
if ($Portable) {
    dotnet publish "$PSScriptRoot/src/SableKit.Desktop" -c Release -r win-x64 --self-contained true -o "$PSScriptRoot/artifacts/SableKit-win-x64"
    if ($LASTEXITCODE -ne 0) { throw 'Portable publish failed.' }
}
