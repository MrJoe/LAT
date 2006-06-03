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
		Glade.XML ui;

		[Glade.Widget] Gtk.Window mainWindow;
		[Glade.Widget] Gtk.Entry filterEntry;
		[Glade.Widget] Gtk.Button searchBaseButton;
		[Glade.Widget] Gtk.ScrolledWindow viewScrolledWindow;
		[Glade.Widget] Gtk.ScrolledWindow resultsScrolledWindow;
		[Glade.Widget] ScrolledWindow browserScrolledWindow;
		[Glade.Widget] ScrolledWindow schemaScrolledWindow;
		[Glade.Widget] Notebook viewNotebook;
		[Glade.Widget] HPaned hpaned1;
		[Glade.Widget] Gtk.ToolButton newToolButton;
		[Glade.Widget] Gtk.ToolButton propertiesToolButton;
		[Glade.Widget] Gtk.ToolButton deleteToolButton;
		[Glade.Widget] Gtk.ToolButton refreshToolButton;
		[Glade.Widget] Gtk.ToolButton templateToolButton;
		[Glade.Widget] Gtk.CheckMenuItem showAllAttributes;
		[Glade.Widget] Gtk.RadioMenuItem viewsView;
		[Glade.Widget] Gtk.RadioMenuItem browserView;
		[Glade.Widget] Gtk.RadioMenuItem searchView;
		[Glade.Widget] Gtk.RadioMenuItem schemaView;
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

		[Glade.Widget] Gtk.Entry matNameEntry;
		[Glade.Widget] Gtk.Entry matOIDEntry;
		[Glade.Widget] Gtk.Entry matSyntaxEntry;
		
		[Glade.Widget] Gtk.Entry synDescriptionEntry;
		[Glade.Widget] Gtk.Entry synOIDEntry;

		[Glade.Widget] Gtk.ScrolledWindow valuesScrolledWindow;
		[Glade.Widget] Gtk.Image sslImage;
		[Glade.Widget] Gnome.AppBar appBar;

		private LdapTreeView ldapTreeView;
		private SchemaTreeView _schemaTreeview;
		private SearchResultsTreeView _searchTreeView;

		LdapServer server;

		private ListStore objRequiredStore;
		private ListStore objOptionalStore;

		private string _cutDN = null;
		private TreeIter _cutIter;

		private string _pasteDN = null;
		private bool _isCopy = false;

		ViewsTreeView viewsTreeView;
		ViewDataTreeView viewDataTreeView;
		AttributeEditorWidget attributeEditor;
		ServerInfoView serverInfoView;

		public latWindow (LdapServer ldapServer) 
		{
			server = ldapServer;

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

			LoadPreference (Preferences.DISPLAY_VERBOSE_MESSAGES);

			// Watch for any changes
			Preferences.SettingChanged += OnPreferencesChanged;

			// Setup views
			viewsTreeView = new ViewsTreeView (server, mainWindow);
			viewsTreeView.ViewSelected += new ViewSelectedHandler (OnViewSelected);

			viewScrolledWindow.AddWithViewport (viewsTreeView);
			viewScrolledWindow.Show ();

			viewDataTreeView = new ViewDataTreeView (server, mainWindow);
			valuesScrolledWindow.AddWithViewport (viewDataTreeView);
			valuesScrolledWindow.Show ();			

			// Setup browser			
			ldapTreeView = new LdapTreeView (server, mainWindow);
			ldapTreeView.dnSelected += new dnSelectedHandler (ldapDNSelected);

			browserScrolledWindow.AddWithViewport (ldapTreeView);
			browserScrolledWindow.Show ();

			LoadPreference (Preferences.BROWSER_SELECTION);

			// Setup schema browser
			_schemaTreeview = new SchemaTreeView (server, mainWindow);
			_schemaTreeview.schemaSelected += new schemaSelectedHandler (schemaDNSelected);

			schemaScrolledWindow.AddWithViewport (_schemaTreeview);
			schemaScrolledWindow.Show ();

			// Setup search
			_searchTreeView = new SearchResultsTreeView (server);
			_searchTreeView.SearchResultSelected += new SearchResultSelectedHandler (OnSearchSelected);

			resultsScrolledWindow.AddWithViewport (_searchTreeView);
			resultsScrolledWindow.Show ();

			searchBaseButton.Label = server.DirectoryRoot;
			toggleButtons (false);

			// status bar			
			updateStatusBar ();

			// handlers		
			viewNotebook.SwitchPage += new SwitchPageHandler (notebookViewChanged);

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
		
		public void OnPreferencesActivate (object sender, EventArgs args)
		{
			new PreferencesDialog (server);
			viewsTreeView.Refresh ();
			Global.profileManager.SaveProfiles ();
		}
	
		public void OnViewSelected (object o, ViewSelectedEventArgs args)
		{
			ViewPlugin vp = Global.viewPluginManager.Find (args.Name);
			
			if (vp == null) {
				if (viewDataTreeView != null) {
					viewDataTreeView.Destroy ();
					viewDataTreeView = null;
				}
			
				serverInfoView = new ServerInfoView (server);
				valuesScrolledWindow.AddWithViewport (serverInfoView);
				valuesScrolledWindow.ShowAll ();
				
				return;
			}

			if (viewDataTreeView == null) {
				serverInfoView.Destroy ();
				serverInfoView = null;
				
				viewDataTreeView = new ViewDataTreeView (server, mainWindow);
				valuesScrolledWindow.AddWithViewport (viewDataTreeView);
				valuesScrolledWindow.ShowAll ();			
			}
		
			viewDataTreeView.ConfigureView (vp);
			viewDataTreeView.Populate ();
		}

		public void OnSearchSelected (object o, SearchResultSelectedEventArgs args)
		{
			LdapEntry entry = server.GetEntry (args.DN);

			if (entry != null) 
				attributeEditor.Show (server, entry, showAllAttributes.Active);
		}

		void updateStatusBar ()
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

		void toggleInfoNotebook (bool show)
		{
			if (show) {
				infoNotebook.Show ();
				valuesScrolledWindow.Hide ();
			} else {
				infoNotebook.Hide ();
				valuesScrolledWindow.Show ();
			}
		}

		void setInfoNotePage (int page)
		{
			Gtk.Widget w = null;
		
			switch (page) {
			
			case 0:
				w = infoNotebook.GetNthPage (3);
				w.HideAll ();
				
				w = infoNotebook.GetNthPage (2);
				w.HideAll ();
				
				w = infoNotebook.GetNthPage (1);
				w.HideAll ();

				w = infoNotebook.GetNthPage (0);
				w.ShowAll ();

				infoNotebook.Show ();
				break;
					
			case 1:
				w = infoNotebook.GetNthPage (3);
				w.HideAll ();
				
				w = infoNotebook.GetNthPage (2);
				w.HideAll ();
				
				w = infoNotebook.GetNthPage (1);
				w.ShowAll ();

				w = infoNotebook.GetNthPage (0);
				w.HideAll ();

				infoNotebook.Show ();
				break;

			case 2:
				w = infoNotebook.GetNthPage (3);
				w.HideAll ();
				
				w = infoNotebook.GetNthPage (2);
				w.ShowAll ();
				
				w = infoNotebook.GetNthPage (1);
				w.HideAll ();

				w = infoNotebook.GetNthPage (0);
				w.HideAll ();

				infoNotebook.Show ();			
				break;
				
			case 3:
				w = infoNotebook.GetNthPage (3);
				w.ShowAll ();
				
				w = infoNotebook.GetNthPage (2);
				w.HideAll ();
				
				w = infoNotebook.GetNthPage (1);
				w.HideAll ();

				w = infoNotebook.GetNthPage (0);
				w.HideAll ();

				infoNotebook.Show ();			
				break;
				
			default:
				infoNotebook.HideAll ();
				break;
			}
		}
	
		void toggleButtons (bool btnState)
		{
			newToolButton.Sensitive = btnState;
			propertiesToolButton.Sensitive = btnState;
			deleteToolButton.Sensitive = btnState;
			refreshToolButton.Sensitive = btnState;

			propertiesToolButton.Show ();
			refreshToolButton.Show ();
		}

		void schemaDNSelected (object o, schemaSelectedEventArgs args)
		{
			if (args.Name == "Object Classes" || args.Name == "Attribute Types" || args.Name == "Matching Rules" || args.Name == "LDAP Syntaxes")
				return;

			if (args.Parent == "Object Classes") {
				
				setInfoNotePage (0);
				SchemaParser sp = server.GetObjectClassSchema (args.Name);
				showEntrySchema (sp);

			} else if (args.Parent == "Attribute Types") {

				setInfoNotePage (1);
				SchemaParser sp = server.GetAttributeTypeSchema (args.Name);
				showAttrTypeSchema (sp);
				
			} else if (args.Parent == "Matching Rules") {

				setInfoNotePage (2);
				SchemaParser sp = server.GetMatchingRule (args.Name);
				showMatchingRule (sp);				
			
			} else if (args.Parent == "LDAP Syntaxes") {
			
				setInfoNotePage (3);
				SchemaParser sp = server.GetLdapSyntax (args.Name);
				showLdapSyntax (sp);
			}
		}

		void ldapDNSelected (object o, dnSelectedEventArgs args)
		{
			if (args.IsHost) {
				if (attributeEditor != null)
					attributeEditor.Destroy ();
			
				serverInfoView = new ServerInfoView (server);
				valuesScrolledWindow.AddWithViewport (serverInfoView);
				valuesScrolledWindow.ShowAll ();
				
				return;
			}

			if (serverInfoView != null) {
				serverInfoView.Destroy ();
				serverInfoView = null;
				
				attributeEditor = new AttributeEditorWidget ();
				valuesScrolledWindow.AddWithViewport (attributeEditor);
				valuesScrolledWindow.ShowAll ();				
			}
			
			LdapEntry entry = server.GetEntry (args.DN);			
			if (entry != null)
				attributeEditor.Show (server, entry, showAllAttributes.Active);
		}

		void Close ()
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
			Preferences.Set (Preferences.BROWSER_SELECTION, ldapTreeView.BrowserSelectionMethod);
			Preferences.Set (Preferences.DISPLAY_VERBOSE_MESSAGES, Global.VerboseMessages);

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

		public void OnShowAllAttributes (object o, EventArgs args)
		{
			string dn = null;

			if (viewNotebook.CurrentPage == 1) {

				dn = ldapTreeView.getSelectedDN ();

				if (dn == null)
					return;

				LdapEntry entry = server.GetEntry (dn);
				attributeEditor.Show (server, entry, showAllAttributes.Active);
			}
		}

		void showMatchingRule (SchemaParser sp)
		{
			if (sp == null)
				return;
				
			matNameEntry.Text = sp.Names[0];
			matOIDEntry.Text = sp.ID;
			matSyntaxEntry.Text = sp.Syntax;
		}

		void showLdapSyntax (SchemaParser sp)
		{
			if (sp == null)
				return;
				
			synDescriptionEntry.Text = sp.Description;
			synOIDEntry.Text = sp.ID;
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

//		void changeView (string name)
//		{
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
//		}

		private void notebookViewChanged (object o, SwitchPageArgs args)
		{
			if (args.PageNum == 0) {

				ldapTreeView.removeToolbarHandlers ();
				toggleButtons (false);
				toggleInfoNotebook (false);

				if (serverInfoView != null) {
					serverInfoView.Destroy ();
					serverInfoView = null;
				}

				if (attributeEditor != null) {
					attributeEditor.Destroy ();
					attributeEditor = null;
				}

				if (viewDataTreeView == null) {
					viewDataTreeView = new ViewDataTreeView (server, mainWindow);
					valuesScrolledWindow.AddWithViewport (viewDataTreeView);
					valuesScrolledWindow.Show ();
				}

				templateToolButton.Hide ();

			} else if (args.PageNum == 1) {

				cleanupView ();

				toggleButtons (true);
				toggleInfoNotebook (false);

				templateToolButton.Show ();
				propertiesToolButton.Hide ();
				refreshToolButton.Hide ();

				if (serverInfoView != null) {
					serverInfoView.Destroy ();
					serverInfoView = null;
				}

				if (viewDataTreeView != null) {
					viewDataTreeView.Destroy ();
					viewDataTreeView = null;
				}
				
				if (attributeEditor == null) {
					attributeEditor = new AttributeEditorWidget ();
					valuesScrolledWindow.AddWithViewport (attributeEditor);
					valuesScrolledWindow.Show ();
				}

				ldapTreeView.setToolbarHandlers (newToolButton, deleteToolButton);

			} else if (args.PageNum == 2) {

				cleanupView ();

				ldapTreeView.removeToolbarHandlers ();

				if (serverInfoView != null) {
					serverInfoView.Destroy ();
					serverInfoView = null;
				}

				if (viewDataTreeView != null) {
					viewDataTreeView.Destroy ();
					viewDataTreeView = null;
				}
				
				if (attributeEditor == null) {
					attributeEditor = new AttributeEditorWidget ();
					valuesScrolledWindow.AddWithViewport (attributeEditor);
					valuesScrolledWindow.Show ();
				}
				
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
				hpaned1.Position = (int) Preferences.Get (Preferences.MAIN_WINDOW_HPANED);
				break;
				
			case Preferences.BROWSER_SELECTION:
				ldapTreeView.BrowserSelectionMethod = (int) val; 
				break;
				
			case Preferences.DISPLAY_VERBOSE_MESSAGES:
				Global.VerboseMessages = (bool) val;
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
//				ldapTreeView.OnNewEntryActivate (o, args);
		}

		public void OnDeleteActivate (object o, EventArgs args)
		{
//			if (viewNotebook.CurrentPage == 0)
//				if (currentView != null)
//					currentView.OnDeleteActivate (o, args);
//			else if (viewNotebook.CurrentPage == 1)
//				ldapTreeView.OnDeleteActivate (o, args);
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

			_cutDN = ldapTreeView.getSelectedDN ();
			_cutIter = ldapTreeView.getSelectedIter ();

			Logger.Log.Debug ("cut - dn: {0}", _cutDN);
		}

		public void OnCopyActivate (object o, EventArgs args)
		{
			if (!(viewNotebook.Page == 1))
				return;

			_cutDN = ldapTreeView.getSelectedDN ();

			_isCopy = true;

			Logger.Log.Debug ("copy - dn: {0}", _cutDN);
		}

		public void OnPasteActivate (object o, EventArgs args)
		{
			if (!(viewNotebook.Page == 1))
				return;

			_pasteDN = ldapTreeView.getSelectedDN ();

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
					ldapTreeView.RemoveRow (_cutIter);

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
			if (viewsView.Active) {
				viewNotebook.Page = 0;
				toggleInfoNotebook (false);
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
	
	public class ServerInfoView : Gtk.TreeView
	{
		public ServerInfoView (LdapServer server) : base ()
		{	
			ListStore store = new ListStore (typeof (string), typeof (string));
			this.Model = store;

			this.AppendColumn ("Name", new CellRendererText (), "text", 0); 
			this.AppendColumn ("Value", new CellRendererText (), "text", 1); 

			store.AppendValues (Mono.Unix.Catalog.GetString ("Host"), server.Host);
			store.AppendValues (Mono.Unix.Catalog.GetString ("Port"), server.Port.ToString());
			store.AppendValues (Mono.Unix.Catalog.GetString ("User"), server.AuthDN);
			store.AppendValues (Mono.Unix.Catalog.GetString ("Base DN"), server.DirectoryRoot);
			store.AppendValues (Mono.Unix.Catalog.GetString ("Connected"), server.Connected.ToString());
			store.AppendValues (Mono.Unix.Catalog.GetString ("Bound"), server.Bound.ToString());
			store.AppendValues (Mono.Unix.Catalog.GetString ("TLS/SSL"), server.UseSSL.ToString());
			store.AppendValues (Mono.Unix.Catalog.GetString ("Protocol Version"), server.Protocol.ToString());

			if (server.ServerType == LdapServerType.ActiveDirectory) {
				store.AppendValues (Mono.Unix.Catalog.GetString ("DNS Host Name"), server.ADInfo.DnsHostName);
				store.AppendValues (Mono.Unix.Catalog.GetString ("Domain Controller Functionality"), server.ADInfo.DomainControllerFunctionality);
				store.AppendValues (Mono.Unix.Catalog.GetString ("Forest Functionality"),	server.ADInfo.ForestFunctionality);
				store.AppendValues (Mono.Unix.Catalog.GetString ("Domain Functionality"),	server.ADInfo.DomainFunctionality);
				store.AppendValues (Mono.Unix.Catalog.GetString ("Global Catalog Ready"),	server.ADInfo.IsGlobalCatalogReady.ToString());
				store.AppendValues (Mono.Unix.Catalog.GetString ("Synchronized"),	server.ADInfo.IsSynchronized.ToString());
			}

			this.ShowAll ();
		}
	}
}
