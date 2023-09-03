using Sandbox;

namespace Sandmbit.common;

public class SelfDestruct : EntityComponent
{
	public TimeUntil Lifetime { get; set; }

	public SelfDestruct()
	{
		Lifetime = 5;
	}
	
	public SelfDestruct( float lifetime )
	{
		Lifetime = lifetime;
	}


	[Sandbox.GameEvent.Tick.Server]
	public void OnTick()
	{
		if ( Lifetime <= 0 )
		{
			Entity.Delete();
		}
	}
}
