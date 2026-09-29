using Terraria;

namespace SerpienteVenenosa.Content.NPCs;

/// <summary>
/// El jefe devuelve los disparos de penetración infinita, como las estrellas del Cañón de Estrellas y del
/// Súper Disparaestrellas, que si no atravesarían todos los segmentos del cuerpo de una.
/// </summary>
internal static class SerpentReflection
{
	// CanBeReflected() es el filtro de Terraria para disparos "reflejables" (balas, flechas, estrellas...):
	// deja afuera yoyós, lanzas, minions, látigos, etc.
	public static bool ShouldReflect(Projectile projectile) =>
		projectile.penetrate == -1 && projectile.CanBeReflected();

	/// <summary>Para usar desde CanBeHitByProjectile de cada segmento.</summary>
	public static bool? CanBeHitByProjectile(NPC npc, Projectile projectile) {
		if (!ShouldReflect(projectile))
			return null;

		// Este hook se llama antes de comprobar si el proyectil toca al NPC, así que el choque lo miramos acá.
		// ReflectProjectile es el de Terraria: lo vuelve hostil, lo manda hacia quien lo disparó y le baja el daño.
		if (npc.CanReflectProjectile(projectile)) {
			npc.ReflectProjectile(projectile);
			projectile.netUpdate = true;
		}
		return false;
	}
}
