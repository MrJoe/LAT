// 
// lat - Window.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; Version 2 .
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
using GLib;
using Glade;
using System;
using System.Collections;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat 
{
	public class latWindow
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Window mainWindow;
		[Glade.Widget] TreeView viewsTreeview;
		[Glade.Widget] Gtk.Entry filterEntry;
		[Glade.Widget] Gtk.Button searchBuilderButton;
		[Glade.Widget] Gtk.Button searchBaseButton;
		[Glade.Widget] Gtk.Button searchButton;
		[Glade.Widget] TreeView resultsTreeview;
		[Glade.Widget] ScrolledWindow browserScrolledWindow;
		[Glade.Widget] TreeView valuesListview;
		[Glade.Widget] Notebook viewNotebook;
		[Glade.Widget] HPaned hpaned1;
		[Glade.Widget] Gtk.Button newButton;
		[Glade.Widget] Gtk.Button editButton;
		[Glade.Widget] Gtk.Button deleteButton;
		[Glade.Widget] Gtk.Button refreshButton;
		[Glade.Widget] Gtk.RadioMenuItem userView;
		[Glade.Widget] Gtk.RadioMenuItem groupView;
		[Glade.Widget] Gtk.RadioMenuItem hostView;
		[Glade.Widget] Gtk.RadioMenuItem contactView;
		[Glade.Widget] Gtk.RadioMenuItem customView;
		[Glade.Widget] Gtk.RadioMenuItem browserView;
		[Glade.Widget] Gtk.RadioMenuItem searchView;
		[Glade.Widget] Gtk.Button applyButton;
		[Glade.Widget] Gtk.Statusbar statusBar;

		private LdapTreeView _ldapTreeview;

		private Connection _conn;
		private ArrayList _modList;
		private ArrayList _searchResults = null;

		private TreeStore viewsStore;
		private ListStore valuesStore;
		private ListStore resultsStore;

		private TreeIter viewRootIter;
		private TreeIter viewCustomIter;

		private Hashtable customIters = new Hashtable ();

		private static ViewFactory viewFactory;
		private lat.View _currentView = null;

		private string _cutDN = null;
		private TreeIter _cutIter;

		private string _pasteDN = null;
		private bool _isCopy = false;
		
		private const int _id = 1;

		private static TargetEntry[] searchSourceTable = new TargetEntry[]
		{
			new TargetEntry ("text/plain", 0, 1),
		};

		public latWindow (Connection conn) 
		{
			_conn = conn;
			_modList = new ArrayList ();

			ui = new Glade.XML (null, "lat.glade", "mainWindow", null);
			ui.Autoconnect (this);

			mainWindow.DeleteEvent += new DeleteEventHandler (OnAppDelete);
			mainWindow.Resize (640, 480);

			hpaned1.Position = 250;

			// Setup views
			viewFactory = new ViewFactory (valuesStore, valuesListview, 
							mainWindow, _conn);
	
			viewsStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string));
			viewsTreeview.Model = viewsStore;

			viewsTreeview.RowActivated += new RowActivatedHandler (viewRowActivated);
			viewsTreeview.AppendColumn ("viewsIcon", new CellRendererPixbuf (), "pixbuf", 0);
			viewsTreeview.AppendColumn ("viewsRoot", new CellRendererText (), "text", 1);

			Gdk.Pixbuf pb = mainWindow.RenderIcon (Stock.Convert, IconSize.Menu, "");

			viewRootIter = viewsStore.AppendValues (pb, _conn.Host);

			pb = mainWindow.RenderIcon (Stock.Open, IconSize.Menu, "");

			switch (conn.ServerType.ToLower())
			{
				case "microsoft active directory":
					viewsStore.AppendValues (viewRootIter, pb, "Computers");
					viewsStore.AppendValues (viewRootIter, pb, "Contacts");
					viewsStore.AppendValues (viewRootIter, pb, "Groups");
					viewsStore.AppendValues (viewRootIter, pb, "Users");
					break;

				case "generic ldap server":
				case "openldap":
				default:
					viewsStore.AppendValues (viewRootIter, pb, "Users");
					viewsStore.AppendValues (viewRootIter, pb, "Groups");
					viewsStore.AppendValues (viewRootIter, pb, "Hosts");
					viewsStore.AppendValues (viewRootIter, pb, "Contacts");
					break;
			}

			viewCustomIter = viewsStore.AppendValues (viewRootIter, pb, 
				"Custom Views");

			CustomViewManager cvm = new CustomViewManager ();
			string[] views = cvm.getViewNames ();

			foreach (string v in views)
			{
				TreeIter citer;

				citer = viewsStore.AppendValues (viewCustomIter, pb, v);
				customIters.Add (v, citer);
			}

			customIters.Add ("root", viewCustomIter);

			viewFactory._viewStore = viewsStore;
			viewFactory._ti = customIters;
			
			viewsTreeview.ExpandAll ();

			// Setup browser			
			_ldapTreeview = new LdapTreeView (_conn, mainWindow);
			_ldapTreeview.dnSelected += new dnSelectedHandler (ldapDNSelected);

			browserScrolledWindow.AddWithViewport (_ldapTreeview);
			browserScrolledWindow.Show ();

			// Setup search
			searchBaseButton.Label = _conn.LdapRoot;

			resultsStore = new ListStore (typeof (string));
			resultsTreeview.Model = resultsStore;

			resultsTreeview.RowActivated += new RowActivatedHandler (resultsRowActivated);
			resultsTreeview.AppendColumn ("resultDN", new CellRendererText (), "text", 0);

			searchBuilderButton.Clicked += new EventHandler (OnSearchBuilderClicked);
			searchBaseButton.Clicked += new EventHandler (OnSearchBaseClicked);
			searchButton.Clicked += new EventHandler (OnSearchClicked);

			toggleButtons (false);

			Gtk.Drag.SourceSet (resultsTreeview, 
				Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask, 
				searchSourceTable, Gdk.DragAction.Copy | DragAction.Move);

			resultsTreeview.DragBegin += new DragBeginHandler (OnSearchDragBegin);
			resultsTreeview.DragDataGet += new DragDataGetHandler (OnSearchDragDataGet);

			// status bar
			statusBar.HasResizeGrip = false;
			updateStatusBar ();

			// handlers		
			viewNotebook.SwitchPage += new SwitchPageHandler (notebookViewChanged);

			applyButton.Sensitive = false;
			applyButton.Clicked += new EventHandler (OnApplyClicked);
		}

		public void OnSearchDragBegin (object o, DragBeginArgs args)
		{
			// FIXME: change icon
			// FIXME: Drag.SetIconPixbuf (args.Context, <obj>, 0, 0);
		}

		public void OnSearchDragDataGet (object o, DragDataGetArgs args)
		{
			Gtk.TreeModel model;
			Gtk.TreeIter iter;

			if (!resultsTreeview.Selection.GetSelected (out model, out iter))
				return;

			string dn = (string) model.GetValue (iter, 0);
			string data = null;

			Util.ExportData (_conn, dn, out data);

			Atom[] targets = args.Context.Targets;

			args.SelectionData.Set (targets[0], 8,
				System.Text.Encoding.UTF8.GetBytes (data));
		}

		private void updateStatusBar ()
		{
			string msg = null;

			if (_conn.AuthDN == null)
			{
				msg = String.Format("Bind DN: anonymous");
			}
			else
			{
				msg = String.Format("Bind DN: {0}", _conn.AuthDN);
			}

			statusBar.Pop (_id);
			statusBar.Push (_id, msg);
		}
	
		private void toggleButtons (bool btnState)
		{
			newButton.Sensitive = btnState;
			editButton.Sensitive = btnState;
			deleteButton.Sensitive = btnState;
			refreshButton.Sensitive = btnState;

			editButton.Show ();
			refreshButton.Show ();
		}

		private void ldapDNSelected (object o, dnSelectedEventArgs args)
		{
			if (args.IsHost)
			{
				showConnectionAttributes ();
				return;
			}

			LdapEntry entry = _conn.getEntry (args.DN);
			showEntryAttributes (entry);
		}

		private void OnAppDelete (object o, DeleteEventArgs args) 
		{	
			Application.Quit ();
		}

		private void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!valuesStore.GetIterFromString (out iter, args.Path))
			{
				return;
			}

			string oldText = (string) valuesStore.GetValue (iter, 1);

			if (oldText.Equals (args.NewText))
			{
				// no modification
				return;
			}
			
			string _name = (string) valuesStore.GetValue (iter, 0);

			string dn = null;

			if (viewNotebook.CurrentPage == 1)
			{
				dn = _ldapTreeview.getSelectedDN ();
			}
			else if (viewNotebook.CurrentPage == 2)
			{
				dn = getSelectedSearchResult ();
			}
		
			if (dn == null)
				return;
			
			if (dn.Equals (_conn.Host))
			{
				return;
			}

			valuesStore.SetValue (iter, 1, args.NewText);

			LdapAttribute attribute = new LdapAttribute (_name, args.NewText);
			LdapModification lm = new LdapModification (LdapModification.REPLACE, attribute);

			_modList.Add (lm);

			applyButton.Sensitive = true;		
		}
		
		private void updateSearchResults ()
		{
			resultsStore.Clear ();

			if (!(_searchResults.Count > 0))
			{
				resultsStore.AppendValues (
					Mono.Unix.Catalog.GetString ("No matches found."));
			}
			
			foreach (LdapEntry le in _searchResults)
			{
				resultsStore.AppendValues (le.DN);
			}
		}

		private void OnSearchBuilderClicked (object o, EventArgs args)
		{
			SearchBuilderDialog sbd = new SearchBuilderDialog ();
			filterEntry.Text = sbd.UserFilter;
		}

		private void OnSearchClicked (object o, EventArgs args)
		{
			if (_searchResults != null)
			{
				_searchResults.Clear ();
				_searchResults = null;
			}

			_searchResults = _conn.Search (
				searchBaseButton.Label, filterEntry.Text);

			if (_searchResults != null)
			{
				updateSearchResults ();
			}
			else
			{
				Util.MessageBox (mainWindow, 
					Mono.Unix.Catalog.GetString ("Invalid search filter."), 
					MessageType.Error);
			}			
		}

		private void OnSearchBaseClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = 
				new SelectContainerDialog (_conn, mainWindow);

			scd.Message = String.Format (
				Mono.Unix.Catalog.GetString ("Where in the directory would\nyou like to start the search?"));

			scd.Title = Mono.Unix.Catalog.GetString ("Select search base");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (_conn.Host))
				searchBaseButton.Label = scd.DN;
		}

		private void OnApplyClicked (object o, EventArgs args)
		{
			string dn = _ldapTreeview.getSelectedDN ();

			Util.ModifyEntry (_conn, mainWindow, dn, _modList);

			applyButton.Sensitive = false;
		}

		private void showConnectionAttributes ()
		{
			valuesStore.Clear ();

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Host"), _conn.Host);

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Port"), _conn.Port.ToString());

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("User"), _conn.AuthDN);

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Base DN"), _conn.LdapRoot);

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Connected"),
					 _conn.IsConnected.ToString());

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Bound"), _conn.IsBound.ToString());

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("TLS/SSL"), _conn.UseSSL.ToString());

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Protocol Version"),
					 _conn.Protocol.ToString());

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Server Type"), _conn.ServerType);
		}

		private void showEntryAttributes (LdapEntry entry)
		{
			valuesStore.Clear ();
			_modList.Clear ();
		
			LdapAttributeSet attributeSet = entry.getAttributeSet ();

			foreach (LdapAttribute attr in attributeSet)
			{
				string[] svalues;
				svalues = attr.StringValueArray;
							
				foreach (string s in svalues)
				{
					valuesStore.AppendValues (attr.Name, s);
				}
			}		
			
		}

		private void setNameValueView ()
		{
			TreeViewColumn col;

			valuesStore = new ListStore (typeof (string), typeof (string));
			valuesListview.Model = valuesStore;

			col = valuesListview.AppendColumn (
				Mono.Unix.Catalog.GetString ("Name"), 
				new CellRendererText (), "text", 0);

			col.SortColumnId = 0;

			CellRendererText cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnAttributeEdit);

			col = valuesListview.AppendColumn (
				Mono.Unix.Catalog.GetString ("Value"), cell, "text", 1);
		
			valuesStore.SetSortColumnId (0, SortType.Ascending);
		}

		private void removeButtonHandlers ()
		{
			newButton.Clicked -= new EventHandler (_currentView.OnNewEntryActivate);
			editButton.Clicked -= new EventHandler (_currentView.OnEditActivate);
			deleteButton.Clicked -= new EventHandler (_currentView.OnDeleteActivate);
			refreshButton.Clicked -= new EventHandler (_currentView.OnRefreshActivate);
		}

		private void changeView (string name)
		{
			if (_currentView != null)
			{
				removeButtonHandlers ();
				_currentView.removeDndHandlers ();
				_currentView.removeHandlers ();
				_currentView = null;
			}

			lat.View _view = viewFactory.Create (name);
			_currentView = _view;

			if (_view != null)
			{
				_view.Populate ();
			}

			newButton.Clicked += new EventHandler (_currentView.OnNewEntryActivate);
			editButton.Clicked += new EventHandler (_currentView.OnEditActivate);
			deleteButton.Clicked += new EventHandler (_currentView.OnDeleteActivate);
			refreshButton.Clicked += new EventHandler (_currentView.OnRefreshActivate);

			toggleButtons (true);
		}

		private void viewRowActivated (object o, RowActivatedArgs args)
		{
			TreePath path = args.Path;
			TreeIter iter;
			
			if (viewsStore.GetIter (out iter, path))
			{
				string name = null;
				name = (string) viewsStore.GetValue (iter, 1);

				if (name.Equals (_conn.Host))
				{
					clearValues ();

					// FIXME: Need a way to remove the handlers

					setNameValueView ();
					showConnectionAttributes ();

					return;
				}

				clearValues ();

				changeView (name);
			}
		}

		private string getSelectedSearchResult ()
		{
			TreeModel model;
			TreeIter iter;

			if (!resultsTreeview.Selection.GetSelected (out model, out iter))
				return null;
			
			string name = null;
			name = (string) resultsStore.GetValue (iter, 0);

			return name;
		}

		private void resultsRowActivated (object o, RowActivatedArgs args)
		{
			LdapEntry entry = _conn.getEntry (getSelectedSearchResult());

			if (entry != null)
				showEntryAttributes (entry);
		}

		private void clearValues ()
		{
			if (valuesStore != null)
			{
				valuesStore.Clear ();
				valuesStore = null;			
			}

			foreach (TreeViewColumn col in valuesListview.Columns)
			{
				valuesListview.RemoveColumn (col);
			}
		}

		private void notebookViewChanged (object o, SwitchPageArgs args)
		{
			clearValues ();

			if (args.PageNum == 0)
			{
				_ldapTreeview.removeToolbarHandlers ();
				toggleButtons (false);
			}
			if (args.PageNum == 1)
			{
				if (_currentView != null)
				{
					_currentView.removeHandlers ();
					_currentView.removeDndHandlers ();
					_currentView = null;
				}

				toggleButtons (true);
				editButton.Hide ();
				refreshButton.Hide ();

				setNameValueView ();

				_ldapTreeview.setToolbarHandlers (newButton, deleteButton);
			}
			else if (args.PageNum == 2)
			{
				if (_currentView != null)
				{
					_currentView.removeHandlers ();
					_currentView.removeDndHandlers ();
					_currentView = null;
				}

				_ldapTreeview.removeToolbarHandlers ();

				setNameValueView ();	
				toggleButtons (false);
			}
		}



		public void OnReloginActivate (object o, EventArgs args)
		{

			new LoginDialog (_conn);
			updateStatusBar ();
		}

		public void OnDisconnectActivate (object o, EventArgs args) 
		{
			_conn.Disconnect ();

			mainWindow.Hide ();
			mainWindow = null;

			new ConnectDialog ();
		}

		public void OnImportActivate (object o, EventArgs args)
		{
			FileSelection fs = new FileSelection (
				Mono.Unix.Catalog.GetString ("Choose a file"));
			fs.Run ();
			fs.Hide ();

			if (fs.Filename.Equals (""))
				return;

			UriBuilder ub = new UriBuilder ();
			ub.Scheme = "file";
			ub.Path = fs.Filename;

			Util.ImportData (_conn, mainWindow, ub.Uri);
		}

		public void OnExportActivate (object o, EventArgs args)
		{
			SelectContainerDialog scd = new SelectContainerDialog (_conn, mainWindow);

			scd.Title = Mono.Unix.Catalog.GetString ("Export entry");

			scd.Message = 
				Mono.Unix.Catalog.GetString ("Select the container you wish to export.");
			scd.Run ();

			if (scd.DN.Equals (""))
				return;

			Util.ExportData (_conn, scd.DN);
		}

		public void OnCutActivate (object o, EventArgs args)
		{
			if (!(viewNotebook.Page == 1))
				return;

			_cutDN = _ldapTreeview.getSelectedDN ();
			_cutIter = _ldapTreeview.getSelectedIter ();

			Logger.Log.Debug ("cut - dn: {0}", _cutDN);
		}

		public void OnCopyActivate (object o, EventArgs args)
		{
			if (!(viewNotebook.Page == 1))
				return;

			_cutDN = _ldapTreeview.getSelectedDN ();

			_isCopy = true;

			Logger.Log.Debug ("copy - dn: {0}", _cutDN);
		}

		public void OnPasteActivate (object o, EventArgs args)
		{
			if (!(viewNotebook.Page == 1))
				return;

			_pasteDN = _ldapTreeview.getSelectedDN ();

			if (_pasteDN.Equals (null))
				return;

			DN dn = new DN (_cutDN);
			RDN r = (RDN) dn.RDNs[0];

			bool result = false;

			if (_isCopy)
			{
				result = _conn.Copy (_cutDN, r.toString(false), _pasteDN);
			}
			else
			{
				result = _conn.Move (_cutDN, r.toString(false), _pasteDN);
			}


			if (result)
			{
				string msg = null;

				if (_isCopy)
				{
					msg = String.Format (
						Mono.Unix.Catalog.GetString ("Entry {0} copied to {1}"), 
						_cutDN, _pasteDN);
				}
				else
				{
					msg = String.Format (
						Mono.Unix.Catalog.GetString ("Entry {0} moved to {1}"), 
						_cutDN, _pasteDN);
				}

				Util.MessageBox (mainWindow, 
					msg, 
					MessageType.Info);

				if (!_isCopy)
					_ldapTreeview.RemoveRow (_cutIter);
			}
			else
			{
				string msg = null;

				if (_isCopy)
				{
					string txt = Mono.Unix.Catalog.GetString ("Unable to copy entry ");
					msg = txt + _cutDN;
				}
				else
				{
					string txt = Mono.Unix.Catalog.GetString ("Unable to move entry ");
					msg = txt + _cutDN;
				}

				Util.MessageBox (mainWindow, 
					msg, 
					MessageType.Error);
			}

			if (_isCopy)
			{
				_isCopy = false;
			}
		}

		public void OnViewChanged (object o, EventArgs args)
		{
			clearValues ();

			if (userView.Active)
			{
				viewNotebook.Page = 0;
				changeView ("Users");
			}
			else if (groupView.Active)
			{
				viewNotebook.Page = 0;
				changeView ("Groups");
			}
			else if (hostView.Active)
			{
				viewNotebook.Page = 0;
				changeView ("Hosts");
			}
			else if (contactView.Active)
			{
				viewNotebook.Page = 0;
				changeView ("Contacts");
			}
			else if (customView.Active)
			{
				viewNotebook.Page = 0;
				changeView ("Custom Views");
			}
			else if (browserView.Active)
			{
				viewNotebook.Page = 1;
			}
			else if (searchView.Active)
			{
				viewNotebook.Page = 2;
			}
		}

		public void OnQuitActivate (object o, EventArgs args) 
		{
			Application.Quit ();
		}

		public void OnAboutActivate (object o, EventArgs args) 
		{
			AboutDialog.Show ();
		}
	}
}
