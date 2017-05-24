using System;
using xmpponent.Stanzas;

namespace xmpponent.Stanzas
{
	public class InfoQuery : Stanza
	{
		
		public InfoQuery ()
		{
		}

		public static InfoQuery Parse(string InBuffer)
		{
			InfoQuery toret = new InfoQuery();
			toret.RawXML = InBuffer + "</iq>";

			return toret;
		}
	}
}

