using Sandbox;

namespace MyGame.common;

public class SelfDestruct : EntityComponent
{
	public float Lifetime { get; set; }

	public SelfDestruct()
	{
		Lifetime = 5;
	}
	
	public SelfDestruct( float lifetime )
	{
		Lifetime = lifetime;
	}


	[GameEvent.Tick.Server]
	public void OnTick()
	{
		Lifetime -= Time.Delta;
		if ( Lifetime <= 0 )
		{
			Entity.Delete();
		}
	}
}
