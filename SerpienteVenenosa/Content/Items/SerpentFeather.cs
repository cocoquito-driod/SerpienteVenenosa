using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SerpienteVenenosa.Common.Players;
using SerpienteVenenosa.Common.Systems;
using SerpienteVenenosa.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Content.Items;

/// <summary>Te transforma en la serpiente emplumada (o te devuelve a la normalidad). La suelta el jefe.</summary>
public class SerpentFeather : ModItem
{
	private static uint nextSurfaceWarning;

	public override void SetDefaults() {
		Item.width = 32;
		Item.height = 32;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.UseSound = SoundID.Item8;
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.sellPrice(gold: 3);
	}

	public override bool CanUseItem(Player player) {
		SerpentPlayer serpent = player.GetModPlayer<SerpentPlayer>();
		if (serpent.IsSerpent && serpent.IsBuried) {
			if (player.whoAmI == Main.myPlayer && Main.GameUpdateCount >= nextSurfaceWarning) {
				CombatText.NewText(player.Hitbox, Color.LightGreen, Mod.GetLocalization("Common.MustSurface").Value);
				nextSurfaceWarning = Main.GameUpdateCount + 60;
			}
			return false;
		}
		return true;
	}

	public override bool? UseItem(Player player) {
		if (player.whoAmI != Main.myPlayer)
			return true;

		int buff = ModContent.BuffType<SerpentForm>();
		if (player.HasBuff(buff))
			player.ClearBuff(buff);
		else
			player.AddBuff(buff, 2);

		for (int i = 0; i < 30; i++) {
			Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.JungleSpore, Scale: 1.4f);
			dust.velocity *= 2.5f;
			dust.noGravity = true;
		}
		return true;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips) {
		tooltips.Add(new TooltipLine(Mod, "SpitKey", SerpentKeybinds.SpitKeyHint(Mod)));
	}
}
