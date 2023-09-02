using Sandmbit.Mechanics;
using Sandbox;
using System.Collections.Generic;
using Sandbox.entity;

namespace Sandmbit.Weapons;

[Prefab]
public partial class MoteGun : WeaponComponent, ISingletonComponent
{
	protected override bool CanStart( Pawn player )
	{
		if ( Input.Pressed( "attack3" ) ) return true;
		return false;
	}

	protected override void OnStart( Pawn player )
	{
		base.OnStart( player );

		if ( Game.IsServer && Entity.Player.LifeState == LifeState.Alive )
		{
			var mote = TypeLibrary.Create<Mote>( "gambit_mote" );
			mote.Position = Camera.Position + Camera.Rotation.Forward * 100;
			mote.Velocity = Camera.Rotation.Forward * 512;
			mote.Rotation = Rotation.Random;
		}
	}

}
