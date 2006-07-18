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
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using GnomeKeyring;

namespace lat
{
	public class ConnectionProfile 
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
		public bool Dynamic;
		public ArrayList ActiveAttributeViewers;		
		public ArrayList ActiveServerViews;
		
		public ConnectionProfile ()
		{
		}
		
		public void SetDefaultServerViews ()
		{
			ActiveServerViews = new ArrayList ();

			switch (this.ServerType.ToLower ()) {
			
			case "openldap":
			case "fedora directory server":
			case "generic ldap server":			
				ActiveServerViews.Add ("lat.PosixComputerViewPlugin");
				ActiveServerViews.Add ("lat.PosixContactsViewPlugin");
				ActiveServerViews.Add ("lat.PosixGroupViewPlugin");
				ActiveServerViews.Add ("lat.PosixUserViewPlugin");			
				break;
				
			case "microsoft active directory":
				ActiveServerViews.Add ("lat.ActiveDirectoryComputerViewPlugin");
				ActiveServerViews.Add ("lat.ActiveDirectoryContactsViewPlugin");
				ActiveServerViews.Add ("lat.ActiveDirectoryGroupViewPlugin");
				ActiveServerViews.Add ("lat.ActiveDirectoryUserViewPlugin");			
				break;
				
			default:	
				throw new ArgumentOutOfRangeException (this.ServerType);
			}
			
			Log.Debug ("Active server views: {0}", ActiveServerViews.Count);
		}

		public void SetDefaultAttributeViewers ()
		{
			ActiveAttributeViewers = new ArrayList ();

			ActiveAttributeViewers.Add ("lat.JpegAttributeViewPlugin");
			ActiveAttributeViewers.Add ("lat.PassswordAttributeViewPlugin");
		}	
	}

	public class ProfileManager : IEnumerable
	{
		string configFileName;
		ListDictionary profileDictionary;
		
		public ProfileManager ()
		{
			profileDictionary = new ListDictionary ();
			
			string dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			string tmp = Path.Combine (dir, ".lat");
			
			configFileName = Path.Combine (tmp, "profiles.xml");

			DirectoryInfo di = new DirectoryInfo (tmp);
			if (!di.Exists)
				di.Create ();

			FileInfo fi = new FileInfo (configFileName);
			if (!fi.Exists)
				return;
			
			LoadProfiles ();		
		}
		
		public string[] GetProfileNames ()
		{
			string[] retVal = new string [profileDictionary.Count];
			int count = 0;

			foreach (string s in profileDictionary.Keys) {
				ConnectionProfile cp = (ConnectionProfile) profileDictionary[s];
				retVal [count] = cp.Name;				
				count++;
			}
			
			return retVal;
		}
		
		public int Length
		{
			get { return profileDictionary.Count; }
		}
		
		public IEnumerator GetEnumerator ()
		{
			return profileDictionary.GetEnumerator ();
		}
		
		public ConnectionProfile this [string name]
		{
			get { return (ConnectionProfile) profileDictionary[name]; }
			set { profileDictionary[name] = value; }
		}
		
		public void Remove (string profileName)
		{
			profileDictionary.Remove (profileName);
		}

		void ParseProfile (XmlElement profileElement)
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
			cp.Dynamic = false;

			XmlNodeList nl = profileElement.GetElementsByTagName ("server_view");
			if ((nl.Count > 0)) {
				cp.ActiveServerViews = new ArrayList ();
				foreach (XmlElement sv in nl)
					cp.ActiveServerViews.Add (sv.InnerText);
			}

			nl = profileElement.GetElementsByTagName ("attribute_viewer");
			if ((nl.Count > 0)) {
				cp.ActiveAttributeViewers = new ArrayList ();
				foreach (XmlElement av in nl)
					cp.ActiveAttributeViewers.Add (av.InnerText);
			}

			GnomeKeyring.Result gkr;
			NetworkPasswordData[] list;

			gkr = GnomeKeyring.Global.FindNetworkPassword (cp.User, out list);
			Log.Debug ("gnome-keyring-result: {0}", gkr);
						
			foreach (NetworkPasswordData i in list)
				cp.Pass = i.Password;

			profileDictionary.Add (cp.Name, cp);
		}
		
		public void LoadProfiles ()
		{
			try {

				XmlDocument doc = new XmlDocument ();
				doc.Load (configFileName);

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
		
		static void KeyringCallback (Result result, uint val) 
		{
			Log.Debug ("gnome-keyring-callback: result: {0} - ID: {1}", result, val);
		}

		public void SaveProfiles ()	
		{
			XmlDocument doc = new XmlDocument ();
			XmlNode xmlnode = doc.CreateNode (XmlNodeType.XmlDeclaration,"","");
			doc.AppendChild (xmlnode);
			
			XmlElement profiles = doc.CreateElement ("profiles");
			doc.AppendChild (profiles);
		
			foreach (string s in profileDictionary.Keys) {
				ConnectionProfile cp = (ConnectionProfile) profileDictionary[s];
				
				if (cp.Dynamic)
					return;
				
				XmlElement profile = doc.CreateElement ("profile");
				profile.SetAttribute ("name", cp.Name);
				profile.SetAttribute ("host", cp.Host);
				profile.SetAttribute ("port", cp.Port.ToString());
				profile.SetAttribute ("base", cp.LdapRoot);
				profile.SetAttribute ("user", cp.User);
				profile.SetAttribute ("save_password", cp.DontSavePassword.ToString());
				
				switch (cp.Encryption) {

				case EncryptionType.TLS:
					profile.SetAttribute ("encryption", "tls");
					break;

				case EncryptionType.SSL:
					profile.SetAttribute ("encryption", "ssl");
					break;

				case EncryptionType.None:
					profile.SetAttribute ("encryption", "none");
					break;

				default:
					break;
				}				
				
				profile.SetAttribute ("server_type", cp.ServerType);
				
				if (cp.ActiveServerViews == null)
					cp.SetDefaultServerViews ();
				
				XmlElement server_views = doc.CreateElement ("server_views");
				foreach (string sv in cp.ActiveServerViews) {
					XmlElement server_view = doc.CreateElement ("server_view");
					server_view.InnerText = sv;
					server_views.AppendChild (server_view);
				}				
				profile.AppendChild (server_views);
				
				if (cp.ActiveAttributeViewers == null) 
					cp.SetDefaultAttributeViewers ();
				
				XmlElement attribute_viewers = doc.CreateElement ("attribute_viewers");
				foreach (string a in cp.ActiveAttributeViewers) {
					XmlElement av = doc.CreateElement ("attribute_viewer");
					av.InnerText = a;
					attribute_viewers.AppendChild (av);
				}				
				profile.AppendChild (attribute_viewers);
				
				profiles.AppendChild (profile);
				
				OperationGetIntCallback theCallback = new OperationGetIntCallback (KeyringCallback);
				GnomeKeyring.Global.SetNetworkPassword(
					cp.User,			// user
					cp.Host,			// server 
					"ldap", 			// protocol
					(uint)cp.Port, 		// port
					cp.Pass,	 		// password
					theCallback 		// callback
				);				
			}
	
			doc.Save (configFileName);
		}
	}
}
