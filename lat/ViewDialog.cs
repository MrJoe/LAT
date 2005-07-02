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
using GLib;
using System;
using System.Collections;
using Novell.Directory.Ldap;

namespace lat
{
	public class ViewDialog
	{
		protected lat.Connection _conn;
		protected Gtk.Dialog _viewDialog;

		public ViewDialog (lat.Connection conn)
		{
			_conn = conn;
		}

		public static Hashtable getEntryInfo (string[] attrs, LdapEntry le)
		{
			Hashtable ei = new Hashtable ();

			foreach (string a in attrs)
			{
				LdapAttribute attr;
				attr = le.getAttribute (a);

				if (attr == null)
				{
					ei.Add (a, "");
				}
				else
				{
					ei.Add (a, attr.StringValue);
				}
			}

			return ei;
		}

		public static string getAttribute (LdapEntry le, string attr)
		{
			LdapAttribute la = le.getAttribute (attr);

			if (la != null)
			{
				return la.StringValue;
			}

			return "";
		}

		public static ArrayList getAttributes (string[] objClass, string[] attrs, Hashtable entryInfo)
		{
			ArrayList retVal = new ArrayList ();

			LdapAttribute la;

			la = new LdapAttribute ("objectclass", objClass);
			retVal.Add (la);

			foreach (string a in attrs)
			{
				string entryValue = (string) entryInfo[a];

				if (entryValue.Equals (""))
					continue;

				la = new LdapAttribute (a, entryValue);
				retVal.Add (la);
			}

			return retVal;
		}

		public static ArrayList getMods (string[] attrs, Hashtable oldInfo, Hashtable newInfo)
		{
			ArrayList retVal = new ArrayList ();

			foreach (string a in attrs)
			{
				string oldValue = (string) oldInfo[a];
				string newValue = (string) newInfo[a];

				if (!oldValue.Equals (newValue))
				{
					LdapAttribute la = new LdapAttribute (a, newValue);
					LdapModification lm = new LdapModification (LdapModification.REPLACE, la);
					retVal.Add (lm);
				}
			}

			return retVal;
		}

		public void missingAlert (string[] missing)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString ("You must provide values for the following attributes:\n\n"));

			foreach (string m in missing)
			{
				msg += String.Format ("{0}\n", m);
			}

			Util.MessageBox (_viewDialog, msg, MessageType.Warning);
		}

		private static bool checkReq (string name, Hashtable entryInfo)
		{
			string attrValue = (string) entryInfo [name];

			if (attrValue == null)
			{
				return false;
			} 
			else if (attrValue.Equals (""))
			{
				return false;
			}

			return true;
		}

		public bool checkReqAttrs (string[] objectClass, Hashtable entryInfo, out string[] missing)
		{
			ArrayList outMiss = new ArrayList ();

			foreach (string obj in objectClass)
			{
				if (obj.Equals ("top"))
					continue;

				string[] reqs = _conn.getRequiredAttrs (obj);
	
				if (reqs == null)
					continue;

				foreach (string r in reqs)
				{
					if (!checkReq (r, entryInfo))
					{
						outMiss.Add (r);
						continue;
					}
				}
			}

			if (outMiss.Count > 0)
			{
				missing = (string[]) outMiss.ToArray (typeof (string));
				return false;
			}

			missing = null;
			return true;
		}

		public virtual void OnCancelClicked (object o, EventArgs args)
		{
			_viewDialog.HideAll ();
		}

		public virtual void OnDlgDelete (object o, DeleteEventArgs args)
		{
			_viewDialog.HideAll ();
		}
	}
}
