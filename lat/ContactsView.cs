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
		private static string[] _posixCols = { 
			Mono.Unix.Catalog.GetString ("Name"), 
			Mono.Unix.Catalog.GetString ("Email"), 
			Mono.Unix.Catalog.GetString ("Work"), 
			Mono.Unix.Catalog.GetString ("Home"), 
			Mono.Unix.Catalog.GetString ("Mobile") };

		private static string[] _adCols = { 
			Mono.Unix.Catalog.GetString ("Name"), 
			Mono.Unix.Catalog.GetString ("Description"),
			Mono.Unix.Catalog.GetString ("Email"),
			Mono.Unix.Catalog.GetString ("Web Page") };

		private static string[] _adColAttrs = { "name", "description", "mail", "wWWHomePage" };
		private static string[] _posixColAttrs = { "cn", "mail", "telephoneNumber", "homePhone", "mobile" };

		public ContactsView (lat.Connection conn, TreeView tv, Gtk.Window parent) 
				: base (conn, tv, parent)
		{
			this._store = new ListStore (typeof (string), typeof (string),
				typeof (string), typeof (string), typeof (string));

			this._tv.Model = this._store;
			this._viewName = "Contacts";

			switch (conn.ServerType.ToLower())
			{
				case "microsoft active directory":
					this._lookupKeyCol = 0;
					this._filter = "contact";
					this.setupColumns (_adCols);
					break;

				case "generic ldap server":
				case "openldap":
				default:
					this._lookupKeyCol = 0;
					this._filter = "inetOrgPerson";

					this.setupColumns (_posixCols);
					break;
			}
		}

		public override void Populate ()
		{
			switch (_conn.ServerType.ToLower())
			{
				case "microsoft active directory":
					this.insertData (_adColAttrs);
					break;

				case "generic ldap server":
				case "openldap":
				default:
					this.insertData (_posixColAttrs);
					break;
			}
		}

		public override void customPopUp ()
		{
			SeparatorMenuItem sm = new SeparatorMenuItem ();
			sm.Show ();
		
			_popup.Append (sm);

			MenuItem mailItem = new MenuItem ("Send email");
			mailItem.Activated += new EventHandler (OnEmailActivate);
			mailItem.Show ();

			_popup.Append (mailItem);

			MenuItem wwwItem = new MenuItem ("Open Home Page");
			wwwItem.Activated += new EventHandler (OnWWWActivate);
			wwwItem.Show ();

			_popup.Append (wwwItem);
		}
	}
}
