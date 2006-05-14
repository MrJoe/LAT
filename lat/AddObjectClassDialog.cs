// 
// lat - AddObjectClassDialog.cs
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
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class AddObjectClassDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog addObjectClassDialog;
		[Glade.Widget] Gtk.TreeView objClassTreeView;

		private LdapServer server;
		private ArrayList objectClasses;
		private ListStore store;

		public AddObjectClassDialog (LdapServer ldapServer)
		{
			server = ldapServer;
			objectClasses = new ArrayList ();

			ui = new Glade.XML (null, "lat.glade", "addObjectClassDialog", null);
			ui.Autoconnect (this);

			store = new ListStore (typeof (bool), typeof (string));

			CellRendererToggle crt = new CellRendererToggle();
			crt.Activatable = true;
			crt.Toggled += OnClassToggled;

			objClassTreeView.AppendColumn ("Enabled", crt, "active", 0);
			objClassTreeView.AppendColumn ("Name", new CellRendererText (), "text", 1);
			
			objClassTreeView.Model = store;

			try {
				// class
				LdapEntry[] les = server.GetObjectClasses ();
				ArrayList tmp = new ArrayList ();

				foreach (LdapEntry le in les) {
					LdapAttribute la = le.getAttribute ("objectclasses");
						
					foreach (string s in la.StringValueArray) {
						SchemaParser sp = new SchemaParser (s);
						tmp.Add (sp.Names[0]);
					}
				}

				tmp.Sort ();

				foreach (string n in tmp)
					store.AppendValues (false, n);

			} catch {

				store.AppendValues (false, "Error getting object classes");
			}
			
			addObjectClassDialog.Icon = Global.latIcon;
			addObjectClassDialog.Resize (300, 400);
			addObjectClassDialog.Run ();
			addObjectClassDialog.Destroy ();
		}

		void OnClassToggled (object o, ToggledArgs args)
		{
			TreeIter iter;

			if (store.GetIter (out iter, new TreePath(args.Path))) {
				bool old = (bool) store.GetValue (iter,0);
				string name = (string) store.GetValue (iter, 1);

				if (!old)
					objectClasses.Add (name);
				else
					objectClasses.Remove (name);

				store.SetValue(iter,0,!old);
			}
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			addObjectClassDialog.HideAll ();
		}

		public void OnDlgDelete (object o, DeleteEventArgs args)
		{
			addObjectClassDialog.HideAll ();
		}

		public string[] ObjectClasses
		{
			get { return (string[]) objectClasses.ToArray (typeof(string)); }
		}
	}
}