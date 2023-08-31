using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.entity
{
	[Library( "gambit_motedispenser" ), HammerEntity]
	[Title( "Mote Dispenser" ), Category( "Gambit" ), Icon( "place" )]
	[Model]
	public class MoteDispenser : Entity
	{
		/// <summary>
		/// Amount of motes to dispense
		/// </summary>
		[Property( Title = "Amount" )]
		public int Amount { get; set; } = 3;

		[Input( Name = "Dispense" )]
		public void Dispense()
		{
			for ( int i = 0; i < Amount; i++ )
			{
				var mote = TypeLibrary.Create<Mote>( "gambit_mote" );
				mote.Position = Position;
				mote.Rotation = Rotation.Random;
			}
		}
	}
}
