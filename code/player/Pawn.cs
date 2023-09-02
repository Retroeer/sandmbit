using Sandbox;
using Sandbox.entity;
using Sandbox.player;
using System;
using System.Linq;
using Sandmbit.Weapons;
using Sandmbit.Mechanics;

namespace Sandmbit;

public partial class Pawn : AnimatedEntity
{
	private Nameplate Nameplate { get; set; }

	/// <summary>
	/// Accessor for getting a player's active weapon.
	/// </summary>
	public Weapon ActiveWeapon => Inventory?.ActiveWeapon;

	/// <summary>
	/// The information for the last piece of damage this player took.
	/// </summary>
	public DamageInfo LastDamage { get; protected set; }

	/// <summary>
	/// How long since the player last played a footstep sound.
	/// </summary>
	public TimeSince TimeSinceFootstep { get; protected set; } = 0;

	public BBox Hull
	{
		get => new
		(
			new Vector3( -16, -16, 0 ),
			new Vector3( 16, 16, 64 )
		);
	}

	[BindComponent] public PlayerController Controller { get; }
	[BindComponent] public PawnAnimator Animator { get; }
	[BindComponent] public PlayerCamera Camera { get; }
	[BindComponent] public MoteBag Motebag { get; }
	[BindComponent] public Inventory Inventory { get; }

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
		EnableLagCompensation = true;
		EnableShadowInFirstPerson = true;

		SetupPhysicsFromCapsule( PhysicsMotionType.Keyframed,
			Capsule.FromHeightAndRadius( Hull.Maxs.z - Hull.Mins.z, Math.Abs( Hull.Maxs.x - Hull.Mins.x ) ) );
		Tags.Add( "player" );
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

		Components.Create<PlayerController>();
		// Remove old mechanics.
		Components.RemoveAny<PlayerControllerMechanic>();

		// Add mechanics.
		Components.Create<WalkMechanic>();
		Components.Create<JumpMechanic>();
		Components.Create<AirMoveMechanic>();
		Components.Create<SprintMechanic>();
		Components.Create<CrouchMechanic>();
		Components.Create<InteractionMechanic>();

		Components.Create<PlayerCamera>();
		Components.Create<PawnAnimator>();
		Components.Create<MoteBag>();

		var inventory = Components.Create<Inventory>();
		inventory.AddWeapon( PrefabLibrary.Spawn<Weapon>( "prefabs/pistol.prefab" ) );

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
		Animator?.Simulate( cl );
		Inventory?.Simulate( cl );
		EyeLocalPosition = Vector3.Up * (64f * Scale);
	}

	public override void FrameSimulate( IClient cl )
	{
		SimulateRotation();
		Controller?.FrameSimulate( cl );
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
		EyeRotation = LookInput.ToRotation();
		Rotation = LookInput.WithPitch( 0f ).ToRotation();
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

			Controller?.Remove();
			Animator?.Remove();
			Motebag?.Remove();
			Camera?.Remove();
			Inventory?.Remove();

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

	/// <summary>
	/// Called clientside every time we fire the footstep anim event.
	/// </summary>
	public override void OnAnimEventFootstep( Vector3 pos, int foot, float volume )
	{
		if ( !Game.IsClient )
			return;

		if ( LifeState != LifeState.Alive )
			return;

		if ( TimeSinceFootstep < 0.2f )
			return;

		volume *= GetFootstepVolume();

		TimeSinceFootstep = 0;

		var tr = Trace.Ray( pos, pos + Vector3.Down * 20 )
			.Radius( 1 )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		tr.Surface.DoFootstep( this, tr, foot, volume );
	}

	protected float GetFootstepVolume()
	{
		return Controller.Velocity.WithZ( 0 ).Length.LerpInverse( 0.0f, 200.0f ) * 1f;
	}
}
