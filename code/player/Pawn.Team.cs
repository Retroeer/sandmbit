using Sandbox;

namespace Sandmbit;

public partial class Pawn
{
	[Net, Change] public Team Team { get; private set; }

	public void SetTeam( Team team )
	{
		Team = team;
		Game.LocalClient?.SetInt( "team", (int)team );
	}

	protected virtual void OnTeamChanged( Team oldTeam, Team newTeam )
	{
		if ( Game.IsServer )
		{
			Log.Info( $"{Game.LocalPawn.Name} Switched from {oldTeam} to {newTeam}" );
		}
	}
}
