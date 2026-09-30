using System;
using System.Linq;
using System.Windows.Controls;
using HearthMirror.Objects;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;

namespace HstMulligan.Plugin
{
	public class MulliganPlugin : IPlugin
	{
		public string Name => "HST Mulligan V2";

		public string Description => "Mulligan-Guide-Overlay im Stil von Mulligan G-V2, mit eigenen Daten.";

		public string ButtonText => "Einstellungen";

		public string Author => "Mursibert187";

		public Version Version => GetType().Assembly.GetName().Version;

		public MenuItem MenuItem => null!;

		public void OnLoad()
		{
			Watchers.MulliganStateWatcher.Change += OnMulliganStateChange;
			Log.Info($"{Name} {Version} loaded");
		}

		public void OnUnload()
		{
			Watchers.MulliganStateWatcher.Change -= OnMulliganStateChange;
		}

		public void OnButtonPress()
		{
		}

		public void OnUpdate()
		{
		}

		private void OnMulliganStateChange(object sender, MulliganState state)
		{
			var cards = string.Join(", ", state.MulliganCards.Select(c => $"{c.ZonePosition}:{c.CardId}:{c.State}"));
			Log.Info($"Mulligan state: waiting={state.WaitingForUserInput} [{cards}]");
		}
	}
}
