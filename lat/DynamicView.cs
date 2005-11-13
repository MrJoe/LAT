// 
// lat - DynamicView.cs
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
	public class DynamicView : View
	{
		private string[] _cols;
		private CustomViewData cvd;

		public DynamicView (string name, LdapServer server, TreeView treeView, Gtk.Window parentWindow) 
				: base (server, treeView, parentWindow)
		{
			CustomViewManager cvm = new CustomViewManager ();
			cvd = cvm.Lookup (name);

			char[] delim = {','};
			_cols = cvd.Cols.Split (delim);

			System.Type[] types = new System.Type [_cols.Length];

			for (int i = 0; i < _cols.Length; i++)
			{
				types[i] = typeof (string);
			}

			this.store = new ListStore (types);
			this.tv.Model = this.store;

			this.viewName = name;
			this.filter = cvd.Filter;
			this.lookupKeyCol = 0;

			this.setupColumns (_cols);
		}

		public override void Populate ()
		{
			this.insertData (cvd.Base, _cols);
		}
	}
}
