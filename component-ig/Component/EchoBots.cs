using System;
using xmpponent.Stanzas;
using System.IO;
using System.Collections.Generic;

namespace xmpponent
{
	public class EchoBots : Component
	{
		public List<string> Subscribers = new List<string>();
		DateTime UnixEpoc;
		int LastUpdate;

		public EchoBots ()
		{
		}

		public override void onLoad ()
		{
			//	Load things and initialize them here.

			AutoSendReceipt = true;

			//	Load a list of users subscribed to the echo bot.
			if(File.Exists("echobots.txt"))
			{
				StreamReader file = new StreamReader("echobots.txt");
				string line;
				while((line = file.ReadLine()) != null)
				{
					line = line.Trim();
					if(line != String.Empty && !Subscribers.Contains(line)) { Subscribers.Add(line); }
				}

				file.Close();
			}

			UnixEpoc = new DateTime(1970, 1, 1);

		}

		public override void onConnect ()
		{
			//	This is where the xmpp server should be alerted that
			//	there are a few bots online.

			Stanzas.Presence presence = new Presence();
			foreach(string subscriber in Subscribers)
			{
				presence.Available = true;
				presence.From = "echo@" + ComponentAddress;
				presence.To = subscriber;
				presence.pType = "";
				presence.Id = "doo-comp-" + StanzaID.ToString();
				presence.ShowType = Presence.ShowAvailable;
				presence.Status = "I'm a copy cat!";

				StanzaID++;

				SendPresence(presence);
			}
				
		}

		public override void onUpdate ()
		{
			//	Update things here.
			Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(UnixEpoc)).TotalSeconds;
			if(LastUpdate == 0) { LastUpdate = unixTimestamp; }

			if(unixTimestamp - LastUpdate > 300)
			{
				//	Update all subscribers every so often about the echo bot's
				//	presence.
				DebugInfo("::Sending presence status to all subscribers.");
				LastUpdate = unixTimestamp;

				Presence presence = new Presence();
				presence.Available = true;
				presence.pType = "";
				presence.ShowType = Presence.ShowAvailable;
				presence.Status = "Stop copying me";
				presence.From = "echo@" + ComponentAddress.ToLower();

				foreach(string subscriber in Subscribers)
				{
					DebugWrite(String.Format("::> {0}", subscriber));
					presence.To = subscriber;
					presence.Id = Stanza.NextStanzaID;
					SendPresence(presence);
				}
			}
		}

		public override void onMessageReceived (xmpponent.Stanzas.Message message)
		{
			DebugInfo(String.Format("{0} => {1}: {2}", message.From, message.To, message.Body));

			if(message.To.ToLower() == "echo@" + ComponentAddress.ToLower()
				|| message.To.ToLower().StartsWith("echo@" + ComponentAddress.ToLower() + "/"))
			{
				string newfrom = message.To;
				message.To = message.From;
				message.From = newfrom;// + "/bot";
				message.StanzaBy = newfrom;

				SendMessage(message);


			}
		}

		public override void onPresenceReceived (xmpponent.Stanzas.Presence presence)
		{
			//	The base method routes presence stanzas to the appropriate
			//	callbacks.  So, unless you have a reason, there is really
			//	not a point in overriding this method.

			DebugInfo(String.Format("{0}: [Presence] {1}", presence.From, presence.pType));

			base.onPresenceReceived(presence);
		}

		public override void onPresenceProbe (Presence presence)
		{
			DebugInfo(String.Format("[{0}] is probing for [{1}]'s presence.", presence.From, presence.To));
			//DebugInfo(presence.RawXML);
			//DebugInfo("--------------------");

			Presence npres = new Presence();
			npres.From = presence.To;
			npres.To = presence.From;
			npres.Id = presence.Id;
			npres.pType = "";
			npres.ShowType = Presence.ShowChat;
			npres.Status = "beep bloop";
			npres.Available = true;


			SendPresence(npres);
		}

		public override void onPresenceAvailable (Presence presence)
		{
			DebugInfo(String.Format("[{0}] Available: {1} {2}", presence.From, presence.ShowToString(), presence.Status));

			if(presence.To.ToLower().StartsWith("echo@" + ComponentAddress))
			{
				Presence npres = new Presence();
				npres.From = presence.To;
				npres.To = presence.From;
				npres.Id = presence.Id;
				npres.pType = "";
				npres.ShowType = Presence.ShowChat;
				npres.Status = "beep bloop";
				npres.Available = true;


				SendPresence(npres);
			}
		}

		public override void onPresenceUnavailable (Presence presence)
		{
			DebugInfo(String.Format("[{0}] Unavailable", presence.From));
		}

		public override void onUnknownPresenceReceived (Presence presence)
		{
			DebugInfo(String.Format("[{0}] Unknown Presence", presence.From));

			//DebugInfo(String.Format("[{0}] Unknown Presence Received:", presence.From));
			//DebugInfo(presence.RawXML);
			//DebugInfo("--------------------");
		}

		public override void onInfoQueryReceived (xmpponent.Stanzas.InfoQuery iq)
		{
			//	The base method for onInfoQueryReceived does the routing for
			//	appropriate callbacks.  So this method doesn't need to be
			//	overridden unless you have a specific need for it.
			base.onInfoQueryReceived(iq);
		}

		public override void onVCardQueryReceived (InfoQuery iq)
		{
			//	xmpponent currently does not have built in support for vCards,
			//	but you could bake your own support in here.
			DebugInfo(String.Format("[{0}] requests a vCard for [{1}]", iq.From, iq.To));
		}

		public override void onDiscoQueryReceived (InfoQuery iq)
		{
			//	xmpponent currently does not have built in support for disco
			//	queries, but it could be built here.
			DebugInfo(String.Format("[{0}] requests disco information", iq.From));
		}

		public override void onMessageReceiptReceived (Message message)
		{
			//	Nothing really needs to be done with these.
			//	Note that this is a 'message' and not a 'receipt' type.
		}

		public override void onSubscribeRequested (Presence presence)
		{
			if(presence.To.ToLower().StartsWith("echo@" + ComponentAddress))
			{
				DebugInfo(String.Format("[{0}] requested a subscription to {1}", presence.From, presence.To));

				if(!Subscribers.Contains(presence.From)) { Subscribers.Add(presence.From); }
				string newfrom = presence.To;
				presence.To = presence.From;
				presence.From = newfrom;
				presence.pType = "subscribed";

				SendPresence(presence);

				//	Now request a subscription from the user that just subscribed to the bot
				presence.pType = "subscribe";
				SendPresence(presence);
			}

			StreamWriter file = new StreamWriter("echobots.txt");
			foreach(string sub in Subscribers)
			{
				file.WriteLine(sub.Trim());
			}
			file.Close();

		}
	}
}

