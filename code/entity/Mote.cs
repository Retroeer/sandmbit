using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using MyGame.common;

namespace Sandbox.entity
{
	[Library( "gambit_mote", Title = "Mote" )]
	class Mote : ModelEntity
	{
		[BindComponent] public SelfDestruct SelfDestruct { get; }

		public override void Spawn()
		{
			Log.Info( "Summoned mote" );
			Model = Cloud.Model( "destiny.gambit_mote" );
			Components.Add( new SelfDestruct( 15 ) );

			// Always network this entity to all clients
			Transmit = TransmitType.Always;

			Scale = 1.25f;
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

			EnableAllCollisions = true;
			EnableSolidCollisions = true;
			PhysicsEnabled = true;
			UsePhysicsCollision = true;

			Tags.Add( "trigger" );

			PointLightEntity moteGlow = new();
			moteGlow.Parent = this;
			moteGlow.Brightness = 0.1f;
			moteGlow.Range = 128f;
			moteGlow.Color = Color.White;
		}
	}
}
