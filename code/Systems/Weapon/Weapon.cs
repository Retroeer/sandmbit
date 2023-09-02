using Sandbox.UI;
using Sandbox;
using static Sandmbit.Weapons.PrimaryFire;

namespace Sandmbit.Weapons;

[Prefab, Title( "Weapon" ), Icon( "track_changes" )]
public partial class Weapon : AnimatedEntity
{
	protected PrimaryFire PrimaryFireData => GetComponent<PrimaryFire>();

	// Won't be Net eventually, when we serialize prefabs on client
	[Net, Prefab, Category( "Animation" )] public WeaponHoldType HoldType { get; set; } = WeaponHoldType.Pistol;
	[Net, Prefab, Category( "Animation" )] public WeaponHandedness Handedness { get; set; } = WeaponHandedness.Both;
	[Net, Prefab, Category( "Animation" )] public float HoldTypePose { get; set; } = 0;

	[Net, Prefab, Category( "General" )] public Ammo_Type AmmoType { get; set; }
	public enum Ammo_Type
	{
		Primary,
		Special,
		Heavy
	}

	[Net, Prefab, Category( "General" )] public int Ammo { get; set; }
	[Net, Prefab, Category( "General" )] public int ReserveAmmo { get; set; }
	[Net, Prefab, Category( "General" ), ResourceType("png")] public string IconPath { get; set; }

	public AnimatedEntity EffectEntity => ViewModelEntity.IsValid() ? ViewModelEntity : this;
	public WeaponViewModel ViewModelEntity { get; protected set; }
	public Pawn Player => Owner as Pawn;

	[Net] public int MaxAmmo { get; set; }

	public override void Spawn()
	{
		MaxAmmo = Ammo;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		EnableDrawing = false;
	}

	/// <summary>
	/// Can we holster the weapon right now? Reasons to reject this could be that we're reloading the weapon..
	/// </summary>
	/// <returns></returns>
	public bool CanHolster( Pawn player )
	{
		return true;
	}

	/// <summary>
	/// Called when the weapon gets holstered.
	/// </summary>
	public void OnHolster( Pawn player )
	{
		EnableDrawing = false;
		
		if ( Game.IsServer )
			DestroyViewModel( To.Single( player ) );
	}

	/// <summary>
	/// Can we deploy this weapon? Reasons to reject this could be that we're performing an action.
	/// </summary>
	/// <returns></returns>
	public bool CanDeploy( Pawn player )
	{
		return true;
	}

	/// <summary>
	/// Called when the weapon gets deployed.
	/// </summary>
	public void OnDeploy( Pawn player )
	{
		SetParent( player, true );
		Owner = player;

		EnableDrawing = true;

		if ( Game.IsServer )
			CreateViewModel( To.Single( player ) );
	}

	[ClientRpc]
	public void CreateViewModel()
	{
		if ( GetComponent<ViewModelComponent>() is not ViewModelComponent comp ) return;

		var vm = new WeaponViewModel( this );
		vm.Model = Model.Load( comp.ViewModelPath );

		//var arms = new WeaponViewModel( this, true );
		//arms.Model = Model.Load( "models/arms/v_warlock_arms.vmdl" );
		//arms.SetParent( vm, true );

		//var test = Material.Load( "materials/calus-s-selected-weapon.vmat" );
		//test.Set( "g_tDiffuse", Texture.Load( FileSystem.Mounted, "models/weapons/seventhseraphcarbine/textures/5e4aeb80_gstack.png" ) );
		//test.Set( "GStackTexture", Texture.Load( FileSystem.Mounted, "models/weapons/seventhseraphcarbine/textures/5e4aeb80_gstack.png" ) );
		//test.Set( "NormalTexture", Texture.Load( FileSystem.Mounted, "models/weapons/seventhseraphcarbine/textures/5d4aeb80_normal.png" ) );

		//vm.SetMaterialOverride( test, "shader" );
		//vm.SceneObject.Attributes.Set( "g_tDiffuse", Texture.Load( FileSystem.Mounted, "models/weapons/seventhseraphcarbine/textures/5e4aeb80_gstack.png" ) );

		ViewModelEntity = vm;
		ViewModelEntity.SetAnimParameter( "deploy", true );
	}

	[ClientRpc]
	public void DestroyViewModel()
	{
		if ( ViewModelEntity.IsValid() )
		{
			ViewModelEntity.Delete();
		}
	}

	public override void Simulate( IClient cl )
	{
		SimulateComponents( cl );
	}

	protected override void OnDestroy()
	{
		ViewModelEntity?.Delete();
	}

	public override string ToString()
	{
		return $"Weapon ({Name})";
	}
}

/// <summary>
/// Describes the holdtype of a weapon, which tells our animgraph which animations to use.
/// </summary>
public enum WeaponHoldType
{
	None,
	Pistol,
	Rifle,
	Shotgun,
	Item,
	Fists,
	Swing
}

/// <summary>
/// Describes the handedness of a weapon, which hand (or both) we hold the weapon in.
/// </summary>
public enum WeaponHandedness
{
	Both,
	Right,
	Left
}

