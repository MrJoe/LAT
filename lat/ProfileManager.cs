// 
// lat - ProfileManager.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
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
using System.IO;
using System.Xml;
using GnomeKeyring;

namespace lat
{
	public struct ConnectionProfile 
	{
		public string Name;
		public string Host;
		public int Port;
		public string LdapRoot;
		public string User;
		public string Pass;
		public bool DontSavePassword;
		public EncryptionType Encryption;
		public string ServerType;
	}

	public class ProfileManager 
	{
		private string _configFile;
		private Hashtable _profiles;

		public ProfileManager ()
		{
			_profiles = new Hashtable ();
		
			string dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			string tmp = Path.Combine (dir, ".lat");
			
			_configFile = Path.Combine (tmp, "profiles.xml");

			DirectoryInfo di = new DirectoryInfo (tmp);
			if (!di.Exists)
				di.Create ();

			FileInfo fi = new FileInfo (_configFile);
			if (!fi.Exists)
				return;
			
			loadProfiles ();		
		}
		
		public string[] getProfileNames ()
		{
			string[] retVal = new string [_profiles.Count];
			int count = 0;
			
			foreach (string s in _profiles.Keys) {

				ConnectionProfile cp = (ConnectionProfile) _profiles[s];
				retVal [count] = cp.Name;
				
				count++;
			}
			
			return retVal;
		}
		
		public ConnectionProfile Lookup (string name)
		{
			return (ConnectionProfile) _profiles[name];
		}
		
		public void addProfile (ConnectionProfile cp)
		{
			_profiles.Add (cp.Name, cp);
		}
		
		public void updateProfile (ConnectionProfile cp)
		{
			_profiles[cp.Name] = cp;
		}
		
		public void deleteProfile (string name)
		{
			_profiles.Remove (name);
		}

		private void ParseProfile (XmlElement profileElement)
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

			ConnectionProfile cp = new ConnectionProfile ();
			cp.Name = profileElement.GetAttribute ("name");
			cp.Host = profileElement.GetAttribute ("host");
			cp.Port = int.Parse (profileElement.GetAttribute ("port"));
			cp.LdapRoot = profileElement.GetAttribute ("base");
			cp.User = profileElement.GetAttribute ("user");
			cp.Pass = "";
			cp.DontSavePassword = savePassword;
			cp.Encryption = e;
			cp.ServerType = profileElement.GetAttribute ("server_type");

			GnomeKeyring.Result gkr;
			NetworkPasswordData[] list;

			gkr = GnomeKeyring.Global.FindNetworkPassword (
				cp.User, out list );

			Logger.Log.Debug ("gnome-keyring-result: {0}", gkr);
						
			foreach (NetworkPasswordData i in list) 
				cp.Pass = i.Password;

			_profiles.Add (cp.Name, cp);
		}
		
		public void loadProfiles ()
		{
			try {

				XmlDocument doc = new XmlDocument ();
				doc.Load (_configFile);

				XmlElement profileRoot = doc.DocumentElement;
				XmlNodeList nl = profileRoot.GetElementsByTagName ("profile");

				if (!(nl.Count > 0))
					return;

				foreach (XmlElement p in nl)
					ParseProfile (p);

				doc = null;

			} catch (Exception e) {

				Console.WriteLine (e.Message);
			}
		}
		
		private static void myCallback (Result result, uint val) 
		{
			Logger.Log.Debug ("gnome-keyring-callback: result: {0} - ID: {1}", result, val);
		}

		public void saveProfiles ()	
		{	
			XmlTextWriter writer = new XmlTextWriter(_configFile,
						System.Text.Encoding.UTF8);
						
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument(false);
			
			writer.WriteStartElement("profiles");
			
			foreach (string name in _profiles.Keys) {
		     			 	
				ConnectionProfile cp = (ConnectionProfile) _profiles[name];
		 	
				writer.WriteStartElement("profile", null);
			 		
				writer.WriteAttributeString ("name", cp.Name);
				writer.WriteAttributeString ("host", cp.Host);
				writer.WriteAttributeString ("port", cp.Port.ToString());
				writer.WriteAttributeString ("base", cp.LdapRoot);
				writer.WriteAttributeString ("user", cp.User);
				writer.WriteAttributeString ("save_password", cp.DontSavePassword.ToString());

				switch (cp.Encryption) {

				case EncryptionType.TLS:
					writer.WriteAttributeString ("encryption", "tls");
					break;

				case EncryptionType.SSL:
					writer.WriteAttributeString ("encryption", "ssl");
					break;

				case EncryptionType.None:
					writer.WriteAttributeString ("encryption", "none");
					break;

				default:
					break;
				}

				writer.WriteAttributeString ("server_type", cp.ServerType);
				
	        		writer.WriteEndElement();

				OperationGetIntCallback theCallback = new OperationGetIntCallback (myCallback);

				GnomeKeyring.Global.SetNetworkPassword(
					cp.User,			// user
					cp.Host,			// server 
					"ldap", 			// protocol
					(uint)cp.Port, 			// port
					cp.Pass,	 		// password
					theCallback 			// callback
				);
			}
	    		
			writer.WriteEndElement();
			
			writer.Flush();
			writer.Close();	
		}
		
	}
}
