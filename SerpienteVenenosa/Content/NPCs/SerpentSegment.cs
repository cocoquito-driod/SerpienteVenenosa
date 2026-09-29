using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerpienteVenenosa.Common;
using SerpienteVenenosa.Content.Projectiles;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Content.NPCs;

/// <summary>
/// Segmento del jefe (cuerpo o cola). ai[1] = índice del NPC de adelante, ai[2] = posición en el cuerpo.
/// Comparte vida con la cabeza (realLife) y desaparece si la cabeza ya no está.
/// </summary>
public abstract class SerpentSegment : ModNPC
{
	protected abstract Vector2 Pivot { get; }

	protected virtual Rectangle? Frame => null;

	protected int SegmentIndex => (int)NPC.ai[2];

	public override void SetStaticDefaults() {
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
		NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
	}

	public override void SetDefaults() {
		NPC.width = 40;
		NPC.height = 40;
		NPC.damage = 24;
		NPC.defense = 14;
		NPC.lifeMax = 5400;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 0f;
		NPC.noGravity = true;
		NPC.noTileCollide = true;
		NPC.behindTiles = true;
		NPC.dontCountMe = true;
		NPC.aiStyle = -1;
		NPC.netAlways = true;
	}

	// Nunca desaparece por su cuenta: lo maneja la cabeza.
	public override bool CheckActive() => false;

	public override bool? CanBeHitByProjectile(Projectile projectile) =>
		SerpentReflection.CanBeHitByProjectile(NPC, projectile);

	// La vida es la de la cabeza (realLife): la barra la dibuja solo la cabeza.
	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

	public override void AI() {
		NPC ahead = Main.npc[(int)NPC.ai[1]];
		NPC head = NPC.realLife >= 0 ? Main.npc[NPC.realLife] : null;
		if (head is null || !head.active || head.type != ModContent.NPCType<SerpentHead>() || !ahead.active) {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				NPC.life = 0;
				NPC.active = false;
				NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
			}
			return;
		}

		Vector2 aheadPosition;
		float aheadAngle;
		float spacing;
		if (ahead.whoAmI == head.whoAmI) {
			SerpentGeometry.GetBodyAnchor(head.Center, head.rotation, head.spriteDirection, out aheadPosition, out aheadAngle);
			spacing = SerpentGeometry.BodyAnchorGap;
		}
		else {
			aheadPosition = ahead.Center;
			aheadAngle = SerpentGeometry.AngleFromSpriteRotation(ahead.rotation);
			spacing = SerpentGeometry.SegmentSpacing;
		}

		Vector2 center = NPC.Center;
		float angle = SerpentGeometry.FollowSegment(ref center, aheadPosition, aheadAngle, spacing);
		NPC.Center = center;
		NPC.rotation = SerpentGeometry.SpriteRotation(angle);
		NPC.velocity = Vector2.Zero;
		NPC.target = head.target;

		SegmentAI(head);
	}

	protected virtual void SegmentAI(NPC head) {
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
		Texture2D texture = TextureAssets.Npc[Type].Value;
		spriteBatch.Draw(texture, NPC.Center - screenPos, Frame, NPC.GetAlpha(drawColor), NPC.rotation, Pivot, NPC.scale, SpriteEffects.None, 0f);
		return false;
	}
}

public class SerpentBody : SerpentSegment
{
	protected override Vector2 Pivot => SerpentGeometry.BodyPivot;

	protected override Rectangle? Frame => SerpentGeometry.BodyFrame(SegmentIndex);

	private ref float ShootTimer => ref NPC.localAI[0];

	public override void SetStaticDefaults() {
		base.SetStaticDefaults();
		Main.npcFrameCount[Type] = 2;
	}

	protected override void SegmentAI(NPC head) {
		// Segunda fase: uno de cada ocho segmentos del cuerpo también escupe veneno.
		if (Main.netMode == NetmodeID.MultiplayerClient || head.life >= head.lifeMax / 2 || SegmentIndex % 8 != 2)
			return;

		if (ShootTimer <= 0f)
			ShootTimer = Main.rand.Next(60, 240);
		if (--ShootTimer > 0f)
			return;

		ShootTimer = Main.rand.Next(200, 320);
		Player player = Main.player[head.target];
		if (!player.active || player.dead || NPC.Distance(player.Center) > 900f)
			return;

		Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, VenomBehavior.AimVelocity(NPC.Center, player.Center, 6f),
			ModContent.ProjectileType<VenomSpitHostile>(), 9, 0f, Main.myPlayer);
	}
}

public class SerpentTail : SerpentSegment
{
	protected override Vector2 Pivot => SerpentGeometry.TailPivot;

	public override void SetDefaults() {
		base.SetDefaults();
		NPC.width = 36;
		NPC.height = 36;
		NPC.damage = 18;
		NPC.defense = 8;
	}
}
