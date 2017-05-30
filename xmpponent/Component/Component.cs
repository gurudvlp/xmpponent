using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Net;
using System.Configuration;
using System.Security.Permissions;
using System.Diagnostics;
using System.Collections.Generic;
using xmpponent.Stanzas;
using System.Security.AccessControl;

namespace xmpponent
{
	public class Component
	{
		public static Component Eng;

		public Dictionary<string, Accounts.Account> ComponentAccounts;

		private string _ComponentAddress = "";
		public string ComponentAddress
		{	get { return _ComponentAddress;	} set { _ComponentAddress = value; } }

		private int _Port = 5347;
		public int Port
		{ get { return _Port; } set { _Port = value; } }

		private string _ServerAddress = "";
		public string ServerAddress
		{ get { return _ServerAddress; } set { _ServerAddress = value; } }

		private string _Secret = "";
		public string Secret
		{	get { return _Secret; } set { _Secret = value; } }

		private uint _StanzaID = 0;
		public uint StanzaID
		{
			get { return _StanzaID; }
			set { _StanzaID = value; }
		}

		private bool _Debug = false;
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="xmpponent.Component"/> has debugging
		/// information enabled.
		/// </summary>
		/// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
		public bool Debug
		{ get { return _Debug; } set { _Debug = value; } }

		private bool _InfoText = true;
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="xmpponent.Component"/> should output
		/// status information.
		/// </summary>
		/// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
		public bool InfoText
		{ get { return _InfoText; } set { _InfoText = value; } }

		private bool _AutoSendReceipt = true;
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="xmpponent.Component"/> automatically
		/// sends a receipt for each incoming message.
		/// </summary>
		/// <value><c>true</c> handles receipts automatically; otherwise, <c>false</c>.</value>
		public bool AutoSendReceipt
		{ get { return _AutoSendReceipt; } set { _AutoSendReceipt = value; } }


		public TcpClient TcpSocket = null;
		public NetworkStream SockStream = null;

		bool StreamsInitialized = false;
		string StreamID = "";
		bool Handsshaken = false;

		public Component ()
		{
			ComponentAccounts = new Dictionary<string, xmpponent.Accounts.Account>();
		}

		public void Run()
		{
			Console.WriteLine("Launching xmpponent");

			onLoad();

			if(this.ComponentAddress == "")
			{
				DebugWrite("A component address must be specified.");
				return;
			}

			if(this.ServerAddress == "")
			{
				DebugWrite("A server address must be specified.");
				return;
			}

			if(!Connect()) { return; }

			bool dorun = true;
			string InBuffer = "";
			Byte[] readbuf = new Byte[1024];

			while(dorun)
			{
				if(SockStream.DataAvailable)
				{
					int rlen = SockStream.Read(readbuf, 0, readbuf.Length);
					InBuffer += System.Text.Encoding.ASCII.GetString(readbuf, 0, rlen);
				}

				if(InBuffer.Length > 0)
				{
					if(!StreamsInitialized)
					{
						if(InBuffer.IndexOf("<?xml version='1.0'?>") > -1)
						{
							InBuffer = InBuffer.Replace("<?xml version='1.0'?>", "");
						}
						else if(InBuffer.IndexOf(">") > 0)
						{
							//DebugWrite("'>' found in string.");
							InBuffer = InBuffer.Replace("<", "");
							InBuffer = InBuffer.Replace(">", "");

							String[] prts = InBuffer.Split(new char[]{' '}, StringSplitOptions.None);
							if(prts[0].ToLower() == "stream:stream")
							{
								//DebugWrite("prts.len {0}", prts.Length);

								for(int eprt = 1; eprt < prts.Length; eprt++)
								{
									string k;
									string v;

									GetTagKeyVal(prts[eprt], out k, out v);
									//DebugWrite("k {0} v {1}", k, v);

									if(k.ToLower() == "id")
									{
										
										StreamID = v;
										StreamsInitialized = true;

										string hndsh = "<handshake>" +
											GenerateHandshake() +
											"</handshake>";

										Byte[] data = System.Text.Encoding.ASCII.GetBytes(hndsh);
										SockStream.Write(data, 0, data.Length);

										//DebugWrite("Handshake sent to server.");
									}
								}

								InBuffer = "";
							}
							else
							{
								//	Invalid stream from server ?
								DebugWrite("The xmpp server did not return a stream element.");
								dorun = false;
							}

						}
					}
					else if(!Handsshaken)
					{
						//	Hands not yet shaken
						if(InBuffer.IndexOf("<handshake/>") > -1)
						{
							DebugWrite("Connected and ready to go!");
							Handsshaken = true;
							InBuffer = InBuffer.Replace("<handshake/>", "");

							onConnect();
						}
					}
					else
					{
						//	Read and write stanzas

						Stanzas.Stanza inStanza = Stanzas.Stanza.Parse(ref InBuffer);

						if(inStanza == null)
						{
							DebugWrite("Incoming stanza was not parsed (it's null).");
							DebugWrite(InBuffer);
						}
						else if(inStanza.GetType() == typeof(Stanzas.Presence))
						{
							/*DebugWrite("Presence stanza received from server.");
							DebugWrite("Attributes:");
							foreach(KeyValuePair<string, string> kvp in inStanza.Attributes)
							{
								DebugWrite(String.Format("\t{0} = '{1}'", kvp.Key, kvp.Value));
							}
							DebugWrite("Elements:");
							foreach(KeyValuePair<string, Stanzas.Stanza> kvp in inStanza.Elements)
							{
								DebugWrite(String.Format("\t{0} = [{1}]", kvp.Key, kvp.Value.Element));
							}
							DebugWrite(String.Format("Presence internalXML {0}", inStanza.InternalXML));*/
							onPresenceReceived((Stanzas.Presence)inStanza);
						}
						else if(inStanza.GetType() == typeof(Stanzas.Message))
						{
							//DebugWrite("Message stanza received from server.");
							/*DebugWrite("Attributes:");
							foreach(KeyValuePair<string, string> kvp in inStanza.Attributes)
							{
								DebugWrite(String.Format("\t{0} = '{1}'", kvp.Key, kvp.Value));
							}
							DebugWrite("Elements (body):");
							foreach(KeyValuePair<string, Stanzas.Stanza> kvp in inStanza.Elements["body"].Elements)
							{
								DebugWrite(String.Format("\t{0} = [{1}]", kvp.Key, kvp.Value.Element));
							}
							DebugWrite(String.Format("body internalXML {0}", inStanza.Elements["body"].InternalXML));*/

							if(!((Stanzas.Message)inStanza).IsReceipt
								&& !((Stanzas.Message)inStanza).IsPaused)
							{

								if(((Stanzas.Message)inStanza).Body == "")
								{
									//	There was no body to the incoming message
								}
								else
								{
									
									onMessageReceived((Stanzas.Message)inStanza);
									if(AutoSendReceipt)
									{
										Stanzas.Receipt receipt = Stanzas.Receipt.CreateFromMessage((Stanzas.Message)inStanza);
										SendReceipt(receipt);
									}
								}
							}
							else
							{
								//	This incoming message was just a receipt of
								//	the previous message.
								onMessageReceiptReceived((Stanzas.Message)inStanza);
							}
						}
						else if(inStanza.GetType() == typeof(Stanzas.InfoQuery))
						{
							onInfoQueryReceived((Stanzas.InfoQuery)inStanza);
						}
						else if(inStanza.GetType() == typeof(Stanzas.StreamError))
						{
							onStreamError((Stanzas.StreamError)inStanza);
						}
						else if(inStanza.GetType() == typeof(Stanzas.StreamEnd))
						{
							onStreamEnd((Stanzas.StreamEnd)inStanza);
							dorun = false;
						}
						else
						{
							onUndefinedStanzaReceived(inStanza);
						}
					}
				}

				if(dorun) { onUpdate(); }

				Thread.Sleep(1);
			}
		}

		public bool Connect()
		{
			if(TcpSocket == null)
			{
				try
				{
					TcpSocket = new TcpClient(ServerAddress, Port);

					if(!TcpSocket.Connected)
					{
						DebugWrite("Connection to xmpp server failed.");
						return false;
					}
					DebugWrite(String.Format("Connected to {0}:{1}", ServerAddress, Port));

					SockStream = TcpSocket.GetStream();
					string strmhead = "<stream:stream"+'\n'+
						"xmlns='jabber:component:accept'" + '\n' +
						"xmlns:stream='http://etherx.jabber.org/streams'" +'\n' +
						"to='" + ComponentAddress + "'>";

					Byte[] data = System.Text.Encoding.ASCII.GetBytes(strmhead);
					SockStream.Write(data, 0, data.Length);

					DebugWrite("Component >> Server Stream Initialized");

				}
				catch(Exception ex)
				{
					DebugWrite("Failed to connect to xmpp server.");
					DebugWrite("Exception:");
					DebugWrite(ex.Message);
					return false;
				}
			}

			return true;
		}

		public void GetTagKeyVal(string tagkeyval, out string key, out string val)
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

		public string GenerateHandshake()
		{
			string toret;

			toret = StreamID + Secret;
			byte[] bytes = Encoding.ASCII.GetBytes(toret);
			var sha1 = SHA1.Create();
			byte[] hashed = sha1.ComputeHash(bytes);

			var sb = new StringBuilder();
			foreach (byte b in hashed)
			{
				var hex = b.ToString("x2");
				sb.Append(hex);
			}


			return sb.ToString();
		}


		/// <summary>
		/// Sends a pre-created message to another xmpp user.
		/// </summary>
		/// <returns><c>true</c>, if message was sent, <c>false</c> otherwise.</returns>
		/// <param name="message">Message.</param>
		public bool SendMessage(Stanzas.Message message)
		{
			if(message.To == "")
			{
				DebugWrite("SendMessage: Failed due to no recipient.");
				return false;
			}

			if(message.From == "")
			{
				DebugWrite("SendMessage: Failed due to no sender.");
				return false;
			}

			if(message.Body == "")
			{
				DebugWrite("SendMessage: Failed due to no body.");
				return false;
			}

			if(message.mType == "") { message.mType = "chat"; }
			if(message.StanzaBy == "") { message.StanzaBy = message.From; }
			if(message.StanzaId == "")
			{
				message.StanzaId = "doo-comp-" + StanzaID.ToString();
				StanzaID++;
			}

			if(message.Thread == "") { message.Thread = "doo-comp-thread-" + StanzaID.ToString(); }
			if(message.Id == "")
			{
				message.Id = "doo-comp-mid-" + StanzaID.ToString();
				StanzaID++;
			}

			Byte[] data = System.Text.Encoding.ASCII.GetBytes(message.GenerateXML());
			SockStream.Write(data, 0, data.Length);
			return true;
		}

		/// <summary>
		/// Sends a new message.
		/// </summary>
		/// <returns><c>true</c>, if message was sent, <c>false</c> otherwise.</returns>
		/// <param name="tojid">Recipient's JID.</param>
		/// <param name="fromjid">Sender's JID.</param>
		/// <param name="body">Body of message.</param>
		public bool SendMessage(string tojid, string fromjid, string body)
		{
			if(tojid == "")
			{
				DebugWrite("SendMessage: Failed due to no recipient.");
				return false;
			}

			if(fromjid == "")
			{
				DebugWrite("SendMessage: Failed due to no sender.");
				return false;
			}

			Message message = new Message();
			message.Body = body;
			message.To = tojid;
			message.From = fromjid;
			message.mType = "chat";
			message.StanzaBy = fromjid;

			message.StanzaId = Stanza.NextStanzaID;
			message.Id = Stanza.NextStanzaID;

			return SendMessage(message);
		}

		public bool SendPresence(Stanzas.Presence presence)
		{
			if(presence.From == "")
			{
				DebugWrite("SendPresence: Failed due to no sender.");
				return false;
			}

			if(presence.To == "")
			{
				DebugWrite("SendPresence: Failed because components require a recipient.");
				return false;
			}

			string tosend = presence.GenerateXML();
			/*DebugWrite("-------------------");
			DebugWrite(tosend);
			DebugWrite("-------------------");*/
			Byte[] data = System.Text.Encoding.ASCII.GetBytes(tosend);
			SockStream.Write(data, 0, data.Length);
			return true;
		}

		/// <summary>
		/// Sends a receipt for a message.
		/// </summary>
		/// <returns><c>true</c>, if receipt was sent, <c>false</c> otherwise.</returns>
		/// <param name="receipt">Receipt.</param>
		public bool SendReceipt(Stanzas.Receipt receipt)
		{
			if(receipt.To == "")
			{
				DebugWrite("SendReceipt: Failed due to no recipient.");
				return false;
			}

			if(receipt.From == "")
			{
				DebugWrite("SendReceipt: Failed due to no sender.");
				return false;
			}

			if(receipt.MessageID == "")
			{
				DebugWrite("SendReceipt: Failed due to no message id.");
				return false;
			}

			string tosend = receipt.GenerateXML();

			/*DebugWrite("Outgoing Message Receipt:");
			DebugWrite(tosend);
			DebugWrite("----------------------------");*/
			Byte[] data = System.Text.Encoding.ASCII.GetBytes(tosend);
			SockStream.Write(data, 0, data.Length);

			return true;
		}

		/// <summary>
		/// Write debug information to stdout.
		/// </summary>
		/// <param name="text">Text to write.</param>
		public virtual void DebugWrite(string text)
		{
			if(Debug) { Console.WriteLine(text); }
		}

		/// <summary>
		/// Write an info message to stdout.
		/// </summary>
		/// <param name="text">Text to write.</param>
		public virtual void DebugInfo(string text)
		{
			if(InfoText) { Console.WriteLine(text); }
		}

		/// <summary>
		/// Gets called before the component starts and connects to
		/// the xmpp server.  Load resources and configurations
		/// here.
		/// </summary>
		public virtual void onLoad()
		{

		}

		/// <summary>
		/// Gets called after the component connects to the xmpp
		/// server and has completed the handshake process.
		/// </summary>
		public virtual void onConnect()
		{

		}

		/// <summary>
		/// Get's called at every opportunity to update any non-xmpp
		/// related logic.
		/// </summary>
		public virtual void onUpdate()
		{

		}

		/// <summary>
		/// Called each time an imcoming message is received.
		/// </summary>
		/// <param name="message">Message.</param>
		public virtual void onMessageReceived(Stanzas.Message message)
		{
			DebugWrite("onMessageReceived not implemented.");
		}

		/// <summary>
		/// Called when a message receipt is received.
		/// </summary>
		/// <param name="message">Message.</param>
		public virtual void onMessageReceiptReceived(Stanzas.Message message)
		{
			DebugWrite("onMessageReceiptReceived not implemented.");
		}

		/// <summary>
		/// Called each time an unhandled presence stanzas is received.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onPresenceReceived(Stanzas.Presence presence)
		{
			if(presence.pType.ToLower() == "subscribe") { onSubscribeRequested(presence); }
			else if(presence.pType.ToLower() == "unsubscribe") { onUnsubscribeRequested(presence); }
			else if(presence.pType.ToLower() == "subscribed") { onSubscribeConfirmed(presence); }
			else if(presence.pType.ToLower() == "unsubscribed") { onUnsubscribeConfirmed(presence); }
			else if(presence.pType.ToLower() == "probe") { onPresenceProbe(presence); }
			else if(presence.pType.ToLower() == "unavailable") { onPresenceUnavailable(presence); }
			else if(presence.pType.ToLower() == "") { onPresenceAvailable(presence); }
			else { onUnknownPresenceReceived(presence); }
		}

		/// <summary>
		/// Called each time another user requests a subscription from
		/// a component user.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onSubscribeRequested(Stanzas.Presence presence)
		{
			DebugWrite("onSubscribeRequested not implemented.");
		}

		/// <summary>
		/// Called each time another user requests to be unsubscribed
		/// from a component user.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onUnsubscribeRequested(Stanzas.Presence presence)
		{
			DebugWrite("onUnsubscribeRequested not implemented.");
		}

		/// <summary>
		/// Called each time a remote user confirms a subscription from
		/// one of the component users.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onSubscribeConfirmed(Stanzas.Presence presence)
		{
			DebugWrite("onSubscribeConfirmed not implemented.");
		}

		/// <summary>
		/// Called each time a remote user confirms that it has unsubscribed
		/// a component user.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onUnsubscribeConfirmed(Stanzas.Presence presence)
		{
			DebugWrite("onUnsubscribeConfirmed not implemented.");
		}

		/// <summary>
		/// Called each time the server informs us that a contact has
		/// gone unavailable.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onPresenceUnavailable(Stanzas.Presence presence)
		{
			DebugWrite("onPresenceUnavailable not implemented.");
		}

		/// <summary>
		/// Called each time ther server informs us that a contact has
		/// become available.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onPresenceAvailable(Stanzas.Presence presence)
		{
			string tobjid = Accounts.Contact.BareJID(presence.To);

			if(ComponentAccounts.ContainsKey(tobjid)) { ComponentAccounts[tobjid].onPresenceAvailable(presence); }
		}

		/// <summary>
		/// Called each time the server is probed for the presence of
		/// a component user.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onPresenceProbe(Stanzas.Presence presence)
		{
			DebugWrite("onPresenceProbe not implemented.");
		}

		/// <summary>
		/// Called when a presence stanza is received that is not
		/// recognized by the core xmpponent.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onUnknownPresenceReceived(Stanzas.Presence presence)
		{
			DebugWrite("onUnknownPresenceReceived not implemented.");
		}

		/// <summary>
		/// Called each time an info query is received.  This method determines
		/// the type of query, and calls the appropriate callback.
		///
		/// This should only be overridden if you know why you are overriding
		/// it.
		/// </summary>
		/// <param name="iq">InfoQuery.</param>
		public virtual void onInfoQueryReceived(Stanzas.InfoQuery iq)
		{
			if(iq.qType.ToLower() == "get")
			{
				if(iq.iType.ToLower() == "disco#info") { onDiscoQueryReceived(iq); }
				else if(iq.iType.ToLower() == "vcard") { onVCardQueryReceived(iq); }
				else { onUnknownInfoQueryReceived(iq); }
			}
			else
			{
				onUnknownInfoQueryReceived(iq);
			}
		}

		/// <summary>
		/// Called when an info query seeking information on available
		/// services is received.
		/// </summary>
		/// <param name="iq">Iq.</param>
		public virtual void onDiscoQueryReceived(Stanzas.InfoQuery iq)
		{
			DebugWrite("onDiscoQueryReceived not implemented.");
		}

		/// <summary>
		/// Called when a remote user is attempting to retrieve a
		/// component user's vCard.
		/// </summary>
		/// <param name="iq">Iq.</param>
		public virtual void onVCardQueryReceived(Stanzas.InfoQuery iq)
		{
			DebugWrite("onVCardQueryReceived not implemented.");
		}

		public virtual void onUnknownInfoQueryReceived(Stanzas.InfoQuery iq)
		{
			DebugWrite("onUnknownInfoQueryReceived not implemented.");
		}

		/// <summary>
		/// Called when the xmpp server sends a stream error message.
		/// </summary>
		/// <param name="error">Error.</param>
		public virtual void onStreamError(Stanzas.StreamError error)
		{
			DebugWrite("onStreamError not implemented.");
		}

		/// <summary>
		/// Called when the xmpp server indicates it is ending the stream.
		/// </summary>
		/// <param name="streamend">Streamend.</param>
		public virtual void onStreamEnd(Stanzas.StreamEnd streamend)
		{
			DebugWrite("Stream closed by server.  Good bye.");
		}

		/// <summary>
		/// Called if a stanza is received that the component engine doesn't
		/// know how to interpret.
		/// </summary>
		/// <param name="stanza">Stanza.</param>
		public virtual void onUndefinedStanzaReceived(Stanzas.Stanza stanza)
		{
			DebugWrite("Undefined/blank stanza received from server.");
		}
	}
}

