using Sandbox.player;
using Sandmbit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.player
{
	public enum BlockerType
	{
		None,
		Small,
		Medium,
		Large,
		Giant
	}

	public static class BlockerTypeMethods
	{
		public static char Glyph( this BlockerType type )
		{
			switch ( type )
			{
				case BlockerType.None: return ' ';
				case BlockerType.Small: return '\uE053';
				case BlockerType.Medium: return '\uE054';
				case BlockerType.Large: return '\uE055';
				case BlockerType.Giant: return '\uE068';
				default: return '?';
			}
		}
	}

	public partial class MoteBag : EntityComponent
	{
		public int Motes { get; set; }
		public int MaxMotes = 20;
		
		/// <summary>
		/// Get the largest blocker that can be summoned with the contents of this mote bag
		/// </summary>
		public BlockerType AffordableBlocker()
		{
			switch (Motes)
			{
				case int i when i >= 5 && i < 10: return BlockerType.Small;
				case int i when i >= 10 && i < 15: return BlockerType.Medium;
				case int i when i >= 15 && i < 20: return BlockerType.Large;
				case int i when i >= 20: return BlockerType.Giant;
				default: return BlockerType.None;
			}
		}

		public bool AddMote()
		{
			if ((Motes+1) <= MaxMotes)
			{
				Motes++;
				Log.Info( $"picked up mote (have {Motes}/{MaxMotes}, blocker={AffordableBlocker()})" );
				Game.RootPanel.StateHasChanged();
				return true;
			}
			else
				return false;
		}

		//public void SetScore( Team team, int score )
		//{
		//	var newScore = Math.Clamp( score, 0, GetMaxScore() );
		//	Scores[(int)team] = newScore;
		//	OldScores = new int[ArraySize];

		//	WriteNetworkData();
		//}
	}
}
