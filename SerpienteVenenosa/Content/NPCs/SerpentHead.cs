using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerpienteVenenosa.Common;
using SerpienteVenenosa.Content.Items;
using SerpienteVenenosa.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Content.NPCs;

/// <summary>
/// Cabeza del jefe. Es la que piensa: alterna entre perseguirte un rato corto y quedarse apuntándote
/// (moviéndose lento) para escupir veneno. El cuerpo y la cola son NPCs aparte (<see cref="SerpentSegment"/>)
/// que comparten su vida mediante realLife.
/// </summary>
[AutoloadBossHead]
public class SerpentHead : ModNPC
{
	public const int BodyCount = 48;

	private const float PreferredAimDistance = 340f;
	// Distancia desde el centro de la cara hasta la barra de vida chica.
	private const float HealthBarHeight = 90f;
	private const float SpitSpeed = 9f;
	// Momentos (en ticks desde que empieza a apuntar) en los que escupe. 60 ticks = 1 segundo.
	private static readonly int[] SpitTicks = [55, 100];
	private static readonly int[] EnragedSpitTicks = [40, 70, 100];
	// Durante estos ticks antes de cada escupitajo la boca junta veneno, como aviso.
	private const int SpitWarningTicks = 25;

	private enum AIState
	{
		Chase,
		Aim,
	}

	private AIState State {
		get => (AIState)NPC.ai[0];
		set => NPC.ai[0] = (float)value;
	}

	private ref float Timer => ref NPC.ai[1];

	private bool SegmentsSpawned {
		get => NPC.localAI[0] == 1f;
		set => NPC.localAI[0] = value ? 1f : 0f;
	}

	private ref float DigSoundTimer => ref NPC.localAI[1];

	// Hacia dónde mira la boca. Gira de a poco, así la cabeza no da saltos al cambiar de estado.
	private ref float LookAngle => ref NPC.localAI[2];

	// Segunda fase: por debajo de la mitad de vida es más rápida y agresiva.
	private bool Enraged => NPC.life < NPC.lifeMax / 2;

	// Velocidad de persecución. En la segunda fase era 14; se bajó un 15%.
	private float ChaseSpeed => Enraged ? 14f * 0.85f : 11f;

	private int ChaseTime => Enraged ? 120 : 150;

	private int AimTime => Enraged ? 115 : 130;

	public override void SetStaticDefaults() {
		NPCID.Sets.MPAllowedEnemies[Type] = true;
		NPCID.Sets.BossBestiaryPriority.Add(Type);
		NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
		NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
	}

	public override void SetDefaults() {
		NPC.width = 64;
		NPC.height = 64;
		NPC.damage = 36;
		NPC.defense = 10;
		NPC.lifeMax = 5400;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 0f;
		NPC.noGravity = true;
		NPC.noTileCollide = true;
		NPC.behindTiles = true;
		NPC.boss = true;
		NPC.npcSlots = 10f;
		NPC.aiStyle = -1;
		NPC.netAlways = true;
		NPC.value = Item.buyPrice(gold: 6);
		if (!Main.dedServ)
			Music = MusicID.Boss1;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot) {
		npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SerpentFeather>()));
	}

	public override void BossLoot(ref int potionType) {
		potionType = ItemID.HealingPotion;
	}

	public override bool? CanBeHitByProjectile(Projectile projectile) =>
		SerpentReflection.CanBeHitByProjectile(NPC, projectile);

	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) {
		// Una sola barra chica para todo el jefe (los segmentos no dibujan la suya), siempre sobre la cabeza,
		// por encima de las plumas. La barra grande de jefe de abajo de la pantalla no cambia.
		scale = 1.5f;
		position = NPC.Center - new Vector2(0f, HealthBarHeight);
		return true;
	}

	public override void AI() {
		if (Main.netMode != NetmodeID.MultiplayerClient && !SegmentsSpawned) {
			SpawnSegments();
			SegmentsSpawned = true;
		}

		if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
			NPC.TargetClosest();
		Player player = Main.player[NPC.target];

		if (player.dead || !player.active || NPC.Distance(player.Center) > 6000f) {
			// Sin nadie a quien perseguir: se hunde y desaparece.
			NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.4f, 20f);
			NPC.EncourageDespawn(10);
		}
		else {
			switch (State) {
				case AIState.Chase:
					Chase(player);
					break;
				case AIState.Aim:
					Aim(player);
					break;
			}
		}

		UpdateHeadOrientation(player);
		DigEffects();
	}

	private void Chase(Player player) {
		float inertia = Enraged ? 18f : 22f;
		NPC.velocity = (NPC.velocity * (inertia - 1f) + NPC.DirectionTo(player.Center) * ChaseSpeed) / inertia;

		if (++Timer >= ChaseTime)
			SwitchState(AIState.Aim, player);
	}

	private void Aim(Player player) {
		// Se mueve lento: se acerca o se aleja para quedar a cierta distancia y se mece de costado.
		Vector2 toPlayer = NPC.DirectionTo(player.Center);
		float approach = MathHelper.Clamp((NPC.Distance(player.Center) - PreferredAimDistance) * 0.02f, -2.5f, 3f);
		Vector2 sway = toPlayer.RotatedBy(MathHelper.PiOver2) * MathF.Sin(Timer * 0.05f) * 1.5f;
		NPC.velocity = Vector2.Lerp(NPC.velocity, toPlayer * approach + sway, 0.06f);

		Timer++;
		foreach (int tick in Enraged ? EnragedSpitTicks : SpitTicks) {
			if (Timer > tick - SpitWarningTicks && Timer < tick)
				MouthDust();

			if (Timer == tick) {
				SoundEngine.PlaySound(SoundID.Item17, MouthPosition());
				if (Main.netMode != NetmodeID.MultiplayerClient)
					SpitVolley(player, Enraged ? 5 : 3);
			}
		}

		if (Timer >= AimTime)
			SwitchState(AIState.Chase, player);
	}

	private void SwitchState(AIState state, Player player) {
		State = state;
		Timer = 0f;
		NPC.netUpdate = true;

		if (state == AIState.Chase) {
			// La persecución arranca con un impulso hacia el jugador.
			NPC.velocity = NPC.DirectionTo(player.Center) * ChaseSpeed * 0.8f;
			SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
		}
	}

	private void UpdateHeadOrientation(Player player) {
		// Persiguiendo mira hacia donde va; apuntando, mira al jugador.
		bool aiming = State == AIState.Aim && player.active && !player.dead;
		Vector2 look = aiming ? player.Center - NPC.Center : NPC.velocity;
		if (look.LengthSquared() > 0.25f)
			LookAngle = LookAngle.AngleTowards(look.ToRotation(), 0.12f);

		Vector2 lookDirection = LookAngle.ToRotationVector2();
		NPC.spriteDirection = SerpentGeometry.UpdateFacing(NPC.spriteDirection, lookDirection, 0.2f);
		NPC.rotation = SerpentGeometry.HeadRotation(lookDirection, NPC.spriteDirection);
	}

	private Vector2 MouthPosition() =>
		SerpentGeometry.HeadPoint(NPC.Center, NPC.rotation, NPC.spriteDirection, SerpentGeometry.MouthOffset);

	private void MouthDust() {
		if (Main.dedServ || !Main.rand.NextBool(2))
			return;

		Dust dust = Dust.NewDustDirect(MouthPosition() - new Vector2(10f), 20, 20, DustID.GreenTorch, Alpha: 100, Scale: 1.6f);
		dust.noGravity = true;
		dust.velocity *= 1.5f;
	}

	private void SpitVolley(Player player, int count) {
		Vector2 mouth = MouthPosition();
		Vector2 velocity = VenomBehavior.AimVelocity(mouth, player.Center, SpitSpeed);
		float spread = MathHelper.ToRadians(14f);
		// El daño de proyectiles hostiles se multiplica según la dificultad (x2 normal, x4 experto...).
		int damage = 10;
		for (int i = 0; i < count; i++) {
			float offset = MathHelper.Lerp(-spread, spread, i / (float)(count - 1));
			Projectile.NewProjectile(NPC.GetSource_FromAI(), mouth, velocity.RotatedBy(offset),
				ModContent.ProjectileType<VenomSpitHostile>(), damage, 0f, Main.myPlayer);
		}
	}

	private void SpawnSegments() {
		int previous = NPC.whoAmI;
		for (int i = 0; i <= BodyCount; i++) {
			int type = i == BodyCount ? ModContent.NPCType<SerpentTail>() : ModContent.NPCType<SerpentBody>();
			// Start = previous: cada segmento queda en un índice mayor que el de adelante, así se actualiza
			// después de él en el mismo tick y lo sigue sin retraso.
			int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, type, previous, ai1: previous, ai2: i);
			if (index >= Main.maxNPCs)
				break;

			Main.npc[index].realLife = NPC.whoAmI;
			Main.npc[index].target = NPC.target;
			NetMessage.SendData(MessageID.SyncNPC, number: index);
			previous = index;
		}
	}

	private void DigEffects() {
		if (Main.dedServ || !Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
			return;

		if (Main.rand.NextBool(3))
			Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Dirt, NPC.velocity.X * -0.2f, NPC.velocity.Y * -0.2f);

		if (--DigSoundTimer <= 0f) {
			SoundEngine.PlaySound(SoundID.WormDig, NPC.Center);
			DigSoundTimer = 20f;
		}
	}

	public override void BossHeadRotation(ref float rotation) {
		rotation = 0f;
	}

	public override void BossHeadSpriteEffects(ref SpriteEffects spriteEffects) {
		spriteEffects = SerpentGeometry.HeadEffects(NPC.spriteDirection);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
		Texture2D texture = TextureAssets.Npc[Type].Value;
		spriteBatch.Draw(texture, NPC.Center - screenPos, null, NPC.GetAlpha(drawColor), NPC.rotation,
			SerpentGeometry.HeadOrigin(texture, NPC.spriteDirection), NPC.scale, SerpentGeometry.HeadEffects(NPC.spriteDirection), 0f);
		return false;
	}
}
