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
using System.Net.Sockets;
using Novell.Directory.Ldap;

namespace lat
{
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