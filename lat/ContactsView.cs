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

namespace lat
{
	public class ContactsView : View
	{
		private static string[] _cols = { 
			Mono.Posix.Catalog.GetString ("Name"), 
			Mono.Posix.Catalog.GetString ("Email"), 
			Mono.Posix.Catalog.GetString ("Work"), 
			Mono.Posix.Catalog.GetString ("Home"), 
			Mono.Posix.Catalog.GetString ("Mobile") };

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
	}
}
