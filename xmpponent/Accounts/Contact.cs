﻿using System;

namespace xmpponent.Accounts
{
	public class Contact
	{
		private string _JID = "";
		/// <summary>
		/// Gets or sets the JID of the contact.
		/// </summary>
		/// <value>The JID.</value>
		public string JID
		{ get { return _JID; } set { _JID = value; } }


		public Contact ()
		{
		}

		public static string BareJID(string jid)
		{
			if(jid.Contains("/")) { return jid.Substring(0, jid.IndexOf('/')); }
			return jid;
		}
	}
}
