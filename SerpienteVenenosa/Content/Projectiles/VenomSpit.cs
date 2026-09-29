using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerpienteVenenosa.Content.Buffs;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerpienteVenenosa.Content.Projectiles;

/// <summary>Veneno que escupe el jugador transformado.</summary>
public class VenomSpit : ModProjectile
{
	public override void SetDefaults() {
		Projectile.width = 14;
		Projectile.height = 14;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Generic;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 150;
		Projectile.tileCollide = false;
	}

	public override void AI() => VenomBehavior.Update(Projectile);

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
		target.AddBuff(ModContent.BuffType<SerpentVenom>(), 300);
	}

	public override void OnKill(int timeLeft) => VenomBehavior.Splash(Projectile);

	public override bool PreDraw(ref Color lightColor) => VenomBehavior.Draw(Projectile);
}

/// <summary>Veneno que escupe el jefe.</summary>
public class VenomSpitHostile : ModProjectile
{
	public override string Texture => "SerpienteVenenosa/Content/Projectiles/VenomSpit";

	public override void SetDefaults() {
		Projectile.width = 14;
		Projectile.height = 14;
		Projectile.hostile = true;
		Projectile.timeLeft = 300;
		Projectile.tileCollide = false;
	}

	public override void AI() => VenomBehavior.Update(Projectile);

	public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Poisoned, 240);

	public override void OnKill(int timeLeft) => VenomBehavior.Splash(Projectile);

	public override bool PreDraw(ref Color lightColor) => VenomBehavior.Draw(Projectile);
}

internal static class VenomBehavior
{
	public const float Gravity = 0.12f;

	/// <summary>
	/// Velocidad de disparo para que el veneno, que cae por la gravedad, llegue cerca de <paramref name="target"/>
	/// (compensa la caída apuntando un poco más arriba).
	/// </summary>
	public static Vector2 AimVelocity(Vector2 from, Vector2 target, float speed) {
		Vector2 velocity = (target - from).SafeNormalize(Vector2.UnitY) * speed;
		float flightTime = Math.Min(Vector2.Distance(from, target) / speed, 60f);
		velocity.Y -= 0.5f * Gravity * flightTime;
		return velocity;
	}

	public static void Update(Projectile projectile) {
		// Las serpientes escupen desde bajo tierra: el veneno atraviesa bloques hasta salir al aire,
		// y a partir de ahí choca normalmente.
		if (!projectile.tileCollide && !Collision.SolidCollision(projectile.position, projectile.width, projectile.height))
			projectile.tileCollide = true;

		projectile.velocity.Y = Math.Min(projectile.velocity.Y + Gravity, 14f);
		projectile.rotation = projectile.velocity.ToRotation();
		Lighting.AddLight(projectile.Center, 0.25f, 0.55f, 0.1f);

		if (Main.rand.NextBool(3)) {
			Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.GreenTorch, Alpha: 100, Scale: 1.1f);
			dust.noGravity = true;
			dust.velocity *= 0.3f;
		}
	}

	public static void Splash(Projectile projectile) {
		SoundEngine.PlaySound(SoundID.Item10, projectile.Center);
		for (int i = 0; i < 12; i++) {
			Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.GreenTorch, Alpha: 100, Scale: 1.3f);
			dust.noGravity = true;
			dust.velocity *= 2f;
		}
	}

	public static bool Draw(Projectile projectile) {
		// El veneno brilla: se dibuja sin la iluminación del entorno.
		Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
		Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, null, Color.White * projectile.Opacity,
			projectile.rotation, texture.Size() / 2f, projectile.scale, SpriteEffects.None, 0f);
		return false;
	}
}
