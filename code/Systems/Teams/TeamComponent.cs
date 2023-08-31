using Sandbox;

//Most of the Team code here is from Conquest, made by Devultj
//https://github.com/DevulTj/sbox-conquest

namespace Sandmbit;

public partial class TeamComponent : EntityComponent
{
	[Net] public Team Team { get; set; } = Team.None;
}
