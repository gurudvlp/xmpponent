using System;
using xmpponent.Stanzas;
using xmpponent.Accounts;

namespace xmpponent.Stanzas
{
	public class Receipt : Stanza
	{
		public string MessageID
		{
			get { return Id; }
			set { Id = value; }
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

		public Receipt ()
		{
		}

		public static Receipt CreateFromMessage(Message message)
		{
			
			Receipt toret = new Receipt();

			toret.From = Contact.BareJID(message.To);
			toret.To = Contact.BareJID(message.From);
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

