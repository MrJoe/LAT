// 
// lat - ServerView.cs
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
using GLib;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using Novell.Directory.Ldap;

namespace lat
{
	public class ServerViewFactory
	{
		private ListStore	valueStore;
		private TreeView	valueTreeView;
		private Gtk.Window	parentWindow;
		private LdapServer	ldapServer;

		public ServerViewFactory (ListStore store, 
					  TreeView treeView,
					  Gtk.Window parent,
					  LdapServer server)
		{
			valueStore = store;
			valueTreeView = treeView;
			parentWindow = parent;
			ldapServer = server;
		}

		public ServerView Create (string viewName)
		{
			ServerView serverView = null;
			string prefix = null;
			ViewData viewData;

			Cleanup ();

			viewData = (ViewData) Global.viewManager.Lookup (viewName);

			if (viewData.Name == null)
			{
				// Probably a standard view; search again
				prefix = GetPrefix ();

				viewData = (ViewData) Global.viewManager.Lookup (
					prefix + viewName);
			}

			serverView = new ServerView (viewData,
						     ldapServer, 
						     valueTreeView,
						     parentWindow);

			return serverView;
		}

		private string GetPrefix ()
		{
			string prefix = null;
	
			switch (ldapServer.ServerType.ToLower())
			{
				case "microsoft active directory":
					prefix = "ad";
					break;

				case "openldap":
					prefix = ldapServer.ServerType.ToLower();
					break;
			}

			return prefix;
		}

		private void Cleanup ()
		{
			if (valueStore != null)
			{
				valueStore.Clear ();
			}			

			foreach (TreeViewColumn col in valueTreeView.Columns)
			{
				valueTreeView.RemoveColumn (col);
			}
		}

	}

	public class ServerView
	{
		private ListStore 	store;
		private TreeView 	tv;
		private Gtk.Window 	parent;
		private LdapServer 	server;
		private ViewData	vd;
		private Menu 		popup;
		private Hashtable 	lookupTable;

		private static TargetEntry[] sourceTable = new TargetEntry[]
		{
			new TargetEntry ("text/plain", 0, 1),
		};

		private static TargetEntry[] targetsTable = new TargetEntry[]
		{
			new TargetEntry ("text/uri-list", 0, 0),
			new TargetEntry ("text/plain", 0, 1),
		};

		public ServerView (ViewData viewData,
				   LdapServer ldapServer, 
				   TreeView treeView, 
				   Gtk.Window parentWindow)
		{
			vd = viewData;
			server = ldapServer;
			parent = parentWindow;

			tv = treeView;
			tv.Selection.Mode = SelectionMode.Multiple;
			tv.ButtonPressEvent += new ButtonPressEventHandler (OnEntryRightClick);

			if (vd.Base.Equals (""))
			{
				vd.Base = server.DirectoryRoot;
			}

			lookupTable = new Hashtable ();

			System.Type[] types = new System.Type [vd.Cols.Length];

			for (int i = 0; i < vd.Cols.Length; i++)
			{
				types[i] = typeof (string);
			}

			store = new ListStore (types);
			tv.Model = store;

			SetupColumns ();

			SetupDragAndDrop ();

			tv.RowActivated += new RowActivatedHandler (OnRowActivated);
		}

		private void SetupColumns ()
		{
			CellRenderer crt = new CellRendererText ();

			for (int i = 0; i < vd.Cols.Length; i++)
			{
				TreeViewColumn col = new TreeViewColumn ();
				col.Title = vd.ColNames[i];
				col.PackStart (crt, true);
				col.AddAttribute (crt, "text", i);
				col.SortColumnId = i;

				tv.AppendColumn (col);
			}

			tv.ShowAll ();
		}

		private void SetupDragAndDrop ()
		{
			Gtk.Drag.DestSet (tv, DestDefaults.All, targetsTable,
					Gdk.DragAction.Copy);

			Gtk.Drag.SourceSet (tv, 
				Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask, 
				sourceTable, Gdk.DragAction.Copy | DragAction.Move);

			tv.DragBegin += new DragBeginHandler (OnDragBegin);
			tv.DragDataGet += new DragDataGetHandler (OnDragDataGet);
			tv.DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);
		}

		public void Populate ()
		{
			InsertData (vd.Base, vd.Cols);
		}

		private void DoInsert (LdapEntry[] objs, string[] attributes)
		{
			try
			{
				if (store != null)
					store.Clear ();

				foreach (LdapEntry le in objs)
				{
					string[] values = server.GetAttributeValuesFromEntry (
						le, attributes);

					store.AppendValues (values);

					if (vd.PrimaryKey == -1)
						continue;

					if (lookupTable.ContainsKey (values [vd.PrimaryKey]))
						lookupTable.Remove (values [vd.PrimaryKey]);

					lookupTable.Add (values [vd.PrimaryKey], le);
				}
			}
			catch 
			{
				string	msg = Mono.Unix.Catalog.GetString (
					"Unable to read data from server");

				Util.MessageBox (parent, msg, Gtk.MessageType.Info);
			}
		}

		private void InsertData (string searchBase, string[] attributes)
		{
			LdapEntry[] data = server.Search (searchBase, vd.Filter);

			Logger.Log.Debug (
			  "InsertData()\n\tbase: [{0}]\n\tfilter: [{1}]\n\tnumResults: [{2}]",
			   searchBase, vd.Filter, data.Length);

			DoInsert (data, attributes);
		}

		public void DoPopUp()
		{
			popup = new Menu();

			AccelGroup ag = new AccelGroup ();

			ImageMenuItem newItem = new ImageMenuItem (Stock.New, ag);
			newItem.Activated += new EventHandler (OnNewEntryActivate);
			newItem.Show ();

			popup.Append (newItem);

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("document-save.png");
			ImageMenuItem exportItem = new ImageMenuItem ("Export");
			exportItem.Image = new Gtk.Image (pb);
			exportItem.Activated += new EventHandler (OnExportActivate);
			exportItem.Show ();

			popup.Append (exportItem);

			ImageMenuItem deleteItem = new ImageMenuItem (Stock.Delete, ag);
			deleteItem.Activated += new EventHandler (OnDeleteActivate);
			deleteItem.Show ();

			popup.Append (deleteItem);

			ImageMenuItem propItem = new ImageMenuItem (Stock.Properties, ag);
			propItem.Activated += new EventHandler (OnEditActivate);
			propItem.Show ();

			popup.Append (propItem);

			if (vd.Name == "openldapUsers")
			{
				SeparatorMenuItem sm = new SeparatorMenuItem ();
				sm.Show ();
		
				popup.Append (sm);

				Gdk.Pixbuf pwdImage = Gdk.Pixbuf.LoadFromResource ("locked16x16.png");
				ImageMenuItem pwdItem = new ImageMenuItem ("Change password");
				pwdItem.Image = new Gtk.Image (pwdImage);
				pwdItem.Activated += new EventHandler (OnPwdActivate);
				pwdItem.Show ();

				popup.Append (pwdItem);

				PopupAddExtra ();
			}

			if (vd.Name.IndexOf ("Contacts") >= 0)
			{
				SeparatorMenuItem sm = new SeparatorMenuItem ();
				sm.Show ();
		
				popup.Append (sm);

				PopupAddExtra ();
			}

			popup.Popup(null, null, null, 3,
					Gtk.Global.CurrentEventTime);
		}

		private void PopupAddExtra ()
		{
			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("mail-message-new.png");
			ImageMenuItem mailItem = new ImageMenuItem ("Send email");
			mailItem.Image = new Gtk.Image (pb);
			mailItem.Activated += new EventHandler (OnEmailActivate);
			mailItem.Show ();

			popup.Append (mailItem);

			Gdk.Pixbuf wwwImage = Gdk.Pixbuf.LoadFromResource ("go-home.png");
			ImageMenuItem wwwItem = new ImageMenuItem ("Open Home Page");
			wwwItem.Image = new Gtk.Image (wwwImage);
			wwwItem.Activated += new EventHandler (OnWWWActivate);
			wwwItem.Show ();

			popup.Append (wwwItem);
		}

		[ConnectBefore]
		public void OnEntryRightClick (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
			{
				DoPopUp ();
			}
		}

		public LdapEntry LookupEntry (TreePath path)
		{
			TreeIter iter;
			
			if (store.GetIter (out iter, path))
			{
				string key = null;
				key = (string) store.GetValue (iter, vd.PrimaryKey);
				
				LdapEntry le = (LdapEntry) lookupTable [key];

				return le;
			} 

			return null;
		}

		public void OnRowActivated (object o, RowActivatedArgs args)
		{	
			LdapEntry le = LookupEntry (args.Path);

			ViewDialogFactory.Create (vd.Name, server, le);

			Populate ();
		}

		public void OnDragBegin (object o, DragBeginArgs args)
		{
			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("text-x-generic.png");
			Gtk.Drag.SetIconPixbuf (args.Context, pb, 0, 0);
		}

		public void OnDragDataGet (object o, DragDataGetArgs args)
		{
			Gtk.TreeModel model;

			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp)
			{
				LdapEntry le = LookupEntry (path);

				LDIF _ldif = new LDIF (le);

				Atom[] targets = args.Context.Targets;

				args.SelectionData.Set (targets[0], 8,
					System.Text.Encoding.UTF8.GetBytes (_ldif.Export()));
			}
		}

		public void RemoveDndHandlers ()
		{
			tv.DragBegin -= new DragBeginHandler (OnDragBegin);
			tv.DragDataGet -= new DragDataGetHandler (OnDragDataGet);
			tv.DragDataReceived -= new DragDataReceivedHandler (OnDragDataReceived);
		}
		
		public void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
			bool success = false;

			string data = System.Text.Encoding.UTF8.GetString (
					args.SelectionData.Data);

			switch (args.Info)
			{
				case 0:
				{
					string[] uri_list = Regex.Split (data, "\r\n");

					Util.ImportData (server, parent, uri_list);
					
					success = true;
					break;
				}

				case 1:
				{
					Util.ImportData (server, parent, data);

					success = true;
					break;
				}
			}

			Gtk.Drag.Finish (args.Context, success, false, args.Time);
		}

		public void OnNewEntryActivate (object o, EventArgs args) 
		{
			ViewDialogFactory.Create (vd.Name, server, null);

			Populate ();
		}

		public void OnEditActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp)
			{
				LdapEntry le = LookupEntry (path);

				ViewDialogFactory.Create (vd.Name, server, le);
				
				Populate ();
			}
		}

		private void DeleteEntry (TreePath[] path)
		{
			try
			{
				if (!(path.Length > 1))
				{
					LdapEntry le = LookupEntry (path[0]);

					Util.DeleteEntry (server, parent, le.DN);

					return;
				}

				ArrayList dnList = new ArrayList ();

				foreach (TreePath tp in path)
				{
					LdapEntry le = LookupEntry (tp);
					dnList.Add (le.DN);
				}

				string[] dns = (string[]) dnList.ToArray (typeof(string));

				Util.DeleteEntry (server, parent, dns);
			}
			catch {}
		}

		public void OnDeleteActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			DeleteEntry (tp);
			
			Populate ();
		}

		public void OnExportActivate (object o, EventArgs args)
		{
			TreeModel model;
			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			try
			{
				LdapEntry le = LookupEntry (tp[0]);
				Util.ExportData (server, parent, le.DN);
			}
			catch {}
		}

		private string getSelectedAttribute (string attrName)
		{
			Gtk.TreeModel model;

			TreePath[] tp = this.tv.Selection.GetSelectedRows (out model);

			try
			{
				LdapEntry le = this.LookupEntry (tp[0]);
				LdapAttribute la = le.getAttribute (attrName);

				return la.StringValue;
			}
			catch {}

			return "";
		}

		private LdapEntry GetSelectedEntry ()
		{
			Gtk.TreeModel model;

			TreePath[] tp = this.tv.Selection.GetSelectedRows (out model);

			try
			{
				LdapEntry le = this.LookupEntry (tp[0]);

				return le;
			}
			catch 
			{
				return null;
			}
		}

		public void OnEmailActivate (object o, EventArgs args) 
		{
			string url = getSelectedAttribute ("mail");

			if (url == null || url == "")
			{
				string msg = Mono.Unix.Catalog.GetString (
					"Invalid or empty email address");
				
				Util.MessageBox (parent, msg, MessageType.Error);

				return;
			}

			try
			{
				Gnome.Url.Show ("mailto:" + url);
			}
			catch (Exception e)
			{
				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to send mail to ") + url;

				errorMsg += "\nError: " + e.Message;

				Util.MessageBox (parent, errorMsg, MessageType.Error);
			}
		}

		public void OnWWWActivate (object o, EventArgs args) 
		{
			string url = getSelectedAttribute ("wWWHomePage");

			try
			{
				Gnome.Url.Show (url);
			}
			catch (Exception e)
			{
				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to open page ") + url;

				errorMsg += "\nError: " + e.Message;

				Util.MessageBox (parent, errorMsg, MessageType.Error);
			}
		}

		public void OnPwdActivate (object o, EventArgs args)
		{
			PasswordDialog pd = new PasswordDialog ();

			if (pd.UnixPassword.Equals (""))
				return;

			ArrayList mods = new ArrayList ();
			LdapEntry le = GetSelectedEntry ();
			
			LdapAttribute la; 
			LdapModification lm;

			la = new LdapAttribute ("userPassword", pd.UnixPassword);
			lm = new LdapModification (LdapModification.REPLACE, la);

			mods.Add (lm);

			if (Util.CheckSamba (le))
			{
				la = new LdapAttribute ("sambaLMPassword", pd.LMPassword);
				lm = new LdapModification (LdapModification.REPLACE, la);

				mods.Add (lm);

				la = new LdapAttribute ("sambaNTPassword", pd.NTPassword);
				lm = new LdapModification (LdapModification.REPLACE, la);

				mods.Add (lm);
			}

			Util.ModifyEntry (server, parent, le.DN, mods, true);
		}

		public void OnRefreshActivate (object o, EventArgs args)
		{
			Populate ();
		}

		public void RemoveHandlers ()
		{
			tv.RowActivated -= new RowActivatedHandler (OnRowActivated);
			tv.ButtonPressEvent -= new ButtonPressEventHandler (OnEntryRightClick);
		}
	}
}
