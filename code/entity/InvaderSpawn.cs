using Editor;
using Sandmbit;

namespace Sandbox.entity;

[Library( "gambit_invader_spawn" ), HammerEntity]
[Title( "Invader Spawn" ), Category( "Gambit" ), Icon( "place" )]
public class InvaderSpawn : Entity
{
	/// <summary>
	/// The team that belongs to the side where the spawn is placed
	/// NOT the team of the invader
	/// </summary>
	[Property( Title = "Side Team" )]
	public Team Team { get; set; }
}
