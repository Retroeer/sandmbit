using Sandbox;
using Sandmbit;
using Sandmbit.Mechanics;
using static Sandbox.Event;

namespace Sandmbit.Weapons;

[Prefab]
public partial class AimDownSights : WeaponComponent, ISingletonComponent
{
	protected Reload ReloadData => Weapon.GetComponent<Reload>();
	protected PlayerController Controller;

	[Net, Prefab, Category( "General" )] public Vector3 AimingPositionOffset { get; set; }
	[Net, Prefab, Category( "General" )] public Angles AimingAngleOffset { get; set; }
	[Net, Prefab, Category( "General" )] public float AimFOV { get; set; }
	[Net, Prefab, Category( "General" )] public float AimSpeed { get; set; }
	[Net, Prefab, Category( "Animations" )] public bool AnimateInADS { get; set; }
	[Net, Prefab, Category( "Developer" )] private bool DebugAim { get; set; }

	[Net]
	public bool IsAiming { get; set; }

	protected override bool CanStart( Pawn player )
	{
		if ( player.LifeState != LifeState.Alive )
			return false;

		Controller = player?.Controller;
		
		if ( IsAiming || Controller.IsMechanicActive<SprintMechanic>() || ReloadData.IsReloading ) return false;
		if ( Input.Down( "attack2" ) || DebugAim) return true;
		return false;
	}

	protected override void OnStart( Pawn player )
	{
		base.OnStart( player );
		IsAiming = true;
	}

	protected virtual void DoADS( Pawn player )
	{
		// Player anim
		player.SetAnimParameter( "b_reload", true );
	}

	public virtual void OnADSFinish()
	{
		
	}

	public override void Simulate( IClient cl, Pawn player )
	{
		base.Simulate( cl, player );

		if ( !Weapon.IsValid() )
			return;
		//if(IsAiming)
		//	Log.Info( IsAiming );

		if( IsAiming && (!Input.Down("attack2") || Controller.IsMechanicActive<SprintMechanic>() || ReloadData.IsReloading) )
		{
			if(!DebugAim)
				IsAiming = false;
		}
	}
}
