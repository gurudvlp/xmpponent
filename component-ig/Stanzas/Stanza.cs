using System;

namespace xmpponent.Stanzas
{
	public class Stanza
	{

		private string _RawXML = "";
		public string RawXML
		{ get { return _RawXML; } set { _RawXML = value; } }

		public Stanza ()
		{
		}

		public static Stanza Parse(ref string InBuffer)
		{
			//<presence type='subscribe' to='bot@comp.example.com' from='doophus@example.com' id='2U7Rs-773'/>
			//<iq type='get' to='bot@comp.examle.com' from='doophus@example.com/r3s0urc3' id='2U7Rs-778'><vCard xmlns='vcard-temp'/></iq>
			//<message type='chat' to='bot@comp.example.com' from='doophus@example.com/r3s0urc3' id='2U7Rs-821'><body>Poop a do</body><thread>yD47iRwN9A27</thread><active xmlns='http://jabber.org/protocol/chatstates'/><request xmlns='urn:xmpp:receipts'/><stanza-id id='a60fed2c-23d6-45a5-9e51-de5c888422c8' by='doophus@example.com' xmlns='urn:xmpp:sid:0'/></message>

			if(InBuffer.Length  < 1) { return null; }

			//	Continue to let InBuffer fill up until the opening and closing
			//	of the tag is present.
			if(InBuffer.Substring(0, 1) != "<") { return null; }

			if(InBuffer.StartsWith("<presence")) { return CollectPresence(ref InBuffer); }
			else if(InBuffer.StartsWith("<message")) { return CollectMessage(ref InBuffer); }
			else if(InBuffer.StartsWith("<iq")) { return CollectIq(ref InBuffer); }
			else if(InBuffer.StartsWith("<stream:error")) { return CollectStreamError(ref InBuffer); }
			else if(InBuffer.StartsWith("</stream:stream")) { return CollectStreamEnd(ref InBuffer); }
			else if(InBuffer.EndsWith("</stream:stream>")) { return CollectStreamEnd(ref InBuffer); }
			return null;
		}

		public static void GetTagKeyVal(string tagkeyval, out string key, out string val)
		{
			string[] spl = tagkeyval.Split(new char[]{'='}, 2);
			if(spl.Length == 1)
			{
				key = "";
				val = "";
				return;
			}

			key = spl[0];
			val = spl[1].Substring(1, spl[1].Length - 2);
			if(val.EndsWith("'")) { val = val.Substring(0, val.Length - 1); }

		}

		private static Stanza CollectPresence(ref string InBuffer)
		{
			if(InBuffer.IndexOf("/>") == -1) { return null; }
			string prline = InBuffer.Substring(0, InBuffer.IndexOf("/>"));
			InBuffer = InBuffer.Substring(prline.Length + 2);



			prline = prline.Substring(1);
			//string[] prts = prline.Split(new char[]{'>'}, 2);

			//if(prts.Length == 1) { return null; }

			string to = "";
			string from = "";
			string ptype = "";
			string id = "";

			string[] chunks = prline.Split(new char[]{' '}, StringSplitOptions.None);

			for(int eprt = 1; eprt < chunks.Length; eprt++)
			{
				string k;
				string v;
				GetTagKeyVal(chunks[eprt], out k, out v);

				//Console.WriteLine("ParsePresence kv {0} :: {1} {2}", chunks[eprt], k, v);

				if(k.ToLower() == "type") { ptype = v; }
				if(k.ToLower() == "to") { to = v; }
				if(k.ToLower() == "from") { from = v; }
				if(k.ToLower() == "id") { id = v; }
			}

			Presence toret = new Presence();
			toret.RawXML = "<" + prline + "/>";
			toret.From = from;
			toret.To = to;
			toret.pType = ptype;
			toret.Id = id;

			return toret;
		}

		private static Stanza CollectMessage(ref string InBuffer)
		{
			if(InBuffer.IndexOf("</message>") == -1) { return null; }
			string prline = InBuffer.Substring(0, InBuffer.IndexOf("</message>"));
			InBuffer = InBuffer.Substring(prline.Length + 10);

			Message toret = Message.Parse(prline);

			return toret;
		}

		private static Stanza CollectIq(ref string InBuffer)
		{
			if(InBuffer.IndexOf("</iq>") == -1) { return null; }
			string prline = InBuffer.Substring(0, InBuffer.IndexOf("</iq>"));
			InBuffer = InBuffer.Substring(prline.Length + 5);

			InfoQuery toret = InfoQuery.Parse(prline);
			//toret.RawXML = prline + "</iq>";
			return toret;
		}

		private static StreamError CollectStreamError(ref string InBuffer)
		{
			if(InBuffer.IndexOf("</stream:error>") == -1) { return null; }
			string prline = InBuffer.Substring(0, InBuffer.IndexOf("</stream:error>"));
			InBuffer = InBuffer.Substring(prline.Length + 15);

			StreamError toret = new StreamError();
			toret.RawXML = prline + "</stream:error>";
			return toret;
		}

		private static StreamEnd CollectStreamEnd(ref string InBuffer)
		{
			
			Console.WriteLine(InBuffer);

			InBuffer = InBuffer.Replace("</stream:stream>", "");

			StreamEnd toret = new StreamEnd();
			toret.RawXML = "</stream:stream>";
			return toret;
		}

		public virtual string GenerateXML()
		{
			return "";
		}
	}
}

