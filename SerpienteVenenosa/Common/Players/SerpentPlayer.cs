using Microsoft.Xna.Framework;
using SerpienteVenenosa.Common.Systems;
using SerpienteVenenosa.Content.Buffs;
using SerpienteVenenosa.Content.Items;
using SerpienteVenenosa.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Common.Players;

/// <summary>
/// Transformación en serpiente: el jugador pasa a ser la cabeza, se mueve libremente atravesando bloques
/// y el cuerpo lo sigue. Mientras dure el buff <see cref="SerpentForm"/>, esto reemplaza al movimiento normal.
/// </summary>
public class SerpentPlayer : ModPlayer
{
	public const int BodySegments = 14;
	// Con la Reliquia Serpentina equipada el cuerpo mide el doble.
	public const int LongBodySegments = BodySegments * 2;

	private const float MaxSpeed = 10f;
	private const float Acceleration = 0.5f;
	private const float Drag = 0.92f;
	// Por debajo de esta velocidad la cabeza no se reorienta (y al escupir mira hacia el cursor).
	private const float TurnSpeed = 1f;
	private const int SpitCooldownTicks = 24;
	private const int SpitDamage = 34;
	private const int ContactDamage = 28;
	private const int ContactCooldownTicks = 20;

	// Cuerpo visual: se usan los elementos 0..TailIndex, y el de TailIndex es la cola. Tienen lugar para el
	// cuerpo largo. Cada cliente lo calcula por su cuenta: no se sincroniza.
	public readonly Vector2[] SegmentPositions = new Vector2[LongBodySegments + 1];
	public readonly float[] SegmentAngles = new float[LongBodySegments + 1];
	public float HeadRotation;
	public int Facing = -1;
	public bool LongBody;

	public int TailIndex => LongBody ? LongBodySegments : BodySegments;

	private Vector2 serpentVelocity;
	// Posición a la que se movió la serpiente en este tick; se reaplica al final (ver PostUpdate).
	private Vector2 serpentPosition;
	private bool movedThisTick;
	private bool wasSerpent;
	private int spitCooldown;
	private int digSoundTimer;
	private readonly int[] contactCooldown = new int[Main.maxNPCs];

	public bool IsSerpent => Player.HasBuff(ModContent.BuffType<SerpentForm>());

	public bool IsBuried => Collision.SolidCollision(Player.position, Player.width, Player.height);

	public override void ResetEffects() {
		LongBody = false;
	}

	public override void SetControls() {
		if (!IsSerpent)
			return;

		// Saltar también sube. Después se anulan salto, gancho y montura para que no disparen
		// efectos vanilla (doble salto, alas, cohetes...) que pelearían con nuestro movimiento.
		if (Player.controlJump)
			Player.controlUp = true;
		Player.controlJump = false;
		Player.controlHook = false;
		Player.controlMount = false;
	}

	public override void ProcessTriggers(TriggersSet triggersSet) {
		if (IsSerpent && spitCooldown <= 0 && SerpentKeybinds.SpitVenom.Current) {
			SpitVenom();
			spitCooldown = SpitCooldownTicks;
		}
	}

	public override void PostUpdateEquips() {
		if (!IsSerpent)
			return;

		Player.dashType = 0;
		Player.noKnockback = true;
	}

	public override void PreUpdateMovement() {
		if (!IsSerpent) {
			serpentVelocity = Vector2.Zero;
			return;
		}

		if (Player.mount.Active)
			Player.mount.Dismount(Player);
		Player.RemoveAllGrapplingHooks();

		Vector2 input = Vector2.Zero;
		if (Player.controlLeft)
			input.X -= 1f;
		if (Player.controlRight)
			input.X += 1f;
		if (Player.controlUp)
			input.Y -= 1f;
		if (Player.controlDown)
			input.Y += 1f;

		if (input != Vector2.Zero) {
			serpentVelocity += Vector2.Normalize(input) * Acceleration;
			if (serpentVelocity.Length() > MaxSpeed)
				serpentVelocity = Vector2.Normalize(serpentVelocity) * MaxSpeed;
		}
		else {
			serpentVelocity *= Drag;
		}

		// Para atravesar bloques, la velocidad vanilla queda en cero (así la colisión no nos frena) y la posición
		// la manejamos nosotros. Se aplica recién en PostUpdate: después de este hook Terraria todavía acomoda al
		// jugador contra el piso y las pendientes, y eso lo "pegaba" al suelo cuando intentaba salir a la superficie.
		serpentPosition = Player.position + serpentVelocity;
		movedThisTick = true;
		Player.velocity = Vector2.Zero;
	}

	public override void PostUpdate() {
		bool serpent = IsSerpent;
		if (serpent && !wasSerpent)
			ResetSegments();
		wasSerpent = serpent;

		// Si la transformación empezó a mitad de este tick, PreUpdateMovement no llegó a calcular la posición.
		bool moved = movedThisTick;
		movedThisTick = false;
		if (!serpent)
			return;

		if (moved) {
			Player.position = serpentPosition;
			ClampToWorld();
			// Con velocidad real, Terraria no lo trata como "parado en el piso" al empezar el próximo tick.
			Player.velocity = serpentVelocity;
			Player.gfxOffY = 0f;
			Player.fallStart = (int)(Player.position.Y / 16f);
		}

		if (serpentVelocity.Length() >= TurnSpeed) {
			Facing = SerpentGeometry.UpdateFacing(Facing, serpentVelocity);
			HeadRotation = SerpentGeometry.HeadRotation(serpentVelocity, Facing);
		}
		Player.direction = Facing;

		UpdateSegments();
		DigEffects();

		if (Player.whoAmI == Main.myPlayer) {
			if (spitCooldown > 0)
				spitCooldown--;
			DealContactDamage();
		}
	}

	public override bool CanUseItem(Item item) =>
		!IsSerpent || item.type == ModContent.ItemType<SerpentFeather>();

	public override void HideDrawLayers(PlayerDrawSet drawInfo) {
		if (!IsSerpent)
			return;

		// Se oculta todo el personaje; la serpiente la dibuja SerpentDrawLayer.
		foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.Layers) {
			if (layer is not SerpentDrawLayer)
				layer.Hide();
		}
	}

	private void ClampToWorld() {
		// Mismos márgenes que usa Terraria para los bordes del mundo.
		Vector2 min = new(Main.leftWorld + 656f, Main.topWorld + 656f);
		Vector2 max = new(Main.rightWorld - 672f - Player.width, Main.bottomWorld - 672f - Player.height);
		Vector2 clamped = Vector2.Clamp(Player.position, min, max);
		if (clamped.X != Player.position.X)
			serpentVelocity.X = 0f;
		if (clamped.Y != Player.position.Y)
			serpentVelocity.Y = 0f;
		Player.position = clamped;
	}

	private void ResetSegments() {
		serpentVelocity = Vector2.Zero;
		Facing = Player.direction == 1 ? 1 : -1;
		HeadRotation = SerpentGeometry.HeadRotation(new Vector2(Facing, 0f), Facing);

		// El cuerpo arranca estirado hacia atrás desde la nuca y se acomoda al moverse.
		SerpentGeometry.GetBodyAnchor(Player.Center, HeadRotation, Facing, out Vector2 position, out float angle);
		float spacing = SerpentGeometry.BodyAnchorGap;
		for (int i = 0; i < SegmentPositions.Length; i++) {
			position -= angle.ToRotationVector2() * spacing;
			SegmentPositions[i] = position;
			SegmentAngles[i] = angle;
			spacing = SerpentGeometry.SegmentSpacing;
		}
	}

	private void UpdateSegments() {
		SerpentGeometry.GetBodyAnchor(Player.Center, HeadRotation, Facing, out Vector2 aheadPosition, out float aheadAngle);
		float spacing = SerpentGeometry.BodyAnchorGap;
		// Si recién se equipó la reliquia, los segmentos nuevos salen de donde estaban y FollowSegment los alinea.
		for (int i = 0; i <= TailIndex; i++) {
			aheadAngle = SerpentGeometry.FollowSegment(ref SegmentPositions[i], aheadPosition, aheadAngle, spacing);
			SegmentAngles[i] = aheadAngle;
			aheadPosition = SegmentPositions[i];
			spacing = SerpentGeometry.SegmentSpacing;
		}
	}

	private void SpitVenom() {
		Vector2 aim = Main.MouseWorld - Player.Center;
		if (serpentVelocity.Length() < TurnSpeed && aim != Vector2.Zero) {
			Facing = aim.X >= 0f ? 1 : -1;
			HeadRotation = SerpentGeometry.HeadRotation(aim, Facing);
		}

		Vector2 mouth = SerpentGeometry.HeadPoint(Player.Center, HeadRotation, Facing, SerpentGeometry.MouthOffset);
		Vector2 velocity = VenomBehavior.AimVelocity(mouth, Main.MouseWorld, 12f);
		int damage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(SpitDamage);
		int type = ModContent.ProjectileType<VenomSpit>();
		for (int i = -1; i <= 1; i++)
			Projectile.NewProjectile(Player.GetSource_FromThis(), mouth, velocity.RotatedBy(i * 0.12f), type, damage, 2f, Player.whoAmI);

		SoundEngine.PlaySound(SoundID.Item17, mouth);
	}

	private void DealContactDamage() {
		for (int i = 0; i < contactCooldown.Length; i++) {
			if (contactCooldown[i] > 0)
				contactCooldown[i]--;
		}

		int damage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(ContactDamage);
		foreach (NPC npc in Main.ActiveNPCs) {
			// Los segmentos de un gusano comparten vida (realLife): un solo cooldown para todo el bicho.
			int owner = npc.realLife >= 0 ? npc.realLife : npc.whoAmI;
			if (contactCooldown[owner] > 0 || !npc.CanBeChasedBy() || !TouchesSerpent(npc.Hitbox))
				continue;

			npc.SimpleStrikeNPC(damage, npc.Center.X > Player.Center.X ? 1 : -1, knockBack: 6f, damageType: DamageClass.Generic);
			npc.AddBuff(ModContent.BuffType<SerpentVenom>(), 180);
			FlaskEffects.Apply(Player, npc);
			contactCooldown[owner] = ContactCooldownTicks;
		}
	}

	private bool TouchesSerpent(Rectangle hitbox) {
		if (hitbox.Intersects(Utils.CenteredRectangle(Player.Center, new Vector2(64f))))
			return true;

		for (int i = 0; i <= TailIndex; i++) {
			if (hitbox.Intersects(Utils.CenteredRectangle(SegmentPositions[i], new Vector2(40f))))
				return true;
		}
		return false;
	}

	private void DigEffects() {
		if (Main.dedServ || serpentVelocity.LengthSquared() < 1f || !IsBuried)
			return;

		if (Main.rand.NextBool(2)) {
			Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Dirt);
			dust.velocity = -serpentVelocity * 0.2f;
		}

		if (--digSoundTimer <= 0) {
			SoundEngine.PlaySound(SoundID.WormDig, Player.Center);
			digSoundTimer = 18;
		}
	}
}
