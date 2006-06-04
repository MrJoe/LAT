// 
// lat - ViewDataTreeView.cs
// Author: Loren Bandiera
// Copyright 2006 MMG Security, Inc.
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
using GLib;
using System;
using System.Collections;
using Novell.Directory.Ldap;

namespace lat {

	public class ViewDataTreeView : Gtk.TreeView
	{
		LdapServer	server;
		Gtk.Window	parentWindow;
		Menu 		popup;
		ListStore	dataStore;
		
		ViewPlugin viewPlugin;	
		int dnColumn;
		
		public ViewDataTreeView (LdapServer ldapServer, Gtk.Window parent) : base ()
		{
			server = ldapServer;
			parentWindow = parent;
			
			this.ButtonPressEvent += new ButtonPressEventHandler (OnRightClick);
			this.RowActivated += new RowActivatedHandler (OnRowActivated);
			this.ShowAll ();
		}
		
		public void ConfigureView (ViewPlugin vp)
		{
			viewPlugin = vp;			
			SetViewColumns ();			
		}

		public void Populate ()
		{
			if (viewPlugin.SearchBase == null)
				viewPlugin.SearchBase = server.DirectoryRoot;
				
			LdapEntry[] data = server.Search (viewPlugin.SearchBase, viewPlugin.Filter);

			Logger.Log.Debug ("InsertData()\n\tbase: [{0}]\n\tfilter: [{1}]\n\tnumResults: [{2}]",
					viewPlugin.SearchBase, viewPlugin.Filter, data.Length);

			DoInsert (data, viewPlugin.ColumnAttributes);
		}

		void DoInsert (LdapEntry[] objs, string[] attributes)
		{
			try {

				if (this.dataStore != null)
					this.dataStore.Clear ();

				foreach (LdapEntry le in objs) {
				
					string[] values = server.GetAttributeValuesFromEntry (le, attributes);
					string[] newvalues = new string [values.Length + 1];
													
					values.CopyTo (newvalues, 0);
					newvalues [values.Length] = le.DN;
					
					this.dataStore.AppendValues (newvalues);
				}

			} catch {

				string	msg = Mono.Unix.Catalog.GetString (
					"Unable to read data from server");

				HIGMessageDialog dialog = new HIGMessageDialog (
					parentWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Network error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		void DoPopUp()
		{
			popup = new Menu();

			ImageMenuItem newItem = new ImageMenuItem ("New");
			Gtk.Image newImage = new Gtk.Image (Stock.New, IconSize.Menu);
			newItem.Image = newImage;
			newItem.Activated += new EventHandler (OnNewEntryActivate);
			newItem.Show ();

			popup.Append (newItem);

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("document-save.png");
			ImageMenuItem exportItem = new ImageMenuItem ("Export");
			exportItem.Image = new Gtk.Image (pb);
			exportItem.Activated += new EventHandler (OnExportActivate);
			exportItem.Show ();

			popup.Append (exportItem);

			ImageMenuItem deleteItem = new ImageMenuItem ("Delete");
			Gtk.Image deleteImage = new Gtk.Image (Stock.Delete, IconSize.Menu);
			deleteItem.Image = deleteImage;
			deleteItem.Activated += new EventHandler (OnDeleteActivate);
			deleteItem.Show ();

			popup.Append (deleteItem);

			ImageMenuItem propItem = new ImageMenuItem ("Properties");
			Gtk.Image propImage = new Gtk.Image (Stock.Properties, IconSize.Menu);
			propItem.Image = propImage;
			propItem.Activated += new EventHandler (OnEditActivate);
			propItem.Show ();

			popup.Append (propItem);

// 			FIXME: make work for the plugins
//			viewPlugin.OnPopupShow (popup);

			popup.Popup(null, null, null, 3,
					Gtk.Global.CurrentEventTime);
		}

		public string GetDN (TreePath path)
		{
			TreeIter iter;
			
			if (this.dataStore.GetIter (out iter, path)) {
				string dn = (string) this.dataStore.GetValue (iter, dnColumn);
				return dn;
			} 

			return null;
		}

		public void OnNewEntryActivate (object o, EventArgs args) 
		{
			viewPlugin.OnAddEntry (server);
			Populate ();
		}

		public void OnEditActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = this.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp) {
				LdapEntry le = server.GetEntry (GetDN(path));
				viewPlugin.OnEditEntry (server, le);			
				Populate ();
			}
		}

		void DeleteEntry (TreePath[] path)
		{
			try {

				if (!(path.Length > 1)) {

					LdapEntry le = server.GetEntry (GetDN(path[0]));
					Util.DeleteEntry (server, parentWindow, le.DN);
					return;
				}

				ArrayList dnList = new ArrayList ();

				foreach (TreePath tp in path) {
					LdapEntry le = server.GetEntry (GetDN(tp));
					dnList.Add (le.DN);
				}

				string[] dns = (string[]) dnList.ToArray (typeof(string));

				Util.DeleteEntry (server, parentWindow, dns);

			} catch {}
		}

		public void OnDeleteActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = this.Selection.GetSelectedRows (out model);

			DeleteEntry (tp);
			
			Populate ();
		}

		void OnExportActivate (object o, EventArgs args)
		{
			TreeModel model;
			TreePath[] tp = this.Selection.GetSelectedRows (out model);

			try {
				LdapEntry le = server.GetEntry (GetDN(tp[0]));
				Util.ExportData (server, parentWindow, le.DN);
			}
			catch {}
		}

		public void OnRefreshActivate (object o, EventArgs args)
		{
			Populate ();
		}

		[ConnectBefore]
		void OnRightClick (object o, ButtonPressEventArgs args)
		{
			// FIXME: Find a way to not deselect on multiple selection
			if (args.Event.Button == 3)
				DoPopUp ();
		}

		void OnRowActivated (object o, RowActivatedArgs args)
		{	
			TreePath path = args.Path;
			TreeIter iter;
			
			if (this.dataStore.GetIter (out iter, path)) {
				
				string dn = (string) this.dataStore.GetValue (iter, dnColumn);				
				viewPlugin.OnEditEntry (server, server.GetEntry (dn));
			} 		
		}
		
		void SetViewColumns ()
		{
			if (dataStore != null)
				dataStore.Clear ();

			foreach (TreeViewColumn col in this.Columns)
				this.RemoveColumn (col);		
		
			int colLength = viewPlugin.ColumnNames.Length + 1;
			System.Type[] types = new System.Type [colLength];

			for (int i = 0; i < colLength; i++)
				types[i] = typeof (string);

			dataStore = new ListStore (types);
			this.Model = dataStore;
			
			CellRenderer crt = new CellRendererText ();

			for (int i = 0; i < viewPlugin.ColumnNames.Length; i++) {

				TreeViewColumn col = new TreeViewColumn ();
				col.Title = viewPlugin.ColumnNames[i];
				col.PackStart (crt, true);
				col.AddAttribute (crt, "text", i);
				col.SortColumnId = i;

				this.AppendColumn (col);
			}

			dnColumn = viewPlugin.ColumnNames.Length;			
			TreeViewColumn c = new TreeViewColumn ();
			c.Title = "DN";			
			c.PackStart (crt, true);
			c.AddAttribute (crt, "text", dnColumn);
			c.Visible = false;
			this.AppendColumn (c);			

			this.ShowAll ();			
		}
	}
}