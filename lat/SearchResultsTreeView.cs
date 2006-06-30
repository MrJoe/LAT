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

using System;
using System.Collections;
using GLib;
using Gtk;
using Gdk;
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

	public class SearchResultExportEventArgs : EventArgs
	{
		string entryDN;
		string data;
		bool dnd;

		public SearchResultExportEventArgs (string dn, bool isdnd)
		{
			entryDN = dn;
			dnd = isdnd;
		}

		public string DN
		{
			get { return entryDN; }
		}
				
		public bool IsDND
		{
			get { return dnd; }
		}
				
		public string Data
		{
			get { return data; }
			set { data = value; }
		}
	}

	public delegate void SearchResultSelectedHandler (object o, SearchResultSelectedEventArgs args);
	public delegate void SearchResultExportHandler (object o, SearchResultExportEventArgs args);

	public class SearchResultsTreeView : Gtk.TreeView
	{
		ListStore resultsStore;

		private static TargetEntry[] searchSourceTable = new TargetEntry[]
		{
			new TargetEntry ("text/plain", 0, 1),
		};

		public event SearchResultSelectedHandler SearchResultSelected;
		public event SearchResultExportHandler Export;

		public SearchResultsTreeView () : base ()
		{
			resultsStore = new ListStore (typeof (string));
			this.Model = resultsStore;

			this.HeadersVisible = false;
			
			this.ButtonPressEvent += new ButtonPressEventHandler (OnRightClick);
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

			if (!(searchResults.Length > 0)) {

				resultsStore.AppendValues (
					Mono.Unix.Catalog.GetString ("No matches found."));
			}
			
			foreach (LdapEntry le in searchResults)
				resultsStore.AppendValues (le.DN);
		}

		[ConnectBefore]
		void OnRightClick (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				DoPopUp ();
		}

		void DoPopUp()
		{
			Menu popup = new Menu();
			MenuItem exportItem = new MenuItem ("Export...");
			exportItem.Activated += new EventHandler (OnExportActivate);
			exportItem.Show ();

			popup.Append (exportItem);

			popup.Popup(null, null, null, 3, Gtk.Global.CurrentEventTime);
		}

		void OnExportActivate (object o, EventArgs args)
		{
			Gtk.TreeModel model;
			Gtk.TreeIter iter;

			if (!this.Selection.GetSelected (out model, out iter))
				return;

			string dn = (string) model.GetValue (iter, 0);
			
			SearchResultExportEventArgs myargs = new SearchResultExportEventArgs (dn, false);
		
			if (Export != null)
				Export (this, myargs);		
		}

		void OnSearchDragBegin (object o, DragBeginArgs args)
		{
			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("text-x-generic.png");
			Gtk.Drag.SetIconPixbuf (args.Context, pb, 0, 0);
		}

		void OnSearchDragDataGet (object o, DragDataGetArgs args)
		{
			Gtk.TreeModel model;
			Gtk.TreeIter iter;

			if (!this.Selection.GetSelected (out model, out iter))
				return;

			string dn = (string) model.GetValue (iter, 0);
			
			SearchResultExportEventArgs myargs = new SearchResultExportEventArgs (dn, true);
		
			if (Export != null)
				Export (this, myargs);

			if (myargs.Data == null)
				return;
				
			Atom[] targets = args.Context.Targets;

			args.SelectionData.Set (targets[0], 8, System.Text.Encoding.UTF8.GetBytes (myargs.Data));
		}

		void DispatchSearchResultSelectedEvent (string dn)
		{
			if (SearchResultSelected != null)
				SearchResultSelected (this, new SearchResultSelectedEventArgs (dn));
		}

		string getSelectedSearchResult ()
		{
			TreeModel model;
			TreeIter iter;

			if (!this.Selection.GetSelected (out model, out iter))
				return null;
			
			string name = null;
			name = (string) resultsStore.GetValue (iter, 0);

			return name;
		}

		void resultsRowActivated (object o, RowActivatedArgs args)
		{
			DispatchSearchResultSelectedEvent (getSelectedSearchResult ());
		}

		public string SelectedResult
		{
			get { return getSelectedSearchResult(); }
		}
	}
}
