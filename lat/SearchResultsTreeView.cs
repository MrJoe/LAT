// 
// lat - SearchResultsTreeView.cs
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
using Gdk;
using Glade;
using System;
using System.Collections;
using Novell.Directory.Ldap;

namespace lat
{
	public class SearchResultSelectedEventArgs : EventArgs
	{
		private string _dn;

		public SearchResultSelectedEventArgs (string dn)
		{
			_dn = dn;
		}

		public string DN
		{
			get { return _dn; }
		}
	}

	public delegate void SearchResultSelectedHandler (object o, SearchResultSelectedEventArgs args);

	public class SearchResultsTreeView : Gtk.TreeView
	{
		private LdapServer server;
		private ListStore resultsStore;

		private static TargetEntry[] searchSourceTable = new TargetEntry[]
		{
			new TargetEntry ("text/plain", 0, 1),
		};

		public event SearchResultSelectedHandler SearchResultSelected;

		public SearchResultsTreeView (LdapServer ldapServer) : base ()
		{
			server = ldapServer;

			resultsStore = new ListStore (typeof (string));
			this.Model = resultsStore;

			this.HeadersVisible = false;
			
			this.RowActivated += new RowActivatedHandler (resultsRowActivated);
			this.AppendColumn ("resultDN", new CellRendererText (), "text", 0);

			Gtk.Drag.SourceSet (this, 
				Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask, 
				searchSourceTable, Gdk.DragAction.Copy | DragAction.Move);

			this.DragBegin += new DragBeginHandler (OnSearchDragBegin);
			this.DragDataGet += new DragDataGetHandler (OnSearchDragDataGet);

			this.ShowAll ();
		}

		public void UpdateSearchResults (LdapEntry[] searchResults)
		{
			resultsStore.Clear ();

			if (!(searchResults.Length > 0))
			{
				resultsStore.AppendValues (
					Mono.Unix.Catalog.GetString ("No matches found."));
			}
			
			foreach (LdapEntry le in searchResults)
			{
				resultsStore.AppendValues (le.DN);
			}
		}

		private void OnSearchDragBegin (object o, DragBeginArgs args)
		{
			// FIXME: change icon
			// FIXME: Drag.SetIconPixbuf (args.Context, <obj>, 0, 0);
		}

		private void OnSearchDragDataGet (object o, DragDataGetArgs args)
		{
			Gtk.TreeModel model;
			Gtk.TreeIter iter;

			if (!this.Selection.GetSelected (out model, out iter))
				return;

			string dn = (string) model.GetValue (iter, 0);
			string data = null;

			Util.ExportData (server, dn, out data);

			Atom[] targets = args.Context.Targets;

			args.SelectionData.Set (targets[0], 8,
				System.Text.Encoding.UTF8.GetBytes (data));
		}

		private void DispatchSearchResultSelectedEvent (string dn)
		{
			if (SearchResultSelected != null)
			{
				SearchResultSelected (this, new SearchResultSelectedEventArgs (dn));
			}
		}

		private string getSelectedSearchResult ()
		{
			TreeModel model;
			TreeIter iter;

			if (!this.Selection.GetSelected (out model, out iter))
				return null;
			
			string name = null;
			name = (string) resultsStore.GetValue (iter, 0);

			return name;
		}

		private void resultsRowActivated (object o, RowActivatedArgs args)
		{
			DispatchSearchResultSelectedEvent (getSelectedSearchResult ());
		}

		public string SelectedResult
		{
			get { return getSelectedSearchResult(); }
		}
	}
}
