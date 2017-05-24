using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Net;
using System.Configuration;
using System.Security.Permissions;
using System.Diagnostics;

namespace xmpponent
{
	public class Component
	{
		public static Component Eng;

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

		private bool _AutoSendReceipt = true;
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="componentig.Component"/> automatically
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
		}

		public void Run()
		{
			Console.WriteLine("Launching component");

			onLoad();

			if(this.ComponentAddress == "")
			{
				Console.WriteLine("A component address must be specified.");
				return;
			}

			if(this.ServerAddress == "")
			{
				Console.WriteLine("A server address must be specified.");
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
							//Console.WriteLine("'>' found in string.");
							InBuffer = InBuffer.Replace("<", "");
							InBuffer = InBuffer.Replace(">", "");

							String[] prts = InBuffer.Split(new char[]{' '}, StringSplitOptions.None);
							if(prts[0].ToLower() == "stream:stream")
							{
								//Console.WriteLine("prts.len {0}", prts.Length);

								for(int eprt = 1; eprt < prts.Length; eprt++)
								{
									string k;
									string v;

									GetTagKeyVal(prts[eprt], out k, out v);
									//Console.WriteLine("k {0} v {1}", k, v);

									if(k.ToLower() == "id")
									{
										
										StreamID = v;
										StreamsInitialized = true;

										string hndsh = "<handshake>" +
											GenerateHandshake() +
											"</handshake>";

										Byte[] data = System.Text.Encoding.ASCII.GetBytes(hndsh);
										SockStream.Write(data, 0, data.Length);

										//Console.WriteLine("Handshake sent to server.");
									}
								}

								InBuffer = "";
							}
							else
							{
								//	Invalid stream from server ?
								Console.WriteLine("The xmpp server did not return a stream element.");
								dorun = false;
							}

						}
					}
					else if(!Handsshaken)
					{
						//	Hands not yet shaken
						if(InBuffer.IndexOf("<handshake/>") > -1)
						{
							Console.WriteLine("Connected and ready to go!");
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
							Console.WriteLine("Incoming stanza was not parsed (it's null).");
							Console.WriteLine(InBuffer);
						}
						else if(inStanza.GetType() == typeof(Stanzas.Presence))
						{
							onPresenceReceived((Stanzas.Presence)inStanza);
						}
						else if(inStanza.GetType() == typeof(Stanzas.Message))
						{
							//Console.WriteLine("Message stanza received from server.");

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

					Console.WriteLine("Connected to {0}:{1}", ServerAddress, Port);

					SockStream = TcpSocket.GetStream();
					string strmhead = "<stream:stream"+'\n'+
						"xmlns='jabber:component:accept'" + '\n' +
						"xmlns:stream='http://etherx.jabber.org/streams'" +'\n' +
						"to='" + ComponentAddress + "'>";

					Byte[] data = System.Text.Encoding.ASCII.GetBytes(strmhead);
					SockStream.Write(data, 0, data.Length);

					Console.WriteLine("Component >> Server Stream Initialized");

				}
				catch(Exception ex)
				{
					Console.WriteLine("Failed to connect to xmpp server.");
					Console.WriteLine("Exception:");
					Console.WriteLine(ex.Message);
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



		public bool SendMessage(Stanzas.Message message)
		{
			if(message.To == "")
			{
				Console.WriteLine("SendMessage: Failed due to no recipient.");
				return false;
			}

			if(message.From == "")
			{
				Console.WriteLine("SendMessage: Failed due to no sender.");
				return false;
			}

			if(message.Body == "")
			{
				Console.WriteLine("SendMessage: Failed due to no body.");
				return false;
			}

			if(message.mType == "") { message.mType = "chat"; }
			if(message.StanzaBy == "") { message.StanzaBy = message.From; }
			if(message.StanzaId == "")
			{
				message.StanzaId = "doo-comp-" + StanzaID.ToString();
				StanzaID++;
			}

			if(message.Thread == "") { message.Thread = "doo-comp-threadx"; }

			Byte[] data = System.Text.Encoding.ASCII.GetBytes(message.GenerateXML());
			SockStream.Write(data, 0, data.Length);
			return true;
		}

		public bool SendPresence(Stanzas.Presence presence)
		{
			if(presence.From == "")
			{
				Console.WriteLine("SendPresence: Failed due to no sender.");
				return false;
			}

			if(presence.To == "")
			{
				Console.WriteLine("SendPresence: Failed because components require a recipient.");
				return false;
			}

			string tosend = presence.GenerateXML();
			/*Console.WriteLine("-------------------");
			Console.WriteLine(tosend);
			Console.WriteLine("-------------------");*/
			Byte[] data = System.Text.Encoding.ASCII.GetBytes(tosend);
			SockStream.Write(data, 0, data.Length);
			return true;
		}

		public bool SendReceipt(Stanzas.Receipt receipt)
		{
			if(receipt.To == "")
			{
				Console.WriteLine("SendReceipt: Failed due to no recipient.");
				return false;
			}

			if(receipt.From == "")
			{
				Console.WriteLine("SendReceipt: Failed due to no sender.");
				return false;
			}

			if(receipt.MessageID == "")
			{
				Console.WriteLine("SendReceipt: Failed due to no message id.");
				return false;
			}

			string tosend = receipt.GenerateXML();

			/*Console.WriteLine("Outgoing Message Receipt:");
			Console.WriteLine(tosend);
			Console.WriteLine("----------------------------");*/
			Byte[] data = System.Text.Encoding.ASCII.GetBytes(tosend);
			SockStream.Write(data, 0, data.Length);

			return true;
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
			Console.WriteLine("onMessageReceived not implemented.");
		}

		/// <summary>
		/// Called when a message receipt is received.
		/// </summary>
		/// <param name="message">Message.</param>
		public virtual void onMessageReceiptReceived(Stanzas.Message message)
		{
			Console.WriteLine("onMessageReceiptReceived not implemented.");
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
			else
			{
				Console.WriteLine("Unknown Presence Type: {0}", presence.pType);
			}
		}

		/// <summary>
		/// Called each time another user requests a subscription from
		/// a component user.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onSubscribeRequested(Stanzas.Presence presence)
		{
			Console.WriteLine("onSubscribeRequested not implemented.");
		}

		/// <summary>
		/// Called each time another user requests to be unsubscribed
		/// from a component user.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onUnsubscribeRequested(Stanzas.Presence presence)
		{
			Console.WriteLine("onUnsubscribeRequested not implemented.");
		}

		/// <summary>
		/// Called each time a remote user confirms a subscription from
		/// one of the component users.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onSubscribeConfirmed(Stanzas.Presence presence)
		{
			Console.WriteLine("onSubscribeConfirmed not implemented.");
		}

		/// <summary>
		/// Called each time a remote user confirms that it has unsubscribed
		/// a component user.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onUnsubscribeConfirmed(Stanzas.Presence presence)
		{
			Console.WriteLine("onUnsubscribeConfirmed not implemented.");
		}

		/// <summary>
		/// Called each time the server informs us that a contact has
		/// gone unavailable.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onPresenceUnavailable(Stanzas.Presence presence)
		{
			Console.WriteLine("onPresenceUnavailable not implemented.");
		}

		/// <summary>
		/// Called each time ther server informs us that a contact has
		/// become available.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onPresenceAvailable(Stanzas.Presence presence)
		{
			Console.WriteLine("onPresenceAvailable not implemented.");
		}

		/// <summary>
		/// Called each time the server is probed for the presence of
		/// a component user.
		/// </summary>
		/// <param name="presence">Presence.</param>
		public virtual void onPresenceProbe(Stanzas.Presence presence)
		{
			Console.WriteLine("onPresenceProbe not implemented.");
		}

		/// <summary>
		/// Called each time an info query is received.
		/// </summary>
		/// <param name="iq">Iq.</param>
		public virtual void onInfoQueryReceived(Stanzas.InfoQuery iq)
		{
			Console.WriteLine("onInfoQueryReceived not implemented.");
		}

		/// <summary>
		/// Called when the xmpp server sends a stream error message.
		/// </summary>
		/// <param name="error">Error.</param>
		public virtual void onStreamError(Stanzas.StreamError error)
		{
			Console.WriteLine("onStreamError not implemented.");
		}

		/// <summary>
		/// Called when the xmpp server indicates it is ending the stream.
		/// </summary>
		/// <param name="streamend">Streamend.</param>
		public virtual void onStreamEnd(Stanzas.StreamEnd streamend)
		{
			Console.WriteLine("Stream closed by server.  Good bye.");
		}

		/// <summary>
		/// Called if a stanza is received that the component engine doesn't
		/// know how to interpret.
		/// </summary>
		/// <param name="stanza">Stanza.</param>
		public virtual void onUndefinedStanzaReceived(Stanzas.Stanza stanza)
		{
			Console.WriteLine("Undefined/blank stanza received from server.");
		}
	}
}

