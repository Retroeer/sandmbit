
using Sandbox;
using Sandbox.MenuSystem;
using System;
using System.Linq;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace Sandmbit;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class SandmbitGame : Sandbox.GameManager
{
	/// <summary>
	/// Called when the game is created (on both the server and client)
	/// </summary>
	public SandmbitGame()
	{
		if ( Game.IsClient )
		{
			Game.RootPanel = new Hud();
		}
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new Pawn();
		client.Pawn = pawn;
		pawn.Respawn();
		pawn.DressFromClient( client );

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}

		//Randomly set the players team if both teams are not filled
		Random random = new Random();
		Team randomTeam = (Team)random.Next( 1, 2 );

		int playerCount = Entity.All.OfType<Pawn>().Count();

		int inAlpha = TeamExtensions.GetCount( Team.Alpha );
		int inBravo = TeamExtensions.GetCount( Team.Bravo );

		if ( inAlpha >= playerCount / 2 )
			pawn.SetTeam( Team.Bravo );
		else if ( inBravo >= playerCount / 2 )
			pawn.SetTeam( Team.Alpha );
		else
			pawn.SetTeam( randomTeam );

		Log.Info( $"{pawn.Client.Name} is on {pawn.Team}" );
	}

	[ConCmd.Server( "kill" )]
	public static void DoSuicide()
	{
		(ConsoleSystem.Caller.Pawn as Pawn)?.TakeDamage( DamageInfo.Generic( 1000f ) );
	}
}

