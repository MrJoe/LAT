// 
// lat - ViewFactory.cs
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

namespace lat
{
	public class ViewFactory
	{
		private ListStore _ls;
		private TreeView _tv;
		private Gtk.Window _pw;
		private lat.Connection _cn;
		public TreeStore _viewStore;
		public Hashtable _ti;

		public ViewFactory (ListStore ls, TreeView tv, Gtk.Window pw, lat.Connection cn)
		{
			_ls = ls;
			_tv = tv;
			_pw = pw;
			_cn = cn;
		}

		private void Cleanup ()
		{
			if (_ls != null)
			{
				_ls.Clear ();
			}			

			foreach (TreeViewColumn col in _tv.Columns)
			{
				_tv.RemoveColumn (col);
			}
		}

		public View Create (string name)
		{
			View retVal = null;

			Cleanup ();

			switch (name)
			{
				case "Users":
					retVal = new UsersView (_cn, _tv, _pw);
					break;

				case "Groups":
					retVal = new GroupsView (_cn, _tv, _pw);
					break;
				
				case "Computers":
				case "Hosts":
					retVal = new HostsView (_cn, _tv, _pw);
					break;

				case "Contacts":
					retVal = new ContactsView (_cn, _tv, _pw);
					break;

				case "Custom Views":
					retVal = new CustomView (_cn, _tv, _pw, _ti, _viewStore);
					break;

				default:
					retVal = new DynamicView (name, _cn, _tv, _pw);
					break;
			}

			return retVal;
		}
	}
}
