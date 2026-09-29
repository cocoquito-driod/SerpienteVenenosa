using SerpienteVenenosa.Common.GlobalNPCs;
using Terraria;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Content.Buffs;

/// <summary>
/// Veneno de la serpiente transformada. A diferencia del Envenenado de Terraria, afecta también a enemigos
/// inmunes al veneno normal (como los de la jungla) y al propio jefe. El efecto está en <see cref="SerpentVenomNPC"/>.
/// </summary>
public class SerpentVenom : ModBuff
{
	public override void SetStaticDefaults() {
		Main.debuff[Type] = true;
	}

	public override void Update(NPC npc, ref int buffIndex) {
		npc.GetGlobalNPC<SerpentVenomNPC>().Venomed = true;
	}
}
