using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerpienteVenenosa.Content.NPCs;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Common.Players;

/// <summary>Dibuja al jugador transformado usando las mismas texturas que el jefe.</summary>
public class SerpentDrawLayer : PlayerDrawLayer
{
	public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;

	public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) =>
		drawInfo.drawPlayer.GetModPlayer<SerpentPlayer>().IsSerpent;

	protected override void Draw(ref PlayerDrawSet drawInfo) {
		// Sin estelas de "sombra" (se dibujan en pasadas extra con shadow > 0).
		if (drawInfo.shadow != 0f)
			return;

		Player player = drawInfo.drawPlayer;
		SerpentPlayer serpent = player.GetModPlayer<SerpentPlayer>();
		Texture2D head = TextureAssets.Npc[ModContent.NPCType<SerpentHead>()].Value;
		Texture2D body = TextureAssets.Npc[ModContent.NPCType<SerpentBody>()].Value;
		Texture2D tail = TextureAssets.Npc[ModContent.NPCType<SerpentTail>()].Value;

		// De la cola hacia adelante, para que cada segmento tape al de atrás y la cabeza quede encima.
		int last = serpent.SegmentPositions.Length - 1;
		for (int i = last; i >= 0; i--) {
			Vector2 position = serpent.SegmentPositions[i];
			float rotation = SerpentGeometry.SpriteRotation(serpent.SegmentAngles[i]);
			Color light = Lighting.GetColor(position.ToTileCoordinates());
			if (i == last)
				Add(ref drawInfo, tail, position, null, light, rotation, SerpentGeometry.TailPivot, SpriteEffects.None);
			else
				Add(ref drawInfo, body, position, SerpentGeometry.BodyFrame(i), light, rotation, SerpentGeometry.BodyPivot, SpriteEffects.None);
		}

		Add(ref drawInfo, head, player.Center, null, Lighting.GetColor(player.Center.ToTileCoordinates()), serpent.HeadRotation,
			SerpentGeometry.HeadOrigin(head, serpent.Facing), SerpentGeometry.HeadEffects(serpent.Facing));
	}

	private static void Add(ref PlayerDrawSet drawInfo, Texture2D texture, Vector2 worldPosition, Rectangle? frame, Color color, float rotation, Vector2 origin, SpriteEffects effects) {
		Vector2 screenPosition = (worldPosition - Main.screenPosition).Floor();
		drawInfo.DrawDataCache.Add(new DrawData(texture, screenPosition, frame, color, rotation, origin, 1f, effects, 0f));
	}
}
