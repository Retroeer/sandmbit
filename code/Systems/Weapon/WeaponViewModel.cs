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
		//if ( Input.Pressed( "attack2" ) )
		//{
		//	var test = Material.Load( "materials/calus-s-selected-weapon.vmat" );
		//	//test.Set( "g_tDiffuse", Texture.Load( FileSystem.Mounted, "models/weapons/seventhseraphcarbine/textures/5e4aeb80_gstack.png" ) );
		//	//test.Set( "GStack", Texture.Load( FileSystem.Mounted, "models/weapons/seventhseraphcarbine/textures/5e4aeb80_gstack.png" ) );
		//	//test.Set( "NormalTexture", Texture.Load( FileSystem.Mounted, "models/weapons/seventhseraphcarbine/textures/5d4aeb80_normal.png" ) );

		//	SetMaterialOverride( test, "shader" );
		//}
		//if (Input.Pressed("duck"))
		//{
		//	var test = Material.Load( "materials/testing-weapon.vmat" );
		//	test.Set( "GStack", Texture.Load( FileSystem.Mounted, "models/weapons/seventhseraphcarbine/textures/5e4aeb80_gstack.png" ) );
		//	SetMaterialOverride( test, "shader" );
		//}
	}

	public override Sound PlaySound( string soundName, string attachment )
	{
		if ( Owner.IsValid() )
			return Owner.PlaySound( soundName, attachment );

		return base.PlaySound( soundName, attachment );
	}
}
