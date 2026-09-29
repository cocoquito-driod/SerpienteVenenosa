using SerpienteVenenosa.Common.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Content.Buffs;

/// <summary>Mientras el jugador tenga este buff, está transformado (ver SerpentPlayer).</summary>
public class SerpentForm : ModBuff
{
	public override void SetStaticDefaults() {
		Main.buffNoTimeDisplay[Type] = true;
		Main.buffNoSave[Type] = true;
		// Marcarlo como debuff evita que se quite con clic derecho: para volver hay que usar la pluma
		// (que no deja destransformarse bajo tierra).
		Main.debuff[Type] = true;
		BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex) {
		player.buffTime[buffIndex] = 18000;

		player.statDefense += 12;
		player.endurance += 0.1f;
		player.noFallDmg = true;
		player.gills = true;
		player.buffImmune[BuffID.Poisoned] = true;
		player.buffImmune[BuffID.Venom] = true;
		player.buffImmune[BuffID.Suffocation] = true;
	}

	public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare) {
		tip += "\n" + SerpentKeybinds.SpitKeyHint(Mod);
	}
}
