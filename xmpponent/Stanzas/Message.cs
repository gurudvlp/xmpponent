using System;
using System.Reflection;

namespace xmpponent.Stanzas
{
	public class Message : Stanza
	{
		//<message type='chat' to='ig@ig.dooph.us' from='dooph@dooph.us/Xabmoto' id='2U7Rs-821'><body>Poop a do</body><thread>yD47iRwN9A27</thread><active xmlns='http://jabber.org/protocol/chatstates'/><request xmlns='urn:xmpp:receipts'/><stanza-id id='a60fed2c-23d6-45a5-9e51-de5c888422c8' by='dooph@dooph.us' xmlns='urn:xmpp:sid:0'/></message>
		//<request xmlns='urn:xmpp:receipts'/>
		//<received id='ytV2Q-443' xmlns='urn:xmpp:receipts'/>

		public string mType
		{
			get { if(!Attributes.ContainsKey("type")) { return ""; } else { return Attributes["type"]; } }
			set	{ if(!Attributes.ContainsKey("type")) { Attributes.Add("type", value); } else { Attributes["type"] = value; } }
		}

		public string Body
		{
			get { if(!Elements.ContainsKey("body")) { return ""; } else { return Elements["body"].InternalXML; } }
			set 
			{ 
				if(!Elements.ContainsKey("body")) 
				{
					Stanza bstanz = new Stanza();
					bstanz.InternalXML = value;
					Elements.Add("body", bstanz);
				} 
				else 
				{
					Elements["body"].InternalXML = value; 
				} 
			}
		}

		public string Thread
		{
			get { if(!Elements.ContainsKey("thread")) { return ""; } else { return Elements["thread"].InternalXML; } }
			set 
			{ 
				if(!Elements.ContainsKey("thread")) 
				{
					Stanza bstanz = new Stanza();
					bstanz.InternalXML = value;
					Elements.Add("thread", bstanz); 
				} 
				else 
				{ 
					Elements["thread"].InternalXML = value; 
				} 
			}
		}

		public string StanzaId
		{
			get { if(!Attributes.ContainsKey("stanzaid")) { return ""; } else { return Attributes["stanzaid"]; } }
			set { if(!Attributes.ContainsKey("stanzaid")) { Attributes.Add("stanzaid", value); } else { Attributes["stanzaid"] = value; } }
		}

		public string StanzaBy
		{
			get { if(!Attributes.ContainsKey("stanzaby")) { return ""; } else { return Attributes["stanzaby"]; } }
			set { if(!Attributes.ContainsKey("stanzaby")) { Attributes.Add("stanzaby", value); } else { Attributes["stanzaby"] = value; } }
		}

		public bool ReceiptRequested
		{
			get
			{
				if(Elements.ContainsKey("request"))
				{
					if(Elements["request"].Attributes.ContainsKey("xmlns"))
					{
						if(Elements["request"].Attributes["xmlns"].ToLower() == "urn:xmpp:receipts") { return true; }
					}
				}
				return false;
			}

		}

		public bool IsReceipt
		{
			get { if(Elements.ContainsKey("received")) { return true; } else { return false; } }
		}

		public bool IsPaused
		{
			get { if(Elements.ContainsKey("paused")) { return true; } else { return false; } }
		}

		public Message ()
		{
		}

		/*public static Message Parse(string InBuffer)
		{
			Message toret = new Message();
			toret.RawXML = InBuffer + "</message>";

			if(InBuffer.IndexOf("<received ") > -1)
			{
				toret.IsReceipt = true;
				return toret;
			}

			if(InBuffer.IndexOf("<paused ") > -1)
			{
				toret.IsPaused = true;
				return toret;
			}

			if(InBuffer.IndexOf("<request xmlns='urn:xmpp:receipts'/>") > -1)
			{
				toret.ReceiptRequested = true;
			}

			string[] prts = InBuffer.Split(new string[]{"<body>", "</body>"}, StringSplitOptions.None);
			if(prts.Length == 3)
			{
				toret.Body = prts[1];
				InBuffer = prts[0] + prts[2];
			}
			//Console.WriteLine("Body: {0}", toret.Body);

			prts = InBuffer.Split(new string[]{"<thread>", "</thread>"}, StringSplitOptions.None);
			if(prts.Length == 3)
			{
				toret.Thread = prts[1];
				InBuffer = prts[0] + prts[2];
			}
			//Console.WriteLine("Thread: {0}", toret.Thread);

			prts = InBuffer.Split(new char[]{'>'}, 2);
			InBuffer = prts[1];
			string mtag = prts[0];
			prts = mtag.Split(new char[]{' '}, StringSplitOptions.None);

			for(int eprt = 1; eprt < prts.Length; eprt++)
			{
				string[] kvp = prts[eprt].Split(new char[]{'='}, 2);
				string tval = kvp[1].Substring(1, kvp[1].Length - 2);

				if(kvp[0].ToLower() == "to") { toret.To = tval; }
				if(kvp[0].ToLower() == "from") { toret.From = tval; }
				if(kvp[0].ToLower() == "id") { toret.Id = tval; }
				if(kvp[0].ToLower() == "type") { toret.mType = tval; }
			}

			//Console.WriteLine("{0} / {1} {2}", toret.To, toret.From, toret.Id);

			prts = InBuffer.Split(new string[]{"<stanza-id"}, StringSplitOptions.None);
			mtag = prts[1];
			prts = mtag.Split(new char[]{' '}, StringSplitOptions.None);

			for(int eprt = 1; eprt < prts.Length; eprt++)
			{
				string[] kvp = prts[eprt].Split(new char[]{'='}, 2);
				string tval = kvp[1].Substring(1, kvp[1].Length - 2);


				if(kvp[0].ToLower() == "by") { toret.StanzaBy = tval; }
				if(kvp[0].ToLower() == "id") { toret.StanzaId = tval; }
			}

			//Console.Write("Stanza: {0} {1}", toret.StanzaBy, toret.StanzaId);

			return toret;
		}*/

		public override string GenerateXML ()
		{
			string toret = "<message type='" + mType + "' ";
			toret += "to='" + To + "' ";
			toret += "from='" + From + "' ";
			toret += "id='" + Id + "'>";
			toret += "<body>" + Body + "</body>";
			toret += "<thread>" + Thread + "</thread>";
			toret += "<active xmlns='http://jabber.org/protocol/chatstates'/>";
			toret += "<request xmlns='urn:xmpp:receipts'/>";
			toret += "<stanza-id id='" + StanzaId + "' ";
			toret += "by='" + StanzaBy + "' ";
			toret += "xmlns='urn:xmpp:sid:0'/>";
			toret += "</message>";

			return toret;
		}
	}
}

