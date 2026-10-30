using NUnit.Framework;
using UnityEngine;

namespace ITKamianets.Engine.Combat.Tests
{
	public class HealthTests
	{
		GameObject _root;

		[TearDown]
		public void TearDown()
		{
			if (_root != null) Object.DestroyImmediate(_root);
		}

		Health NewTarget(Vector3 position, int team, float max = 50f)
		{
			if (_root == null) _root = new GameObject("Arena");
			var go = new GameObject("Target");
			go.transform.SetParent(_root.transform, false);
			go.transform.position = position;
			go.AddComponent<SphereCollider>();
			var health = go.AddComponent<Health>();
			health.MaxHealth = max;
			health.Team = team;
			health.Restore();
			return health;
		}

		[Test]
		public void Damage_lowers_health_and_the_last_hit_kills_once()
		{
			var health = NewTarget(Vector3.zero, 2, 30f);
			var died = 0;
			health.Died += (h, i) => died++;

			Assert.AreEqual(DamageOutcome.Damaged, health.ApplyDamage(new DamageInfo(10f)));
			Assert.AreEqual(20f, health.Current, 0.001f);
			Assert.AreEqual(DamageOutcome.Killed, health.ApplyDamage(new DamageInfo(25f)));
			Assert.IsFalse(health.IsAlive);
			Assert.AreEqual(DamageOutcome.Ignored, health.ApplyDamage(new DamageInfo(5f)), "the dead cannot be hurt again");
			Assert.AreEqual(1, died);
		}

		[Test]
		public void Zero_damage_does_nothing_and_healing_stops_at_the_maximum()
		{
			var health = NewTarget(Vector3.zero, 2, 40f);
			Assert.AreEqual(DamageOutcome.Ignored, health.ApplyDamage(new DamageInfo(0f)));
			health.ApplyDamage(new DamageInfo(30f));
			health.Heal(100f);
			Assert.AreEqual(40f, health.Current, 0.001f);
			Assert.AreEqual(1f, health.Fraction, 0.001f);
		}

		[Test]
		public void Dead_targets_cannot_be_healed_back_but_can_be_restored()
		{
			var health = NewTarget(Vector3.zero, 2, 10f);
			health.ApplyDamage(new DamageInfo(99f));
			health.Heal(5f);
			Assert.IsFalse(health.IsAlive);
			health.Restore();
			Assert.IsTrue(health.IsAlive);
			Assert.AreEqual(10f, health.Current, 0.001f);
		}

		[Test]
		public void A_cone_hits_enemies_in_front_and_skips_friends_and_those_behind()
		{
			var front = NewTarget(new Vector3(0f, 0f, 2f), 2);
			var side = NewTarget(new Vector3(2f, 0f, 0.2f), 2);
			var behind = NewTarget(new Vector3(0f, 0f, -2.5f), 2);
			var friend = NewTarget(new Vector3(0.3f, 0f, 2f), 1);
			var far = NewTarget(new Vector3(0f, 0f, 9f), 2);
			Physics.SyncTransforms();

			var hits = CombatQueries.DamageInCone(Vector3.zero, Vector3.forward, 3f, 60f, 1, new DamageInfo(10f));

			Assert.AreEqual(1, hits);
			Assert.AreEqual(40f, front.Current, 0.001f);
			Assert.AreEqual(50f, side.Current, 0.001f, "outside the cone");
			Assert.AreEqual(50f, behind.Current, 0.001f);
			Assert.AreEqual(50f, friend.Current, 0.001f, "same team as the attacker");
			Assert.AreEqual(50f, far.Current, 0.001f, "out of range");
		}

		[Test]
		public void An_area_attack_hits_everyone_around_once_and_not_the_dead()
		{
			var a = NewTarget(new Vector3(1f, 0f, 0f), 2);
			var b = NewTarget(new Vector3(-2f, 0f, 1f), 3);
			var dead = NewTarget(new Vector3(0f, 0f, 1.5f), 2);
			dead.ApplyDamage(new DamageInfo(999f));
			Physics.SyncTransforms();

			var hits = CombatQueries.DamageInRadius(Vector3.zero, 3f, 1, new DamageInfo(20f));

			Assert.AreEqual(2, hits);
			Assert.AreEqual(30f, a.Current, 0.001f);
			Assert.AreEqual(30f, b.Current, 0.001f);
		}
	}
}
