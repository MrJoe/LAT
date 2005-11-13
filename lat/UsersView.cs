// 
// lat - UsersView.cs
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
	public class UsersView : View
	{
		private static string[] _cols = { 
			Mono.Unix.Catalog.GetString ("Username"), 
			Mono.Unix.Catalog.GetString ("Full Name") };

		private static string[] _adColAttrs = { "sAMAccountName", "cn" };
		private static string[] _posixColAttrs = { "uid", "cn" };

		public UsersView (LdapServer server, TreeView treeView, Gtk.Window parentWindow) 
				: base (server, treeView, parentWindow)
		{
			this.store = new ListStore (typeof (string), typeof (string));
			this.tv.Model = this.store;

			this.viewName = "Users";

			switch (server.ServerType.ToLower())
			{
				case "microsoft active directory":
					this.filter = "(&(objectclass=user)(objectcategory=Person))";
					break;

				case "generic ldap server":
				case "openldap":
				default:
					this.filter = "(&(objectclass=posixAccount)(objectclass=shadowAccount))";
					break;
			}		

			this.lookupKeyCol = 0;

			this.setupColumns (_cols);			
		}

		public override void Populate ()
		{
			switch (server.ServerType.ToLower())
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

		public override void customPopUp  ()
		{
			SeparatorMenuItem sm = new SeparatorMenuItem ();
			sm.Show ();
		
			popup.Append (sm);

			MenuItem mailItem = new MenuItem ("Send email");
			mailItem.Activated += new EventHandler (OnEmailActivate);
			mailItem.Show ();

			popup.Append (mailItem);

			MenuItem wwwItem = new MenuItem ("Open Home Page");
			wwwItem.Activated += new EventHandler (OnWWWActivate);
			wwwItem.Show ();

			popup.Append (wwwItem);
		}
	}
}
