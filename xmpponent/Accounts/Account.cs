using System;
using System.Collections.Generic;
using xmpponent.Stanzas;

namespace xmpponent.Accounts
{
	public class Account
	{


		private string _JID = "";
		public string JID
		{ get { return _JID; } set { _JID = value; } }

		public string Username
		{
			get
			{
				if(JID.Contains("@"))
				{
					return JID.Substring(0, JID.IndexOf('@'));
				}
				else
				{
					return "";
				}
			}
		}

		private bool _Online = false;
		public bool Online
		{ get { return _Online; } set { _Online = value; } }

		private bool _AutoSubscriptions = true;
		public bool AutoSubscriptions
		{ get { return _AutoSubscriptions; } set { _AutoSubscriptions = value; } }

		private string _Status = "";
		public string Status
		{ get { return _Status; } set { _Status = value; } }

		public Dictionary<string, Contact> Contacts;

		public Account ()
		{
			Contacts = new Dictionary<string, Contact>();
		}

		public static Account Create(string jid)
		{
			Account toret = new Account();
			toret.JID = jid;
			toret.Status = "";

			return toret;
		}

		/// <summary>
		/// Adds a contact with default subscription settings.
		/// </summary>
		/// <returns><c>true</c>, if contact was added, <c>false</c> otherwise.</returns>
		/// <param name="jid">jid of contact.</param>
		public bool AddContact(string jid)
		{
			string njid = Contact.BareJID(jid).ToLower();
			if(Contacts.ContainsKey(njid)) 
			{ 
				Component.Eng.DebugWrite("AddContact: Failed due to already being a contact.");
				return false; 
			}

			Contact ncontact = new Contact();
			ncontact.JID = njid;
			Contacts.Add(njid, ncontact);
			Component.Eng.DebugInfo(String.Format("AddContact: [{0}] << [{1}]", JID, njid));

			return true;
		}

		/// <summary>
		/// Called when this account is loaded.
		/// </summary>
		public virtual void onLoad()
		{

		}

		public virtual void onPresenceAvailable(Presence presence)
		{

		}
	}
}

