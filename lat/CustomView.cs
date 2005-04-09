// 
// lat - CustomView.cs
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
	public class CustomView : View
	{
		private static string[] _cols = { Mono.Posix.Catalog.GetString ("Name") };

		private CustomViewManager cvm;
		private Hashtable _ti;
		private TreeStore _vs;

		public CustomView (lat.Connection conn, TreeView tv, Gtk.Window parent, Hashtable ti, TreeStore viewStore) 
				: base (conn, tv, parent)
		{
			this._store = new ListStore (typeof (string));
			this._tv.Model = this._store;

			this._viewName = "Custom Views";

			this.setupColumns (_cols);

			cvm = new CustomViewManager ();

			_ti = ti;
			_vs = viewStore;
		}

		public override void Populate ()
		{
			_store.Clear ();

			string[] views = cvm.getViewNames ();

			foreach (string v in views)
			{
				_store.AppendValues (v);
			}
		}

		public override void OnNewEntryActivate (object o, EventArgs args) 
		{
			CustomViewDialog cvd = new CustomViewDialog (_conn, cvm);
			cvd.Run ();

			cvm.reloadViews ();
			Populate ();

			TreeIter iter = (TreeIter) _ti ["root"];
			
			Gdk.Pixbuf pb = _parent.RenderIcon (Stock.Open, IconSize.Menu, "");

			TreeIter newIter = _vs.AppendValues (iter, pb, cvd.Name);

			_ti.Add (cvd.Name, newIter);
		}

		public override void OnEditActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = _tv.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp)
			{
				TreeIter iter;
				_store.GetIter (out iter, path);
			
				string name = (string) _store.GetValue (iter, 0);

				new CustomViewDialog (_conn, cvm, name);
			}

			Populate ();
		}

		private void deleteView (TreePath[] path)
		{
			foreach (TreePath tp in path)
			{
				TreeIter iter;

				_store.GetIter (out iter, tp);
				string name = (string) _store.GetValue (iter, 0);


				string msg = String.Format (
					Mono.Posix.Catalog.GetString ("Are you sure you want to delete:\n{0}"), name);

				MessageDialog md = new MessageDialog (_parent, 
					DialogFlags.DestroyWithParent,
					MessageType.Question, 
					ButtonsType.YesNo, 
					msg);

				ResponseType result = (ResponseType)md.Run ();

				if (result == ResponseType.Yes)
				{
					cvm.deleteView (name);
	
					TreeIter i = (TreeIter) _ti [name];

					_vs.Remove (ref i);
				}
			
				md.Destroy ();
			}
		}

		public override void OnDeleteActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = _tv.Selection.GetSelectedRows (out model);

			deleteView (tp);

			cvm.saveViews ();
			cvm.reloadViews ();

			Populate ();
		}

		public override void OnRowActivated (object o, RowActivatedArgs args)		
		{
			TreeIter iter;
			_store.GetIter (out iter, args.Path);
			
			string name = (string) _store.GetValue (iter, 0);

			CustomViewDialog cvd = new CustomViewDialog (_conn, cvm, name);
			cvd.Run ();

			Populate ();
		}
	}
}
