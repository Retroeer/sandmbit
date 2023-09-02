using static Sandbox.Event;
using Sandbox;
using Sandmbit;
using System;
using Sandmbit.Mechanics;

namespace Sandmbit.Weapons;

public partial class WeaponViewModel
{
	protected ViewModelComponent ViewModelData => Weapon.GetComponent<ViewModelComponent>();
	protected PrimaryFire PrimaryFireData => Weapon.GetComponent<PrimaryFire>();
	protected AimDownSights AimData => Weapon.GetComponent<AimDownSights>();

	// Fields
	Vector3 SmoothedVelocity;
	Vector3 velocity;
	Vector3 acceleration;
	float VelocityClamp => 20f;
	float walkBob = 0;
	float upDownOffset = 0;
	float sprintLerp = 0;
	float crouchLerp = 0;
	float airLerp = 0;
	float sideLerp = 0;
	float fireLerp = 0;
	float adsLerp = 0;

	float mouseXLerp = 0;
	float mouseYLerp = 0;

	protected float MouseDeltaLerpX;
	protected float MouseDeltaLerpY;

	Vector3 positionOffsetTarget = Vector3.Zero;
	Rotation rotationOffsetTarget = Rotation.Identity;

	Vector3 realPositionOffset;
	Rotation realRotationOffset;

	protected void ApplyPositionOffset( Vector3 offset, float delta )
	{
		var left = Camera.Rotation.Left;
		var up = Camera.Rotation.Up;
		var forward = Camera.Rotation.Forward;

		positionOffsetTarget += forward * offset.x * delta;
		positionOffsetTarget += left * offset.y * delta;
		positionOffsetTarget += up * offset.z * delta;
	}

	private float WalkCycle( float speed, float power, bool abs = false )
	{
		var sin = MathF.Sin( walkBob * speed );
		var sign = Math.Sign( sin );

		if ( abs )
		{
			sign = 1;
		}

		return MathF.Pow( sin, power ) * sign;
	}

	private void LerpTowards( ref float value, float desired, float speed )
	{
		var delta = (desired - value) * speed * Time.Delta;
		var deltaAbs = MathF.Min( MathF.Abs( delta ), MathF.Abs( desired - value ) ) * MathF.Sign( delta );

		if ( MathF.Abs( desired - value ) < 0.001f )
		{
			value = desired;

			return;
		}

		value += deltaAbs;
	}

	private void ApplyDamping( ref Vector3 value, float damping )
	{
		var magnitude = value.Length;

		if ( magnitude != 0 )
		{
			var drop = magnitude * damping * Time.Delta;
			value *= Math.Max( magnitude - drop, 0 ) / magnitude;
		}
	}

	private void ApplyDamping( ref float value, float damping )
	{
		var magnitude = value;

		if ( magnitude != 0 )
		{
			var drop = magnitude * damping * Time.Delta;
			value *= Math.Max( magnitude - drop, 0 ) / magnitude;
		}
	}

	public void AddEffects()
	{
		var player = Weapon.Player;
		var controller = player?.Controller;
		if ( controller == null )
			return;

		SmoothedVelocity += (controller.Velocity - SmoothedVelocity) * 5f * Time.Delta;

		var isGrounded = controller.GroundEntity != null;
		var isShooting = PrimaryFireData.TimeSinceActivated < 0;
		var isAiming = AimData.IsAiming;
		var isSprinting = controller.IsMechanicActive<SprintMechanic>();
		var isCrouching = controller.IsMechanicActive<CrouchMechanic>();

		var left = Camera.Rotation.Left;
		var up = Camera.Rotation.Up;
		var forward = Camera.Rotation.Forward;

		var speed = controller.Velocity.WithZ( 0 ).Length.LerpInverse( 0, 700 );
		var bobSpeed = SmoothedVelocity.Length.LerpInverse( -700, 700 );

		var mouseDeltaX = -Input.MouseDelta.x * Time.Delta * ViewModelData.OverallWeight;
		var mouseDeltaY = -Input.MouseDelta.y * Time.Delta * ViewModelData.OverallWeight;

		LerpTowards( ref fireLerp, isShooting ? 1 : 0, 50f );
		LerpTowards( ref sprintLerp, isSprinting ? 1 : 0, 10f );
		LerpTowards( ref crouchLerp, isCrouching ? 1 : 0, 7f );
		LerpTowards( ref airLerp, isGrounded ? 0 : 1, 10f );
		LerpTowards( ref adsLerp, isAiming ? 1 : 0, 10f * AimData.AimSpeed );

		var leftAmt = left.WithZ( 0 ).Normal.Dot( controller.Velocity.Normal );
		LerpTowards( ref sideLerp, (leftAmt * Input.AnalogMove.Length) * (isSprinting ? 0 : 1), 5f );

		LerpTowards( ref mouseXLerp, mouseDeltaX * 2 * ((isSprinting || isAiming) ? 0f : 1f), 3f );
		LerpTowards( ref mouseYLerp, mouseDeltaY * 2 * ((isSprinting || isAiming) ? 0f : 1f), 3f );

		bobSpeed += sprintLerp * 0.1f;

		if ( isGrounded && acceleration != 0)
		{
			walkBob += (Time.Delta * 30.0f * bobSpeed * (isSprinting ? 1f : 0.75f));
		}

		walkBob %= 3600;

		acceleration += Vector3.Left * mouseDeltaX * -1f;
		acceleration += Vector3.Up * mouseDeltaY * -2f;
		acceleration += -velocity * ViewModelData.WeightReturnForce * Time.Delta;

		// Apply horizontal offsets based on walking direction
		var horizontalForwardBob = WalkCycle( (isAiming ? 0f : 0.5f), 3f ) * speed * ViewModelData.WalkCycleOffset.x * Time.Delta;

		acceleration += forward.WithZ( 0 ).Normal.Dot( controller.Velocity.Normal ) * Vector3.Forward * ViewModelData.BobAmount.x * horizontalForwardBob;

		// Apply left bobbing and up/down bobbing
		acceleration += Vector3.Left * WalkCycle( isSprinting ? 0.4f : 0.5f, 2f ) * speed * ViewModelData.WalkCycleOffset.y * (1 + sprintLerp * 4) * Time.Delta;
		acceleration += Vector3.Up * WalkCycle( isSprinting ? 0.4f : 0.5f, 2f, true ) * speed * ViewModelData.WalkCycleOffset.z * (1 + sprintLerp * 4) *  Time.Delta;
		acceleration += left.WithZ( 0 ).Normal.Dot( controller.Velocity.Normal ) * Vector3.Left * speed * ViewModelData.BobAmount.y * Time.Delta;

		if ( isAiming )
			acceleration *= 0.45f;

		velocity += acceleration * Time.Delta;

		ApplyDamping( ref acceleration, ViewModelData.AccelerationDamping );
		ApplyDamping( ref velocity, ViewModelData.WeightDamping );
		velocity = velocity.Normal * Math.Clamp( velocity.Length, 0, VelocityClamp );

		Position = Camera.Position;
		Rotation = Camera.Rotation;

		positionOffsetTarget = Vector3.Zero;
		rotationOffsetTarget = Rotation.Identity;

		{
			// Global
			rotationOffsetTarget *= Rotation.From( ViewModelData.GlobalAngleOffset + new Angles( Math.Clamp( -mouseYLerp, -5, 5 ), 0, sideLerp * (isAiming ? 0.5f : 4f) + Math.Clamp(mouseXLerp, -5, 5) ));
			positionOffsetTarget += forward * (velocity.x * ViewModelData.VelocityScale + ViewModelData.GlobalPositionOffset.x);
			positionOffsetTarget += left * (velocity.y * ViewModelData.VelocityScale + ViewModelData.GlobalPositionOffset.y);
			positionOffsetTarget += up * (velocity.z * ViewModelData.VelocityScale + ViewModelData.GlobalPositionOffset.z + upDownOffset);

			//Aiming
			rotationOffsetTarget *= Rotation.From( (AimData.AimingAngleOffset - ViewModelData.GlobalAngleOffset) * adsLerp );
			ApplyPositionOffset( AimData.AimingPositionOffset - ViewModelData.GlobalPositionOffset, adsLerp );

			//Idle
			if(!isAiming)
			{
				float breatheTime = RealTime.Now * 4f;
				rotationOffsetTarget *= Rotation.From( new Angles( MathF.Cos( breatheTime / 2.0f ) / 8.0f, 0.0f, 0.0f ) );
				ApplyPositionOffset( new Vector3( MathF.Cos( breatheTime / 8.0f ) / 32.0f, 0.0f, -MathF.Cos( breatheTime / 4.0f ) / 8.0f ), 1 );
			}
			
			// Crouching
			rotationOffsetTarget *= Rotation.From( ViewModelData.CrouchAngleOffset * crouchLerp );
			ApplyPositionOffset( ViewModelData.CrouchPositionOffset, crouchLerp );

			// Air
			rotationOffsetTarget *= Rotation.From( new Angles(-3,0,-5) * airLerp );
			ApplyPositionOffset( new(0,0,-0.25f), airLerp );

			// Sprint
			rotationOffsetTarget *= Rotation.From( ViewModelData.SprintAngleOffset * sprintLerp*10f );
			ApplyPositionOffset( ViewModelData.SprintPositionOffset, sprintLerp*10f );

			// Shooting
			ApplyPositionOffset( new( -0.5f, 0, 0 ), fireLerp );

			// Shooting Camera Rotation
			//var rot = Rotation.From( new Angles( -20f, 0f, 0f ) );
			//rotationOffsetTarget *= rot * fireLerp * 0.3f;
			//Camera.Rotation *= rot * fireLerp * 0.3f;

			// Sprinting Camera Rotation
			float cycle = Time.Now * 10.0f;
			Camera.Rotation *= Rotation.From(
				new Angles(
					MathF.Abs( MathF.Sin( cycle ) * 1.0f ),
					MathF.Cos( cycle ),
					0
				) * sprintLerp * 0.1f );

			//if ( isShooting )
			//{
			//	LerpTowards( ref fireLerp, 5, 10f );
			//	LerpTowards( ref fireLerp, 5, 10f );
			//	positionOffsetTarget += new Vector3( 0, MathX.Lerp(0, -10, Time.Delta*5), 0 );
			//	Log.Info( "Firing" );
			//}

		}

		realRotationOffset = rotationOffsetTarget;
		realPositionOffset = positionOffsetTarget;

		Rotation *= realRotationOffset;
		Position += realPositionOffset;

		Camera.Main.SetViewModelCamera( MathX.Lerp(ViewModelData.ViewModelFOV, AimData.AimFOV, adsLerp), 1, 2048 );
		Camera.FieldOfView = Sandbox.Screen.CreateVerticalFieldOfView( MathX.Lerp( Game.Preferences.FieldOfView, AimData.AimFOV, adsLerp ) );
	}
}
