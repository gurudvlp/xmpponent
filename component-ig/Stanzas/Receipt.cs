using System;
using xmpponent.Stanzas;

namespace xmpponent.Stanzas
{
	public class Receipt : Stanza
	{
		public string MessageID = "";
		public string To = "";
		public string From = "";
		public string Thread = "";

		public Receipt ()
		{
		}

		public static Receipt CreateFromMessage(Message message)
		{
			
			Receipt toret = new Receipt();

			toret.From = message.From;
			toret.To = message.To;
			toret.MessageID = message.Id;
			toret.Thread = message.Thread;

			return toret;
		}

		public override string GenerateXML ()
		{
			string rawxml = "";

			rawxml = "<message id='" + this.MessageID + "' ";
			rawxml += "to='" + this.To + "' ";
			rawxml += "from='" + this.From + "'>";
			rawxml += "<thread>" + this.Thread + "</thread>";
			rawxml += "<received id='" + this.MessageID + "' ";
			rawxml += "xmlns='urn:xmpp:receipts'/>";
			rawxml += "</message>";

			return rawxml;
		}
	}
}

