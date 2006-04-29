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
using Gnome;
using System;
using System.Collections;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat 
{
	public class latWindow
	{
		ViewsTreeView viewsTreeView;
		ViewDataTreeView viewDataTreeView;
		AttributeEditorWidget attributeEditor;
	
		// =================
	
		Glade.XML ui;

		[Glade.Widget] Gtk.Window mainWindow;
		[Glade.Widget] Gtk.Entry filterEntry;
		[Glade.Widget] Gtk.Button searchBaseButton;
		[Glade.Widget] Gtk.ScrolledWindow viewScrolledWindow;
		[Glade.Widget] Gtk.ScrolledWindow resultsScrolledWindow;
		[Glade.Widget] ScrolledWindow browserScrolledWindow;
		[Glade.Widget] ScrolledWindow schemaScrolledWindow;
//		[Glade.Widget] TreeView valuesListview;
		[Glade.Widget] Notebook viewNotebook;
		[Glade.Widget] HPaned hpaned1;
		[Glade.Widget] Gtk.ToolButton newToolButton;
		[Glade.Widget] Gtk.ToolButton propertiesToolButton;
		[Glade.Widget] Gtk.ToolButton deleteToolButton;
		[Glade.Widget] Gtk.ToolButton refreshToolButton;
		[Glade.Widget] Gtk.ToolButton templateToolButton;
		[Glade.Widget] Gtk.CheckMenuItem showAllAttributes;
		[Glade.Widget] Gtk.RadioMenuItem userView;
		[Glade.Widget] Gtk.RadioMenuItem groupView;
		[Glade.Widget] Gtk.RadioMenuItem computersView;
		[Glade.Widget] Gtk.RadioMenuItem contactView;
		[Glade.Widget] Gtk.RadioMenuItem browserView;
		[Glade.Widget] Gtk.RadioMenuItem searchView;
		[Glade.Widget] Gtk.RadioMenuItem schemaView;
//		[Glade.Widget] Gtk.Button applyButton;
		[Glade.Widget] Notebook infoNotebook;
		[Glade.Widget] Gtk.TextView objNameTextview;
		[Glade.Widget] Gtk.Entry objDescriptionEntry;
		[Glade.Widget] Gtk.Entry objIDEntry;
		[Glade.Widget] Gtk.TextView objSuperiorTextview;
		[Glade.Widget] TreeView objRequiredTreeview;
		[Glade.Widget] TreeView objOptionalTreeview;
		[Glade.Widget] Gtk.CheckButton objObsoleteCheckbutton;
		[Glade.Widget] VPaned infoVpaned1;

		[Glade.Widget] Gtk.TextView attrNameTextview;
		[Glade.Widget] Gtk.Entry attrDescriptionEntry;
		[Glade.Widget] Gtk.Entry attrIDEntry;
		[Glade.Widget] Gtk.TextView attrSuperiorTextview;
		[Glade.Widget] Gtk.CheckButton attrObsoleteCheckbutton;
		[Glade.Widget] Gtk.CheckButton attrSingleCheckbutton;
		[Glade.Widget] Gtk.CheckButton attrCollectiveCheckbutton;
		[Glade.Widget] Gtk.CheckButton attrUserModCheckbutton;
		[Glade.Widget] Gtk.Entry attrEqualityEntry;
		[Glade.Widget] Gtk.Entry attrOrderingEntry;
		[Glade.Widget] Gtk.Entry attrSubstringEntry;
		[Glade.Widget] Gtk.Entry attrSyntaxEntry;		

		[Glade.Widget] Gtk.ScrolledWindow valuesScrolledWindow;
//		[Glade.Widget] Gtk.HButtonBox hbuttonbox3;
		[Glade.Widget] Gtk.Image sslImage;
		[Glade.Widget] Gnome.AppBar appBar;

		private LdapTreeView _ldapTreeview;
		private SchemaTreeView _schemaTreeview;
		private SearchResultsTreeView _searchTreeView;

		private LdapServer server;
//		private ServerViewFactory serverViewFactory;
//		private ServerView currentView;

		private ArrayList _modList;

		private ListStore valuesStore;

		private ListStore objRequiredStore;
		private ListStore objOptionalStore;

		private string _cutDN = null;
		private TreeIter _cutIter;

		private string _pasteDN = null;
		private bool _isCopy = false;

		public latWindow (LdapServer ldapServer) 
		{
			server = ldapServer;
			_modList = new ArrayList ();

			ui = new Glade.XML (null, "lat.glade", "mainWindow", null);
			ui.Autoconnect (this);

			// set window icon
			Gdk.Pixbuf dirIcon = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			mainWindow.Icon = dirIcon;

			// Restore window positions
			LoadPreference (Preferences.MAIN_WINDOW_WIDTH);
			LoadPreference (Preferences.MAIN_WINDOW_X);
			LoadPreference (Preferences.MAIN_WINDOW_MAXIMIZED);
			LoadPreference (Preferences.MAIN_WINDOW_HPANED);

			// Watch for any changes
			Preferences.SettingChanged += OnPreferencesChanged;

			// Setup views
			viewsTreeView = new ViewsTreeView (server, mainWindow);
			viewsTreeView.ViewSelected += new ViewSelectedHandler (OnViewSelected);

			viewScrolledWindow.AddWithViewport (viewsTreeView);
			viewScrolledWindow.Show ();

//			viewDataTreeView = new ViewDataTreeView (server, mainWindow);
//			valuesScrolledWindow.AddWithViewport (viewDataTreeView);
//			valuesScrolledWindow.Show ();

			attributeEditor = new AttributeEditorWidget ();
			valuesScrolledWindow.AddWithViewport (attributeEditor);
			valuesScrolledWindow.Show ();			

//			serverViewFactory = new ServerViewFactory (valuesStore, 
//				valuesListview, mainWindow, server);

			// Setup browser			
			_ldapTreeview = new LdapTreeView (server, mainWindow);
			_ldapTreeview.dnSelected += new dnSelectedHandler (ldapDNSelected);
			_ldapTreeview.AttributeAdded += new AttributeAddedHandler (ldapAttrAdded);

			browserScrolledWindow.AddWithViewport (_ldapTreeview);
			browserScrolledWindow.Show ();

			// Setup schema browser
			_schemaTreeview = new SchemaTreeView (server, mainWindow);
			_schemaTreeview.schemaSelected += new schemaSelectedHandler (schemaDNSelected);

			schemaScrolledWindow.AddWithViewport (_schemaTreeview);
			schemaScrolledWindow.Show ();

			// Setup search
			_searchTreeView = new SearchResultsTreeView (server);
			_searchTreeView.SearchResultSelected += 
				new SearchResultSelectedHandler (OnSearchSelected);

			resultsScrolledWindow.AddWithViewport (_searchTreeView);
			resultsScrolledWindow.Show ();

			searchBaseButton.Label = server.DirectoryRoot;
			toggleButtons (false);

			// status bar			
			updateStatusBar ();

			// handlers		
			viewNotebook.SwitchPage += new SwitchPageHandler (notebookViewChanged);

//			applyButton.Sensitive = false;

			// setup schema

			objRequiredStore = new ListStore (typeof (string));
			objRequiredTreeview.Model = objRequiredStore;

			objOptionalStore = new ListStore (typeof (string));
			objOptionalTreeview.Model = objOptionalStore;

			objRequiredTreeview.AppendColumn ("Required Attributes", new CellRendererText (), "text", 0);
			objOptionalTreeview.AppendColumn ("Optional Attributes", new CellRendererText (), "text", 0);

			infoVpaned1.Position = 150;

			toggleInfoNotebook (false);

			templateToolButton.Hide ();
		}

		public void OnPreferencesChanged (object sender, GConf.NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}
		
		public void OnPluginManagerClicked (object o, EventArgs args)
		{
			new PluginManagerDialog (server, mainWindow);			
		}
	
		public void OnViewSelected (object o, ViewSelectedEventArgs args)
		{
			ViewPlugin vp = Global.viewPluginManager.Find (args.Name);
			
			if (vp == null) {
				Logger.Log.Debug ("OnViewSelected: ViewPlugin == null");
				return;
			}
		
			viewDataTreeView.ConfigureView (vp);
			viewDataTreeView.Populate ();
			
//			clearValues ();
//
//			if (args.Name.Equals (server.Host)) {
//
//				// FIXME: Need a way to remove the handlers
//
//				setNameValueView ();
//				showConnectionAttributes ();
//
//				return;
//			}
//
//			changeView (args.Name);
		}

		public void OnSearchSelected (object o, SearchResultSelectedEventArgs args)
		{
			LdapEntry entry = server.GetEntry (args.DN);

			if (entry != null)
				showEntryAttributes (entry);
		}

		private void updateStatusBar ()
		{
			string msg = null;

			if (server.AuthDN == null)
				msg = String.Format("Bind DN: anonymous");
			else
				msg = String.Format("Bind DN: {0}", server.AuthDN);

			appBar.Pop ();
			appBar.Push (msg);

			sslImage.Pixbuf = Util.GetSSLIcon (server.UseSSL);
		}

		private void toggleInfoNotebook (bool show)
		{
			if (show) {
				infoNotebook.Show ();
//				hbuttonbox3.Hide ();
				valuesScrolledWindow.Hide ();
			} else {
				infoNotebook.Hide ();
//				hbuttonbox3.Show ();
				valuesScrolledWindow.Show ();
			}
		}

		private void setInfoNotePage (int page)
		{
			if (page == 0) {

				Gtk.Widget w = infoNotebook.GetNthPage (1);
				w.HideAll ();

				w = infoNotebook.GetNthPage (0);
				w.ShowAll ();

				infoNotebook.Show ();

			} else if (page == 1) {

				Gtk.Widget w = infoNotebook.GetNthPage (1);
				w.ShowAll ();

				w = infoNotebook.GetNthPage (0);
				w.HideAll ();

				infoNotebook.Show ();

			} else {

				infoNotebook.HideAll ();
			}
		}
	
		private void toggleButtons (bool btnState)
		{
			newToolButton.Sensitive = btnState;
			propertiesToolButton.Sensitive = btnState;
			deleteToolButton.Sensitive = btnState;
			refreshToolButton.Sensitive = btnState;

			propertiesToolButton.Show ();
			refreshToolButton.Show ();
		}

		private void ldapAttrAdded (object o, AddAttributeEventArgs args)
		{
			valuesStore.AppendValues (args.Name, args.Value);

			LdapAttribute attribute = new LdapAttribute (args.Name, args.Value);
			LdapModification lm = new LdapModification (LdapModification.ADD, attribute);

			_modList.Add (lm);

//			applyButton.Sensitive = true;
		}

		private void schemaDNSelected (object o, schemaSelectedEventArgs args)
		{
			if (args.Name == "Object Classes" || args.Name == "Attribute Types")
				return;

			if (args.Parent == "Object Classes") {
				setInfoNotePage (0);

				SchemaParser sp = server.GetObjectClassSchema (args.Name);
				showEntrySchema (sp);

			} else if (args.Parent == "Attribute Types") {

				setInfoNotePage (1);

				SchemaParser sp = server.GetAttributeTypeSchema (args.Name);
				showAttrTypeSchema (sp);
			}
		}

		private void ldapDNSelected (object o, dnSelectedEventArgs args)
		{
			if (args.IsHost) {
//				showConnectionAttributes ();
				return;
			}

			LdapEntry entry = server.GetEntry (args.DN);
			attributeEditor.Show (server, entry, showAllAttributes.Active);
//			showEntryAttributes (entry);
		}

		private void Close ()
		{
			int x, y, width, height;
			mainWindow.GetPosition (out x, out y);
			mainWindow.GetSize (out width, out height);

			bool maximized = ((mainWindow.GdkWindow.State & Gdk.WindowState.Maximized) > 0);
			Preferences.Set (Preferences.MAIN_WINDOW_MAXIMIZED, maximized);

			if (!maximized) {
				Preferences.Set (Preferences.MAIN_WINDOW_X, x);
				Preferences.Set (Preferences.MAIN_WINDOW_Y, y);
				Preferences.Set (Preferences.MAIN_WINDOW_WIDTH, width);
				Preferences.Set (Preferences.MAIN_WINDOW_HEIGHT, height);
			}

			Preferences.Set (Preferences.MAIN_WINDOW_HPANED, hpaned1.Position);

			Application.Quit ();
		}

		public void OnAppDelete (object o, DeleteEventArgs args) 
		{	
			Close ();
			args.RetVal = true;
		}
		
		public void OnSearchBuilderClicked (object o, EventArgs args)
		{
			SearchBuilderDialog sbd = new SearchBuilderDialog ();
			filterEntry.Text = sbd.UserFilter;
		}

		public void OnSearchClicked (object o, EventArgs args)
		{
			LdapEntry[] searchResults = server.Search (
				searchBaseButton.Label, filterEntry.Text);

			if (searchResults == null) {
				HIGMessageDialog dialog = new HIGMessageDialog (
					mainWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Search error",
					Mono.Unix.Catalog.GetString ("Invalid search filter."));

				dialog.Run ();
				dialog.Destroy ();

			} else if (searchResults.Length > 0 && filterEntry.Text != "") {
				_searchTreeView.UpdateSearchResults (searchResults);

				string msg = String.Format ("Found {0} matching entries", searchResults.Length);
				appBar.Pop ();
				appBar.Push (msg);
			}
		}

		public void OnSearchBaseClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = 
				new SelectContainerDialog (server, mainWindow);

			scd.Message = String.Format (
				Mono.Unix.Catalog.GetString ("Where in the directory would\nyou like to start the search?"));

			scd.Title = Mono.Unix.Catalog.GetString ("Select search base");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (server.Host))
				searchBaseButton.Label = scd.DN;
		}

		private void showConnectionAttributes ()
		{
			valuesStore.Clear ();

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Host"), server.Host);

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Port"), server.Port.ToString());

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("User"), server.AuthDN);

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Base DN"), server.DirectoryRoot);

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Connected"),
					 server.Connected.ToString());

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Bound"), server.Bound.ToString());

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("TLS/SSL"), server.UseSSL.ToString());

			valuesStore.AppendValues (
				Mono.Unix.Catalog.GetString ("Protocol Version"),
					 server.Protocol.ToString());

			if (server.ServerType == LdapServerType.ActiveDirectory) {

				valuesStore.AppendValues (
					Mono.Unix.Catalog.GetString ("DNS Host Name"),
					server.ADInfo.DnsHostName);

				valuesStore.AppendValues (
					Mono.Unix.Catalog.GetString ("Domain Controller Functionality"),
					server.ADInfo.DomainControllerFunctionality);

				valuesStore.AppendValues (
					Mono.Unix.Catalog.GetString ("Forest Functionality"),
					server.ADInfo.ForestFunctionality);

				valuesStore.AppendValues (
					Mono.Unix.Catalog.GetString ("Domain Functionality"),
					server.ADInfo.DomainFunctionality);

				valuesStore.AppendValues (
					Mono.Unix.Catalog.GetString ("Global Catalog Ready"),
					server.ADInfo.IsGlobalCatalogReady.ToString());

				valuesStore.AppendValues (
					Mono.Unix.Catalog.GetString ("Synchronized"),
					server.ADInfo.IsSynchronized.ToString());
			}
		}

		private void showEntryAttributes (LdapEntry entry)
		{
			valuesStore.Clear ();
			_modList.Clear ();

			ArrayList allAttrs = new ArrayList ();
		
			LdapAttribute a = entry.getAttribute ("objectClass");

			foreach (string o in a.StringValueArray) {

				string[] attrs = server.GetAllAttributes (o);
				
				foreach (string at in attrs)
					if (!allAttrs.Contains (at))
						allAttrs.Add (at);
			}

			LdapAttributeSet attributeSet = entry.getAttributeSet ();

			foreach (LdapAttribute attr in attributeSet) {

				if (allAttrs.Contains (attr.Name))
					allAttrs.Remove (attr.Name);

				foreach (string s in attr.StringValueArray)
					valuesStore.AppendValues (attr.Name, s);
			}

			if (!showAllAttributes.Active)
				return;

			foreach (string n in allAttrs)
				valuesStore.AppendValues (n, "");
		}

		public void OnShowAllAttributes (object o, EventArgs args)
		{
			string dn = null;

			if (viewNotebook.CurrentPage == 1) {

				dn = _ldapTreeview.getSelectedDN ();

				if (dn == null)
					return;

				LdapEntry entry = server.GetEntry (dn);
				attributeEditor.Show (server, entry, showAllAttributes.Active);
			}
		}

		private void showAttrTypeSchema (SchemaParser sp)
		{
			try {

				attrIDEntry.Text = sp.ID;
				attrDescriptionEntry.Text = sp.Description;

				string tmp = "";

				foreach (string a in sp.Names)
					tmp += String.Format ("{0}\n", a);

				attrNameTextview.Buffer.Text = tmp;

				attrEqualityEntry.Text = sp.Equality;
				attrOrderingEntry.Text = sp.Ordering;
				attrSubstringEntry.Text = sp.Substring;
				attrSyntaxEntry.Text = sp.Syntax;

				attrObsoleteCheckbutton.Active = sp.Obsolete;
				attrSingleCheckbutton.Active = sp.Single;
				attrCollectiveCheckbutton.Active = sp.Collective;
				attrUserModCheckbutton.Active = sp.UserMod;
		
				tmp = "";

				foreach (string b in sp.Superiors)
					tmp += String.Format ("{0}\n", b);

				attrSuperiorTextview.Buffer.Text = tmp;

			} catch {}
		}

		private void showEntrySchema (SchemaParser sp)
		{
			try {

				objRequiredStore.Clear ();
				objOptionalStore.Clear ();

				objIDEntry.Text = sp.ID;
				objDescriptionEntry.Text = sp.Description;

				string tmp = "";

				foreach (string a in sp.Names)
					tmp += String.Format ("{0}\n", a);

				objNameTextview.Buffer.Text = tmp;

				tmp = "";

				foreach (string b in sp.Superiors)
					tmp += String.Format ("{0}\n", b);

				objSuperiorTextview.Buffer.Text = tmp;

				foreach (string c in sp.Required)
					objRequiredStore.AppendValues (c);

				foreach (string d in sp.Optional)
					objOptionalStore.AppendValues (d);

				objObsoleteCheckbutton.Active = sp.Obsolete;

			} catch {}
		}

		private void setNameValueView ()
		{
//			TreeViewColumn col;
//
//			valuesStore = new ListStore (typeof (string), typeof (string));
//			valuesListview.Model = valuesStore;
//
//			col = valuesListview.AppendColumn (
//				Mono.Unix.Catalog.GetString ("Name"), 
//				new CellRendererText (), "text", 0);
//
//			col.SortColumnId = 0;
//
//			CellRendererText cell = new CellRendererText ();
//			cell.Editable = true;
//			cell.Edited += new EditedHandler (OnAttributeEdit);
//
//			col = valuesListview.AppendColumn (
//				Mono.Unix.Catalog.GetString ("Value"), cell, "text", 1);
//		
//			valuesStore.SetSortColumnId (0, SortType.Ascending);
		}

//		private void removeButtonHandlers ()
//		{
//			newToolButton.Clicked -= new EventHandler
//				 (currentView.OnNewEntryActivate);
//
//			propertiesToolButton.Clicked -= new EventHandler
//				 (currentView.OnEditActivate);
//
//			deleteToolButton.Clicked -= new EventHandler
//				 (currentView.OnDeleteActivate);
//
//			refreshToolButton.Clicked -= new EventHandler
//				 (currentView.OnRefreshActivate);
//		}

		private void cleanupView ()
		{
//			if (currentView != null) {
//
//				removeButtonHandlers ();
//				currentView.RemoveDndHandlers ();
//				currentView.RemoveHandlers ();
//				currentView = null;
//			}
		}

		private void changeView (string name)
		{
//			cleanupView ();
//
//			currentView = serverViewFactory.Create (name);
//
//			if (currentView != null)
//				currentView.Populate ();
//
//			newToolButton.Clicked += new EventHandler
//				(currentView.OnNewEntryActivate);
//
//			propertiesToolButton.Clicked += new EventHandler
//				(currentView.OnEditActivate);
//
//			deleteToolButton.Clicked += new EventHandler
//				(currentView.OnDeleteActivate);
//
//			refreshToolButton.Clicked += new EventHandler
//				(currentView.OnRefreshActivate);
//
//			toggleButtons (true);
		}

		private void clearValues ()
		{
//			if (valuesStore != null) {
//				valuesStore.Clear ();
//				valuesStore = null;
//			}
//
//			foreach (TreeViewColumn col in valuesListview.Columns)
//				valuesListview.RemoveColumn (col);
		}

		private void notebookViewChanged (object o, SwitchPageArgs args)
		{
			clearValues ();

			if (args.PageNum == 0) {

				_ldapTreeview.removeToolbarHandlers ();
				toggleButtons (false);
				toggleInfoNotebook (false);

				templateToolButton.Hide ();

			} else if (args.PageNum == 1) {

				cleanupView ();

				toggleButtons (true);
				toggleInfoNotebook (false);

				templateToolButton.Show ();
				propertiesToolButton.Hide ();
				refreshToolButton.Hide ();

				setNameValueView ();

				_ldapTreeview.setToolbarHandlers (newToolButton, deleteToolButton);

			} else if (args.PageNum == 2) {

				cleanupView ();

				_ldapTreeview.removeToolbarHandlers ();

				setNameValueView ();	
				toggleButtons (false);
				toggleInfoNotebook (false);

				templateToolButton.Hide ();

			} else if (args.PageNum == 3) {

				cleanupView ();

				toggleButtons (false);

				toggleInfoNotebook (true);

				setInfoNotePage (-1);

				templateToolButton.Hide ();
			}
		}

		public void OnTemplatesClicked (object o, EventArgs args)
		{
			new TemplatesDialog (server);
		}

		private void LoadPreference (String key)
		{
			object val = Preferences.Get (key);

			if (val == null) {

				if (key == Preferences.MAIN_WINDOW_HPANED)
					hpaned1.Position = 250;

				return;
			}
			
			Logger.Log.Debug ("Setting {0} to {1}", key, val);

			switch (key) {
			case Preferences.MAIN_WINDOW_MAXIMIZED:
				if ((bool) val)
					mainWindow.Maximize ();
				else
					mainWindow.Unmaximize ();
				break;

			case Preferences.MAIN_WINDOW_X:
			case Preferences.MAIN_WINDOW_Y:
				mainWindow.Move((int) Preferences.Get(Preferences.MAIN_WINDOW_X),
						(int) Preferences.Get(Preferences.MAIN_WINDOW_Y));
				break;
			
			case Preferences.MAIN_WINDOW_WIDTH:
			case Preferences.MAIN_WINDOW_HEIGHT:
				mainWindow.SetDefaultSize((int) Preferences.Get(Preferences.MAIN_WINDOW_WIDTH),
						(int) Preferences.Get(Preferences.MAIN_WINDOW_HEIGHT));

				mainWindow.ReshowWithInitialSize();
				break;

			case Preferences.MAIN_WINDOW_HPANED:
				hpaned1.Position = (int) Preferences.Get(
					Preferences.MAIN_WINDOW_HPANED);
				break;
			}
		}

		// Menu

		public void OnNewActivate (object o, EventArgs args)
		{
//			if (viewNotebook.CurrentPage == 0)
//				if (currentView != null)
//					currentView.OnNewEntryActivate (o, args);
//			else if (viewNotebook.CurrentPage == 1)
//				_ldapTreeview.OnNewEntryActivate (o, args);
		}

		public void OnDeleteActivate (object o, EventArgs args)
		{
//			if (viewNotebook.CurrentPage == 0)
//				if (currentView != null)
//					currentView.OnDeleteActivate (o, args);
//			else if (viewNotebook.CurrentPage == 1)
//				_ldapTreeview.OnDeleteActivate (o, args);
		}

		public void OnPropertiesActivate (object o, EventArgs args)
		{
//			if (viewNotebook.CurrentPage == 0)
//				if (currentView != null)
//					currentView.OnEditActivate (o, args);
		}

		public void OnRefreshActivate (object o, EventArgs args)
		{
//			if (viewNotebook.CurrentPage == 0)
//				if (currentView != null)
//					currentView.OnRefreshActivate (o, args);
		}

		public void OnReloginActivate (object o, EventArgs args)
		{
			string msg = Mono.Unix.Catalog.GetString (
				"Enter the new username and password\nyou wish to re-login with");

			LoginDialog ld = new LoginDialog (server, msg);
			ld.Run ();

			updateStatusBar ();
		}

		public void OnDisconnectActivate (object o, EventArgs args) 
		{
			string msg = Mono.Unix.Catalog.GetString (
				"Are you sure you want to disconnect from\nserver: ") + server.Host;

			MessageDialog md = new MessageDialog (mainWindow, 
					DialogFlags.DestroyWithParent,
					MessageType.Question, 
					ButtonsType.YesNo, 
					msg);
	     
			ResponseType result = (ResponseType)md.Run ();

			if (result == ResponseType.Yes) {

				server.Disconnect ();

				mainWindow.Hide ();
				mainWindow = null;

				new ConnectDialog ();
			}

			md.Destroy ();
		}

		public void OnImportActivate (object o, EventArgs args)
		{
			FileChooserDialog fcd = new FileChooserDialog (
				Mono.Unix.Catalog.GetString ("Choose an LDIF file to import"),
				Gtk.Stock.Open, 
				mainWindow, 
				FileChooserAction.Open);

			fcd.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			fcd.AddButton (Gtk.Stock.Open, ResponseType.Ok);

			fcd.SelectMultiple = false;

			ResponseType response = (ResponseType) fcd.Run();
			if (response == ResponseType.Ok) {

				UriBuilder ub = new UriBuilder ();
				ub.Scheme = "file";
				ub.Path = fcd.Filename;

				Util.ImportData (server, mainWindow, ub.Uri);
			} 
		
			fcd.Destroy();
		}

		public void OnExportActivate (object o, EventArgs args)
		{
			SelectContainerDialog scd = new SelectContainerDialog (server, mainWindow);

			scd.Title = Mono.Unix.Catalog.GetString ("Export entry");

			scd.Message = 
				Mono.Unix.Catalog.GetString ("Select the container you wish to export.");
			scd.Run ();

			if (scd.DN.Equals (""))
				return;

			Util.ExportData (server, mainWindow, scd.DN);
		}

		public void OnPopulateActivate (object o, EventArgs args)
		{
			new SambaPopulateDialog (server);
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

			try {

				string msg = null;

				if (_isCopy) {

					server.Copy (_cutDN, r.toString(false), _pasteDN);

					msg = String.Format (
						Mono.Unix.Catalog.GetString ("Entry {0} copied to {1}"), 
						_cutDN, _pasteDN);

				} else {

					server.Move (_cutDN, r.toString(false), _pasteDN);

					msg = String.Format (
						Mono.Unix.Catalog.GetString ("Entry {0} moved to {1}"), 
						_cutDN, _pasteDN);

				}

				HIGMessageDialog dialog = new HIGMessageDialog (
					mainWindow,
					0,
					Gtk.MessageType.Info,
					Gtk.ButtonsType.Ok,
					"Paste results",
					msg);

				dialog.Run ();
				dialog.Destroy ();

				if (!_isCopy)
					_ldapTreeview.RemoveRow (_cutIter);

			} catch (Exception e) {

				string msg = null;

				if (_isCopy) {
					string txt = Mono.Unix.Catalog.GetString ("Unable to copy entry ");
					msg = txt + _cutDN;
				} else {

					string txt = Mono.Unix.Catalog.GetString ("Unable to move entry ");
					msg = txt + _cutDN;
				}

				msg += "\nError: " + e.Message;

				HIGMessageDialog dialog = new HIGMessageDialog (
					mainWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Paste error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			}

			if (_isCopy)
				_isCopy = false;
		}

		public void OnMassEditActivate (object o, EventArgs args)
		{
			new MassEditDialog (server);
		}

		public void OnViewChanged (object o, EventArgs args)
		{
			clearValues ();

			string viewPrefix = Util.GetServerPrefix (server);

			if (userView.Active) {
				viewNotebook.Page = 0;
				toggleInfoNotebook (false);
				changeView (viewPrefix + "Users");
			} else if (groupView.Active) {
				viewNotebook.Page = 0;
				toggleInfoNotebook (false);
				changeView (viewPrefix + "Groups");
			} else if (computersView.Active) {
				viewNotebook.Page = 0;
				toggleInfoNotebook (false);
				changeView (viewPrefix + "Computers");
			} else if (contactView.Active) {
				viewNotebook.Page = 0;
				toggleInfoNotebook (false);
				changeView (viewPrefix + "Contacts");
			} else if (browserView.Active) {
				viewNotebook.Page = 1;
				toggleInfoNotebook (false);
			} else if (searchView.Active) {
				viewNotebook.Page = 2;
				toggleInfoNotebook (false);
			} else if (schemaView.Active) {
				viewNotebook.Page = 3;
				toggleInfoNotebook (true);
				setInfoNotePage (-1);
			}
		}

		public void OnQuitActivate (object o, EventArgs args) 
		{
			Close ();
		}

		public void OnHelpContentsActivate (object o, EventArgs args)
		{
			try {

				Gnome.Help.DisplayDesktopOnScreen (Global.latProgram, 
					Defines.PACKAGE, 
					"lat.xml", 
					null, 
					Gdk.Screen.Default);

			} catch (Exception e) {

				HIGMessageDialog dialog = new HIGMessageDialog (
					mainWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Help error",
					e.Message);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		public void OnAboutActivate (object o, EventArgs args) 
		{
			AboutDialog.Show ();
		}
	}
}
