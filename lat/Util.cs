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
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Unix;
using Novell.Directory.Ldap;

namespace lat 
{
	public class Util
	{
		public Util ()
		{
		}

		// Taken from Banshee; written by Aaron Bockover (aaron@aaronbock.net)
        [DllImport("libc")]
        static extern int prctl(int option, byte [] arg2, ulong arg3 , ulong arg4, ulong arg5);
        
       	public static void SetProcessName(string name)
        {
            if(prctl(15 /* PR_SET_NAME */, Encoding.ASCII.GetBytes(name), 0, 0, 0) != 0) {
       	        throw new ApplicationException("Error setting process name: " + 
               	    Mono.Unix.Native.Stdlib.GetLastError());
            }
       	}

		public static void DisplaySambaSIDWarning (Gtk.Window parent)
		{
			string msg = Mono.Unix.Catalog.GetString (
			   "LAT could not determine your Samba System ID (SID). If you " +
			   "haven't configured your directory for Samba yet, select " +
			   "'Populate directory for samba' from the Server menu");

			HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Warning,
					Gtk.ButtonsType.Ok,
					"Unable to enable Samba support",
					msg);

			dialog.Run ();
			dialog.Destroy ();
		}

		public static LdapAttribute[] CreateSambaAttributes (int uid, string sid, string lm, string nt)
		{
			List<LdapAttribute> mods = new List<LdapAttribute> ();

			mods.Add (new LdapAttribute ("sambaLMPassword", lm));
			mods.Add (new LdapAttribute ("sambaNTPassword", nt));
			mods.Add (new LdapAttribute ("sambaAcctFlags", "[U          ]"));

			int user_rid = Convert.ToInt32 (uid) * 2 + 1000;

			mods.Add (new LdapAttribute ("sambaSID", String.Format ("{0}-{1}", sid, user_rid)));
			mods.Add (new LdapAttribute ("sambaPrimaryGroupSID", String.Format ("{0}-513", sid)));
			
			return mods.ToArray();
		}
		
		public static bool CheckSamba (LdapEntry le)
		{
			bool retVal = false;
			
			LdapAttribute la = le.getAttribute ("objectClass");
			
			if (la == null)
				return retVal;

			foreach (string s in la.StringValueArray)
				if (s.ToLower() == "sambasamaccount")
					retVal = true;

			return retVal;
		}

		public static string SuggestUserName (string firstName, string lastName)
		{
			string retVal = "";

			if (firstName.Length >= 1)
				retVal += firstName.Substring (0,1);
			
			if (lastName.Length >= 6)
				retVal += lastName.Substring (0,6);
			else
				retVal += lastName;
			
			retVal = retVal.ToLower();
			
			return retVal;
		}

		public static Gdk.Pixbuf GetSSLIcon (bool ssl)
		{
			Gdk.Pixbuf retIcon = null;

			if (ssl)
				retIcon = Gdk.Pixbuf.LoadFromResource ("locked16x16.png");
			else
				retIcon = Gdk.Pixbuf.LoadFromResource ("unlocked16x16.png");

			return retIcon;
		}

		public static bool CheckUserName (LdapServer server, string name)
		{
			if (server.Search(String.Format("(uid={0})", name)).Length == 0)
				return true;
		
			return false;
		}

		public static bool CheckUID (LdapServer server, int uid)
		{
			if (server.Search(String.Format("(uidNumber={0})", uid)).Length == 0)
				return true;
		
			return false;
		}

		public static byte[] ConvertSbyteToByte (sbyte[] data)
		{
			byte[] retVal = new byte[data.Length];
			int count = 0;

			foreach (sbyte sb in data) {
				retVal[count] = (byte) sb;
				count++;
			}
			
			return retVal;
		}

		public static bool AddEntry (LdapServer server, LdapEntry entry)
		{
			try {

				server.Add (entry);
				return true;

			} catch (Exception e) {

				string errorMsg = Mono.Unix.Catalog.GetString ("Unable to add entry ") + entry.DN;
				errorMsg += "\nError: " + e.Message;

				Log.Debug (e);

				HIGMessageDialog dialog = new HIGMessageDialog (
						null,
						0,
						Gtk.MessageType.Error,
						Gtk.ButtonsType.Ok,
						"Add error",
						errorMsg);

				dialog.Run ();
				dialog.Destroy ();

				return false;
			}		
		}

		public static bool ModifyEntry (LdapServer server, string dn, LdapModification[] modList)
		{
			if (modList.Length == 0) {
				Log.Debug ("No modifications to make to entry {0}", dn);
				return false;
			}

			try {

				server.Modify (dn, modList);
				return true;

			} catch (Exception e) {

				string errorMsg = 
					Mono.Unix.Catalog.GetString ("Unable to modify entry ") + dn;

				errorMsg += "\nError: " + e.Message;

				HIGMessageDialog dialog = new HIGMessageDialog (
					null,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Modify entry",
					errorMsg);

				dialog.Run ();
				dialog.Destroy ();

				return false;
			}		
		}
		
		public static bool AskYesNo (Gtk.Window parent, string msg)
		{
			MessageDialog md = new MessageDialog (parent, 
					DialogFlags.DestroyWithParent,
					MessageType.Question, 
					ButtonsType.YesNo, 
					msg);
	     
			ResponseType result = (ResponseType)md.Run ();
			md.Destroy ();

			if (result == ResponseType.Yes)
				return true;


			return false;
		}

		private static bool deleteEntry (LdapServer server, string dn)
		{
			try {
				server.Delete (dn);
				return true;

			} catch (Exception e) {

				Log.Debug ("deleteEntry error: {0}", e.Message);
				return false;
			}
		}

		public static bool DeleteEntry (LdapServer server, Gtk.Window parent, string[] dn)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString (
				"Are you sure you want to delete:\n\n"));

			string errorMsg =
				Mono.Unix.Catalog.GetString (
				"Unable to delete the following entries:\n");
			
			foreach (string n in dn)
				msg += String.Format ("{0}\n", n);

			if (!Util.AskYesNo (parent, msg))
				return false;

			bool allGood = true;

			foreach (string d in dn) {

				allGood = deleteEntry (server, d);

				if (!allGood)
					errorMsg += d;
			}

			if (allGood) {

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Info,
					Gtk.ButtonsType.Ok,
					"Delete entries",
					Mono.Unix.Catalog.GetString (
					"Entries successfully deleted."));

				dialog.Run ();
				dialog.Destroy ();

			} else {

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Delete error",
					errorMsg);

				dialog.Run ();
				dialog.Destroy ();
			}

			return allGood;
		}

		public static bool DeleteEntry (LdapServer server, Gtk.Window parent, string dn)
		{
			string msg = String.Format ("{0}\n{1}",
				Mono.Unix.Catalog.GetString (
				"Are you sure you want to delete"), 
				dn);

			bool retVal = false;
				
			if (Util.AskYesNo (parent, msg)) {

				try {

					server.Delete (dn);

					string resMsg = String.Format (
						Mono.Unix.Catalog.GetString (
						"Entry {0} has been deleted."), dn);

					HIGMessageDialog dialog = new HIGMessageDialog (
						parent,
						0,
						Gtk.MessageType.Info,
						Gtk.ButtonsType.Ok,
						"Delete entries",
						resMsg);

					dialog.Run ();
					dialog.Destroy ();

					retVal = true;

				} catch (Exception e) {

					string errorMsg =
						Mono.Unix.Catalog.GetString (
						"Unable to delete entry ") + dn;

					errorMsg += "\nError: " + e.Message;

					HIGMessageDialog dialog = new HIGMessageDialog (
						parent,
						0,
						Gtk.MessageType.Error,
						Gtk.ButtonsType.Ok,
						"Delete entries",
						errorMsg);

					dialog.Run ();
					dialog.Destroy ();
				}
			}

			return retVal;
		}

		private static void import (LdapServer server, Gtk.Window parent, Uri uri)
		{
			int numImported = 0;

			LDIF ldif = new LDIF (server);
	
			numImported = ldif.Import (uri);

			string msg = String.Format (
				Mono.Unix.Catalog.GetString ("Imported {0} entries\nfrom {1}."), 
				numImported, uri.ToString());

			if (numImported > 0) {

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Info,
					Gtk.ButtonsType.Ok,
					"Import entries",
					msg);

				dialog.Run ();
				dialog.Destroy ();

			} else {

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Import error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}
		
		public static void ImportData (LdapServer server, Gtk.Window parent, Uri uri)
		{
			import (server, parent, uri);
		}

		public static void ImportData (LdapServer server, Gtk.Window parent, string[] uriList)
		{
			foreach (string u in uriList) {
				if (!(u.Length > 0))
					continue;

				import (server, parent, new Uri(u));
			}
		}

		public static void ImportData (LdapServer server, Gtk.Window parent, string data)
		{
			int numImported = 0;
			string msg = null;

			LDIF ldif = new LDIF (server);

			numImported = ldif.Import (data);

			msg = String.Format (
				Mono.Unix.Catalog.GetString ("Imported {0} entries."),
				numImported);

			if (numImported > 0) {

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Info,
					Gtk.ButtonsType.Ok,
					"Import entries",
					msg);

				dialog.Run ();
				dialog.Destroy ();

			} else {

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Import error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		public static void getChildren (LdapServer server, string name, StringBuilder sb)
		{
			LdapEntry[] children = server.GetEntryChildren (name);
			
			if (children == null)
				return;

			foreach (LdapEntry cle in children) {
				LDIF cldif = new LDIF (cle);
				sb.AppendFormat ("{0}\n", cldif.Export());

				getChildren (server, cle.DN, sb);
			}
		}

		private static string export (LdapServer server, string dn)
		{
			StringBuilder data = new StringBuilder();

			LdapEntry le = (LdapEntry) server.GetEntry (dn);
			LDIF _ldif = new LDIF (le);

			data.AppendFormat ("{0}\n", _ldif.Export());

			LdapEntry[] children = server.GetEntryChildren (dn);

			foreach (LdapEntry cle in children) {
				LDIF cldif = new LDIF (cle);
				data.AppendFormat ("{0}\n", cldif.Export());

				getChildren (server, cle.DN, data);
			}

			return data.ToString ();
		}

		public static void ExportData (LdapServer server, string dn, out string data)
		{
			data = export (server, dn);
		}
	
		public static void ExportData (LdapServer server, Gtk.Window parent, string dn)
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
			if (response == ResponseType.Ok)  {

				string data = export (server, dn);

				try {

					using (StreamWriter sw = new StreamWriter(fcd.Filename)) 
					{
						sw.Write (data);
					}

				} catch {}
			} 
		
			fcd.Destroy();
		}
	}

	// taken from Tomboy; written by Alex Graveley <alex@beatniksoftware.com>
	public class HIGMessageDialog : Gtk.Dialog
	{
		Gtk.AccelGroup accel_group;

		public HIGMessageDialog (Gtk.Window parent,
					 Gtk.DialogFlags flags,
					 Gtk.MessageType type,
					 Gtk.ButtonsType buttons,
					 string          header,
					 string          msg)
			: base ()
		{
			HasSeparator = false;
			BorderWidth = 5;
			Resizable = false;
			Title = "";

			VBox.Spacing = 12;
			ActionArea.Layout = Gtk.ButtonBoxStyle.End;

			accel_group = new Gtk.AccelGroup ();
			AddAccelGroup (accel_group);

			Gtk.HBox hbox = new Gtk.HBox (false, 12);
			hbox.BorderWidth = 5;
			hbox.Show ();
			VBox.PackStart (hbox, false, false, 0);

			Gtk.Image image = null;

			switch (type) {
			case Gtk.MessageType.Error:
				image = new Gtk.Image (Gtk.Stock.DialogError, 
						       Gtk.IconSize.Dialog);
				break;
			case Gtk.MessageType.Question:
				image = new Gtk.Image (Gtk.Stock.DialogQuestion, 
						       Gtk.IconSize.Dialog);
				break;
			case Gtk.MessageType.Info:
				image = new Gtk.Image (Gtk.Stock.DialogInfo, 
						       Gtk.IconSize.Dialog);
				break;
			case Gtk.MessageType.Warning:
				image = new Gtk.Image (Gtk.Stock.DialogWarning, 
						       Gtk.IconSize.Dialog);
				break;
			}

			image.Show ();
			hbox.PackStart (image, false, false, 0);
			
			Gtk.VBox label_vbox = new Gtk.VBox (false, 0);
			label_vbox.Show ();
			hbox.PackStart (label_vbox, true, true, 0);

			string title = String.Format ("<span weight='bold' size='larger'>{0}" +
						      "</span>\n",
						      header);

			Gtk.Label label;

			label = new Gtk.Label (title);
			label.UseMarkup = true;
			label.Justify = Gtk.Justification.Left;
			label.LineWrap = true;
			label.SetAlignment (0.0f, 0.5f);
			label.Show ();
			label_vbox.PackStart (label, false, false, 0);

			label = new Gtk.Label (msg);
			label.UseMarkup = true;
			label.Justify = Gtk.Justification.Left;
			label.LineWrap = true;
			label.SetAlignment (0.0f, 0.5f);
			label.Show ();
			label_vbox.PackStart (label, false, false, 0);
			
			switch (buttons) {
			case Gtk.ButtonsType.None:
				break;
			case Gtk.ButtonsType.Ok:
				AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok, true);
				break;
			case Gtk.ButtonsType.Close:
				AddButton (Gtk.Stock.Close, Gtk.ResponseType.Close, true);
				break;
			case Gtk.ButtonsType.Cancel:
				AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, true);
				break;
			case Gtk.ButtonsType.YesNo:
				AddButton (Gtk.Stock.No, Gtk.ResponseType.No, false);
				AddButton (Gtk.Stock.Yes, Gtk.ResponseType.Yes, true);
				break;
			case Gtk.ButtonsType.OkCancel:
				AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, false);
				AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok, true);
				break;
			}

			if (parent != null)
				TransientFor = parent;

			if ((int) (flags & Gtk.DialogFlags.Modal) != 0)
				Modal = true;

			if ((int) (flags & Gtk.DialogFlags.DestroyWithParent) != 0)
				DestroyWithParent = true;
		}

		void AddButton (string stock_id, Gtk.ResponseType response, bool is_default)
		{
			Gtk.Button button = new Gtk.Button (stock_id);
			button.CanDefault = true;
			button.Show ();

			AddActionWidget (button, response);

			if (is_default) {
				DefaultResponse = response;
				button.AddAccelerator ("activate",
						       accel_group,
						       (uint) Gdk.Key.Escape, 
						       0,
						       Gtk.AccelFlags.Visible);
			}
		}
	}
}
