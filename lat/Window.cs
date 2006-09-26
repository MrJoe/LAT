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
using System.Text;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Rfc2251;
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

		string editDN;
		string editName;
		bool editIsCopy;
		TreeIter editIter;
		
#if ENABLE_AVAHI
		ServiceFinder finder;
#endif
		
		public MainWindow (Gnome.Program mainProgram)
		{
			program = mainProgram;
			
			ui = new Glade.XML (null, "lat.glade", "mainWindow", null);
			ui.Autoconnect (this);

			// set window icon
			Global.latIcon = Gdk.Pixbuf.LoadFromResource ("lat.png");
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

			ToggleButtons (false);
			ToggleInfoNotebook (false);
			
			templateToolButton.Hide ();			
						
			// setup menu
			newAccelGroup = new AccelGroup ();
			mainWindow.AddAccelGroup (newAccelGroup);
			
			// status bar			
			UpdateStatusBar ();

#if ENABLE_NETWORKMANAGER
			Global.Network = NetworkDetect.Instance;
			Global.Network.StateChanged += OnNetworkStateChanged;
#endif

#if ENABLE_AVAHI
			// FIXME: causes delay/crashes on exit for some reason
			finder = new ServiceFinder ();
			finder.Found += new FoundServiceEventHandler (OnServerFound);
			finder.Removed += new RemovedServiceEventHandler (OnServerRemoved);
			finder.Start ();
#endif
			
			viewNotebook.SwitchPage += new SwitchPageHandler (OnNotebookViewChanged);
			
			if (Global.Connections.ConnectionNames.Length == 0) {
				new ConnectDialog ();		
				
				viewsTreeView.Refresh ();
				ldapTreeView.Refresh ();
				schemaTreeview.Refresh ();
			}
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
			serverComboBox = Util.CreateServerCombo (); 
			hbox448.PackEnd (serverComboBox, true, true, 5);
		}

		void GenerateNewMenu (Connection conn)
		{		
			Gtk.Menu newMenu = new Gtk.Menu ();	
 				 	
			foreach (ViewPlugin vp in Global.Plugins.ServerViewPlugins)		
					if (conn.ServerViews.Contains (vp.GetType().ToString())) {
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

		Connection GetActiveConnection ()
		{
			string serverName = null;
			Connection conn = null;
			
			if (viewsView.Active)
				serverName = viewsTreeView.GetActiveServerName ();					
			else if (browserView.Active)
				serverName = ldapTreeView.GetActiveServerName ();			
			else if (schemaView.Active)
				serverName = schemaTreeview.GetActiveServerName ();	
					
			if (serverName == null)
				return conn;

			conn = Global.Connections [serverName];			
			return conn;
		}

		void LoadPreference (String key)
		{
			object val = Preferences.Get (key);

			if (val == null) {

				if (key == Preferences.MAIN_WINDOW_HPANED)
					hpaned1.Position = 250;

				return;
			}
			
			Log.Debug ("Setting {0} to {1}", key, val);

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

		void TogglePages (bool pageOneState, bool pageTwoState, bool pageThreeState, bool pageFourState)
		{
			Gtk.Widget w;
			
			w = infoNotebook.GetNthPage (3);
			if (pageFourState)
				w.ShowAll ();
			else
				w.HideAll ();
			
			w = infoNotebook.GetNthPage (2);
			if (pageThreeState)
				w.ShowAll ();
			else
				w.HideAll ();
			
			w = infoNotebook.GetNthPage (1);
			if (pageTwoState)
				w.ShowAll ();
			else
				w.HideAll ();

			w = infoNotebook.GetNthPage (0);
			if (pageOneState)
				w.ShowAll ();
			else
				w.HideAll ();

			infoNotebook.Show ();			
		}

		void SetInfoNotePage (int page)
		{	
			switch (page) {
			
			case 0:
				TogglePages (false, false, false, true);
				break;
					
			case 1:
				TogglePages (false, false, true, false);
				break;

			case 2:
				TogglePages (false, true, false, false);
				break;
				
			case 3:
				TogglePages (true, false, false, false);
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

		void ClearSchemaValues ()
		{
			objNameTextview.Buffer.Text = "";
			objDescriptionEntry.Text = "";
			objIDEntry.Text = "";
			objObsoleteCheckbutton.Active = false;
			attrNameTextview.Buffer.Text = "";
			attrDescriptionEntry.Text = "";
			attrIDEntry.Text = "";
			attrSuperiorTextview.Buffer.Text = "";
			attrObsoleteCheckbutton.Active = false;
			attrSingleCheckbutton.Active = false;
			attrCollectiveCheckbutton.Active = false;
			attrUserModCheckbutton.Active = false;
			attrEqualityEntry.Text = "";
			attrOrderingEntry.Text = "";
			attrSubstringEntry.Text = "";
			attrSyntaxEntry.Text = "";
			
			matNameEntry.Text = "";
			matOIDEntry.Text = "";
			matSyntaxEntry.Text = "";
			
			synDescriptionEntry.Text = "";
			synOIDEntry.Text = "";
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

			Connection conn = GetActiveConnection ();
			if (conn == null)
				return;

			try {
			
				if (conn.AuthDN == null)
					msg = String.Format("Bind DN: anonymous");
				else
					msg = String.Format("Bind DN: {0}", conn.AuthDN);

				appBar.Pop ();
				appBar.Push (msg);

				sslImage.Pixbuf = Util.GetSSLIcon (conn.UseSSL);
				
			} catch (Exception e) {
				Log.Debug (e);
			}
		}

		public void WriteStatusMessage (string msg)
		{
			appBar.Pop ();
			appBar.Push (msg);
		}
		
		// Handlers

		public void OnAboutActivate (object o, EventArgs args) 
		{
			new AboutDialog ();			
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
			Connection conn = GetActiveConnection ();
			if (conn == null)
				return;		
		
			string msg = Mono.Unix.Catalog.GetString (
				"Enter the new username and password\nyou wish to re-login with");

			LoginDialog ld = new LoginDialog (conn, msg);
			ld.Run ();

			UpdateStatusBar ();
		}

		void OnSearchExport (object o, SearchResultExportEventArgs args)
		{
			TreeIter iter;			
			if (!serverComboBox.GetActiveIter (out iter))
				return;

			string profileName = (string) serverComboBox.Model.GetValue (iter, 0);		
		
			Connection conn = Global.Connections [profileName];
			if (conn == null)
				return;
		
			if (args.IsDND) {
				string data = null;
				Util.ExportData (conn, args.DN, out data);
				args.Data = data;
			} else { 
				Util.ExportData (conn, mainWindow, args.DN);
			}			
		}


		public void OnTemplatesClicked (object o, EventArgs args)
		{
			Connection conn = GetActiveConnection ();
			if (conn == null)
				return;
		
			new TemplatesDialog (conn);
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
			Connection conn = GetActiveConnection ();
			if (conn == null)
				return;
				
			conn.Disconnect ();
		}

		public void OnImportActivate (object o, EventArgs args)
		{
			Connection conn = GetActiveConnection ();
			if (conn == null)
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

				Util.ImportData (conn, mainWindow, ub.Uri);
			} 
		
			fcd.Destroy();
		}

		public void OnExportActivate (object o, EventArgs args)
		{
			Connection conn = GetActiveConnection ();
			if (conn == null)
				return;
		
			SelectContainerDialog scd = new SelectContainerDialog (conn, mainWindow);
			scd.Title = Mono.Unix.Catalog.GetString ("Export entry");
			scd.Message = Mono.Unix.Catalog.GetString ("Select the container you wish to export.");
			scd.Run ();

			if (scd.DN.Equals (""))
				return;

			Util.ExportData (conn, mainWindow, scd.DN);
		}

		public void OnPopulateActivate (object o, EventArgs args)
		{
			new SambaPopulateDialog ();
		}

		public void OnPreferencesActivate (object sender, EventArgs args)
		{
			new PreferencesDialog (program);
			
			Global.Connections.Save ();
		}

		public void OnCutActivate (object o, EventArgs args)
		{
			if (!(viewNotebook.Page == 1))
				return;

			Connection conn = GetActiveConnection ();
			if (conn == null)
				return;

			editDN = ldapTreeView.GetSelectedDN ();
			editIter = ldapTreeView.GetSelectedIter ();
			editName = conn.Settings.Name;

			Log.Debug ("Edit->Cut dn: {0}", editDN);
		}

		public void OnCopyActivate (object o, EventArgs args)
		{
			if (!(viewNotebook.Page == 1))
				return;

			Connection conn = GetActiveConnection ();
			if (conn == null)
				return;

			editDN = ldapTreeView.GetSelectedDN ();
			editName = conn.Settings.Name;
			editIsCopy = true;

			Log.Debug ("Edit->Copy dn: {0}", editDN);
		}

		public void OnPasteActivate (object o, EventArgs args)
		{
			if (!(viewNotebook.Page == 1))
				return;

			Connection conn = GetActiveConnection ();
			if (conn == null)
				return;

			if (conn.Settings.Name != editName) {
			
				string msg = Mono.Unix.Catalog.GetString ("Cannot copy/cut enteries between servers");				
				HIGMessageDialog dialog = new HIGMessageDialog (
					mainWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Paste error",
					msg.ToString ());

				dialog.Run ();
				dialog.Destroy ();
				
				return;
			}

			string pasteDN = ldapTreeView.GetSelectedDN ();
			if (pasteDN == null)
				return;

			DN dn = new DN (editDN);
			RDN r = (RDN) dn.RDNs [0];

			try {

				if (editIsCopy) {
					conn.Data.Copy (editDN, r.toString (false), pasteDN);
					WriteStatusMessage ("Entry copied");
				} else {
					conn.Data.Move (editDN, r.toString (false), pasteDN);
					ldapTreeView.RemoveRow (editIter);
					WriteStatusMessage ("Entry moved.");
				}
				
			} catch (Exception e) {

				Log.Debug (e);				
				StringBuilder msg = new StringBuilder ();

				if (editIsCopy)
					msg.AppendFormat ("{0} {1}", Mono.Unix.Catalog.GetString ("Unable to copy entry "), editDN);
				else
					msg.AppendFormat ("{0} {1}", Mono.Unix.Catalog.GetString ("Unable to move entry "), editDN);
				
				msg.AppendFormat ("\nError: {0}", e.Message);

				HIGMessageDialog dialog = new HIGMessageDialog (
					mainWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Paste error",
					msg.ToString ());

				dialog.Run ();
				dialog.Destroy ();
			}

			if (editIsCopy)
				editIsCopy = false;
				
			editDN = null;
			editName = null;
		}

		public void OnMassEditActivate (object o, EventArgs args)
		{
			new MassEditDialog ();
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

#if ENABLE_AVAHI
			finder.Stop ();
#endif

			program.Quit ();
		}

		public void OnSearchSelected (object o, SearchResultSelectedEventArgs args)
		{
			TreeIter iter;
				
			if (!serverComboBox.GetActiveIter (out iter))
				return;

			string profileName = (string) serverComboBox.Model.GetValue (iter, 0);			
			Connection conn = Global.Connections [profileName];			
			LdapEntry entry = conn.Data.GetEntry (args.DN);

			if (entry != null) 
				attributeEditor.Show (conn, entry, showAllAttributes.Active);
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
			Connection conn = Global.Connections [profileName];
			
			RfcFilter filter = new RfcFilter (filterEntry.Text);			
			LdapEntry[] searchResults = conn.Data.Search (searchBaseButton.Label, filter.filterToString());

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
			Connection conn = Global.Connections [profileName];
		
			try {
			
				SelectContainerDialog scd = new SelectContainerDialog (conn, mainWindow);
				scd.Message = String.Format (Mono.Unix.Catalog.GetString ("Where in the directory would\nyou like to start the search?"));
				scd.Title = Mono.Unix.Catalog.GetString ("Select search base");
				scd.Run ();

				if (!scd.DN.Equals ("") && !scd.DN.Equals (conn.Settings.Host))
					searchBaseButton.Label = scd.DN;
					
			} catch {}
		}

		public void OnShowAllAttributes (object o, EventArgs args)
		{
			string dn = null;
			string name = null;
			Connection conn = null;

			if (viewNotebook.CurrentPage == 1) {

				ldapTreeView.GetSelectedDN (out dn, out name);

				if (dn == null || name == null)
					return;

				conn = Global.Connections [name];
				LdapEntry entry = conn.Data.GetEntry (dn);
				attributeEditor.Show (conn, entry, showAllAttributes.Active);
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
			Connection conn = Global.Connections [args.Server];
			
			if (args.IsHost) {
				if (attributeEditor != null)
					attributeEditor.Destroy ();
			
				serverInfoView = new ServerInfoView (conn);
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
			
			LdapEntry entry = conn.Data.GetEntry (args.DN);			
			if (entry != null)
				attributeEditor.Show (conn, entry, showAllAttributes.Active);
		}

#if ENABLE_NETWORKMANAGER
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
#endif

		void OnNotebookViewChanged (object o, SwitchPageArgs args)
		{
			WriteStatusMessage ("");
		
			if (args.PageNum == 0) {

				ldapTreeView.removeToolbarHandlers ();
				ToggleButtons (false);
				ToggleInfoNotebook (false);
				viewsView.Active = true;

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
				browserView.Active = true;

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
				searchView.Active = true;

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
				schemaView.Active = true;

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

			Connection conn = Global.Connections [args.Server];

			ClearSchemaValues ();

			if (args.Parent == "Object Classes") {
				
				SetInfoNotePage (3);
				SchemaParser sp = conn.Data.GetObjectClassSchema (args.Name);
				ShowEntrySchema (sp);

			} else if (args.Parent == "Attribute Types") {

				SetInfoNotePage (2);
				SchemaParser sp = conn.Data.GetAttributeTypeSchema (args.Name);
				ShowAttrTypeSchema (sp);
				
			} else if (args.Parent == "Matching Rules") {

				SetInfoNotePage (1);
				SchemaParser sp = conn.Data.GetMatchingRule (args.Name);
				ShowMatchingRule (sp);				
			
			} else if (args.Parent == "LDAP Syntaxes") {
			
				SetInfoNotePage (0);
				SchemaParser sp = conn.Data.GetLdapSyntax (args.Name);
				ShowLdapSyntax (sp);
			}
		}

#if ENABLE_AVAHI
		void OnServerFound (object o, FoundServiceEventArgs args)
		{
			Global.Connections [args.FoundConnection.Settings.Name] = args.FoundConnection;
			viewsTreeView.AddConnection (args.FoundConnection);
			ldapTreeView.AddConnection (args.FoundConnection.Settings.Name);
			schemaTreeview.AddConnection (args.FoundConnection.Settings.Name);			
		}
		
		void OnServerRemoved (object o, RemovedServiceEventArgs args)
		{
			Global.Connections.Delete (args.ConnectionName);
			viewsTreeView.Refresh ();
			ldapTreeView.Refresh ();
			schemaTreeview.Refresh ();			
		}
#endif
			
		void OnViewSelected (object o, ViewSelectedEventArgs args)
		{
			ViewPlugin vp = Global.Plugins.GetViewPlugin (args.Name, args.ConnectionName);  
			Connection conn = null;
			
			if (vp == null) {
				if (viewDataTreeView != null) {					
					viewDataTreeView.Destroy ();
					viewDataTreeView = null;
				}

				if (serverInfoView != null) {
					serverInfoView.Destroy ();
					serverInfoView = null;
				}

				conn = Global.Connections [args.ConnectionName];
				
				serverInfoView = new ServerInfoView (conn);
				valuesScrolledWindow.AddWithViewport (serverInfoView);
				valuesScrolledWindow.ShowAll ();

				UpdateStatusBar ();
				
				return;
			}

			CleanupView ();

			if (viewDataTreeView == null) {
				if (serverInfoView != null) {
					serverInfoView.Destroy ();
					serverInfoView = null;
				}
				
				conn = Global.Connections [args.ConnectionName];
	
				viewDataTreeView = new ViewDataTreeView (conn, mainWindow);
				valuesScrolledWindow.AddWithViewport (viewDataTreeView);
				valuesScrolledWindow.ShowAll ();
			}

			viewDataTreeView.ConfigureView (vp);
			viewDataTreeView.Populate ();
			SetupToolbar (vp);
			
			GenerateNewMenu (conn);

			UpdateStatusBar ();
		}
	}

	public class ServerInfoView : Gtk.TreeView
	{
		public ServerInfoView (Connection conn) : base ()
		{	
			ListStore store = new ListStore (typeof (string), typeof (string));
			this.Model = store;

			this.AppendColumn ("Name", new CellRendererText (), "text", 0); 
			this.AppendColumn ("Value", new CellRendererText (), "text", 1); 

			store.AppendValues (Mono.Unix.Catalog.GetString ("Host"), conn.Settings.Host);
			store.AppendValues (Mono.Unix.Catalog.GetString ("Port"), conn.Settings.Port.ToString());
			store.AppendValues (Mono.Unix.Catalog.GetString ("User"), conn.AuthDN);
			store.AppendValues (Mono.Unix.Catalog.GetString ("Base DN"), conn.DirectoryRoot);
			store.AppendValues (Mono.Unix.Catalog.GetString ("Connected"), conn.IsConnected.ToString());
			store.AppendValues (Mono.Unix.Catalog.GetString ("Bound"), conn.IsBound.ToString());
			store.AppendValues (Mono.Unix.Catalog.GetString ("TLS/SSL"), conn.UseSSL.ToString());
			store.AppendValues (Mono.Unix.Catalog.GetString ("Protocol Version"), conn.Protocol.ToString());

			if (conn.Settings.ServerType == LdapServerType.ActiveDirectory) {
				store.AppendValues (Mono.Unix.Catalog.GetString ("DNS Host Name"), conn.ActiveDirectory.DnsHostName);
				store.AppendValues (Mono.Unix.Catalog.GetString ("Domain Controller Functionality"), conn.ActiveDirectory.DomainControllerFunctionality);
				store.AppendValues (Mono.Unix.Catalog.GetString ("Forest Functionality"),	conn.ActiveDirectory.ForestFunctionality);
				store.AppendValues (Mono.Unix.Catalog.GetString ("Domain Functionality"),	conn.ActiveDirectory.DomainFunctionality);
				store.AppendValues (Mono.Unix.Catalog.GetString ("Global Catalog Ready"),	conn.ActiveDirectory.IsGlobalCatalogReady.ToString());
				store.AppendValues (Mono.Unix.Catalog.GetString ("Synchronized"),	conn.ActiveDirectory.IsSynchronized.ToString());
			}

			this.ShowAll ();
		}
	}
}
