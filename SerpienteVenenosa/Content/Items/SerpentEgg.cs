using SerpienteVenenosa.Content.NPCs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Content.Items;

/// <summary>Invoca a la Serpiente Emplumada.</summary>
public class SerpentEgg : ModItem
{
	public override void SetStaticDefaults() {
		Item.ResearchUnlockCount = 3;
		ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
	}

	public override void SetDefaults() {
		Item.width = 24;
		Item.height = 30;
		Item.maxStack = Item.CommonMaxStack;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.consumable = true;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.buyPrice(silver: 50);
	}

	public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<SerpentHead>());

	public override bool? UseItem(Player player) {
		if (player.whoAmI == Main.myPlayer) {
			SoundEngine.PlaySound(SoundID.Roar, player.position);

			int type = ModContent.NPCType<SerpentHead>();
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NPC.SpawnOnPlayer(player.whoAmI, type);
			else
				NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
		}
		return true;
	}

	public override void AddRecipes() {
		CreateRecipe()
			.AddIngredient(ItemID.MudBlock, 20)
			.AddIngredient(ItemID.JungleSpores, 5)
			.AddTile(TileID.WorkBenches)
			.Register();
	}
}
