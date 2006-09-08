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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using GnomeKeyring;

namespace lat
{
	public class Connection : IEqualityComparer<Connection>
	{
		ConnectionData settings;
		LdapServer server;
		ServerData data;
		
		List<string> serverViews;
		List<string> attributeViewers;

		public Connection (ConnectionData connectionData)
		{
			server = new LdapServer (connectionData);
			data = new ServerData (server);
			settings = connectionData;
			
			serverViews = new List<string> ();
			attributeViewers = new List<string> ();			
		}
		
		public void Bind (string userName, string userPass)
		{
			settings.UserName = userName;
			settings.Pass = userPass;
			
			server.Bind (userName, userPass);
		}
		
		public void Connect ()
		{
			if (settings.DontSavePassword) {
				LoginDialog ld = new LoginDialog (settings.UserName);				
				
				bool res = ld.Run ();
				if (res) {
					settings.UserName = ld.UserName;
					settings.Pass = ld.UserPass;
				} else { settings.Pass = "dog"; }
			}
		
			try {
			
				server.Connect (settings.Encryption);			
				server.Bind (settings.UserName, settings.Pass);
			
				if (!server.Connected) {
				
					string msg = String.Format (
						Mono.Unix.Catalog.GetString (
						"Unable to connect to: ldap://{0}:{1}"),
						server.Host, server.Port);
				
					throw new ApplicationException (msg);
				}
					
				if (!server.Bound && settings.UserName != "") {
				
					string msg = String.Format (
						Mono.Unix.Catalog.GetString (
						"Unable to bind to: ldap://{0}:{1}"),
						server.Host, server.Port);
						
					throw new ApplicationException (msg);
				}

			} catch (Exception e) {

				Log.Debug (e);
				throw e;
			}
		}
		
		public void Disconnect ()
		{
			server.Disconnect ();
		}
		
		public void StartTLS ()
		{
			server.StartTLS ();
		}
		
		public override bool Equals (object obj)
		{
			if (obj is Connection)
				return Equals (this, (Connection)obj);

			return false;
		}

		public bool Equals (Connection lhs, Connection rhs)
		{
			if (lhs.Settings.Name.Equals (rhs.Settings.Name))
				return true;

			return false;
		}

		public override int GetHashCode ()
		{
			return GetHashCode (this);
		}

		public int GetHashCode (Connection conn)
		{
			return conn.Settings.Name.GetHashCode ();
		}			
			
		public void SetDefaultAttributeViewers ()
		{			
			attributeViewers.Add ("lat.JpegAttributeViewPlugin");
			attributeViewers.Add ("lat.PassswordAttributeViewPlugin");
		}

		public void SetDefaultServerViews ()
		{
			switch (settings.ServerType) {
			
			case LdapServerType.OpenLDAP:
			case LdapServerType.FedoraDirectory:
			case LdapServerType.Generic:
				serverViews.Add ("lat.PosixComputerViewPlugin");
				serverViews.Add ("lat.PosixContactsViewPlugin");
				serverViews.Add ("lat.PosixGroupViewPlugin");
				serverViews.Add ("lat.PosixUserViewPlugin");			
				break;
			
			case LdapServerType.ActiveDirectory:
				serverViews.Add ("lat.ActiveDirectoryComputerViewPlugin");
				serverViews.Add ("lat.ActiveDirectoryContactsViewPlugin");
				serverViews.Add ("lat.ActiveDirectoryGroupViewPlugin");
				serverViews.Add ("lat.ActiveDirectoryUserViewPlugin");			
				break;
				
			default:	
				throw new ArgumentOutOfRangeException ("Invalid server type");
			}			
		}

		public override string ToString ()
		{
			return settings.Name;
		}

		public ActiveDirectoryInfo ActiveDirectory
		{
			get { return server.ADInfo; }
		}

		public string AuthDN
		{
			get { return server.AuthDN; }
		}

		public string DirectoryRoot
		{
			get { return server.DirectoryRoot; }
		}

		public bool IsBound
		{
			get { return server.Bound; }
		}

		public bool IsConnected
		{
			get { return server.Connected; }
		}
		
		public int Protocol
		{
			get { return server.Protocol; }
		}
		
		public bool UseSSL
		{
			get { return server.UseSSL; }
			set { server.UseSSL = value; }
		}
		
		public ConnectionData Settings
		{
			get { return settings; }
			set { settings = value; }
		}
		
		public ServerData Data
		{
			get {
				if (!this.IsConnected)
					this.Connect ();
						
				return data;
			}
		}
		
		public List<string> ServerViews
		{
			get { return serverViews; }
			set { serverViews = value; }
		}
		
		public List<string> AttributeViewers
		{
			get { return attributeViewers; }
			set { attributeViewers = value; }
		}
	}

	public class ConnectionData
	{
		string name;
		string hostName;
		int port;
		string directoryRoot;
		string userName;
		string password;
		bool dontSavePassword;
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
			this.dontSavePassword = savePassword;
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
		
		public string Pass 
		{
			get {		
				if (password == null) {
					GnomeKeyring.Result gkr;
					NetworkPasswordData[] list;

					gkr = GnomeKeyring.Global.FindNetworkPassword (userName, out list);					
					if (list.Length > 0) {
						NetworkPasswordData npd = list[0];
						password = npd.Password;
					} else {
						Log.Warn ("Unable to get password from GNOME keyring. Got result: {0}", gkr);
					}
				} 
				
				return password;
			}
			
			set { password = value; }
		}
		
		public bool DontSavePassword
		{
			get { return dontSavePassword; }
			set { dontSavePassword = value; }
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

	public class ConnectionCollection : ICollection<Connection>
	{
		List<Connection> connections;

		public ConnectionCollection ()
		{
			connections = new List<Connection> ();
		}

		public void Add (Connection conn)
		{
			if (conn == null)
				throw new ArgumentNullException ("conn");
		
			Log.Debug ("Connection manager adding connection {0}", conn.Settings.Name);		
			connections.Add (conn);
		}

		public void Clear ()
		{
			connections.Clear ();
		}

		public bool Contains (Connection conn)
		{
			if (connections.Contains (conn))
				return true;

			return false;
		}

		public bool Contains (string name)
		{
			foreach (Connection c in connections)
				if (name == c.ToString())
					return true;
					
			return false;
		}

		public void CopyTo (Connection[] array, int arrayIndex)
		{
			int count = 0;

			foreach (Connection c in connections) {
				array [arrayIndex + count] = c;
				count++;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return connections.GetEnumerator ();
		}

		public IEnumerator<Connection> GetEnumerator ()
		{
			return connections.GetEnumerator ();
		}

		public void Remove (string name)
		{	
			Connection conn = null;
			foreach (Connection c in connections)
				if (name == c.ToString())
					conn = c;
					
			if (conn != null)
				connections.Remove (conn);
		}

		public bool Remove (Connection conn)
		{
			return connections.Remove (conn);
		}

		public int Count
		{
			get { return connections.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}
		
		public Connection this [string name]
		{
			get {
				Connection conn = null;
			
				foreach (Connection c in connections)
					if (c.Settings.Name == name)
						conn = c;				
				
				return conn;
			}
			
			set { connections.Add (value); }
		}
	}

	public class ConnectionManager
	{
		string connectionDataFileName;
		ConnectionCollection connections;

		public ConnectionManager ()
		{
			connections = new ConnectionCollection ();
		
			string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal); 
			string latDir = Path.Combine (homeDir, ".lat");			
			connectionDataFileName = Path.Combine (latDir, "profiles.xml");

			if (!Directory.Exists (latDir)) 
				Directory.CreateDirectory (latDir);

			if (!File.Exists (connectionDataFileName))
				return;
				
			LoadConnectionData ();
		}
			
		public Connection this [string name]
		{
			get {
				if (!connections.Contains (name))
					return null;
					
				Connection conn = connections[name];
				
				return conn;
			}
			
			set { connections[name] = value; }
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
					Util.GetServerType (profileElement.GetAttribute ("server_type")),
					false);
					
			Connection conn = new Connection (data);					

			XmlNodeList nl = profileElement.GetElementsByTagName ("server_view");
			if ((nl.Count > 0)) {
				foreach (XmlElement sv in nl)
					conn.ServerViews.Add (sv.InnerText);
			}

			nl = profileElement.GetElementsByTagName ("attribute_viewer");
			if ((nl.Count > 0)) {
				foreach (XmlElement av in nl)
					conn.AttributeViewers.Add (av.InnerText);
			}

			connections.Add (conn);
		}

		static void KeyringCallback (Result result, uint val) 
		{
			if (result != Result.Ok)
				Log.Info ("Failed to save password in GNOME keyring: result: {0} - ID: {1}", result, val);
		}
		
		public void Save ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.AppendChild (doc.CreateNode (XmlNodeType.XmlDeclaration,"",""));
			
			XmlElement profiles = doc.CreateElement ("profiles");
			doc.AppendChild (profiles);

			foreach (Connection c in connections) {
				
				if (c.Settings.Dynamic)
					continue;
					
				XmlElement profile = doc.CreateElement ("profile");
				profile.SetAttribute ("name", c.Settings.Name);
				profile.SetAttribute ("host", c.Settings.Host);
				profile.SetAttribute ("port", c.Settings.Port.ToString());
				profile.SetAttribute ("base", c.Settings.DirectoryRoot);
				profile.SetAttribute ("user", c.Settings.UserName);
				profile.SetAttribute ("save_password", c.Settings.DontSavePassword.ToString());
				profile.SetAttribute ("encryption", Util.GetEncryptionType (c.Settings.Encryption));
				profile.SetAttribute ("server_type", Util.GetServerType (c.Settings.ServerType));
				
				if (c.ServerViews.Count == 0)
					c.SetDefaultServerViews ();
				
				XmlElement server_views = doc.CreateElement ("server_views");
				foreach (string sv in c.ServerViews) {
					XmlElement server_view = doc.CreateElement ("server_view");
					server_view.InnerText = sv;
					server_views.AppendChild (server_view);
				}				
				profile.AppendChild (server_views);
				
				if (c.ServerViews.Count == 0) 
					c.SetDefaultAttributeViewers ();
				
				XmlElement attribute_viewers = doc.CreateElement ("attribute_viewers");
				foreach (string a in c.AttributeViewers) {
					XmlElement av = doc.CreateElement ("attribute_viewer");
					av.InnerText = a;
					attribute_viewers.AppendChild (av);
				}				
				profile.AppendChild (attribute_viewers);
				
				profiles.AppendChild (profile);
				
				OperationGetIntCallback theCallback = new OperationGetIntCallback (KeyringCallback);
				GnomeKeyring.Global.SetNetworkPassword(
					c.Settings.UserName,		// user
					c.Settings.Host,			// server 
					"ldap", 					// protocol
					(uint)c.Settings.Port, 		// port
					c.Settings.Pass,	 		// password
					theCallback 				// callback
				);				
			}
	
			doc.Save (connectionDataFileName);
		}

		public void Delete (string name)
		{
			connections.Remove (name);
		}
		
		public string[] ConnectionNames
		{
			get { 
				List<string> names = new List<string> ();
				foreach (Connection c in connections)
					names.Add (c.ToString());
				
				names.Sort ();
				return names.ToArray();
			}
		}
	}
}