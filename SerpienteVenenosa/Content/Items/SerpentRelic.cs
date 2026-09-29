using SerpienteVenenosa.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Content.Items;

/// <summary>
/// Accesorio que duplica el largo de la serpiente transformada. No se puede conseguir jugando
/// (no tiene receta ni lo suelta nadie): solo con mods de trucos o el modo Viaje.
/// </summary>
public class SerpentRelic : ModItem
{
	// Misma textura que el invocador del jefe.
	public override string Texture => "SerpienteVenenosa/Content/Items/SerpentEgg";

	public override void SetStaticDefaults() {
		Item.ResearchUnlockCount = 1;
	}

	public override void SetDefaults() {
		Item.width = 24;
		Item.height = 30;
		Item.accessory = true;
		Item.rare = ItemRarityID.LightPurple;
		Item.value = Item.sellPrice(gold: 10);
	}

	public override void UpdateAccessory(Player player, bool hideVisual) {
		player.GetModPlayer<SerpentPlayer>().LongBody = true;
	}
}
