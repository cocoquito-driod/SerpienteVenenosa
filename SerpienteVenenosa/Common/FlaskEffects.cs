using Terraria;
using Terraria.ID;

namespace SerpienteVenenosa.Common;

/// <summary>
/// Efectos de los frascos (Icor, Fuego Maldito, Veneno...) para los ataques de la serpiente transformada.
/// Terraria solo los aplica a armas cuerpo a cuerpo y látigos, así que acá se replican con los mismos
/// debuffs y duraciones que usa el juego (Projectile.StatusNPC).
/// </summary>
public static class FlaskEffects
{
	public static void Apply(Player player, NPC target) {
		switch (player.meleeEnchant) {
			case 1: // Frasco de Ponzoña
				target.AddBuff(BuffID.Venom, 60 * Main.rand.Next(5, 10));
				break;
			case 2: // Frasco de Fuego Maldito
				target.AddBuff(BuffID.CursedInferno, 60 * Main.rand.Next(3, 7));
				break;
			case 3: // Frasco de Fuego
				target.AddBuff(BuffID.OnFire, 60 * Main.rand.Next(3, 7));
				break;
			case 4: // Frasco de Oro
				target.AddBuff(BuffID.Midas, 120);
				break;
			case 5: // Frasco de Icor
				target.AddBuff(BuffID.Ichor, 60 * Main.rand.Next(10, 20));
				break;
			case 6: // Frasco de Nanobots
				target.AddBuff(BuffID.Confused, 60 * Main.rand.Next(1, 4));
				break;
			case 7: // Frasco de Fiesta: confeti
				Projectile.NewProjectile(player.GetSource_FromThis(), target.Center, target.velocity, ProjectileID.ConfettiMelee, 0, 0f, player.whoAmI);
				break;
			case 8: // Frasco de Veneno
				target.AddBuff(BuffID.Poisoned, 60 * Main.rand.Next(5, 10));
				break;
		}
	}
}
