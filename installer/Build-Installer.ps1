$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$sdk=Join-Path $root '.tools\dotnet\dotnet.exe'
if(-not(Test-Path -LiteralPath $sdk)) { $sdk=(Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_HOME=Join-Path $root '.tools\cli'
$app=Join-Path $root 'release\app'
$package=Join-Path $root 'release\Hako Mark Installer'
New-Item -ItemType Directory -Force -Path $app,$package | Out-Null
& $sdk publish (Join-Path $root 'src\HakoMark.App\HakoMark.App.csproj') -c Release -r win-x64 --self-contained true -o $app -m:1
if($LASTEXITCODE -ne 0){ throw 'Compilation échouée. Aucun paquet livré.' }
Copy-Item -LiteralPath (Join-Path $root 'assets\HakoMark.ico') -Destination (Join-Path $app 'HakoMark.ico') -Force
$zip=Join-Path $root 'release\payload.zip'
Compress-Archive -Path (Join-Path $app '*') -DestinationPath $zip -Force
$csc='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$setup=Join-Path $package 'Installer Hako Mark.exe'
& $csc /nologo /target:winexe /optimize+ "/out:$setup" "/win32icon:$app\HakoMark.ico" "/resource:$zip,payload.zip" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:Microsoft.CSharp.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $PSScriptRoot 'Setup.cs')
if($LASTEXITCODE -ne 0){ throw 'Construction du paquet echouee.' }
$uninstaller=Join-Path $package 'Desinstaller Hako Mark.exe'
& $csc /nologo /target:winexe /optimize+ "/out:$uninstaller" "/win32icon:$app\HakoMark.ico" /reference:System.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'UninstallLauncher.cs')
if($LASTEXITCODE -ne 0){ throw 'Construction du bouton de desinstallation echouee.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Aide-Hako-Mark.txt') -Destination (Join-Path $package 'Aide Hako Mark.txt') -Force
Copy-Item -LiteralPath (Join-Path $root 'LICENSE.md') -Destination (Join-Path $package 'Conditions utilisation Hako Mark.md') -Force
Write-Output $setup
