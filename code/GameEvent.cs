using Sandbox;

namespace Sandmbit;

public class SandmbitGameEvent
{
	public class Server
	{
		public const string OnChatMessage = "OnChatMessage";
		public const string ScoreHitZero = "ScoreHitZero";

		public class OnChatMessageAttribute : EventAttribute
		{
			public OnChatMessageAttribute() : base( OnChatMessage ) { }
		}

		public class ScoreHitZeroAttribute : EventAttribute
		{
			public ScoreHitZeroAttribute() : base( ScoreHitZero ) { }
		}
	}

	public class Client
	{

	}

	public class Shared
	{
		public const string OnScoresChanged = "OnScoresChanged";

		public class OnScoresChangedAttribute : EventAttribute
		{
			public OnScoresChangedAttribute() : base( OnScoresChanged ) { }
		}
	}
}
