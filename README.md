# 3d-unity-combat

Generic combat for Unity games on the IT Kamianets 3D Engine: hit points, damage and death events, teams, and melee and area attacks that find and hurt their targets.

### Logic

- `Health` is the hit points of anything that can be hurt: a hero, a monster, a crate. `ApplyDamage(DamageInfo)` lowers it and answers `Damaged`, `Killed` or `Ignored`; the `Damaged` and `Died` events tell the game what to show. `Heal` and `Restore` bring it back up. `IDamageable` is the same thing as an interface, for targets that are not a `Health`.
- A **team** is a plain number chosen by the game. `Health` does not decide who may hurt whom: the attack queries skip targets on the attacker's own team and the dead.
- `Projectile.Launch` starts a projectile (an arrow, a fireball) that flies in a straight line and hurts the first enemy it meets; it flies through its own team and the dead, and is gone when it hits the world or has flown its range.
- `CombatQueries.DamageInCone` is a melee swing (range and half-angle, judged on the ground plane) and `DamageInRadius` an area attack. `FindInCone` and `FindInRadius` return the targets without hurting them, for aiming or a telegraph. Each target is hit once per attack.

Not here: classes, spells, weapons, loot, experience, UI, networking. Those are the game's, or later packages when a second game needs them.

### Status

v0.2: `Health`, `DamageInfo`, `DamageOutcome`, `IDamageable`, `CombatQueries`, `Projectile`, with edit-mode tests. Used by Temnorid Arena, Caravan Guard, Conquest, Dungeons and Legends; the other games have it because `3d-unity-feedback` depends on it.

### Roadmap

- A cooldown helper if two games end up writing the same one.
