// 
// lat - Util.cs
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
using GLib;
using System;
using System.Collections;
using System.IO;
using System.Text;
using Novell.Directory.Ldap;

namespace lat 
{
	public class Util
	{
		public Util ()
		{
		}

		public static void MessageBox (Gtk.Window parent, string msg, MessageType mType)
		{
			MessageDialog resMd;

			resMd = new MessageDialog (parent, 
					DialogFlags.DestroyWithParent,
					mType, 
					ButtonsType.Close, 
					msg);

			resMd.Run ();
			resMd.Destroy();
		}

		public static void AddEntry (lat.Connection conn, Gtk.Window parent, string dn, ArrayList attrs)
		{
			try
			{
				conn.Add (dn, attrs);

				string resMsg = String.Format (
					Mono.Unix.Catalog.GetString ("Entry {0} has been added."), dn);
	
				MessageBox (parent, resMsg, MessageType.Info);
			}
			catch (Exception e)
			{
				string errorMsg = 
					Mono.Unix.Catalog.GetString ("Unable to add entry ") + dn;

				errorMsg += "\nError: " + e.Message;

				MessageBox (parent, errorMsg, MessageType.Error);
			}
		}

		public static void ModifyEntry (lat.Connection conn, Gtk.Window parent, string dn, ArrayList modList)
		{
			if (modList.Count == 0)
			{
				return;
			}

			LdapModification[] mods;
			mods = new LdapModification [modList.Count];
			mods = (LdapModification[]) modList.ToArray(typeof(LdapModification));

			try
			{
				conn.Modify (dn, mods);

				string resMsg = String.Format (
					Mono.Unix.Catalog.GetString ("Entry {0} has been modified."), dn);

				MessageBox (parent, resMsg, MessageType.Info);

			}
			catch (Exception e)
			{
				string errorMsg = 
					Mono.Unix.Catalog.GetString ("Unable to modify entry ") + dn;

				errorMsg += "\nError: " + e.Message;

				MessageBox (parent, errorMsg, MessageType.Error);				
			}

			modList.Clear ();
		}

		public static bool DeleteEntry (lat.Connection conn, Gtk.Window parent, string[] dn)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString ("Are you sure you want to delete:\n\n"));
			
			foreach (string n in dn)
			{
				msg += String.Format ("{0}\n", n);
			}

			MessageDialog md = new MessageDialog (parent, 
					DialogFlags.DestroyWithParent,
					MessageType.Question, 
					ButtonsType.YesNo, 
					msg);
	     
			ResponseType result = (ResponseType)md.Run ();

			bool allGood = true;

			if (result == ResponseType.Yes)
			{
				foreach (string d in dn)
				{					
					try
					{
						conn.Delete (d);

					}
					catch (Exception e)
					{
						allGood = false;

						string errorMsg =
							Mono.Unix.Catalog.GetString (
							"Unable to delete all entry " + d);

						errorMsg += "\nError: " + e.Message;

						MessageBox (parent, errorMsg, MessageType.Error);

						break;
					}
				}

				if (allGood)
				{
					MessageBox (parent, 
						Mono.Unix.Catalog.GetString (
						"Entries successfully deleted."), 
						MessageType.Info);
				}
			}
			else
			{
				allGood = false;
			}

			md.Destroy ();

			return allGood;
		}

		public static bool DeleteEntry (lat.Connection conn, Gtk.Window parent, string dn)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString ("Are you sure you want to delete\n{0}"), dn);
				
			MessageDialog md = new MessageDialog (parent, 
					DialogFlags.DestroyWithParent,
					MessageType.Question, 
					ButtonsType.YesNo, 
					msg);
	     
			ResponseType result = (ResponseType)md.Run ();

			bool retVal = false;

			if (result == ResponseType.Yes)
			{				
				try
				{
					conn.Delete (dn);

					string resMsg = String.Format (
						Mono.Unix.Catalog.GetString ("Entry {0} has been deleted."), dn);
		
					MessageBox (md, resMsg, MessageType.Info);

					retVal = true;
				}
				catch (Exception e)
				{
					string errorMsg =
						Mono.Unix.Catalog.GetString ("Unable to delete entry ") + dn;

					errorMsg += "\nError: " + e.Message;

					MessageBox (md, errorMsg, MessageType.Error);
				}
			}
				
			md.Destroy ();

			return retVal;
		}

		private static void import (lat.Connection conn, Gtk.Window parent, Uri uri)
		{
			int numImported = 0;

			LDIF ldif = new LDIF (conn);
	
			numImported = ldif.Import (uri);

			string msg = String.Format (
				Mono.Unix.Catalog.GetString ("Imported {0} entries\nfrom {1}."), 
				numImported, uri.ToString());

			if (numImported > 0)
				MessageBox (parent, msg, MessageType.Info);
			else
				MessageBox (parent, msg, MessageType.Error);
		}
		
		public static void ImportData (lat.Connection conn, Gtk.Window parent, Uri uri)
		{
			import (conn, parent, uri);
		}

		public static void ImportData (lat.Connection conn, Gtk.Window parent, string[] uriList)
		{
			foreach (string u in uriList)
			{
				if (!(u.Length > 0))
					continue;

				import (conn, parent, new Uri(u));
			}
		}

		public static void ImportData (lat.Connection conn, Gtk.Window parent, string data)
		{
			int numImported = 0;
			string msg = null;

			LDIF ldif = new LDIF (conn);

			numImported = ldif.Import (data);

			msg = String.Format (
				Mono.Unix.Catalog.GetString ("Imported {0} entries."),
				numImported);

			if (numImported > 0)
				MessageBox (parent, msg, MessageType.Info);
			else
				MessageBox (parent, msg, MessageType.Error);
		}

		public static void getChildren (lat.Connection conn, string name, StringBuilder sb)
		{
			ArrayList children = conn.getChildren (name);
			
			if (children == null)
				return;

			foreach (LdapEntry cle in children)
			{
				LDIF cldif = new LDIF (cle);
				sb.AppendFormat ("{0}\n", cldif.Export());

				getChildren (conn, cle.DN, sb);
			}
		}

		private static string export (lat.Connection conn, string dn)
		{
			StringBuilder data = new StringBuilder();

			LdapEntry le = (LdapEntry) conn.getEntry (dn);
			LDIF _ldif = new LDIF (le);

			data.AppendFormat ("{0}\n", _ldif.Export());

			ArrayList children = conn.getChildren (dn);

			foreach (LdapEntry cle in children)
			{
				LDIF cldif = new LDIF (cle);
				data.AppendFormat ("{0}\n", cldif.Export());

				getChildren (conn, cle.DN, data);
			}

			return data.ToString ();
		}

		public static void ExportData (lat.Connection conn, string dn, out string data)
		{
			data = export (conn, dn);
		}
	
		public static void ExportData (lat.Connection conn, Gtk.Window parent, string dn)
		{
			FileChooserDialog fcd = new FileChooserDialog (
				Mono.Unix.Catalog.GetString ("Save LDIF export as"),
				Gtk.Stock.Save, 
				parent, 
				FileChooserAction.Save);

			fcd.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			fcd.AddButton (Gtk.Stock.Save, ResponseType.Ok);

			fcd.SelectMultiple = false;

			ResponseType response = (ResponseType) fcd.Run();
			if (response == ResponseType.Ok) 
			{
				string data = export (conn, dn);

				try 
				{
					using (StreamWriter sw = new StreamWriter(fcd.Filename)) 
					{
						sw.Write (data);
					}
				}
				catch {}
			} 
		
			fcd.Destroy();
		}
	}
}
