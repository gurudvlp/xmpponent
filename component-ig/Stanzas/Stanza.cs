using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.IO;

namespace xmpponent.Stanzas
{
	public class Stanza
	{
		public Dictionary<string, string> Attributes;
		public Dictionary<string, Stanza> Elements;

		public string To
		{
			get { if(!Attributes.ContainsKey("to")) { return ""; } else { return Attributes["to"]; } }
			set { if(!Attributes.ContainsKey("to")) { Attributes.Add("to", value); } else { Attributes["to"] = value; } }
		}

		public string From
		{
			get { if(!Attributes.ContainsKey("from")) { return ""; } else { return Attributes["from"]; } }
			set { if(!Attributes.ContainsKey("from")) { Attributes.Add("from", value); } else { Attributes["from"] = value; } }
		}

		public string Id
		{
			get { if(!Attributes.ContainsKey("id")) { return ""; } else { return Attributes["id"]; } }
			set { if(!Attributes.ContainsKey("id")) { Attributes.Add("id", value); } else { Attributes["id"] = value; } }
		}

		private string _RawXML = "";
		public string RawXML
		{ get { return _RawXML; } set { _RawXML = value; } }

		private string _Element = "";
		public string Element
		{ get { return _Element; } set { _Element = value; } }

		

		private string _InternalXML = "";
		public string InternalXML
		{ get { return _InternalXML; } set { _InternalXML = value; } }

		public Stanza ()
		{
			Attributes = new Dictionary<string, string>();
			Elements = new Dictionary<string, Stanza>();
		}

		public static Stanza Parse(ref string InBuffer)
		{
			//<presence type='subscribe' to='bot@comp.example.com' from='doophus@example.com' id='2U7Rs-773'/>
			//<iq type='get' to='bot@comp.examle.com' from='doophus@example.com/r3s0urc3' id='2U7Rs-778'><vCard xmlns='vcard-temp'/></iq>
			//<message type='chat' to='bot@comp.example.com' from='doophus@example.com/r3s0urc3' id='2U7Rs-821'><body>Poop a do</body><thread>yD47iRwN9A27</thread><active xmlns='http://jabber.org/protocol/chatstates'/><request xmlns='urn:xmpp:receipts'/><stanza-id id='a60fed2c-23d6-45a5-9e51-de5c888422c8' by='doophus@example.com' xmlns='urn:xmpp:sid:0'/></message>

			Stanza toret = Stanza.xParse(ref InBuffer);

			///TODO:
			/// Create an appropriate way of handling a stream-end scenario.
			/// 
			if(InBuffer.EndsWith("</stream:stream>"))
			{
				InBuffer = "";
			}
			return toret;
		}

		public static Stanza xParse(ref string InBuffer)
		{
			if(InBuffer.Length < 3) { return null; }
			if(InBuffer.Substring(0, 1) != "<") { return null; }

			string elname = "";
			for(int el = 1; el < InBuffer.Length; el++)
			{
				/*if(InBuffer.Substring(el, 1) == "/")
				{

				}
				else if(InBuffer.Substring(el, 1) == " ")
				{

				}
				else if(InBuffer.Substring(el, 1) == ">")
				{

				}
				else*/
				if(InBuffer.Substring(el, 1) != "/"
					&& InBuffer.Substring(el, 1) != " "
					&& InBuffer.Substring(el, 1) != ">")
				{
					elname += InBuffer.Substring(el, 1);
				}
				else { break; }
			}

			string[] elattrs = InBuffer.Split(new char[]{'>'}, 2);
			if(elattrs.Length == 1) { return null; }

			Stanza toret = new Stanza();
			if(elname == "iq") { toret = new InfoQuery(); }
			else if(elname == "message") { toret = new Message(); }
			else if(elname == "presence") { toret = new Presence(); }
			else if(elname == "stream:error") { toret = new StreamError(); }
			else if(elname == "stream:end") { toret = new StreamEnd(); }
			toret.Element = elname;

			int inbufremoval = 0;
			if(InBuffer.IndexOf("/>") > InBuffer.IndexOf(">"))
			{
				toret.InternalXML = elattrs[1].Substring(0, elattrs[1].IndexOf("</" + elname));
				inbufremoval += (3 + elname.Length);

				string interbuf = toret.InternalXML;
				while(interbuf != "")
				{
					Stanza eelem = Stanza.xParse(ref interbuf);
					if(eelem != null)
					{
						toret.Elements.Add(eelem.Element, eelem);
					}
					else
					{
						interbuf = ""; 
					}
				}
			}

			string[] attrs = elattrs[0].Split(new char[]{' '}, StringSplitOptions.None);
			if(attrs.Length > 1)
			{
				for(int eat = 1; eat < attrs.Length; eat++)
				{
					string[] kvp = attrs[eat].Split(new char[]{'='}, 2);
					string nval = kvp[1].Trim().Substring(1);
					if(nval.EndsWith("/")) { nval = nval.Substring(0, nval.Length - 1); }
					nval = nval.Substring(0, nval.Length - 1);
					toret.Attributes.Add(kvp[0].ToLower().Trim(), nval);
					//Console.WriteLine("{0}: kvp {1} = '{2}'", toret.Element, kvp[0].ToLower().Trim(), nval);
				}
			}
			//Console.WriteLine(toret.Element);

			InBuffer = InBuffer.Substring(elattrs[0].Length + 1 + toret.InternalXML.Length + inbufremoval);


			return toret;
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

		/*private static Stanza CollectPresence(ref string InBuffer)
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
		}*/

		/*private static Stanza CollectMessage(ref string InBuffer)
		{
			if(InBuffer.IndexOf("</message>") == -1) { return null; }
			string prline = InBuffer.Substring(0, InBuffer.IndexOf("</message>"));
			InBuffer = InBuffer.Substring(prline.Length + 10);

			Message toret = Message.Parse(prline);

			return toret;
		}*/

		/*private static Stanza CollectIq(ref string InBuffer)
		{
			if(InBuffer.IndexOf("</iq>") == -1) { return null; }
			string prline = InBuffer.Substring(0, InBuffer.IndexOf("</iq>"));
			InBuffer = InBuffer.Substring(prline.Length + 5);

			InfoQuery toret = InfoQuery.Parse(prline);
			//toret.RawXML = prline + "</iq>";
			return toret;
		}*/

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
			
			//Console.WriteLine(InBuffer);

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

