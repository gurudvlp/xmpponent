using System;
using System.Runtime.InteropServices;

namespace xmpponent.Stanzas
{
	public class Presence : Stanza
	{
		public static uint ShowAway = 0;
		public static uint ShowChat = 1;
		public static uint ShowXA = 3;
		public static uint ShowDND = 4;
		public static uint ShowAvailable = 5;

		public string pType
		{
			get { if(!Attributes.ContainsKey("type")) { return ""; } else { return Attributes["type"]; } }
			set { if(!Attributes.ContainsKey("type")) { Attributes.Add("type", value); } else { Attributes["type"] = value; } }
		}

		public string Status
		{
			get { if(!Elements.ContainsKey("status")) { return ""; } else { return Elements["status"].InternalXML; } }
			set 
			{ 
				if(!Elements.ContainsKey("status"))
				{
					Stanza bstanz = new Stanza();
					bstanz.InternalXML = value;
					Elements.Add("status", bstanz);
				}
				else
				{
					Elements["status"].InternalXML = value;
				}
			}
		}
		public uint ShowType = 3;
		public bool Available = true;

		public Presence ()
		{
		}

		public override string GenerateXML()
		{
			//string toret = "<presence type='" +
			//	pType + "' to='" + To + "' from='" + From + "' id='" + Id + "'/>";

			if(!Available) { pType = "unavailable"; }

			string toret = "<presence ";

			if(pType != "") 
			{ 
				toret +=  "type='" + pType + "' "; 

			}
			toret += 
				"from='" + From + "' " +
				"to='" + To + "' " +
				"id='" + Id + "'>";

			if(ShowType == Presence.ShowAway) { toret += "<show>away</show>"; }
			else if(ShowType == Presence.ShowChat) { toret += "<show>chat</show>"; }
			else if(ShowType == Presence.ShowXA) { toret += "<show>xa</show>"; }
			else if(ShowType == Presence.ShowDND) { toret += "<show>dnd</show>"; }


			toret += "<status>" + Status + "</status>";
			toret += "</presence>";
			
			return toret;
		}

		public string ShowToString()
		{
			if(this.ShowType == ShowAvailable) { return "available"; }
			else if(this.ShowType == ShowXA) { return "xa"; }
			else if(this.ShowType == ShowDND) { return "dnd"; }
			else if(this.ShowType == ShowChat) { return "chat"; }
			else if(this.ShowType == ShowAway) { return "away"; }

			return "";
		}
	}
}

