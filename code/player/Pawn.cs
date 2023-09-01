using Sandbox;
using Sandbox.Component;
using Sandbox.entity;
using Sandbox.player;
using Sandbox.UI;
using System;
using System.ComponentModel;
using System.Linq;
using static Sandbox.Material;

namespace Sandmbit;

public partial class Pawn : AnimatedEntity
{
	[Net, Predicted] public Weapon ActiveWeapon { get; set; }

	[ClientInput] public Vector3 InputDirection { get; set; }

	[ClientInput] public Angles ViewAngles { get; set; }

	private Nameplate Nameplate { get; set; }

	/// <summary>
	/// The information for the last piece of damage this player took.
	/// </summary>
	public DamageInfo LastDamage { get; protected set; }

	public BBox Hull
	{
		get => new
		(
			new Vector3( -16, -16, 0 ),
			new Vector3( 16, 16, 64 )
		);
	}

	[BindComponent] public PawnController Controller { get; }
	[BindComponent] public PawnAnimator Animator { get; }
	[BindComponent] public PlayerCamera Camera { get; }
	[BindComponent] public MoteBag Motebag { get; }

	public override void ClientSpawn()
	{
		Nameplate = new Nameplate( this );

		base.ClientSpawn();
	}

	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		Predictable = true;
		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		SetupPhysicsFromCapsule( PhysicsMotionType.Keyframed,
			Capsule.FromHeightAndRadius( Hull.Maxs.z - Hull.Mins.z, Math.Abs( Hull.Maxs.x - Hull.Mins.x ) ) );
		Tags.Add( "player" );
	}

	public void SetActiveWeapon( Weapon weapon )
	{
		ActiveWeapon?.OnHolster();
		ActiveWeapon = weapon;
		ActiveWeapon.OnEquip( this );
	}

	public void Respawn()
	{
		SetupPhysicsFromCapsule( PhysicsMotionType.Keyframed, Capsule.FromHeightAndRadius( Hull.Maxs.z - Hull.Mins.z, Math.Abs( Hull.Maxs.x - Hull.Mins.x ) ) );

		Health = 200;
		LifeState = LifeState.Alive;
		EnableAllCollisions = true;
		EnableDrawing = true;

		// Re-enable all children.
		Children.OfType<ModelEntity>()
			.ToList()
			.ForEach( x => x.EnableDrawing = true );

		Components.Create<PlayerCamera>();
		Components.Create<PawnController>();
		Components.Create<PawnAnimator>();
		Components.Create<MoteBag>();

		SetActiveWeapon( new Pistol() );

		GameManager.Current?.MoveToSpawnpoint( this );
		Position = Position + Vector3.Up * 2;
		ResetInterpolation();
	}

	public void DressFromClient( IClient cl )
	{
		var c = new ClothingContainer();
		c.LoadFromClient( cl );
		c.DressEntity( this );
	}

	public override void Simulate( IClient cl )
	{
		SimulateRotation();
		Controller?.Simulate( cl );
		Animator?.Simulate();
		ActiveWeapon?.Simulate( cl );
		EyeLocalPosition = Vector3.Up * (64f * Scale);
	}

	public override void FrameSimulate( IClient cl )
	{
		SimulateRotation();
		Camera?.Update( this );
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f )
	{
		return TraceBBox( start, end, Hull.Mins, Hull.Maxs, liftFeet );
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f )
	{
		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		var tr = Trace.Ray( start, end )
			.Size( mins, maxs )
			.WithAnyTags( "solid", "playerclip", "passbullets" )
			.Ignore( this )
			.Run();

		return tr;
	}

	protected void SimulateRotation()
	{
		EyeRotation = ViewAngles.ToRotation();
		Rotation = ViewAngles.WithPitch( 0f ).ToRotation();
	}

	public override void StartTouch( Entity other )
	{
		if ( Game.IsServer )
		{
			if ( other is Mote mote )
			{
				if ( Motebag.AddMote() )
				{
					Log.Info(
						$"Player {Client.Name} picked up a mote (have {Motebag.Motes}/{Motebag.MaxMotes}, blocker={Motebag.AffordableBlocker()})" );
					mote.Delete();
				}
			}
		}
	}

	public virtual bool Alive()
	{
		return LifeState != LifeState.Alive;
	}

	public override void TakeDamage( DamageInfo info )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( info.Attacker is Pawn p )
		{
			//No friendly fire
			if ( p.Team == this.Team )
				return;
		}

		// Check if we got hit by a bullet, if we did, play a sound.
		if ( info.HasTag( "bullet" ) )
		{
			Sound.FromScreen( To.Single( Client ), "sounds/player/damage_taken_shot.sound" );
		}

		if ( Health > 0 && info.Damage > 0 )
		{
			Health -= info.Damage;

			if ( Health <= 0 )
			{
				Health = 0;
				OnKilled();
			}
		}
		LastDamage = info;
		this.ProceduralHitReaction( info );
	}

	public override void OnKilled()
	{
		var attacker = LastDamage.Attacker;

		//Get the attackers team, if any
		var attackerTeam = attacker is Pawn p ? p.Team : Team.None;

		//Make player drop motes they were carrying
		if( Game.IsServer )
		{
			Log.Info( $"{Client.Name} dropped {Motebag.Motes} motes" );
			for ( int i = 0; i < Motebag.Motes; i++ )
			{
				var mote = TypeLibrary.Create<Mote>( "gambit_mote" );
				mote.Position = EyePosition;
				mote.Velocity = new Vector3(Random.Shared.Float(-150,150)).WithZ(256);
				mote.Rotation = Rotation.Random;
			}
			Motebag.ClearMotes();
		}
		
		if ( LifeState == LifeState.Alive )
		{
			//Log.Info($"Controller.Entity.Velocity, LastDamage.Position, LastDamage.Force {Controller.Entity.Velocity} {LastDamage.Position} {LastDamage.Force}" );
			CreateRagdoll( Controller.Entity.Velocity, LastDamage.Position, LastDamage.Force,
				LastDamage.BoneIndex, LastDamage.HasTag( "bullet" ), LastDamage.HasTag( "blast" ) );

			LifeState = LifeState.Dead;
			EnableAllCollisions = false;
			EnableDrawing = false;

			Controller.Remove();
			Animator.Remove();
			Motebag.Remove();
			Camera.Remove();
			ActiveWeapon.DestroyViewModel();

			foreach ( var child in Children )
			{
				child.EnableDrawing = false;
			}

			AsyncRespawn();
		}
	}

	private async void AsyncRespawn()
	{
		await GameTask.DelaySeconds( 3f );
		Respawn();
	}
}
