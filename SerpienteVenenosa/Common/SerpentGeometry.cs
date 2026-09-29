using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace SerpienteVenenosa.Common;

/// <summary>
/// Geometría compartida entre el jefe y la transformación del jugador.
/// Los valores están en píxeles de las texturas; si cambiás los sprites, actualizá también tools/SpriteGen.cs.
/// </summary>
public static class SerpentGeometry
{
	// La cabeza mira a la IZQUIERDA en el sprite. El pivote es el centro de la cara (= centro del hitbox).
	public static readonly Vector2 HeadPivot = new(46, 78);
	// Relativos al pivote, con el sprite sin espejar. El cuerpo se engancha en la nuca, debajo de las plumas.
	public static readonly Vector2 BodyAnchorOffset = new(29, 0);
	public static readonly Vector2 MouthOffset = new(-30, 12);
	// Hacia dónde sale el cuerpo desde la nuca: hacia atrás y un poco hacia abajo.
	public static readonly Vector2 BodyAnchorDirection = new(1f, 0.25f);

	// Los segmentos del cuerpo y la cola apuntan hacia ARRIBA en el sprite (hacia la cabeza).
	public const int BodyFrameWidth = 88;
	public const int BodyFrameHeight = 34;
	public static readonly Vector2 BodyPivot = new(44, 17);
	public static readonly Vector2 TailPivot = new(32, 15);

	public const float BodyAnchorGap = 8f;
	public const float SegmentSpacing = 24f;
	// Cuánto puede doblarse cada articulación (radianes). Evita que el cuerpo se abra en abanico en las curvas.
	public const float MaxBend = 0.38f;

	/// <summary>-1 = mira a la izquierda, 1 = a la derecha. Solo cambia con movimiento horizontal claro.</summary>
	public static int UpdateFacing(int facing, Vector2 direction, float threshold = 0.5f) {
		if (direction.X > threshold)
			return 1;
		if (direction.X < -threshold)
			return -1;
		return facing == 0 ? -1 : facing;
	}

	/// <summary>Rotación del sprite de la cabeza para que la boca apunte hacia <paramref name="direction"/>.</summary>
	public static float HeadRotation(Vector2 direction, int facing) {
		float angle = direction.ToRotation();
		return facing == 1 ? angle : angle + MathHelper.Pi;
	}

	/// <summary>Pasa un punto relativo al pivote de la cabeza a coordenadas del mundo.</summary>
	public static Vector2 HeadPoint(Vector2 headCenter, float headRotation, int facing, Vector2 localOffset) {
		if (facing == 1)
			localOffset.X = -localOffset.X;
		return headCenter + localOffset.RotatedBy(headRotation);
	}

	/// <summary>
	/// Punto de la nuca donde se engancha el cuerpo, y hacia dónde "apunta" (hacia la cabeza).
	/// El primer segmento lo sigue como si fuera el segmento de adelante.
	/// </summary>
	public static void GetBodyAnchor(Vector2 headCenter, float headRotation, int facing, out Vector2 anchor, out float anchorAngle) {
		anchor = HeadPoint(headCenter, headRotation, facing, BodyAnchorOffset);
		Vector2 backward = BodyAnchorDirection;
		if (facing == 1)
			backward.X = -backward.X;
		anchorAngle = (-backward).RotatedBy(headRotation).ToRotation();
	}

	/// <summary>
	/// Ubica un segmento a <paramref name="spacing"/> del que tiene adelante, sin doblarse más de <see cref="MaxBend"/>.
	/// Devuelve el ángulo del segmento (la dirección en la que apunta, hacia el de adelante).
	/// </summary>
	public static float FollowSegment(ref Vector2 position, Vector2 aheadPosition, float aheadAngle, float spacing) {
		Vector2 toAhead = aheadPosition - position;
		float angle = toAhead.LengthSquared() < 0.0001f ? aheadAngle : toAhead.ToRotation();
		angle = aheadAngle + MathHelper.Clamp(MathHelper.WrapAngle(angle - aheadAngle), -MaxBend, MaxBend);
		position = aheadPosition - angle.ToRotationVector2() * spacing;
		return angle;
	}

	public static float SpriteRotation(float angle) => angle + MathHelper.PiOver2;

	public static float AngleFromSpriteRotation(float rotation) => rotation - MathHelper.PiOver2;

	/// <summary>Uno de cada tres segmentos lleva plumas (segundo cuadro de la textura del cuerpo).</summary>
	public static Rectangle BodyFrame(int segmentIndex) =>
		new(0, segmentIndex % 3 == 2 ? BodyFrameHeight : 0, BodyFrameWidth, BodyFrameHeight);

	public static SpriteEffects HeadEffects(int facing) =>
		facing == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

	// Al espejar, el origen se mide sobre la imagen ya espejada.
	public static Vector2 HeadOrigin(Texture2D texture, int facing) =>
		facing == 1 ? new Vector2(texture.Width - HeadPivot.X, HeadPivot.Y) : HeadPivot;
}
