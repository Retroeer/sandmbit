using Sandbox;
using Sandmbit.Mechanics;

namespace Sandmbit.Weapons;

[Prefab]
public partial class Reload : WeaponComponent, ISingletonComponent
{
	protected PrimaryFire PrimaryFireData => Weapon.GetComponent<PrimaryFire>();

	[Net]
	public bool IsReloading { get; set; }

	[Predicted]
	public static float ReloadTime { get; set; } = 0;

	protected override bool CanStart( Pawn player )
	{
		if ( player.LifeState != LifeState.Alive )
			return false;

		var controller = player?.Controller;

		if ( IsReloading || controller.IsMechanicActive<SprintMechanic>() ) return false;
		if ( Weapon.Ammo == Weapon.MaxAmmo || Weapon.ReserveAmmo == 0 ) return false;
		if ( Input.Down( "reload" ) ) return true;
		if ( Weapon.Ammo == 0 && Input.Released( "attack1" ) ) return true;
		return false;
	}

	protected override void OnStart( Pawn player )
	{
		base.OnStart( player );
		DoReload( player );
		DoReloadEffects(To.Single(player));
	}

	protected virtual void DoReload( Pawn player )
	{
		IsReloading = true;

		// Player anim
		player.SetAnimParameter( "b_reload", true );
	}

	[ClientRpc]
	public static void DoReloadEffects()
	{
		Game.AssertClient();
		ReloadTimeToServer( WeaponViewModel.Current.GetSequenceDuration( "reload" ) );
		WeaponViewModel.Current?.SetAnimParameter( "reload", true );
	}

	[ConCmd.Server]
	public static void ReloadTimeToServer( float reloadTime )
	{
		ReloadTime = reloadTime;
	}

	public virtual void OnReloadFinish()
	{
		IsReloading = false;
		int bulletsToReload = Weapon.MaxAmmo - Weapon.Ammo;
		if ( Weapon.ReserveAmmo < bulletsToReload )
		{
			bulletsToReload = Weapon.ReserveAmmo;
		}

		Weapon.Ammo += bulletsToReload;
		Weapon.ReserveAmmo -= bulletsToReload;
	}

	public override void Simulate( IClient cl, Pawn player )
	{
		base.Simulate( cl, player );

		if ( !Weapon.IsValid() )
			return;

		if ( IsReloading )
		{
			var controller = player?.Controller;
			if ( controller.IsMechanicActive<SprintMechanic>() ) //Cancel reload if sprinting
			{
				WeaponViewModel.Current?.SetAnimParameter( "forceIdle", true );
				IsReloading = false;
				return;
			}
			if ( PrimaryFireData.TimeSinceActivated <= 0 ) //Cancel reload if shooting
			{
				IsReloading = false;
				return;
			}
		}

		if ( IsReloading && TimeSinceActivated >= ReloadTime )
		{
			OnReloadFinish();
		}
	}
}
