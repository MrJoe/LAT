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
		private static string[] _cols = { Mono.Unix.Catalog.GetString ("Name") };

		private CustomViewManager cvm;
		private Hashtable _ti;
		private TreeStore _vs;

		public CustomView (LdapServer server, TreeView treeView, Gtk.Window parentWindow, Hashtable ti, TreeStore viewStore) 
				: base (server, treeView, parentWindow)
		{
			this.store = new ListStore (typeof (string));
			this.tv.Model = this.store;

			this.viewName = "Custom Views";

			this.setupColumns (_cols);

			cvm = new CustomViewManager ();

			_ti = ti;
			_vs = viewStore;
		}

		public override void Populate ()
		{
			store.Clear ();

			string[] views = cvm.getViewNames ();

			foreach (string v in views)
			{
				store.AppendValues (v);
			}
		}

		public override void OnNewEntryActivate (object o, EventArgs args) 
		{
			try
			{
				CustomViewDialog cvd = new CustomViewDialog (server, cvm);
				cvd.Run ();

				if (cvd.Result != ResponseType.Ok)
					return;

				cvm.reloadViews ();
				Populate ();

				TreeIter iter = (TreeIter) _ti ["root"];
				
				Gdk.Pixbuf pb = parent.RenderIcon (Stock.Open, IconSize.Menu, "");

				TreeIter newIter = _vs.AppendValues (iter, pb, cvd.Name);

				_ti.Add (cvd.Name, newIter);
			}
			catch {}
		}

		public override void OnEditActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp)
			{
				TreeIter iter;
				store.GetIter (out iter, path);
			
				string name = (string) store.GetValue (iter, 0);

				new CustomViewDialog (server, cvm, name);
			}

			Populate ();
		}

		private void deleteView (TreePath[] path)
		{
			foreach (TreePath tp in path)
			{
				TreeIter iter;

				store.GetIter (out iter, tp);
				string name = (string) store.GetValue (iter, 0);


				string msg = String.Format (
					Mono.Unix.Catalog.GetString ("Are you sure you want to delete:\n{0}"), name);

				MessageDialog md = new MessageDialog (parent, 
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
			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			deleteView (tp);

			cvm.saveViews ();
			cvm.reloadViews ();

			Populate ();
		}

		public override void OnRowActivated (object o, RowActivatedArgs args)		
		{
			TreeIter iter;
			store.GetIter (out iter, args.Path);
			
			string name = (string) store.GetValue (iter, 0);

			CustomViewDialog cvd = new CustomViewDialog (server, cvm, name);
			cvd.Run ();

			Populate ();
		}
	}
}
