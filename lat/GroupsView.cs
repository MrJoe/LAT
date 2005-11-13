// 
// lat - GroupsView.cs
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
	public class GroupsView : View
	{
		private static string[] _posixCols = { 
			Mono.Unix.Catalog.GetString ("Group ID"), 
			Mono.Unix.Catalog.GetString ("Name"),
			Mono.Unix.Catalog.GetString ("Description") };

		private static string[] _adCols = { 
			Mono.Unix.Catalog.GetString ("Name"), 
			Mono.Unix.Catalog.GetString ("Description") };

		private static string[] _adColAttrs = { "name", "description" };
		private static string[] _posixColAttrs = { "gidNumber", "cn", "description" };

		public GroupsView (LdapServer server, TreeView treeView, Gtk.Window parentWindow) 
				: base (server, treeView, parentWindow)
		{
			this.store = new ListStore (typeof (string), typeof (string), typeof (string));
			this.tv.Model = this.store;

			this.viewName = "Groups";

			switch (server.ServerType.ToLower())
			{
				case "microsoft active directory":
					this.lookupKeyCol = 0;
					this.filter = "objectclass=group";
					this.setupColumns (_adCols);
					break;

				case "generic ldap server":
				case "openldap":
				default:
					this.lookupKeyCol = 1;
					this.filter = "objectclass=posixGroup";

					this.setupColumns (_posixCols);
					break;
			}
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
	}
}
