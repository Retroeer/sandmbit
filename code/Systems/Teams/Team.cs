using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

//Most of the Team code here is from Conquest, made by Devultj
//https://github.com/DevulTj/sbox-conquest

namespace Sandmbit;

public enum Team
{
	None,
	Alpha,
	Bravo
}

public static class TeamExtensions
{
	public static string GetHudClass( this Team team )
	{
		if ( team == Team.Bravo )
			return "team_blue";
		else if ( team == Team.Alpha )
			return "team_red";
		else
			return "team_none";
	}

	public static Color GetColor( this Team team )
	{
		if ( team == Team.Bravo )
			return new Color( 30f / 255f, 136f / 255f, 223f / 255f );
		else if ( team == Team.Alpha )
			return new Color( 194f / 255f, 0f, 0f );
		else
			return new Color( 1f, 1f, 0f );
	}

	public static string GetName( this Team team )
	{
		if ( team == Team.Bravo )
			return "Bravo";
		else if ( team == Team.Alpha )
			return "Alpha";
		else
			return "Neutral";
	}

	public static To GetTo( this Team team )
	{
		return To.Multiple( team.GetAll().Select( e => Game.LocalClient.Client ) );
	}

	public static IEnumerable<Pawn> GetAll( this Team team )
	{
		return Entity.All.OfType<Pawn>().Where( e => e.Team == team );
	}

	public static int GetCount( this Team team )
	{
		return Entity.All.OfType<Pawn>().Where( e => e.Team == team ).Count();
	}

	public static Team GetLowestCount( this Team team )
	{
		var AlphaCount = GetCount( Team.Alpha );
		var BravoCount = GetCount( Team.Bravo );

		if ( BravoCount < AlphaCount )
			return Team.Bravo;

		return Team.Alpha;
	}
}

public static class TeamSystem
{
	public static T ToEnum<T>( this string enumString )
	{
		return (T) Enum.Parse( typeof( T ), enumString );
	}

	public static Team MyTeam => Game.LocalClient.Client.Components.Get<TeamComponent>()?.Team ?? Team.None;

	public enum FriendlyStatus
	{
		Friendly,
		Hostile,
		Neutral
	}

	public static FriendlyStatus GetFriendState( Team one, Team two )
	{
		if ( one == Team.None || two == Team.None )
			return FriendlyStatus.Neutral;

		if ( one != two )
			return FriendlyStatus.Hostile;

		return FriendlyStatus.Friendly;
	}

	public static bool IsFriendly( Team one, Team two )
	{
		return GetFriendState( one, two ) == FriendlyStatus.Friendly;
	}

	public static bool IsHostile( Team one, Team two )
	{
		return GetFriendState( one, two ) == FriendlyStatus.Hostile;
	}

	public static Team GetEnemyTeam( Team team )
	{
		switch( team )
		{
			case Team.Alpha:
				return Team.Bravo;
			case Team.Bravo:
				return Team.Alpha;
		}

		return Team.None;
	}

	public static string GetTeamName( Team team )
	{
		switch( team )
		{
			case Team.Alpha:
				return "Alpha";
			case Team.Bravo:
				return "Bravo";
		}

		return "NOBODY";
	}

	public static Team GetTeam( IClient cl )
	{
		return cl.Components.Get<TeamComponent>()?.Team ?? Team.None;
	}
}

