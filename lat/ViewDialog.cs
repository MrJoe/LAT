// 
// lat - ViewDialog.cs
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

using Gtk;
using System;
using System.Collections;
using Novell.Directory.Ldap;

namespace lat
{
	public class ViewDialog
	{
		protected LdapServer server;
		protected Gtk.Dialog viewDialog;
		protected bool missingValues = false;
		protected bool errorOccured = false;

		public ViewDialog (LdapServer ldapServer)
		{
			server = ldapServer;
		}

		public static ArrayList getAttributes (string[] objClass, string[] attrs, Hashtable entryInfo)
		{
			ArrayList retVal = new ArrayList ();

			LdapAttribute la;

			la = new LdapAttribute ("objectclass", objClass);
			retVal.Add (la);

			foreach (string a in attrs) {

				string entryValue = (string) entryInfo[a];

				if (entryValue == null || entryValue.Equals (""))
					continue;

				la = new LdapAttribute (a, entryValue);
				retVal.Add (la);
			}

			return retVal;
		}

		public static ArrayList getMods (string[] attrs, Hashtable oldInfo, Hashtable newInfo)
		{
			Logger.Log.Debug ("START ViewDialog.getMods()");

			ArrayList retVal = new ArrayList ();

			foreach (string a in attrs) {

				string oldValue = (string) oldInfo[a];
				string newValue = (string) newInfo[a];

				if (!oldValue.Equals (newValue) && newValue != null) {

					Logger.Log.Debug ("Modification: attribute: [{0}] - oldValue: [{1}] - newValue: [{2}]", a, oldValue, newValue);

					LdapAttribute la; 
					LdapModification lm;

					if (newValue == "") {
						la = new LdapAttribute (a);
						lm = new LdapModification (LdapModification.DELETE, la);
					} else {

						la = new LdapAttribute (a, newValue);
						lm = new LdapModification (LdapModification.REPLACE, la);
					}

					retVal.Add (lm);
				}
			}

			Logger.Log.Debug ("END ViewDialog.getMods()");

			return retVal;
		}

		public void missingAlert (string[] missing)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString ("You must provide values for the following attributes:\n\n"));

			foreach (string m in missing)
				msg += String.Format ("{0}\n", m);

			HIGMessageDialog dialog = new HIGMessageDialog (
					viewDialog,
					0,
					Gtk.MessageType.Warning,
					Gtk.ButtonsType.Ok,
					"Attributes missing",
					msg);

			dialog.Run ();
			dialog.Destroy ();
		}

		private static bool checkReq (string name, Hashtable entryInfo)
		{
			string attrValue = (string) entryInfo [name];

			if (attrValue == null)
				return false;
			else if (attrValue.Equals (""))
				return false;

			return true;
		}

		public bool checkReqAttrs (string[] objectClass, Hashtable entryInfo, out string[] missing)
		{
			ArrayList outMiss = new ArrayList ();

			foreach (string obj in objectClass) {

				if (obj.Equals ("top"))
					continue;

				string[] reqs = server.GetRequiredAttrs (obj);
	
				if (reqs == null)
					continue;

				foreach (string r in reqs) {

					if (r.Equals ("cn"))
						continue;

					if (!checkReq (r, entryInfo)) {
						outMiss.Add (r);
						continue;
					}
				}
			}

			if (outMiss.Count > 0) {
				missing = (string[]) outMiss.ToArray (typeof (string));
				return false;
			}

			missing = null;
			return true;
		}

		public virtual void OnCancelClicked (object o, EventArgs args)
		{
			viewDialog.HideAll ();
		}

		public virtual void OnDlgDelete (object o, DeleteEventArgs args)
		{
			viewDialog.HideAll ();
		}
	}
}
