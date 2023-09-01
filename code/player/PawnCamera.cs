using Sandbox;
using Sandbox.Component;

namespace Sandmbit;

public partial class PlayerCamera : EntityComponent<Pawn>, ISingletonComponent
{
	bool IsThirdPerson { get; set; } = false;

	public virtual void Update( Pawn player )
	{
		Camera.Rotation = player.ViewAngles.ToRotation();
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
		Camera.FirstPersonViewer = player;
		Camera.ZNear = 0.5f;

		// Post Processing
		//var pp = Camera.Main.FindOrCreateHook<Sandbox.Effects.ScreenEffects>();
		//pp.Sharpen = 0.05f;
		//pp.Vignette.Intensity = 0.60f;
		//pp.Vignette.Roundness = 1f;
		//pp.Vignette.Smoothness = 0.3f;
		//pp.Vignette.Color = Color.Black.WithAlpha( 1f );
		//pp.MotionBlur.Scale = 0f;
		//pp.Saturation = 1f;
		//pp.FilmGrain.Response = 1f;
		//pp.FilmGrain.Intensity = 0.01f;

		if ( Input.Pressed( "view" ) )
		{
			IsThirdPerson = !IsThirdPerson;
		}

		if ( IsThirdPerson )
		{
			Vector3 targetPos;
			var pos = player.Position + Vector3.Up * 64;
			var rot = Camera.Rotation * Rotation.FromAxis( Vector3.Up, -16 );

			float distance = 80.0f * player.Scale;
			targetPos = pos + rot.Right * ((player.CollisionBounds.Mins.x + 50) * player.Scale);
			targetPos += rot.Forward * -distance;

			var tr = Trace.Ray( pos, targetPos )
				.WithAnyTags( "solid" )
				.Ignore( player )
				.Radius( 8 )
				.Run();

			Camera.FirstPersonViewer = null;
			Camera.Position = tr.EndPosition;
		}
		else
		{
			Camera.FirstPersonViewer = player;
			Camera.Position = player.EyePosition;

			var tr = Trace.Ray( Camera.Position, Camera.Rotation.Forward * 10000 )
				.WithAnyTags( "world", "player" )
				.Ignore( player )
				.Radius( 8 )
				.Run();

			//If the player is looking at an enemy, make them glow
			if ( tr.Entity?.ClassName == "Pawn" )
			{
				var enemy = tr.Entity as Pawn;
				var glow = enemy.Components.GetOrCreate<Glow>();
				if ( TeamSystem.IsHostile( player.Team, enemy.Team ) )
				{
					glow.Enabled = true;
					glow.Width = 0.25f;
					glow.Color = new Color( 1.0f, 0.0f, 0.0f, 0.5f );
					glow.ObscuredColor = new Color( 1.0f, 0.0f, 0.0f, 1.0f );
					glow.InsideColor = new Color( 1.0f, 0.0f, 0.0f, 0.1f );
				}
			}
			else
			{
				//Remove the glow when looking away
				foreach ( var pawn in TeamExtensions.GetAll( TeamSystem.GetEnemyTeam( player.Team ) ) )
				{
					pawn.Components.Remove( pawn.Components.Get<Glow>() );
				}
			}
		}
	}
}
