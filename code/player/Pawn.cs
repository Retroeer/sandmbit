using Sandbox;
using Sandbox.Component;
using Sandbox.entity;
using Sandbox.player;
using System;
using System.ComponentModel;
using System.Linq;

namespace Sandmbit;

public partial class Pawn : AnimatedEntity
{
	[Net, Predicted]
	public Weapon ActiveWeapon { get; set; }

	[ClientInput]
	public Vector3 InputDirection { get; set; }
	
	[ClientInput]
	public Angles ViewAngles { get; set; }

	/// <summary>
	/// The player's current team
	/// </summary>
	[Net]
	private Team PlayerTeam { get; set; } = Team.None;

	/// <summary>
	/// The information for the last piece of damage this player took.
	/// </summary>
	public DamageInfo LastDamage { get; protected set; }

	/// <summary>
	/// Position a player should be looking from in world space.
	/// </summary>
	[Browsable( false )]
	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	/// <summary>
	/// Position a player should be looking from in local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Vector3 EyeLocalPosition { get; set; }

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity.
	/// </summary>
	[Browsable( false )]
	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity. In local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Rotation EyeLocalRotation { get; set; }

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

	[BindComponent] public MoteBag Motes{ get; }

	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );

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

		Components.Create<PawnController>();
		Components.Create<PawnAnimator>();
		Components.GetOrCreate<MoteBag>();

		SetActiveWeapon( new Pistol() );
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

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		if ( Input.StopProcessing )
			return;

		var look = Input.AnalogLook;

		if ( ViewAngles.pitch > 90f || ViewAngles.pitch < -90f )
		{
			look = look.WithYaw( look.yaw * -1f );
		}

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89f, 89f );
		viewAngles.roll = 0f;
		ViewAngles = viewAngles.Normal;
	}

	bool IsThirdPerson { get; set; } = false;

	public override void FrameSimulate( IClient cl )
	{
		SimulateRotation();

		Camera.Rotation = ViewAngles.ToRotation();
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		if ( Input.Pressed( "view" ) )
		{
			IsThirdPerson = !IsThirdPerson;
		}

		if ( IsThirdPerson )
		{
			Vector3 targetPos;
			var pos = Position + Vector3.Up * 64;
			var rot = Camera.Rotation * Rotation.FromAxis( Vector3.Up, -16 );

			float distance = 80.0f * Scale;
			targetPos = pos + rot.Right * ((CollisionBounds.Mins.x + 50) * Scale);
			targetPos += rot.Forward * -distance;

			var tr = Trace.Ray( pos, targetPos )
				.WithAnyTags( "solid" )
				.Ignore( this )
				.Radius( 8 )
				.Run();
			
			Camera.FirstPersonViewer = null;
			Camera.Position = tr.EndPosition;
		}
		else
		{
			Camera.FirstPersonViewer = this;
			Camera.Position = EyePosition;

			var tr = Trace.Ray( Camera.Position, Camera.Rotation.Forward * 10000 )
				.WithAnyTags( "player" )
				.Ignore( this )
				.Radius( 8 )
				.Run();

			//If the player is looking at an enemy, make them glow
			if(tr.Entity?.ClassName == "Pawn")
			{
				var enemy = tr.Entity as Pawn;
				var glow = enemy.Components.GetOrCreate<Glow>();
				if ( TeamSystem.IsHostile( Team, enemy.Team ) )
				{
					glow.Enabled = true;
					glow.Width = 0.25f;
					glow.Color = new Color( 1.0f, 0.0f, 0.0f, 0.5f );
					glow.ObscuredColor = new Color( 1.0f, 0.0f, 0.0f, 1.0f );
					glow.InsideColor = new Color( 1.0f, 0.0f, 0.0f, 0.1f );
				}
			}
			else
			{
				//Remove the glow when looking away
				foreach(var pawn in TeamExtensions.GetAll( TeamSystem.GetEnemyTeam( Team ) ))
				{
					pawn.Components.Remove( pawn.Components.Get<Glow>() );
				}
			}	
		}

		if ( Input.Pressed( "attack2" ) )
		{
			var mote = TypeLibrary.Create<Mote>( "gambit_mote" );
			mote.Position = Camera.Position + Camera.Rotation.Forward * 100;
			mote.Velocity = Camera.Rotation.Forward * 512;
			mote.Rotation = Rotation.Random;
		}
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
		if(other is Mote mote)
		{
			if(Motes.AddMote())
			{
				mote.Delete();
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
		Log.Info( $"{Client.Pawn} dropped {Motes.Motes} motes" );
		for ( int i = 0; i < Motes.Motes; i++)
		{
			var mote = TypeLibrary.Create<Mote>( "gambit_mote" );
			mote.Position = Position + Position.z * 5;
			mote.Velocity = Camera.Rotation.Up * 512;
			mote.Rotation = Rotation.Random;
		}
			
		Motes.Motes = 0;

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
			//Inventory.Remove();
			//Camera.Remove();

			// Disable all children as well.
			Children.OfType<ModelEntity>()
				.ToList()
				.ForEach( x => x.EnableDrawing = false );

			AsyncRespawn();
		}
	}

	private async void AsyncRespawn()
	{
		await GameTask.DelaySeconds( 3f );
		Respawn();
	}
}
