using System;
using xmpponent.Stanzas;

namespace xmpponent.Stanzas
{
	public class InfoQuery : Stanza
	{
		public string Id = "";
		public string To = "";
		public string From = "";
		public string qType = "";

		public string iType = "";

		public InfoQuery ()
		{
		}

		public static InfoQuery Parse(string InBuffer)
		{
			InfoQuery toret = new InfoQuery();
			toret.RawXML = InBuffer + "</iq>";

			string[] prts = InBuffer.Split(new char[]{'>'}, 2);
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
				if(kvp[0].ToLower() == "type") { toret.qType = tval; }
			}

			if(InBuffer.IndexOf("<query xmlns='http://jabber.org/protocol/disco#info'/>") > -1) { toret.iType = "disco#info"; }
			else if(InBuffer.IndexOf("<vCard xmlns='vcard-temp'/>") > -1) { toret.iType = "vcard"; }

			return toret;
		}
	}
}

