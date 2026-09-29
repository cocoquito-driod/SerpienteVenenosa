# Serpiente Venenosa

Mod de [tModLoader](https://store.steampowered.com/app/1281930/tModLoader/) que agrega a la **Serpiente Emplumada**, un jefe gusano gigante que atraviesa la tierra y escupe veneno, y una transformación para convertirte en ella.

## Instalación

**Steam Workshop (recomendado):** en tModLoader, entrá a **Workshop → Download Mods**, buscá "Serpiente Venenosa" y suscribite. Las actualizaciones llegan solas.

**A mano:** bajá el `SerpienteVenenosa.tmod` de la última versión en [Releases](https://github.com/cocoquito-driod/SerpienteVenenosa/releases), copialo a `Documentos\My Games\Terraria\tModLoader\Mods` y activalo en **Workshop → Manage Mods**.

## Cómo se juega

- **Huevo de Serpiente** (20 bloques de barro + 5 esporas de la jungla, en una mesa de trabajo): invoca al jefe.
- **Pluma de la Serpiente** (la suelta el jefe): te transforma en la serpiente. Te movés con WASD o Espacio, atravesás bloques y escupís veneno con **F** (se puede cambiar en Ajustes → Controles). Usala de nuevo, fuera de la tierra, para volver a la normalidad.

## Desarrollo

- `SerpienteVenenosa/` es el mod. Para compilarlo, enlazá esa carpeta dentro de `Documentos\My Games\Terraria\tModLoader\ModSources` y usá **Develop Mods → Build + Reload**, o corré `dotnet build` dentro de la carpeta (hace falta el .NET 8 SDK).
- `tools/generate-sprites.ps1` regenera los sprites a partir de la cabeza dibujada en PixelLab (`pixellab-cabeza-...png`).
