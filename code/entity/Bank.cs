using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandmbit;

namespace Sandbox.entity
{
	[Library( "gambit_bank" ), HammerEntity]
	[Title( "Bank" ), Category( "Gambit" ), Icon( "place" )]
	[Model]
	public class Bank : InteractibleEntity
	{
		/// <summary>
		/// The team that belongs to the side where the bank is placed
		/// </summary>
		[Property( Title = "Side Team" )]
		public Team Team { get; set; }
		
		public Bank() : base( "models/sambit/bank.vmdl" ) { }

		public override void Spawn()
		{
			SetupPhysicsFromModel( PhysicsMotionType.Static );
			Tags.Add( "solid" );
		}

		public override bool CanInteract( Entity entity )
		{
			if ( entity is Pawn pawn )
			{
				return pawn.Team == Team;
			}

			return false;
		}
	}
}
