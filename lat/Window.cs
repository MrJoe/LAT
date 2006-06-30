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

#if ENABLE_AVAHI
using Avahi;
#endif

namespace lat 
{
	public class MainWindow
	{
		Glade.XML ui;
		
		[Glade.Widget] Gtk.Window mainWindow;
		[Glade.Widget] HPaned hpaned1;
		[Glade.Widget] Notebook infoNotebook;
		[Glade.Widget] Notebook viewNotebook;
		
		[Glade.Widget] Gtk.ScrolledWindow viewScrolledWindow;
		[Glade.Widget] Gtk.ScrolledWindow valuesScrolledWindow;		
		[Glade.Widget] ScrolledWindow browserScrolledWindow;
		[Glade.Widget] ScrolledWindow schemaScrolledWindow;
		[Glade.Widget] Gtk.ScrolledWindow resultsScrolledWindow;
		
		[Glade.Widget] Gtk.MenuItem newMenuItem;
		[Glade.Widget] Gtk.MenuToolButton newMenuToolButton;				
		[Glade.Widget] Gtk.ToolButton propertiesToolButton;
		[Glade.Widget] Gtk.ToolButton deleteToolButton;
		[Glade.Widget] Gtk.ToolButton refreshToolButton;
		[Glade.Widget] Gtk.ToolButton templateToolButton;

		[Glade.Widget] Gtk.CheckMenuItem showAllAttributes;
		[Glade.Widget] Gtk.RadioMenuItem viewsView;
		[Glade.Widget] Gtk.RadioMenuItem browserView;
		[Glade.Widget] Gtk.RadioMenuItem searchView;
		[Glade.Widget] Gtk.RadioMenuItem schemaView;

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

		[Glade.Widget] Gtk.Entry filterEntry;
		[Glade.Widget] Gtk.HBox hbox448;
		[Glade.Widget] Gtk.Button searchBaseButton;

		[Glade.Widget] Gtk.Image sslImage;
		[Glade.Widget] Gnome.AppBar appBar;
		
		Gnome.Program program;
		
		AccelGroup newAccelGroup;
		
		LdapTreeView ldapTreeView;
		ViewsTreeView viewsTreeView;
		ViewDataTreeView viewDataTreeView;
		ServerInfoView serverInfoView;
		AttributeEditorWidget attributeEditor;
		SchemaTreeView schemaTreeview;
		SearchResultsTreeView searchTreeView;

		ListStore objRequiredStore;
		ListStore objOptionalStore;
		
		ComboBox serverComboBox;
		
		public MainWindow (Gnome.Program mainProgram)
		{
			program = mainProgram;
			
			ui = new Glade.XML (null, "lat.glade", "mainWindow", null);
			ui.Autoconnect (this);

			// set window icon
			Global.latIcon = Gdk.Pixbuf.LoadFromResource ("lat.png");
			Gdk.Pixbuf dirIcon = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			mainWindow.Icon = dirIcon;
			
			Global.Profiles = new ProfileManager ();			
			Global.Connections = new ConnectionManager (mainWindow);

			// Restore window positions
			LoadPreference (Preferences.MAIN_WINDOW_WIDTH);
			LoadPreference (Preferences.MAIN_WINDOW_X);
			LoadPreference (Preferences.MAIN_WINDOW_MAXIMIZED);
			LoadPreference (Preferences.MAIN_WINDOW_HPANED);

			LoadPreference (Preferences.DISPLAY_VERBOSE_MESSAGES);

			// Watch for any changes
			Preferences.SettingChanged += OnPreferencesChanged;

			// Setup views
			viewsTreeView = new ViewsTreeView ();
			viewsTreeView.ViewSelected += new ViewSelectedHandler (OnViewSelected);
			viewScrolledWindow.AddWithViewport (viewsTreeView);
			viewScrolledWindow.Show ();

			// Setup browser			
			ldapTreeView = new LdapTreeView (mainWindow);
			ldapTreeView.dnSelected += new dnSelectedHandler (OnLdapDNSelected);
			browserScrolledWindow.AddWithViewport (ldapTreeView);
			browserScrolledWindow.Show ();

			LoadPreference (Preferences.BROWSER_SELECTION);

			// Setup schema browser
			schemaTreeview = new SchemaTreeView (mainWindow);
			schemaTreeview.schemaSelected += new schemaSelectedHandler (OnSchemaDNSelected);
			schemaScrolledWindow.AddWithViewport (schemaTreeview);
			schemaScrolledWindow.Show ();

			// Setup search
			searchTreeView = new SearchResultsTreeView ();
			searchTreeView.SearchResultSelected += new SearchResultSelectedHandler (OnSearchSelected);
			searchTreeView.Export += OnSearchExport;

			resultsScrolledWindow.AddWithViewport (searchTreeView);
			resultsScrolledWindow.Show ();			

			// setup schema
			objRequiredStore = new ListStore (typeof (string));
			objRequiredTreeview.Model = objRequiredStore;

			objOptionalStore = new ListStore (typeof (string));
			objOptionalTreeview.Model = objOptionalStore;

			objRequiredTreeview.AppendColumn ("Required Attributes", new CellRendererText (), "text", 0);
			objOptionalTreeview.AppendColumn ("Optional Attributes", new CellRendererText (), "text", 0);

			infoVpaned1.Position = 150;

#if ENABLE_AVAHI
			// Watch for any services available
			ServiceFinder finder = new ServiceFinder ();
			finder.Found += new ServiceEventHandler (OnServerFound);
			finder.Run ();
#endif

			ToggleButtons (false);
			ToggleInfoNotebook (false);
			
			templateToolButton.Hide ();			
						
			// setup menu
			newAccelGroup = new AccelGroup ();
			mainWindow.AddAccelGroup (newAccelGroup);
			
			// status bar			
			UpdateStatusBar ();

			Global.Network = NetworkDetect.Instance;
			Global.Network.StateChanged += OnNetworkStateChanged;
			
			viewNotebook.SwitchPage += new SwitchPageHandler (OnNotebookViewChanged);
		}

		void CleanupView ()
		{
			if (viewDataTreeView != null) {
				newMenuToolButton.Clicked -= new EventHandler (viewDataTreeView.OnNewEntryActivate);
				propertiesToolButton.Clicked -= new EventHandler (viewDataTreeView.OnEditActivate);
				deleteToolButton.Clicked -= new EventHandler (viewDataTreeView.OnDeleteActivate);
				refreshToolButton.Clicked -= new EventHandler (viewDataTreeView.OnRefreshActivate);
				
				viewDataTreeView.Destroy ();
				viewDataTreeView = null;
			}
		}

		void CreateServerCombo ()
		{
			serverComboBox = ComboBox.NewText ();
			string[] names = Global.Profiles.GetProfileNames ();
			
			foreach (string s in names)
				serverComboBox.AppendText (s);

			serverComboBox.Active = 0;
			serverComboBox.Show ();

			hbox448.PackEnd (serverComboBox, true, true, 5);
		}

		void GenerateNewMenu (ConnectionProfile cp)
		{		
			Gtk.Menu newMenu = new Gtk.Menu ();	
 				 	
			foreach (ViewPlugin vp in Global.Plugins.ServerViewPlugins)		
					if (cp.ActiveServerViews.Contains (vp.GetType().ToString())) {
						ImageMenuItem menuitem = new ImageMenuItem (vp.MenuLabel, newAccelGroup);
						menuitem.AddAccelerator ("activate", newAccelGroup, vp.MenuKey);
						
						Gtk.Label l = (Gtk.Label) menuitem.Child;
						l.Text = vp.MenuLabel;
						
						menuitem.Image = new Gtk.Image (vp.Icon);
						menuitem.Activated += OnNewMenuItemActivate;
						menuitem.Show ();						
						newMenu.Append (menuitem);
					} 

			newMenuItem.Submenu = newMenu;			
			newMenuToolButton.Menu = newMenu;
		}

		LdapServer GetActiveServer ()
		{
			string serverName = null;
			ConnectionProfile cp = null;
			LdapServer server = null;
			
			if (viewsView.Active) {
				serverName = viewsTreeView.GetActiveServerName ();
			} else if (browserView.Active) {
				serverName = ldapTreeView.GetActiveServerName ();
			} else if (schemaView.Active) {
				serverName = schemaTreeview.GetActiveServerName ();
			} 
		
			if (serverName == null)
				return server;

			cp = Global.Profiles [serverName];			
			server = Global.Connections [cp];	
			
			return server;
		}

		void LoadPreference (String key)
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

		void SetBrowserTooltips ()
		{
			Tooltips t = new Tooltips ();
			string tipMsg = null;

			tipMsg = "Create a new directory entry";
			newMenuToolButton.SetTooltip (t,  tipMsg, tipMsg);			

			tipMsg = "Delete an entry from the directory";
			deleteToolButton.SetTooltip (t,  tipMsg, tipMsg);			

			tipMsg = "Manage templates for creating new entries";
			templateToolButton.SetTooltip (t,  tipMsg, tipMsg);			
		}

		void SetInfoNotePage (int page)
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

		void SetupToolbar (ViewPlugin vp)
		{
			Tooltips t = new Tooltips ();
			string tipMsg = null;

			tipMsg = String.Format ("Create a new {0}", vp.Name.ToLower());
			newMenuToolButton.SetTooltip (t,  tipMsg, tipMsg);			
			newMenuToolButton.Clicked += new EventHandler (viewDataTreeView.OnNewEntryActivate);

			tipMsg = String.Format ("Edit the properties of a {0}", vp.Name.ToLower());
			propertiesToolButton.SetTooltip (t,  tipMsg, tipMsg);			
			propertiesToolButton.Clicked += new EventHandler (viewDataTreeView.OnEditActivate);
			
			tipMsg = String.Format ("Delete a {0} from the directory", vp.Name.ToLower());
			deleteToolButton.SetTooltip (t,  tipMsg, tipMsg);			
			deleteToolButton.Clicked += new EventHandler (viewDataTreeView.OnDeleteActivate);

			tipMsg = "Refreshes the data from the server";
			refreshToolButton.SetTooltip (t,  tipMsg, tipMsg);			
			refreshToolButton.Clicked += new EventHandler (viewDataTreeView.OnRefreshActivate);

			ToggleButtons (true);
		}

		void ShowMatchingRule (SchemaParser sp)
		{
			if (sp == null)
				return;
				
			matNameEntry.Text = sp.Names[0];
			matOIDEntry.Text = sp.ID;
			matSyntaxEntry.Text = sp.Syntax;
		}

		void ShowLdapSyntax (SchemaParser sp)
		{
			if (sp == null)
				return;
				
			synDescriptionEntry.Text = sp.Description;
			synOIDEntry.Text = sp.ID;
		}

		void ShowAttrTypeSchema (SchemaParser sp)
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

		void ShowEntrySchema (SchemaParser sp)
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

		void ToggleButtons (bool btnState)
		{
			newMenuToolButton = (MenuToolButton) ui.GetWidget ("newMenuToolButton");
			newMenuToolButton.Sensitive = btnState;
			
			propertiesToolButton = (ToolButton) ui.GetWidget ("propertiesToolButton");
			propertiesToolButton.Sensitive = btnState;
			
			deleteToolButton = (ToolButton) ui.GetWidget ("deleteToolButton");
			deleteToolButton.Sensitive = btnState;
			
			refreshToolButton = (ToolButton) ui.GetWidget ("refreshToolButton");
			refreshToolButton.Sensitive = btnState;

			propertiesToolButton.Show ();
			refreshToolButton.Show ();
		}

		void ToggleInfoNotebook (bool show)
		{
			if (show) {
				infoNotebook.Show ();
				valuesScrolledWindow.HideAll ();
			} else {
				infoNotebook.Hide ();
				valuesScrolledWindow.ShowAll ();
			}
		}

		void UpdateStatusBar ()
		{
			string msg = null;

			LdapServer server = GetActiveServer ();
			if (server == null)
				return;

			if (server.AuthDN == null)
				msg = String.Format("Bind DN: anonymous");
			else
				msg = String.Format("Bind DN: {0}", server.AuthDN);

			appBar.Pop ();
			appBar.Push (msg);

			sslImage.Pixbuf = Util.GetSSLIcon (server.UseSSL);
		}
		
		// Handlers

		public void OnAboutActivate (object o, EventArgs args) 
		{
			AboutDialog.Show ();
		}

		public void OnAppDelete (object o, DeleteEventArgs args) 
		{	
			Close ();
			args.RetVal = true;
		}

		public void OnNewActivate (object o, EventArgs args)
		{
			if (viewNotebook.CurrentPage == 0)
				if (viewDataTreeView != null)
					viewDataTreeView.OnNewEntryActivate (o, args);
			else if (viewNotebook.CurrentPage == 1)
				ldapTreeView.OnNewEntryActivate (o, args);
		}

		public void OnDeleteActivate (object o, EventArgs args)
		{
			if (viewNotebook.CurrentPage == 0)
				if (viewDataTreeView != null)
					viewDataTreeView.OnDeleteActivate (o, args);
			else if (viewNotebook.CurrentPage == 1)
				ldapTreeView.OnDeleteActivate (o, args);
		}

		public void OnPropertiesActivate (object o, EventArgs args)
		{
			if (viewNotebook.CurrentPage == 0)
				if (viewDataTreeView != null)
					viewDataTreeView.OnEditActivate (o, args);
		}

		public void OnRefreshActivate (object o, EventArgs args)
		{
			if (viewNotebook.CurrentPage == 0)
				if (viewDataTreeView != null)
					viewDataTreeView.OnRefreshActivate (o, args);
		}

		public void OnReloginActivate (object o, EventArgs args)
		{
			LdapServer server = GetActiveServer ();
			if (server == null)
				return;		
		
			string msg = Mono.Unix.Catalog.GetString (
				"Enter the new username and password\nyou wish to re-login with");

			LoginDialog ld = new LoginDialog (server, msg);
			ld.Run ();

			UpdateStatusBar ();
		}

		void OnSearchExport (object o, SearchResultExportEventArgs args)
		{
			TreeIter iter;			
			if (!serverComboBox.GetActiveIter (out iter))
				return;

			string profileName = (string) serverComboBox.Model.GetValue (iter, 0);		
		
			ConnectionProfile cp = Global.Profiles [profileName];
			LdapServer server = Global.Connections [cp];
			if (server == null)
				return;
		
			if (args.IsDND) {
				string data = null;
				Util.ExportData (server, args.DN, out data);
				args.Data = data;
			} else { 
				Util.ExportData (server, mainWindow, args.DN);
			}			
		}


		public void OnTemplatesClicked (object o, EventArgs args)
		{
			LdapServer server = GetActiveServer ();
			if (server == null)
				return;
		
			new TemplatesDialog (server);
		}

		public void OnConnectActivate (object o, EventArgs args)
		{
			new ConnectDialog ();
			
			viewsTreeView.Refresh ();
			ldapTreeView.Refresh ();
			schemaTreeview.Refresh ();
			
			if (serverComboBox != null) {
				serverComboBox.Destroy ();
				serverComboBox = null;
			}
			
			CreateServerCombo ();
		}

		public void OnDisconnectActivate (object o, EventArgs args) 
		{
			LdapServer server = GetActiveServer ();
			if (server == null)
				return;
				
			server.Disconnect ();
		}

		public void OnImportActivate (object o, EventArgs args)
		{
			LdapServer server = GetActiveServer ();
			if (server == null)
				return;		
		
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
			LdapServer server = GetActiveServer ();
			if (server == null)
				return;
		
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
			LdapServer server = GetActiveServer ();
			if (server == null)
				return;
				
			new SambaPopulateDialog (server);
		}

		public void OnPreferencesActivate (object sender, EventArgs args)
		{
			LdapServer server = GetActiveServer ();
			if (server == null)
				return;
				
			new PreferencesDialog (server);
			
			Global.Profiles.SaveProfiles ();
		}

		public void OnCutActivate (object o, EventArgs args)
		{
//			if (!(viewNotebook.Page == 1))
//				return;
//
//			_cutDN = ldapTreeView.getSelectedDN ();
//			_cutIter = ldapTreeView.getSelectedIter ();
//
//			Logger.Log.Debug ("cut - dn: {0}", _cutDN);
		}

		public void OnCopyActivate (object o, EventArgs args)
		{
//			if (!(viewNotebook.Page == 1))
//				return;
//
//			_cutDN = ldapTreeView.getSelectedDN ();
//
//			_isCopy = true;
//
//			Logger.Log.Debug ("copy - dn: {0}", _cutDN);
		}

		public void OnPasteActivate (object o, EventArgs args)
		{
//			if (!(viewNotebook.Page == 1))
//				return;
//
//			_pasteDN = ldapTreeView.getSelectedDN ();
//
//			if (_pasteDN.Equals (null))
//				return;
//
//			DN dn = new DN (_cutDN);
//			RDN r = (RDN) dn.RDNs[0];
//
//			try {
//
//				string msg = null;
//
//				if (_isCopy) {
//
//					server.Copy (_cutDN, r.toString(false), _pasteDN);
//
//					msg = String.Format (
//						Mono.Unix.Catalog.GetString ("Entry {0} copied to {1}"), 
//						_cutDN, _pasteDN);
//
//				} else {
//
//					server.Move (_cutDN, r.toString(false), _pasteDN);
//
//					msg = String.Format (
//						Mono.Unix.Catalog.GetString ("Entry {0} moved to {1}"), 
//						_cutDN, _pasteDN);
//
//				}
//
//				HIGMessageDialog dialog = new HIGMessageDialog (
//					mainWindow,
//					0,
//					Gtk.MessageType.Info,
//					Gtk.ButtonsType.Ok,
//					"Paste results",
//					msg);
//
//				dialog.Run ();
//				dialog.Destroy ();
//
//				if (!_isCopy)
//					ldapTreeView.RemoveRow (_cutIter);
//
//			} catch (Exception e) {
//
//				string msg = null;
//
//				if (_isCopy) {
//					string txt = Mono.Unix.Catalog.GetString ("Unable to copy entry ");
//					msg = txt + _cutDN;
//				} else {
//
//					string txt = Mono.Unix.Catalog.GetString ("Unable to move entry ");
//					msg = txt + _cutDN;
//				}
//
//				msg += "\nError: " + e.Message;
//
//				HIGMessageDialog dialog = new HIGMessageDialog (
//					mainWindow,
//					0,
//					Gtk.MessageType.Error,
//					Gtk.ButtonsType.Ok,
//					"Paste error",
//					msg);
//
//				dialog.Run ();
//				dialog.Destroy ();
//			}
//
//			if (_isCopy)
//				_isCopy = false;
		}

		public void OnMassEditActivate (object o, EventArgs args)
		{
			LdapServer server = GetActiveServer ();
			if (server == null)
				return;
				
			new MassEditDialog (server);
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

			program.Quit ();
		}

		public void OnSearchSelected (object o, SearchResultSelectedEventArgs args)
		{
			TreeIter iter;
				
			if (!serverComboBox.GetActiveIter (out iter))
				return;

			string profileName = (string) serverComboBox.Model.GetValue (iter, 0);
			
			ConnectionProfile cp = Global.Profiles [profileName];			
			LdapServer server = Global.Connections [cp];
				
			LdapEntry entry = server.GetEntry (args.DN);

			if (entry != null) 
				attributeEditor.Show (server, entry, showAllAttributes.Active);
		}
		
		public void OnSearchBuilderClicked (object o, EventArgs args)
		{
			SearchBuilderDialog sbd = new SearchBuilderDialog ();
			filterEntry.Text = sbd.UserFilter;
		}

		public void OnSearchClicked (object o, EventArgs args)
		{
			TreeIter iter;
				
			if (!serverComboBox.GetActiveIter (out iter))
				return;

			string profileName = (string) serverComboBox.Model.GetValue (iter, 0);
			
			ConnectionProfile cp = Global.Profiles [profileName];			
			LdapServer server = Global.Connections [cp];
			
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
				searchTreeView.UpdateSearchResults (searchResults);

				string msg = String.Format ("Found {0} matching entries", searchResults.Length);
				appBar.Pop ();
				appBar.Push (msg);
			}
		}

		public void OnSearchBaseClicked (object o, EventArgs args)
		{
			TreeIter iter;
				
			if (!serverComboBox.GetActiveIter (out iter))
				return;

			string profileName = (string) serverComboBox.Model.GetValue (iter, 0);
			
			ConnectionProfile cp = Global.Profiles [profileName];			
			LdapServer server = Global.Connections [cp];
		
			SelectContainerDialog scd = new SelectContainerDialog (server, mainWindow);

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
			LdapServer server = null;

			if (viewNotebook.CurrentPage == 1) {

				ldapTreeView.GetSelectedDN (out dn, out server);

				if (dn == null || server == null)
					return;

				LdapEntry entry = server.GetEntry (dn);
				attributeEditor.Show (server, entry, showAllAttributes.Active);
			}
		}

		public void OnViewChanged (object o, EventArgs args)
		{		
			if (viewsView.Active) {
				viewNotebook.Page = 0;
				ToggleInfoNotebook (false);
			} else if (browserView.Active) {
				viewNotebook.Page = 1;
				ToggleInfoNotebook (false);
			} else if (searchView.Active) {
			
				if (serverComboBox != null) {
					serverComboBox.Destroy ();
					serverComboBox = null;
				}
				
				CreateServerCombo ();			
				viewNotebook.Page = 2;
				ToggleInfoNotebook (false);
			} else if (schemaView.Active) {
				viewNotebook.Page = 3;
				ToggleInfoNotebook (true);
				SetInfoNotePage (-1);
			}
		}

		public void OnQuitActivate (object o, EventArgs args) 
		{
			Close ();
		}

		public void OnHelpContentsActivate (object o, EventArgs args)
		{
			try {

				Gnome.Help.DisplayDesktopOnScreen (program, 
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

		void OnLdapDNSelected (object o, dnSelectedEventArgs args)
		{
			ConnectionProfile cp = Global.Profiles [args.Server];
			LdapServer server = Global.Connections [cp];
			
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

		void OnNetworkStateChanged (object o, NetworkStateChangedArgs args)
		{
			if (args.Connected) {
			
				mainWindow.Sensitive = true;
			
			} else {

				HIGMessageDialog dialog = new HIGMessageDialog (
					mainWindow,
					0,
					Gtk.MessageType.Info,
					Gtk.ButtonsType.Ok,
					"Network disconnected",
					"The network is down. LAT will disable itself until it comes back up");

				dialog.Run ();
				dialog.Destroy ();
				
				mainWindow.Sensitive = false;				
			}
		}

		void OnNotebookViewChanged (object o, SwitchPageArgs args)
		{
			if (args.PageNum == 0) {

				ldapTreeView.removeToolbarHandlers ();
				ToggleButtons (false);
				ToggleInfoNotebook (false);

				if (serverInfoView != null) {
					serverInfoView.Destroy ();
					serverInfoView = null;
				}

				if (attributeEditor != null) {
					attributeEditor.Destroy ();
					attributeEditor = null;
				}

				templateToolButton.Hide ();

			} else if (args.PageNum == 1) {

				CleanupView ();

				ToggleButtons (true);
				ToggleInfoNotebook (false);

				newMenuItem.Submenu = null;
				newMenuToolButton.Menu = null;

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

				ldapTreeView.setToolbarHandlers (newMenuToolButton, deleteToolButton);
				SetBrowserTooltips ();

			} else if (args.PageNum == 2) {

				CleanupView ();

				ldapTreeView.removeToolbarHandlers ();

				newMenuItem.Submenu = null;
				newMenuToolButton.Menu = null;

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
				
				ToggleButtons (false);
				ToggleInfoNotebook (false);

				templateToolButton.Hide ();

				if (serverComboBox != null) {
					serverComboBox.Destroy ();
					serverComboBox = null;
				}

				CreateServerCombo ();

			} else if (args.PageNum == 3) {

				CleanupView ();

				ToggleButtons (false);

				newMenuItem.Submenu = null;
				newMenuToolButton.Menu = null;

				ToggleInfoNotebook (true);

				SetInfoNotePage (-1);

				templateToolButton.Hide ();
			}
		}

		void OnNewMenuItemActivate (object o, EventArgs args)
		{
			ImageMenuItem mi = (ImageMenuItem) o;
			Gtk.Label l = (Gtk.Label) mi.Child;

			viewDataTreeView.ShowNewItemDialog (l.Text);
		}

		public void OnPreferencesChanged (object sender, GConf.NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void OnSchemaDNSelected (object o, schemaSelectedEventArgs args)
		{
			if (args.Name == "Object Classes" || args.Name == "Attribute Types" || args.Name == "Matching Rules" || args.Name == "LDAP Syntaxes")
				return;

			ConnectionProfile cp = Global.Profiles [args.Server];
			LdapServer server = Global.Connections [cp];

			if (args.Parent == "Object Classes") {
				
				SetInfoNotePage (0);
				SchemaParser sp = server.GetObjectClassSchema (args.Name);
				ShowEntrySchema (sp);

			} else if (args.Parent == "Attribute Types") {

				SetInfoNotePage (1);
				SchemaParser sp = server.GetAttributeTypeSchema (args.Name);
				ShowAttrTypeSchema (sp);
				
			} else if (args.Parent == "Matching Rules") {

				SetInfoNotePage (2);
				SchemaParser sp = server.GetMatchingRule (args.Name);
				ShowMatchingRule (sp);				
			
			} else if (args.Parent == "LDAP Syntaxes") {
			
				SetInfoNotePage (3);
				SchemaParser sp = server.GetLdapSyntax (args.Name);
				ShowLdapSyntax (sp);
			}
		}

#if ENABLE_AVAHI
		void OnServerFound (object o, ServiceEventArgs args)
		{
			Global.Profiles [args.Profile.Name] = args.Profile;
			viewsTreeView.AddServer (args.Profile);
			ldapTreeView.AddServer (args.Profile);
			schemaTreeview.AddServer (args.Profile);
		}
#endif
		
		void OnViewSelected (object o, ViewSelectedEventArgs args)
		{
			ViewPlugin vp = Global.Plugins.FindServerView (args.Name);
			LdapServer server = null;
			ConnectionProfile cp = null;
			
			if (vp == null) {
				if (viewDataTreeView != null) {					
					viewDataTreeView.Destroy ();
					viewDataTreeView = null;
				}

				if (serverInfoView != null) {
					serverInfoView.Destroy ();
					serverInfoView = null;
				}
	
				cp = Global.Profiles [args.Name];
				server = Global.Connections [cp];
				
				serverInfoView = new ServerInfoView (server);
				valuesScrolledWindow.AddWithViewport (serverInfoView);
				valuesScrolledWindow.ShowAll ();
				
				return;
			}

			CleanupView ();

			if (viewDataTreeView == null) {
				if (serverInfoView != null) {
					serverInfoView.Destroy ();
					serverInfoView = null;
				}
				
				cp = Global.Profiles [args.ConnectionName];			
				server = Global.Connections [cp];
	
				viewDataTreeView = new ViewDataTreeView (server, mainWindow);
				valuesScrolledWindow.AddWithViewport (viewDataTreeView);
				valuesScrolledWindow.ShowAll ();			
			}

			viewDataTreeView.ConfigureView (vp);
			viewDataTreeView.Populate ();
			SetupToolbar (vp);
			
			GenerateNewMenu (cp);
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
