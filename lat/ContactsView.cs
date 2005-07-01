// 
// lat - ContactsView.cs
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
using Novell.Directory.Ldap;

namespace lat
{
	public class ContactsView : View
	{
		private static string[] _cols = { 
			Mono.Unix.Catalog.GetString ("Name"), 
			Mono.Unix.Catalog.GetString ("Email"), 
			Mono.Unix.Catalog.GetString ("Work"), 
			Mono.Unix.Catalog.GetString ("Home"), 
			Mono.Unix.Catalog.GetString ("Mobile") };

		private static string[] _colAttrs = { "cn", "mail", "telephoneNumber", "homePhone", "mobile" };

		public ContactsView (lat.Connection conn, TreeView tv, Gtk.Window parent) 
				: base (conn, tv, parent)
		{
			this._store = new ListStore (typeof (string), typeof (string),
				typeof (string), typeof (string), typeof (string));

			this._tv.Model = this._store;
			this._viewName = "Contacts";
			this._filter = "inetOrgPerson";

			this._lookupKeyCol = 0;

			this.setupColumns (_cols);
		}

		public override void Populate ()
		{
			this.insertData (_colAttrs);
		}

		public override void DoPopUp ()
		{
			Menu popup = new Menu();

			MenuItem mailItem = new MenuItem ("Send email");
			mailItem.Activated += new EventHandler (OnEmailActivate);
			mailItem.Show ();

			popup.Append (mailItem);

			popup.Popup(null, null, null, IntPtr.Zero, 3,
					Gtk.Global.CurrentEventTime);
		}

		private void OnEmailActivate (object o, EventArgs args) 
		{
			Gtk.TreeModel model;

			TreePath[] tp = this._tv.Selection.GetSelectedRows (out model);

			LdapEntry le = this.lookupEntry (tp[0]);

			if (le == null)
				return;

			LdapAttribute la = le.getAttribute ("mail");

			if (la.StringValue == null || la.StringValue == "")
				return;

			Gnome.Url.Show ("mailto:" + la.StringValue);
		}
	}
}
