using System.Collections.Generic;
using UnityEngine;

namespace ITKamianets.Engine.Combat
{
	/// <summary>
	/// Finding and hurting targets: a melee swing in front of you, or an area around a point. Targets
	/// are anything with a <see cref="Health"/> on or above a collider; those on the attacker's own team,
	/// and the dead, are skipped, and each target is hit once per attack.
	/// </summary>
	public static class CombatQueries
	{
		static readonly Collider[] Buffer = new Collider[128];
		static readonly HashSet<Health> Seen = new HashSet<Health>();
		static readonly List<Health> Found = new List<Health>();

		/// <summary>
		/// Targets within <paramref name="range"/> of <paramref name="origin"/> and within
		/// <paramref name="halfAngleDegrees"/> of <paramref name="forward"/> (judged on the ground plane).
		/// Anything almost touching the attacker is always in reach, whatever the angle.
		/// </summary>
		public static List<Health> FindInCone(Vector3 origin, Vector3 forward, float range, float halfAngleDegrees, int attackerTeam, int layerMask = ~0)
		{
			forward.y = 0f;
			if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
			forward.Normalize();

			Collect(origin, range, attackerTeam, layerMask);
			for (var i = Found.Count - 1; i >= 0; i--)
			{
				var toTarget = Found[i].transform.position - origin;
				toTarget.y = 0f;
				if (toTarget.magnitude < 0.9f) continue;
				if (Vector3.Angle(forward, toTarget) > halfAngleDegrees) Found.RemoveAt(i);
			}
			return Found;
		}

		/// <summary>Targets within <paramref name="radius"/> of <paramref name="center"/>.</summary>
		public static List<Health> FindInRadius(Vector3 center, float radius, int attackerTeam, int layerMask = ~0)
		{
			Collect(center, radius, attackerTeam, layerMask);
			return Found;
		}

		/// <summary>A melee swing: hurts every target in the cone. Returns how many were hit.</summary>
		public static int DamageInCone(Vector3 origin, Vector3 forward, float range, float halfAngleDegrees, int attackerTeam, in DamageInfo info, int layerMask = ~0)
		{
			var targets = FindInCone(origin, forward, range, halfAngleDegrees, attackerTeam, layerMask);
			return Apply(targets, info);
		}

		/// <summary>An area attack: hurts every target in the radius. Returns how many were hit.</summary>
		public static int DamageInRadius(Vector3 center, float radius, int attackerTeam, in DamageInfo info, int layerMask = ~0)
		{
			var targets = FindInRadius(center, radius, attackerTeam, layerMask);
			return Apply(targets, info);
		}

		static int Apply(List<Health> targets, in DamageInfo info)
		{
			// The list is reused by the next query, so take a copy before damage can trigger more queries.
			var hit = targets.ToArray();
			foreach (var health in hit)
			{
				var each = info;
				each.Point = health.transform.position;
				health.ApplyDamage(each);
			}
			return hit.Length;
		}

		static void Collect(Vector3 center, float radius, int attackerTeam, int layerMask)
		{
			Found.Clear();
			Seen.Clear();
			var count = Physics.OverlapSphereNonAlloc(center, radius, Buffer, layerMask, QueryTriggerInteraction.Ignore);
			for (var i = 0; i < count; i++)
			{
				var health = Buffer[i].GetComponentInParent<Health>();
				if (health == null || !health.IsAlive || health.Team == attackerTeam) continue;
				if (Seen.Add(health)) Found.Add(health);
			}
		}
	}
}
