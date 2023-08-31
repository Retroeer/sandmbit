using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.entity
{
	[Library( "gambit_bank" ), HammerEntity]
	[Title( "Bank" ), Category( "Gambit" ), Icon( "place" )]
	[Model]
	public class Bank : InteractibleEntity
	{
		public Bank() : base( "models/sambit/bank.vmdl" ) { }

		public override void Spawn()
		{
			SetupPhysicsFromModel( PhysicsMotionType.Static );
			Tags.Add( "solid", "playerclip" );
		}
	}
}
