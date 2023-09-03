using Sandbox;
using Editor;
using Sandbox.ModelEditor.Internal;

namespace Sandmbit.Weapons;

[Title( "ViewModel" ), Icon( "pan_tool" )]
public partial class WeaponViewModel : AnimatedEntity
{
	/// <summary>
	/// All active view models.
	/// </summary>
	public static WeaponViewModel Current;

	protected Weapon Weapon { get; init; }

	public WeaponViewModel( Weapon weapon, bool isChild = false )
	{
		if(!isChild)
		{
			if ( Current.IsValid() )
			{
				Current.Delete();
			}
			Current = this;
		}

		EnableShadowCasting = false;
		EnableViewmodelRendering = true;
		Weapon = weapon;
	}

	protected override void OnDestroy()
	{
		Current = null;
	}

	[Event.Client.PostCamera]
	public void PlaceViewmodel()
	{
		if ( Game.IsRunningInVR )
			return;

		Camera.Main.SetViewModelCamera( 65f, 1, 500 );

		AddEffects();
	}

	public override Sound PlaySound( string soundName, string attachment )
	{
		if ( Owner.IsValid() )
			return Owner.PlaySound( soundName, attachment );

		return base.PlaySound( soundName, attachment );
	}
}
