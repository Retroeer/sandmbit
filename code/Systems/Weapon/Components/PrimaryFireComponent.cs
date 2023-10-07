using Sandmbit.Mechanics;
using Sandbox;
using static Sandbox.Event;
using System.Collections.Generic;
using System.Numerics;
using static Sandmbit.Weapons.PrimaryFire;

namespace Sandmbit.Weapons;

[Prefab]
public partial class PrimaryFire : WeaponComponent, ISingletonComponent
{
	[Net, Prefab, Category("Damage")] public float BaseDamage { get; set; }
	[Net, Prefab, Category( "Damage" )] public float HeadshotMultiplier { get; set; }

	[Net, Prefab, Category( "Bullet" )] public float BulletRange { get; set; }
	[Net, Prefab, Category( "Bullet" )] public int BulletCount { get; set; }
	[Net, Prefab, Category( "Bullet" )] public float BulletForce { get; set; }
	[Net, Prefab, Category( "Bullet" )] public float BulletSize { get; set; }
	[Net, Prefab, Category( "Bullet" )] public float BulletSpread { get; set; }

	[Net, Prefab, Category( "General" )] public float RPM { get; set; }

	protected AimDownSights AimData => Weapon.GetComponent<AimDownSights>();

	[Net, Prefab, Category( "General" )] public Firing_Type FiringType { get; set; }
	public enum Firing_Type
	{
		Auto,
		Semi,
		Burst
	}
	[Net, Prefab, Category( "General" ), ShowIf(nameof(FiringType), Firing_Type.Burst)] public int MaxBurstCount { get; set; }

	[Net, Prefab, ResourceType( "sound" )] public string FireSound { get; set; }

	[Net, Predicted] private TimeSince TimeSinceBurstFinished { get; set; }
	[Net, Predicted] public bool IsBurstFiring { get; set; } = false;
	[Net, Predicted] private int BurstCount { get; set; } = 0;

	protected override bool CanStart( Pawn player )
	{
		if ( player.Controller.IsMechanicActive<SprintMechanic>() ) return false;
		if ( Weapon.Ammo == 0 ) return false;
		if ( CanBurst() )
		{
			BurstCount = 0;
			IsBurstFiring = true;
		}

		if(IsBurstFiring)
		{
			if( BurstCount < MaxBurstCount)
			{
				if ( TimeSinceActivated > 60f / 900 && TimeSinceBurstFinished > 60f / RPM )
				{
					BurstCount++;
					return true;
				}
			}
			else
			{
				IsBurstFiring = false;
				if ( TimeSinceBurstFinished > 60f / RPM )
				{
					TimeSinceBurstFinished = 0;	
					return false;
				}
			}
		}

		if ( FiringType == Firing_Type.Auto && !Input.Down( "attack1" ) ) return false;
		if ( FiringType == Firing_Type.Semi && !Input.Pressed( "attack1" ) ) return false;

		if ( FiringType != Firing_Type.Burst ) return TimeSinceActivated > 60f / RPM;

		return false;
	}

	public bool CanBurst()
	{
		if ( Weapon.Ammo == 0 ) return false;
		if ( IsBurstFiring ) return false;

		return FiringType == Firing_Type.Burst && Input.Pressed( "attack1" );
	}

	public override void OnGameEvent( string eventName )
	{
		if ( eventName == $"{Identifier}.start" )
		{
			
		}
	}

	protected override void OnStart( Pawn player )
	{
		base.OnStart( player );
		player?.SetAnimParameter( "b_attack", true );
		
		// Send clientside effects to the player.
		if ( Game.IsServer )
		{
			player.PlaySound( FireSound );
			DoShootEffects( To.Single( player ) );
		}

		ShootBullet( BulletSpread, BulletForce, BulletSize, BulletCount, BulletRange );	
	}

	[ClientRpc]
	public void DoShootEffects()
	{
		Game.AssertClient();
		if( !AimData.IsAiming )
			WeaponViewModel.Current?.SetAnimParameter( "fire", true );
		else if( AimData.IsAiming && AimData.AnimateInADS )
			WeaponViewModel.Current?.SetAnimParameter( "fire", true );

	}

	/// <summary>
	/// A single bullet trace from start to end with a certain radius.
	/// </summary>
	public virtual IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		var startsInWater = SurfaceUtil.IsPointWater( start );
		List<string> withoutTags = new() { "trigger" };

		if ( startsInWater )
			withoutTags.Add( "water" );

		var tr = Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "glass" )
				.WithoutTags( withoutTags.ToArray() )
				.Ignore( Entity )
				.Size( radius )
				.Run();

		yield return tr;
	}

	public virtual void ShootBullet( float spread, float force, float bulletSize, int bulletCount = 1, float bulletRange = 5000f )
	{
		//
		// Seed rand using the tick, so bullet cones match on client and server
		//
		Game.SetRandomSeed( Time.Tick );

		for ( int i = 0; i < bulletCount; i++ )
		{
			var rot = Rotation.LookAt( Player.AimRay.Forward );

			var forward = rot.Forward;
			forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
			forward = forward.Normal;

			var damage = BaseDamage;

			foreach ( var tr in TraceBullet( Player.AimRay.Position, Player.AimRay.Position + forward * bulletRange, bulletSize ) )
			{
				if ( !tr.Entity.IsValid() ) continue;
				
				tr.Surface.DoBulletImpact( tr );

				if ( !Game.IsServer ) continue;

				var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 100 * force, damage )
					.UsingTraceResult( tr )
					.WithAttacker( Player )
					.WithWeapon( Weapon );

				// Check for headshot damage
				var isHeadshot = damageInfo.Hitbox.HasTag( "head" );
				if ( isHeadshot )
				{
					damageInfo.Damage *= HeadshotMultiplier;
				}

				tr.Entity.TakeDamage( damageInfo );
			}
		}
		if ( Weapon.Ammo > 0 )
			Weapon.Ammo -= 1;
	}
}
