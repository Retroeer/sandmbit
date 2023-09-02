using Sandbox;

namespace Sandmbit;

public partial class BasicWeaponViewModel : BaseViewModel
{
	protected BasicWeapon Weapon { get; init; }

	public BasicWeaponViewModel( BasicWeapon weapon )
	{
		Weapon = weapon;
		EnableShadowCasting = false;
		EnableViewmodelRendering = true;
	}

	public override void PlaceViewmodel()
	{
		base.PlaceViewmodel();

		Camera.Main.SetViewModelCamera( 80f, 1, 500 );
	}
}
