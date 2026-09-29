# Regenera los sprites del mod a partir de la cabeza dibujada en PixelLab.
# Uso (desde la raiz del repo):  powershell -ExecutionPolicy Bypass -File tools\generate-sprites.ps1
# Ademas de las texturas, deja en tools\preview\ unas imagenes de la serpiente armada para revisar la geometria.
$root = Split-Path $PSScriptRoot -Parent
$preview = Join-Path $PSScriptRoot "preview"
New-Item -ItemType Directory -Force $preview | Out-Null
Add-Type -Path (Join-Path $PSScriptRoot "SpriteGen.cs") -ReferencedAssemblies System.Drawing
[SpriteGen]::Run((Join-Path $root "pixellab-cabeza-de-Serpiente-Con-plumas-1790638107487.png"), (Join-Path $root "SerpienteVenenosa"), $preview)
