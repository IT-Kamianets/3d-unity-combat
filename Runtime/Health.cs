using System;
using UnityEngine;

namespace ITKamianets.Engine.Combat
{
	/// <summary>One hit: how much, who dealt it, and where.</summary>
	public struct DamageInfo
	{
		public float Amount;
		/// <summary>Whoever dealt the damage, if anyone. Not used by the engine; for the game's own rules.</summary>
		public GameObject Source;
		public Vector3 Point;
		/// <summary>A label the game can use to tell kinds of damage apart ("melee", "fire").</summary>
		public string Type;

		public DamageInfo(float amount, GameObject source = null, Vector3 point = default, string type = null)
		{
			Amount = amount;
			Source = source;
			Point = point;
			Type = type;
		}
	}

	/// <summary>What a hit did.</summary>
	public enum DamageOutcome
	{
		/// <summary>Nothing happened: already dead, or no damage.</summary>
		Ignored,
		Damaged,
		Killed,
	}

	/// <summary>Anything that can be hurt and belongs to a team.</summary>
	public interface IDamageable
	{
		int Team { get; }
		bool IsAlive { get; }
		DamageOutcome ApplyDamage(in DamageInfo info);
	}

	/// <summary>
	/// Hit points for a character, monster or breakable. It says nothing about who may hurt whom: the
	/// attack queries skip targets on the attacker's own team, and every other rule is the game's.
	/// </summary>
	public sealed class Health : MonoBehaviour, IDamageable
	{
		[SerializeField] float maxHealth = 100f;
		[SerializeField] int team;

		float _current;
		bool _started;

		public float MaxHealth
		{
			get => maxHealth;
			set
			{
				maxHealth = Mathf.Max(1f, value);
				_current = Mathf.Min(Current, maxHealth);
			}
		}

		/// <summary>Teams are plain numbers chosen by the game; attacks skip targets on their own team.</summary>
		public int Team
		{
			get => team;
			set => team = value;
		}

		public float Current
		{
			get
			{
				EnsureStarted();
				return _current;
			}
		}

		/// <summary>Scales every hit this takes: 1 normally, 0.4 while a shield takes 60% off, 1.5 for a curse. The game sets it; the engine only applies it.</summary>
		public float DamageTaken { get; set; } = 1f;

		public bool IsAlive => Current > 0f;

		/// <summary>0 to 1, for a health bar.</summary>
		public float Fraction => Current / maxHealth;

		/// <summary>After a hit that did not kill.</summary>
		public event Action<Health, DamageInfo> Damaged;

		/// <summary>When health reaches zero. Raised once.</summary>
		public event Action<Health, DamageInfo> Died;

		void Awake() => EnsureStarted();

		void EnsureStarted()
		{
			if (_started) return;
			_started = true;
			_current = maxHealth;
		}

		public DamageOutcome ApplyDamage(in DamageInfo info)
		{
			EnsureStarted();
			if (_current <= 0f || info.Amount <= 0f) return DamageOutcome.Ignored;

			var hit = info;
			hit.Amount = info.Amount * Mathf.Max(0f, DamageTaken);
			if (hit.Amount <= 0f) return DamageOutcome.Ignored;

			_current = Mathf.Max(0f, _current - hit.Amount);
			if (_current <= 0f)
			{
				Died?.Invoke(this, hit);
				return DamageOutcome.Killed;
			}
			Damaged?.Invoke(this, hit);
			return DamageOutcome.Damaged;
		}

		public void Heal(float amount)
		{
			EnsureStarted();
			if (_current <= 0f || amount <= 0f) return;
			_current = Mathf.Min(maxHealth, _current + amount);
		}

		/// <summary>Back to full health, alive again.</summary>
		public void Restore()
		{
			_started = true;
			_current = maxHealth;
		}
	}
}
