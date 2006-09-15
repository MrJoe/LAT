// 
// lat - UserDefaultValuesDialog.cs
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
using System;
using Novell.Directory.Ldap;

namespace lat
{
	public class UserDefaultValuesDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog defaultValuesDialog;

		// General 
		[Glade.Widget] Gtk.HBox hbox417;
		[Glade.Widget] Gtk.Entry passwordEntry;
		[Glade.Widget] Gtk.Entry homeEntry;
		[Glade.Widget] Gtk.Entry shellEntry;
		[Glade.Widget] Gtk.CheckButton sambaCheckButton;

		ComboBox primaryGroupComboBox;
		ViewPlugin vp;
		Connection conn;
		
		string smbLM = "";
		string smbNT = "";

		public UserDefaultValuesDialog (ViewPlugin plugin, Connection connection)
		{
			vp = plugin;
			conn = connection;
			
			ui = new Glade.XML (null, "dialogs.glade", "defaultValuesDialog", null);
			ui.Autoconnect (this);
			
			passwordEntry.Sensitive = false;			
			SetDefaultValues ();
		
			defaultValuesDialog.Run ();
			defaultValuesDialog.Destroy ();
		}
		
		void CreateCombo (string defaultGroup)
		{
			if (primaryGroupComboBox != null) {
				primaryGroupComboBox.Destroy ();
				primaryGroupComboBox = null;
			}
			
			primaryGroupComboBox = ComboBox.NewText ();
			primaryGroupComboBox.AppendText (defaultGroup);

			LdapEntry[] grps = conn.Data.SearchByClass ("posixGroup");
			foreach (LdapEntry e in grps) {
				LdapAttribute nameAttr = e.getAttribute ("cn");
				if (nameAttr.StringValue.ToLower() == defaultGroup.ToLower())
					continue;
					
				primaryGroupComboBox.AppendText (nameAttr.StringValue);
			}

			primaryGroupComboBox.Active = 0;
			primaryGroupComboBox.Show ();

			hbox417.Add (primaryGroupComboBox);
		}
		
		void SetDefaultValues ()
		{
			Log.Debug ("vp.PluginConfiguration.Defaults.Count: {0}", vp.PluginConfiguration.Defaults.Count);
		
			if (vp.PluginConfiguration.Defaults.ContainsKey ("userPassword"))
				passwordEntry.Text = vp.PluginConfiguration.Defaults ["userPassword"]; 

			if (vp.PluginConfiguration.Defaults.ContainsKey ("sambaLMPassword"))
				smbLM = vp.PluginConfiguration.Defaults["sambaLMPassword"];
				
			if (vp.PluginConfiguration.Defaults.ContainsKey ("sambaNTPassword"))
				smbNT = vp.PluginConfiguration.Defaults["sambaNTPassword"];				

			if (vp.PluginConfiguration.Defaults.ContainsKey ("homeDirectory"))
				homeEntry.Text = vp.PluginConfiguration.Defaults ["homeDirectory"]; 
				
			if (vp.PluginConfiguration.Defaults.ContainsKey ("loginShell"))
				shellEntry.Text = vp.PluginConfiguration.Defaults ["loginShell"]; 				

			if (vp.PluginConfiguration.Defaults.ContainsKey ("enableSamba")) {
				bool enableSamba = bool.Parse (vp.PluginConfiguration.Defaults ["enableSamba"]);
				sambaCheckButton.Active = enableSamba;
			}

			if (vp.PluginConfiguration.Defaults.ContainsKey ("defaultGroup"))
				CreateCombo (vp.PluginConfiguration.Defaults["defaultGroup"]);
			else
				CreateCombo ("None");
		}

		public void OnSetPasswordClicked (object o, EventArgs args)
		{
			PasswordDialog pd = new PasswordDialog ();

			if (!passwordEntry.Text.Equals ("") && pd.UnixPassword.Equals (""))
				return;

			passwordEntry.Text = pd.UnixPassword;
			smbLM = pd.LMPassword;
			smbNT = pd.NTPassword;		
		}
		
		public void OnOkClicked (object o, EventArgs args)
		{
			TreeIter iter;
			if (primaryGroupComboBox.GetActiveIter (out iter)) {
				string pg = (string) primaryGroupComboBox.Model.GetValue (iter, 0);
				if (pg != "None")
					vp.PluginConfiguration.Defaults["defaultGroup"] = pg;
			}
			
			if (passwordEntry.Text != "") {
				vp.PluginConfiguration.Defaults["userPassword"] = passwordEntry.Text;
				if (sambaCheckButton.Active) {
					vp.PluginConfiguration.Defaults["sambaLMPassword"] = smbLM;
					vp.PluginConfiguration.Defaults["sambaNTPassword"] = smbNT;
				}
			}

			if (homeEntry.Text != "")
				vp.PluginConfiguration.Defaults["homeDirectory"] = homeEntry.Text;
			
			if (shellEntry.Text != "")
				vp.PluginConfiguration.Defaults["loginShell"] = shellEntry.Text;
			
			vp.PluginConfiguration.Defaults["enableSamba"] = sambaCheckButton.Active.ToString();
			
			Log.Debug ("vp.PluginConfiguration.Defaults.Count: {0}", vp.PluginConfiguration.Defaults.Count);
		}
	}
}