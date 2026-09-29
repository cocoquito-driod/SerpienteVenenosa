using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Common.GlobalNPCs;

/// <summary>Daño por segundo y efectos visuales del debuff Veneno de Serpiente.</summary>
public class SerpentVenomNPC : GlobalNPC
{
	private const int DamagePerSecond = 16;
	private const int HardmodeDamagePerSecond = 36;

	public bool Venomed;

	public override bool InstancePerEntity => true;

	public override void ResetEffects(NPC npc) {
		Venomed = false;
	}

	public override void UpdateLifeRegen(NPC npc, ref int damage) {
		if (!Venomed)
			return;

		int dps = Main.hardMode ? HardmodeDamagePerSecond : DamagePerSecond;
		if (npc.lifeRegen > 0)
			npc.lifeRegen = 0;
		// lifeRegen va en medios puntos de vida por segundo.
		npc.lifeRegen -= dps * 2;
		if (damage < dps / 4)
			damage = dps / 4;
	}

	public override void DrawEffects(NPC npc, ref Color drawColor) {
		if (!Venomed)
			return;

		// Tinte verde y burbujas, como cuando el jugador está envenenado.
		drawColor = new Color((byte)(drawColor.R * 0.55f), drawColor.G, (byte)(drawColor.B * 0.45f), drawColor.A);
		if (Main.rand.NextBool(4)) {
			Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Poisoned, Alpha: 120, Scale: 1.1f);
			dust.velocity = new Vector2(0f, -0.6f);
			dust.noGravity = true;
		}
	}
}
