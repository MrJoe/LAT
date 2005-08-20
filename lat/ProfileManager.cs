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
	public class ProfileManager 
	{
		private string _configFile;
		private Hashtable _profiles;

		public ProfileManager ()
		{
			_profiles = new Hashtable ();
		
			string dir = Environment.GetEnvironmentVariable("HOME");
			string tmp = Path.Combine (dir, ".lat");
			
			_configFile = Path.Combine (tmp, "profiles.xml");

			DirectoryInfo di = new DirectoryInfo (tmp);
			if (!di.Exists)
			{
				di.Create ();
			}

			FileInfo fi = new FileInfo (_configFile);
			if (!fi.Exists)
			{
				return;
			}
			
			loadProfiles ();		
		}
		
		public string[] getProfileNames ()
		{
			string[] retVal = new string [_profiles.Count];
			int count = 0;
			
			foreach (string s in _profiles.Keys)
			{
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
		
		public void loadProfiles ()
		{
			try
			{
				XmlTextReader r = new XmlTextReader (_configFile);
				
				while (r.Read()) 
				{	
							
					if (r.Name == "profile") 
					{								
						ConnectionProfile cp = new ConnectionProfile (
							r.GetAttribute ("name"),
							r.GetAttribute ("host"),
							int.Parse (r.GetAttribute ("port")),
							r.GetAttribute ("base"),
							r.GetAttribute ("user"),
							"",
							bool.Parse (r.GetAttribute ("ssl")),
							r.GetAttribute ("server_type"),
							r.GetAttribute ("sid"));

						GnomeKeyring.Result gkr;
						NetworkPasswordData[] list;
						gkr = GnomeKeyring.Global.FindNetworkPassword(
						 cp.User,
						 null, null, null, null, null, 0, out list
						);

						Logger.Log.Debug ("gnome-keyring-result: {0}", gkr);
						
						foreach (NetworkPasswordData i in list) 
						{
							Logger.Log.Debug (
							  "Got password for: {0}://{1}:{2}/",
							  i.Protocol, i.Server, i.Port);

							cp.Pass = i.Password;
						}
				
						_profiles.Add (cp.Name, cp);	
					} 
			 	}
			 			
			 	r.Close ();
			}
			catch (Exception e)
			{
				Console.WriteLine (e.Message);
			}
		}
		
		static void myCallback (Result result, uint val) 
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
			
			foreach (string name in _profiles.Keys) 
			{		     			 	
				ConnectionProfile cp = (ConnectionProfile) _profiles[name];
		 	
				writer.WriteStartElement("profile", null);
			 		
				writer.WriteAttributeString ("name", cp.Name);
				writer.WriteAttributeString ("host", cp.Host);
				writer.WriteAttributeString ("port", cp.Port.ToString());
				writer.WriteAttributeString ("base", cp.LdapRoot);
				writer.WriteAttributeString ("user", cp.User);
				writer.WriteAttributeString ("ssl", cp.SSL.ToString());
				writer.WriteAttributeString ("server_type", cp.ServerType);
				writer.WriteAttributeString ("sid", cp.SID);
				
	        		writer.WriteEndElement();

				OperationGetIntCallback theCallback = new OperationGetIntCallback (myCallback);

				GnomeKeyring.Global.SetNetworkPassword(
					 null, 				// keyring
					cp.User,			// user
					 null, 				// domain
					cp.Host,			// server 
					null, 				// objekt
					"ldap", 			// protocol
					null, 				// authtype
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
