using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Sandmbit;
using Sandmbit.common;

namespace Sandbox.entity
{
	[Library( "gambit_mote", Title = "Mote" )]
	class Mote : ModelEntity
	{
		[BindComponent] public SelfDestruct SelfDestruct { get; }

		public override void Spawn()
		{
			Model = Cloud.Model( "destiny.gambit_mote" );
			Components.Add( new SelfDestruct( 20 ) );

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
			moteGlow.Color = GameConfig.RainbowMotes ? new ColorHsv(Random.Shared.NextSingle() * 360, 1, 1).ToColor() : Color.White;
		}

		[Sandbox.GameEvent.PreRender]
		public void BeforeRender()
		{
			if ( SelfDestruct.Lifetime.Relative <= 5 )
			{
				EnableDrawing = (SelfDestruct.Lifetime.Relative % 0.25) > 0.125;
			}
			else
			{
				EnableDrawing = true;
			}
		}
	}
}
