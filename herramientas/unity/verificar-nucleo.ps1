param(
    [string]$Editor = "C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor"
)
$ErrorActionPreference = 'Stop'
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$datos = Join-Path $Editor 'Data'
$dotnet = Join-Path $datos 'NetCoreRuntime/dotnet.exe'
$sdk = Get-ChildItem (Join-Path $datos 'DotNetSdk/sdk') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$refs = Get-ChildItem (Join-Path $datos 'DotNetSdk/packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
if (!$sdk -or !$refs -or !(Test-Path $dotnet)) { throw 'No se encontró el SDK incluido con Unity. Indica -Editor con la ruta de tu editor.' }
$salida = Join-Path $raiz 'unity/Logs/PruebasNucleo'
New-Item $salida -ItemType Directory -Force | Out-Null
$dll = Join-Path $salida 'Pruebas.dll'
$argumentos = @('/nologo', '/target:exe', '/langversion:9.0', '/nostdlib+', ('/out:"' + $dll + '"'))
$refDir = Get-ChildItem (Join-Path $refs.FullName 'ref') -Directory | Select-Object -First 1
$argumentos += Get-ChildItem $refDir.FullName -Filter '*.dll' | ForEach-Object { '/reference:"' + $_.FullName + '"' }
$argumentos += Get-ChildItem (Join-Path $raiz 'unity/Assets/HuertaNueva/Scripts/Nucleo') -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' }
$argumentos += '"' + (Join-Path $raiz 'pruebas/unity/NucleoPruebas.cs') + '"'
$rsp = Join-Path $salida 'compilar.rsp'
[IO.File]::WriteAllLines($rsp, [string[]]$argumentos)
& $dotnet (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') ('@' + $rsp)
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de las pruebas C#.' }
$runtime = Get-ChildItem (Join-Path $datos 'NetCoreRuntime/shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
@{ runtimeOptions = @{ tfm = 'net8.0'; framework = @{ name = 'Microsoft.NETCore.App'; version = $runtime.Name } } } | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $salida 'Pruebas.runtimeconfig.json')
& $dotnet $dll (Join-Path $raiz 'data/contenido.json')
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas del núcleo C#.' }
