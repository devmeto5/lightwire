$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
New-Item -ItemType Directory -Force dist | Out-Null
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /win32manifest:src\app.manifest /out:dist\LightWire.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Security.dll /r:System.ServiceProcess.dll src\Config.cs src\Program.cs
if ($LASTEXITCODE -ne 0) { throw 'App compilation failed' }
& $compiler /nologo /target:exe /out:dist\ConfigTests.exe src\Config.cs tests\ConfigTests.cs
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& .\dist\ConfigTests.exe
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /win32manifest:src\app.manifest /out:dist\LightWire-Setup.exe /r:System.Windows.Forms.dll /r:Microsoft.CSharp.dll /resource:dist\LightWire.exe,LightWire.exe /resource:README.md,README.md src\Setup.cs
if ($LASTEXITCODE -ne 0) { throw 'Setup compilation failed' }
Get-FileHash dist\LightWire.exe,dist\LightWire-Setup.exe -Algorithm SHA256 | ForEach-Object { $_.Hash + '  ' + (Split-Path $_.Path -Leaf) } | Set-Content dist\SHA256SUMS.txt

