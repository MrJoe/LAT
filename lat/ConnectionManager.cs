// 
// lat - ConnectionManager.cs
// Author: Loren Bandiera
// Copyright 2005-2006 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; Version 2 
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
//
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using System.Collections;
using System.Collections.Specialized;
using System.Net.Sockets;
using Novell.Directory.Ldap;

namespace lat
{
	public class Connection
	{
		ConnectionData properties;
		LdapServer server;
		ServerData data;
		
		List<string> serverViews;
		List<string> attributeViewers;
	
		public Connection (ConnectionData connectionData)
		{
			server = new LdapServer (connectionData);
			data = new ServerData (server);
			properties = connectionData;
			
			serverViews = new List<string> ();
			attributeViewers = new List<string> ();
		}
		
		public void AddServerView (string viewName)
		{
			if (viewName == null)
				throw new ArgumentNullException ();
				
			serverViews.Add (viewName);
		}
		
		public void AddAttributeViewer (string viewerName)
		{
			if (viewerName == null)
				throw new ArgumentNullException ();
				
			attributeViewers.Add (viewerName);		
		}
		
		public void RemoveServerView (string viewName)
		{
			if (viewName == null)
				throw new ArgumentNullException ();
				
			serverViews.Remove (viewName);
		}
		
		public void RemoveAttributeViewer (string viewerName)
		{
			if (viewerName == null)
				throw new ArgumentNullException ();
				
			attributeViewers.Remove (viewerName);		
		}
		
		public ConnectionData Properties
		{
			get { return properties; }
		}
		
		public ServerData Data
		{
			get { return data; }
		}
	}

	public class ConnectionData
	{
		string name;
		string hostName;
		int port;
		string directoryRoot;
		string userName;
		bool savePassword;
		EncryptionType encryptionType;
		LdapServerType serverType;
		bool dynamic;
	
		public ConnectionData()
		{
		}
		
		public ConnectionData(string name, string hostName, int port, string directoryRoot, string userName, bool savePassword, EncryptionType encryptionType, LdapServerType serverType, bool dynamic)
		{
			this.name = name;
			this.hostName = hostName;
			this.port = port;
			this.directoryRoot = directoryRoot;
			this.userName = userName;
			this.savePassword = savePassword;
			this.encryptionType = encryptionType;
			this.serverType = serverType;
			this.dynamic = dynamic;
		}
		
		public string Name
		{
			get { return name; }
			set { name = value; }
		}
		
		public string Host
		{
			get { return hostName; }
			set { hostName = value; }
		}
		
		public int Port
		{
			get { return port; }
			set { port = value; }
		}
		
		public string DirectoryRoot
		{
			get { return directoryRoot; }
			set { directoryRoot = value; }
		}
		
		public string UserName
		{
			get { return userName; }
			set { userName = value; }
		}
		
		public bool SavePassword
		{
			get { return savePassword; }
			set { savePassword = value; }
		}
		
		public EncryptionType Encryption
		{
			get { return encryptionType; }
			set { encryptionType = value; }
		}
		
		public LdapServerType ServerType
		{
			get { return serverType; }
			set { serverType = value; }
		}
		
		public bool Dynamic
		{
			get { return dynamic; }
			set { dynamic = value; }
		}
	}

	public class ConnectionManagerNG
	{
		string connectionDataFileName;
	
		public ConnectionManagerNG ()
		{
			string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal); 
			string latDir = Path.Combine (homeDir, ".lat");			
			connectionDataFileName = Path.Combine (latDir, "profiles.xml");

			if (!Directory.Exists (latDir)) 
				Directory.CreateDirectory (latDir);

			if (!File.Exists (connectionDataFileName))
				return;
				
			LoadConnectionData ();
		}
		
		void LoadConnectionData ()
		{
			try {

				XmlDocument doc = new XmlDocument ();
				doc.Load (connectionDataFileName);

				XmlElement profileRoot = doc.DocumentElement;
				XmlNodeList nl = profileRoot.GetElementsByTagName ("profile");

				if (!(nl.Count > 0))
					return;

				foreach (XmlElement p in nl)
					ParseConnectionData (p);

			} catch (Exception e) {

				Log.Warn (e);

			}		
		}
		
		void ParseConnectionData (XmlElement profileElement)
		{
			EncryptionType e = EncryptionType.None;
			bool savePassword = false;

			string encryption = profileElement.GetAttribute ("encryption");
			if (encryption != null) {
				if (encryption.ToLower() == "ssl")
					e = EncryptionType.SSL;
				else if (encryption.ToLower() == "tls")
					e = EncryptionType.TLS;
			}

			string sp = profileElement.GetAttribute ("save_password");
			if (sp != null)
				savePassword = bool.Parse (sp);

			ConnectionData data = new ConnectionData (
					profileElement.GetAttribute ("name"),
					profileElement.GetAttribute ("host"),
					int.Parse (profileElement.GetAttribute ("port")),
					profileElement.GetAttribute ("base"),
					profileElement.GetAttribute ("user"),
					savePassword,
					e,
					LdapServer.GetServerType (profileElement.GetAttribute ("server_type")),
					false);
					
			Connection conn = new Connection (data);					

			XmlNodeList nl = profileElement.GetElementsByTagName ("server_view");
			if ((nl.Count > 0)) {
				foreach (XmlElement sv in nl)
					conn.AddServerView (sv.InnerText);
			}

			nl = profileElement.GetElementsByTagName ("attribute_viewer");
			if ((nl.Count > 0)) {
				foreach (XmlElement av in nl)
					conn.AddAttributeViewer (av.InnerText);
			}

//			connectionData.Add (cd);
		}
		
		void SaveConnectionData ()
		{
		}
	}

// =======================================================================================


	public class ConnectionManager : IEnumerable
	{
		ListDictionary serverDictionary;
		Gtk.Window parent;
		
		public ConnectionManager (Gtk.Window parentWindow)
		{
			serverDictionary = new ListDictionary ();
			parent = parentWindow;
		}

		public int Length
		{
			get { return serverDictionary.Count; }
		}
		
		public IEnumerator GetEnumerator ()
		{
			return serverDictionary.GetEnumerator ();
		}
		
		public LdapServer this [ConnectionProfile cp]
		{
			get { 
		
				LdapServer srv = (LdapServer) serverDictionary [cp.Name];
				if (srv == null) {
			
					Log.Debug ("Starting connection to {0}", cp.Host);
					
					if (cp.LdapRoot == "")
						srv = new LdapServer (cp.Host, cp.Port, cp.ServerType);
					else
						srv = new LdapServer (cp.Host, cp.Port, cp.LdapRoot, cp.ServerType);
				
					srv.ProfileName = cp.Name;				

					if (cp.DontSavePassword) {

						LoginDialog ld = new LoginDialog (
							Mono.Unix.Catalog.GetString ("Enter your password"), 
							cp.User);

						ld.Run ();

						DoConnect (srv, cp.Encryption, ld.UserName, ld.UserPass);

					} else {

						DoConnect (srv, cp.Encryption, cp.User, cp.Pass);
					}
					
					if (CheckConnection (srv, cp.User))
						return srv;
					else
						return null;
						
				} else {
					return srv;
				}
			}
						
			set { serverDictionary[cp.Name] = value; }
		}
		
		bool CheckConnection (LdapServer server, string userName)
		{
			string msg = null;

			if (server == null)
				return false;

			if (!server.Connected) {

				msg = String.Format (
					Mono.Unix.Catalog.GetString (
					"Unable to connect to: ldap://{0}:{1}"),
					server.Host, server.Port);
			}

			if (!server.Bound && msg == null && userName != "") {

				msg = String.Format (
					Mono.Unix.Catalog.GetString (
					"Unable to bind to: ldap://{0}:{1}"),
					server.Host, server.Port);
			}

			if (msg != null) {

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Connection Error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			
				return false;
			}
		
			return true;
		}

		void DoConnect (LdapServer server, EncryptionType encryption, string userName, string userPass)
		{
			try {
				server.Connect (encryption);
				server.Bind (userName, userPass);

			} catch (SocketException se) {

				Log.Debug ("Socket error: {0}", se.Message);

			} catch (LdapException le) {

				Log.Debug ("Ldap error: {0}", le.Message);

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Connection error",
					le.Message);

				dialog.Run ();
				dialog.Destroy ();

				return;

			} catch (Exception e) {

				Log.Debug ("Unknown error: {0}", e.Message);

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Unknown connection error",
					Mono.Unix.Catalog.GetString ("An unknown error occured: ") + e.Message);

				dialog.Run ();
				dialog.Destroy ();

				return;
			}
		}		
	}
}