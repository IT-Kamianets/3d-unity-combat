using UnityEngine;

namespace ITKamianets.Engine.Combat
{
	/// <summary>
	/// Something that flies in a straight line and hurts the first enemy it meets: an arrow, a fireball. It
	/// flies through its own team and the dead, and is gone when it hits an enemy, hits the world or has flown
	/// its full range. The shape it is drawn with is up to the game.
	/// </summary>
	public sealed class Projectile : MonoBehaviour
	{
		public float Speed = 18f;
		public float Range = 25f;
		public float Radius = 0.25f;
		public int Team;
		public DamageInfo Damage;
		public LayerMask Mask = ~0;

		/// <summary>Raised when the projectile hurts someone (before it is destroyed).</summary>
		public event System.Action<Projectile, Health> Hit;

		Vector3 _direction;
		float _travelled;

		/// <summary>
		/// Starts a projectile at <paramref name="origin"/> flying along <paramref name="direction"/>. If
		/// <paramref name="visual"/> is null, a small sphere of <paramref name="colour"/> is used.
		/// </summary>
		public static Projectile Launch(Vector3 origin, Vector3 direction, float speed, float range, float radius, int team, in DamageInfo damage, Color colour, GameObject visual = null)
		{
			var go = visual != null ? visual : GameObject.CreatePrimitive(PrimitiveType.Sphere);
			go.name = "Projectile";
			var collider = go.GetComponent<Collider>();
			if (collider != null) Object.Destroy(collider);
			if (visual == null)
			{
				go.transform.localScale = Vector3.one * radius * 2f;
				var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
				var material = new Material(shader);
				material.SetColor("_BaseColor", colour);
				material.color = colour;
				go.GetComponent<Renderer>().sharedMaterial = material;
			}

			go.transform.position = origin;
			go.transform.rotation = Quaternion.LookRotation(direction);
			var projectile = go.AddComponent<Projectile>();
			projectile.Speed = speed;
			projectile.Range = range;
			projectile.Radius = radius;
			projectile.Team = team;
			projectile.Damage = damage;
			projectile._direction = direction.normalized;
			return projectile;
		}

		void Update()
		{
			var step = Speed * Time.deltaTime;
			if (step <= 0f) return;

			// Look ahead along this frame's path; stop at the first enemy or at the world.
			var hits = Physics.SphereCastAll(transform.position, Radius, _direction, step, Mask, QueryTriggerInteraction.Ignore);
			System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
			foreach (var hit in hits)
			{
				var health = hit.collider.GetComponentInParent<Health>();
				if (health != null)
				{
					if (health.Team == Team || !health.IsAlive) continue; // flies through friends and the fallen
					var info = Damage;
					info.Point = hit.point;
					health.ApplyDamage(info);
					Hit?.Invoke(this, health);
				}
				Destroy(gameObject);
				return;
			}

			transform.position += _direction * step;
			_travelled += step;
			if (_travelled >= Range) Destroy(gameObject);
		}
	}
}
