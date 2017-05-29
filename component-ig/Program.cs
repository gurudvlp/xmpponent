using System;
using System.Diagnostics;
using System.IO;

namespace xmpponent
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("xmpponent starting");

			xmpponent.Component.Eng = new EchoBots();
			Component.Eng.ComponentAddress = "component.example.com";
			Component.Eng.ServerAddress = "127.0.0.1";
			Component.Eng.Secret = "my secret passphrase";

			//	Look for a file called config in the current working directory.  This file is just formatted
			//	like,
			//
			//	serveraddress=127.0.0.1
			//	componentaddress=component.example.com
			//	secret=this is my secret pass phrase
			//
			//	You don't have to have this config file if you compile the options into the executable.

			if(File.Exists("config"))
			{
				StreamReader file = new StreamReader("config");
				string line;
				while((line = file.ReadLine()) != null)
				{
					line = line.Trim();
					string[] parts = line.Split(new char[]{'='}, 2);
					if(parts.Length > 1)
					{
						if(parts[0].Trim().ToLower() == "componentaddress") { Component.Eng.ComponentAddress = parts[1].Trim(); }
						if(parts[0].Trim().ToLower() == "serveraddress") { Component.Eng.ServerAddress = parts[1].Trim(); }
						if(parts[0].Trim().ToLower() == "secret") { Component.Eng.Secret = parts[1].Trim(); }
						if(parts[0].Trim().ToLower() == "autoreceipt")
						{
							if(parts[1].Trim().ToLower() == "true" || parts[1].Trim().ToLower() == "1") { Component.Eng.AutoSendReceipt= true; } else { Component.Eng.AutoSendReceipt = false; }
						}
						if(parts[0].Trim().ToLower() == "debug")
						{
							if(parts[1].Trim().ToLower() == "true" || parts[1].Trim().ToLower() == "1") { Component.Eng.Debug = true; } else { Component.Eng.Debug = false; }
						}
						if(parts[0].Trim().ToLower() == "info")
						{
							if(parts[1].Trim().ToLower() == "true" || parts[1].Trim().ToLower() == "1") { Component.Eng.InfoText = true; } else { Component.Eng.InfoText = false; }
						}
					}
				}

				file.Close();
			}

			/*string InBuffer = "<iq type='get' to='bot@comp.examle.com' from='doophus@example.com/r3s0urc3' id='2U7Rs-778'><vCard xmlns='vcard-temp'/></iq>" +
				"<presence type='subscribe' to='bot@comp.example.com' from='doophus@example.com' id='2U7Rs-773'/>" + 
				"<message type='chat' to='bot@comp.example.com' from='doophus@example.com/r3s0urc3' id='2U7Rs-821'><body>Poop a do</body><thread>yD47iRwN9A27</thread><active xmlns='http://jabber.org/protocol/chatstates'/><request xmlns='urn:xmpp:receipts'/><stanza-id id='a60fed2c-23d6-45a5-9e51-de5c888422c8' by='doophus@example.com' xmlns='urn:xmpp:sid:0'/></message>";

			Stanzas.Stanza stanza = Stanzas.Stanza.xParse(ref InBuffer);
			*/
			Component.Eng.Run();
		}
	}
}
