using System;

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

		private bool _LocalSubscription = false;
		/// <summary>
		/// Gets or sets a value indicating whether this account on the component has
		/// a subscription to this <see cref="xmpponent.Accounts.Contact"/>.
		/// </summary>
		/// <value><c>true</c> if local subscription; otherwise, <c>false</c>.</value>
		public bool LocalSubscription
		{ get { return _LocalSubscription; } set { _LocalSubscription = value; } }

		private bool _RemoteSubscription = false;
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="xmpponent.Accounts.Contact"/> has
		/// a subscription to this account on the component.
		/// </summary>
		/// <value><c>true</c> if remote subscription; otherwise, <c>false</c>.</value>
		public bool RemoteSubscription
		{ get { return _RemoteSubscription; } set { _RemoteSubscription = value; } }

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

