using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

//Most of this code is from Conquest, made by Devultj
//https://github.com/DevulTj/sbox-conquest

namespace Sandmbit;

public partial class SandmbitScores : BaseNetworkable, INetworkSerializer
{

    // [Net, Change] 
	// public int test { get; set; }

	// public void OntestChanged( int oldValue, int newValue )
	// {
	// 	Log.Info( $"changed from {oldValue} to {newValue}" );
	// }

	public SandmbitScores()
	{	
		Current = this;
		Scores = new int[ ArraySize ];
	}

	private int MaximumScore { get; set; }

	public static SandmbitScores Current { get; set; }

	protected static int ArraySize => Enum.GetNames( typeof( Team ) ).Length;
	protected int[] Scores { get; set; }
	protected int[] OldScores { get; set; }
	public void Initialize()
	{
		Current = this;
		SetScore( Team.Alpha, 0 );
		SetScore( Team.Bravo, 0 );
		SetMaxScore( MaximumScore );
		//Scores.Clear();
	}

	public void SetScore( Team team, int score )
	{
		var newScore = Math.Clamp( score, 0, GetMaxScore() );
		Scores[(int)team] = newScore;
		OldScores = new int[ ArraySize ];

		WriteNetworkData();
	}

	public void SetMaxScore(int score )
	{
		MaximumScore = score;
	}

	public void Add( Team team, int score )
	{
		SetScore( team, Get( team ) + score );
	}

	//Get method for the scores
	public int Get( Team team )
	{
		return Scores[(int)team];
	}

	public int GetMaxScore()
	{
		return MaximumScore;
	}
	
	public void Reset()
	{		
		Initialize();
	}

	public int? GetOldScore( Team team ) => OldScores?[(int)team];

	public int GetScore( Team team )
	{
		return Scores[(int)team];
	}

	public void AddScore( Team team, int score )
	{
		SetScore( team, GetScore( team ) + score );
	}

	public void RemoveScore( Team team, int score )
	{
		SetScore( team, GetScore( team ) - score );
	}

	public void Read( ref NetRead read )
	{
		OldScores = Scores?.ToArray();

		Scores = new int[ ArraySize ];

		int count = read.Read<int>();
		for ( int i = 0; i < count; i++ )
		{
			Scores[ i ] = read.Read<int>();
		}
		//MaximumScore = read.Read<int>();
		
		Event.Run( SandmbitGameEvent.Shared.OnScoresChanged );
	}

	public void Write( NetWrite write )
	{
		write.Write( Scores.Length );

		foreach( var score in Scores )
			write.Write( score );
	}
}
