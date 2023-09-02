using Sandbox;

namespace Sandmbit.Mechanics;

public enum JumpMode
{
	Normal,
	Double,
	Glide
}

public struct JumpSettings
{
	public JumpMode Mode;
	public TimeSince timeSinceGlide;
	public float FloatDuration;
	public float FloatSpeed;
	public float FloatDownDuration;
}

/// <summary>
/// The jump mechanic for players.
/// </summary>
public partial class JumpMechanic : PlayerControllerMechanic
{
	public override int SortOrder => 25;
	private float Gravity => 700f;

	//[Net]
	private bool CanDoubleJump { get; set; }

	//[Net, Predicted]
	private bool JumpInitiated { get; set; }

	//Double Jump
	//[Net]
	private int numJumps { get; set; } = 0;
	//[Net]
	private int maxJumps { get; set; } = 2;

	//Glide
	//[Net]
	private TimeSince floatUpTime { get; set; } = 3f;
	//[Net]
	private TimeSince floatDownTime { get; set; } = 3f;

	private JumpMode jumpType = JumpMode.Double;

	protected override bool ShouldStart()
	{
		if ( Controller.GroundEntity.IsValid() )
		{
			JumpInitiated = false;
			CanDoubleJump = true;
			switch ( jumpType)
			{
				case JumpMode.Normal:

					break;
				case JumpMode.Double:
					numJumps = 0;
					break;
				case JumpMode.Glide:
					floatUpTime = 0;
					break;
				default:
					// code block
					break;
			}
		}

		if ( !Input.Pressed( InputButton.Jump ) ) return false;
		if ( Controller.GroundEntity.IsValid() )
		{
			return true;
		}
		else if ( CanDoubleJump )
		{
			// Player is not on the ground, but can double jump
			if( numJumps >= maxJumps-1)
				CanDoubleJump = false;

			return true;
		}
		return false;
	}

	protected override void OnStart()
	{
		float flGroundFactor = 1.0f;
		float flMul = 300f;
		float startz = Velocity.z;

		if ( Controller.GroundEntity.IsValid() )
		{
			// Regular jump
			Velocity = Velocity.WithZ( startz + flMul * flGroundFactor );
		}
		else
		{
			JumpInitiated = true;
			if ( jumpType == JumpMode.Double )
			{
				// Double jump
				Velocity = Velocity.WithZ( flMul * 1.5f * flGroundFactor );

				// Add velocity based on player's input and direction
				if ( Input.AnalogMove != Vector3.Zero )
				{
					// Add leftward velocity to the jump
					Velocity += Controller.GetWishVelocity( true ) * 1.25f;
				}

				Entity.PlaySound( "water_bullet_impact" );
				var particles = Particles.Create( "particles/impact.generic.vpcf", Entity.Position );
				particles.SetOrientation( 0, Rotation.FromPitch( 90 ) );

				numJumps++;
			}
			else if ( jumpType == JumpMode.Glide )
			{
				if ( JumpInitiated )
					floatUpTime = 0;
			}
		}

		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;

		Controller.GetMechanic<WalkMechanic>().ClearGroundEntity();
	}
}
