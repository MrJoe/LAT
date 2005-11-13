// 
// lat - View.cs
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
	public abstract class View
	{
		protected ListStore 	store;
		protected TreeView 	tv;
		protected Gtk.Window 	parent;
		protected LdapServer 	server;
		protected Menu 		popup;

		protected string 	viewName = null;
		protected string 	filter = null;

		protected int 		lookupKeyCol;
		protected Hashtable 	lookupTable;

		private static TargetEntry[] _sourceTable = new TargetEntry[]
		{
			new TargetEntry ("text/plain", 0, 1),
		};

		private static TargetEntry[] _targetsTable = new TargetEntry[]
		{
			new TargetEntry ("text/uri-list", 0, 0),
			new TargetEntry ("text/plain", 0, 1),
		};

		public View (LdapServer ldapServer, TreeView treeView, Gtk.Window parentWindow)
		{
			server = ldapServer;
			parent = parentWindow;

			tv = treeView;
			tv.Selection.Mode = SelectionMode.Multiple;
			tv.ButtonPressEvent += new ButtonPressEventHandler (OnEntryRightClick);

			lookupTable = new Hashtable ();

			Gtk.Drag.DestSet (tv, DestDefaults.All, _targetsTable,
					Gdk.DragAction.Copy);

			Gtk.Drag.SourceSet (tv, 
				Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask, 
				_sourceTable, Gdk.DragAction.Copy | DragAction.Move);

			tv.DragBegin += new DragBeginHandler (OnDragBegin);
			tv.DragDataGet += new DragDataGetHandler (OnDragDataGet);
			tv.DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);

			tv.RowActivated += new RowActivatedHandler (OnRowActivated);
		}

		public void DoPopUp()
		{
			popup = new Menu();

			AccelGroup ag = new AccelGroup ();

			ImageMenuItem newItem = new ImageMenuItem (Stock.New, ag);
			newItem.Activated += new EventHandler (OnNewEntryActivate);
			newItem.Show ();

			popup.Append (newItem);

			MenuItem exportItem = new MenuItem ("Export");
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

			customPopUp ();

			popup.Popup(null, null, null, 3,
					Gtk.Global.CurrentEventTime);
		}

		public virtual void customPopUp ()
		{
		}

		[ConnectBefore]
		public void OnEntryRightClick (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
			{
				DoPopUp ();
			}
		}

		public void setupColumns (string[] cols)
		{
			CellRenderer crt = new CellRendererText ();

			for (int i = 0; i < cols.Length; i++)
			{
				TreeViewColumn col = new TreeViewColumn ();
				col.Title = cols[i];
				col.PackStart (crt, true);
				col.AddAttribute (crt, "text", i);
				col.SortColumnId = i;

				tv.AppendColumn (col);
			}

			tv.ShowAll ();
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

					if (lookupTable.ContainsKey (values [lookupKeyCol]))
						lookupTable.Remove (values [lookupKeyCol]);

					lookupTable.Add (values [lookupKeyCol], le);
				}
			}
			catch 
			{
				string	msg = Mono.Unix.Catalog.GetString (
					"Unable to read data from server");

				Gtk.MessageDialog md = new Gtk.MessageDialog (parent, 
					Gtk.DialogFlags.DestroyWithParent,
					Gtk.MessageType.Info, 
					Gtk.ButtonsType.Close, 
					msg);

				md.Run ();
				md.Destroy();

				md = null;
			}
		}

		public void insertData (string[] attributes)
		{
			LdapEntry[] data = server.Search (filter);
			DoInsert (data, attributes);
		}

		public void insertData (string searchBase, string[] attributes)
		{
			LdapEntry[] data = server.Search (searchBase, filter);
			DoInsert (data, attributes);
		}

		public LdapEntry lookupEntry (TreePath path)
		{
			TreeIter iter;
			
			if (store.GetIter (out iter, path))
			{
				string key = null;
				key = (string) store.GetValue (iter, lookupKeyCol);
				
				LdapEntry le = (LdapEntry) lookupTable [key];

				return le;
			} 

			return null;
		}

		public virtual void OnRowActivated (object o, RowActivatedArgs args)
		{	
			LdapEntry le = lookupEntry (args.Path);

			ViewDialogFactory.Create (viewName, server, le);

			Populate ();
		}

		public virtual void OnDragBegin (object o, DragBeginArgs args)
		{
			// FIXME: change icon
			// FIXME: Drag.SetIconPixbuf (args.Context, <obj>, 0, 0);
		}

		public virtual void OnDragDataGet (object o, DragDataGetArgs args)
		{
			Gtk.TreeModel model;

			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp)
			{
				LdapEntry le = lookupEntry (path);

				LDIF _ldif = new LDIF (le);

				Atom[] targets = args.Context.Targets;

				args.SelectionData.Set (targets[0], 8,
					System.Text.Encoding.UTF8.GetBytes (_ldif.Export()));
			}
		}

		public void removeDndHandlers ()
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

		public virtual void OnNewEntryActivate (object o, EventArgs args) 
		{
			ViewDialogFactory.Create (viewName, server, null);

			Populate ();
		}

		public virtual void OnEditActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp)
			{
				LdapEntry le = lookupEntry (path);

				ViewDialogFactory.Create (viewName, server, le);
				
				Populate ();
			}
		}

		private void deleteEntry (TreePath[] path)
		{
			try
			{
				if (!(path.Length > 1))
				{
					LdapEntry le = lookupEntry (path[0]);

					Util.DeleteEntry (server, parent, le.DN);

					return;
				}

				ArrayList dnList = new ArrayList ();

				foreach (TreePath tp in path)
				{
					LdapEntry le = lookupEntry (tp);
					dnList.Add (le.DN);
				}

				string[] dns = (string[]) dnList.ToArray (typeof(string));

				Util.DeleteEntry (server, parent, dns);
			}
			catch {}
		}

		public virtual void OnDeleteActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			deleteEntry (tp);
			
			Populate ();
		}

		public virtual void OnExportActivate (object o, EventArgs args)
		{
			TreeModel model;
			TreePath[] tp = tv.Selection.GetSelectedRows (out model);

			try
			{
				LdapEntry le = lookupEntry (tp[0]);
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
				LdapEntry le = this.lookupEntry (tp[0]);
				LdapAttribute la = le.getAttribute (attrName);

				return la.StringValue;
			}
			catch {}

			return "";
		}

		public virtual void OnEmailActivate (object o, EventArgs args) 
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

		public virtual void OnWWWActivate (object o, EventArgs args) 
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


		public virtual void OnRefreshActivate (object o, EventArgs args)
		{
			Populate ();
		}

		public void removeHandlers ()
		{
			tv.RowActivated -= new RowActivatedHandler (OnRowActivated);
			tv.ButtonPressEvent -= new ButtonPressEventHandler (OnEntryRightClick);
		}

		public abstract void Populate ();
	}
}
