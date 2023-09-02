using Sandbox;

namespace Sandmbit.Weapons;

[Prefab]
public partial class ViewModelComponent : WeaponComponent, ISingletonComponent
{
	// I know that there's a metric fuck ton of Net properties here..
	// ideally, when the prefab gets set up, we'd send the client a message with the prefab's name
	// so we can populate all the Prefab marked properties with their defaults.

	//// General
	[Net, Prefab, ResourceType( "vmdl" )] public string ViewModelPath { get; set; }
	[Net, Prefab, ResourceType( "vmdl" )] public string ViewModelHandsPath { get; set; }

	[Net, Prefab] public float OverallWeight { get; set; }
	[Net, Prefab] public float WeightReturnForce { get; set; }
	[Net, Prefab] public float WeightDamping { get; set; }
	[Net, Prefab] public float AccelerationDamping { get; set; }
	[Net, Prefab] public float VelocityScale { get; set; }

	//// Walking & Bob
	[Net, Prefab, Category( "Walking" )] public Vector3 WalkCycleOffset { get; set; }
	[Net, Prefab, Category( "Walking" )] public Vector2 BobAmount { get; set; }

	//// Global
	[Net, Prefab, Category( "General" )] public Vector3 GlobalPositionOffset { get; set; }
	[Net, Prefab, Category( "General" )] public Angles GlobalAngleOffset { get; set; }
	[Net, Prefab, Category( "General" )] public float ViewModelFOV { get; set; }

	//// Crouching
	[Net, Prefab, Category( "Crouching" )] public Vector3 CrouchPositionOffset { get; set; }
	[Net, Prefab, Category( "Crouching" )] public Angles CrouchAngleOffset { get; set; }

	//// Sprint
	[Net, Prefab, Category( "Sprinting" )] public Vector3 SprintPositionOffset { get; set; } = new Vector3( 0, 0, 0 );
	[Net, Prefab, Category( "Sprinting" )] public Angles SprintAngleOffset { get; set; } = new Angles( 0, 0, 0 );
}
