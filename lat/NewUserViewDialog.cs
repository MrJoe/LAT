// 
// lat - NewUserViewDialog.cs
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
using Glade;
using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Mono.Security.Protocol.Ntlm;
using Novell.Directory.Ldap;

namespace lat
{
	public class NewUserViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog newUserDialog;

		// General 
		[Glade.Widget] Gtk.Label usernameLabel;
		[Glade.Widget] Gtk.Label fullnameLabel;

		[Glade.Widget] Gtk.Entry usernameEntry;
		[Glade.Widget] Gtk.SpinButton uidSpinButton;
		[Glade.Widget] Gtk.Entry firstNameEntry;
		[Glade.Widget] Gtk.Entry initialsEntry;
		[Glade.Widget] Gtk.Entry lastNameEntry;
		[Glade.Widget] Gtk.Entry displayNameEntry;
		[Glade.Widget] Gtk.Entry homeDirEntry;
		[Glade.Widget] Gtk.Entry shellEntry;
		[Glade.Widget] Gtk.Entry passwordEntry;
		[Glade.Widget] Gtk.Button passwordButton;
		[Glade.Widget] Gtk.HBox comboHbox;
		[Glade.Widget] Gtk.CheckButton enableSambaButton;

		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private static string[] userAttrs = { "givenName", "sn", "uid", "uidNumber", "gidNumber",
					      "userPassword", "initials", "loginShell", "cn",
					      "homeDirectory", "displayName" };

		private Hashtable _allGroups;
		private Hashtable _allGroupGids;
		private Hashtable _memberOfGroups;

		private string _smbSID = "";
		private string _smbLM = "";
		private string _smbNT = "";

		private ComboBox primaryGroupComboBox;

		public NewUserViewDialog (lat.Connection conn) : base (conn)
		{
			Init ();		

			getGroups ();

			createCombo ();

			uidSpinButton.Value = _conn.GetNextUID ();
			enableSambaButton.Toggled += new EventHandler (OnSambaChanged);

			newUserDialog.Run ();

			if (missingValues)
			{
				missingValues = false;
				newUserDialog.Run ();				
			}
			else
			{
				newUserDialog.Destroy ();
			}
		}

		private void OnSambaChanged (object o, EventArgs args)
		{
			if (enableSambaButton.Active)
			{
				_smbSID = _conn.GetLocalSID ();
			}
		}
		
		private void getGroups ()
		{
			ArrayList grps = _conn.SearchByClass ("posixGroup");

			foreach (LdapEntry e in grps)
			{
				LdapAttribute nameAttr, gidAttr;
				nameAttr = e.getAttribute ("cn");
				gidAttr = e.getAttribute ("gidNumber");

				_allGroups.Add (nameAttr.StringValue, e);
				_allGroupGids.Add (gidAttr.StringValue, nameAttr.StringValue);
			}
				
		}

		private void createCombo ()
		{
			primaryGroupComboBox = ComboBox.NewText ();

			foreach (string key in _allGroups.Keys)
			{
				primaryGroupComboBox.AppendText (key);
			}

			primaryGroupComboBox.Active = 0;
			primaryGroupComboBox.Show ();

			comboHbox.Add (primaryGroupComboBox);
		}

		private void Init ()
		{
			_memberOfGroups = new Hashtable ();
			_allGroups = new Hashtable ();
			_allGroupGids = new Hashtable ();

			ui = new Glade.XML (null, "lat.glade", "newUserDialog", null);
			ui.Autoconnect (this);

			_viewDialog = newUserDialog;

			passwordEntry.Sensitive = false;

			usernameEntry.Changed += new EventHandler (OnNameChanged);
			firstNameEntry.Changed += new EventHandler (OnNameChanged);
			lastNameEntry.Changed += new EventHandler (OnNameChanged);

			passwordButton.Clicked += new EventHandler (OnPasswordClicked);

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			newUserDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);
		}

		private void OnNameChanged (object o, EventArgs args)
		{
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", usernameEntry.Text);
			fullnameLabel.Text = String.Format ("{0} {1}", firstNameEntry.Text, lastNameEntry.Text);
			
		}

		private void OnPasswordClicked (object o, EventArgs args)
		{
			PasswordDialog pd = new PasswordDialog ();

			if (!passwordEntry.Text.Equals ("") && pd.UnixPassword.Equals (""))
				return;

			passwordEntry.Text = pd.UnixPassword;
			_smbLM = pd.LMPassword;
			_smbNT = pd.NTPassword;
		}

		private void modifyGroup (LdapEntry groupEntry, LdapModification[] mods)
		{
			if (groupEntry == null)
				return;

			try
			{
				_conn.Modify (groupEntry.DN, mods);
			}
			catch (Exception e)
			{
				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to modify group ") + groupEntry.DN;

				errorMsg += "\nError: " + e.Message;

				Util.MessageBox (newUserDialog, errorMsg, MessageType.Error);
			}
		}

		private void updateGroupMembership ()
		{
			LdapEntry groupEntry = null;
			LdapModification[] mods = new LdapModification [1];

			foreach (string key in _memberOfGroups.Keys)
			{
				LdapAttribute attr = new LdapAttribute ("memberUid", usernameEntry.Text);
				LdapModification lm = new LdapModification (LdapModification.ADD, attr);

				groupEntry = (LdapEntry) _allGroups[key];

				mods[0] = lm;
			}

			modifyGroup (groupEntry, mods);
		}

		private string getGidNumber (string name)
		{
			if (name == null)
				return null;

			LdapEntry le = (LdapEntry) _allGroups [name];		
			LdapAttribute attr = le.getAttribute ("gidNumber");

			if (attr != null)
				return attr.StringValue;
			
			return null;
		}

		private Hashtable getUpdatedUserInfo ()
		{
			Hashtable retVal = new Hashtable ();

			TreeIter iter;
				
			primaryGroupComboBox.GetActiveIter (out iter);

			string pg = (string) primaryGroupComboBox.Model.GetValue (iter, 0);

			retVal.Add ("givenName", firstNameEntry.Text);
			retVal.Add ("sn", lastNameEntry.Text);
			retVal.Add ("uid", usernameEntry.Text);
			retVal.Add ("uidNumber", uidSpinButton.Value.ToString());
			retVal.Add ("userPassword", passwordEntry.Text);
			retVal.Add ("gidNumber", getGidNumber(pg));
			retVal.Add ("loginShell", shellEntry.Text);
			retVal.Add ("homeDirectory", homeDirEntry.Text);
			retVal.Add ("displayName", displayNameEntry.Text);
			retVal.Add ("initials", initialsEntry.Text);

			return retVal;
		}
	
		private void OnOkClicked (object o, EventArgs args)
		{
			Hashtable cui = getUpdatedUserInfo ();

			string[] objClass = { "top", "posixaccount","inetorgperson", "person" };
			string[] missing = null;

			if (!checkReqAttrs (objClass, cui, out missing))
			{
				missingAlert (missing);
				missingValues = true;

				return;
			}

			string fullName = (string)cui["displayName"];

			cui["cn"] = fullName;
			cui["gecos"] = fullName;

			ArrayList attrList = getAttributes (objClass, userAttrs, cui);

			LdapAttribute attr;

			attr = new LdapAttribute ("cn", fullName);
			attrList.Add (attr);

			attr = new LdapAttribute ("gecos", fullName);
			attrList.Add (attr);

			if (enableSambaButton.Active)
			{
				int user_rid = Convert.ToInt32 (uidSpinButton.Value) * 2 + 1000;

				ArrayList smbMods = Util.CreateSambaMods (
							user_rid, 
							_smbSID,
							_smbLM,
							_smbNT);

				foreach (LdapModification l in smbMods)
				{
					if (l.Attribute.Name.Equals ("objectclass"))
					{
						LdapAttribute a = (LdapAttribute) attrList[0];
						a.addValue ("sambaSAMAccount");

						attrList[0] = a;
					}
					else
						attrList.Add (l.Attribute);
				}
			}

			SelectContainerDialog scd = 
				new SelectContainerDialog (_conn, newUserDialog);

			scd.Title = "Save User";
			scd.Message = String.Format ("Where in the directory would\nyou like save the user\n{0}?", fullName);

			scd.Run ();

			if (scd.DN == "")
				return;

			string userDN = String.Format ("cn={0},{1}", fullName, scd.DN);

			updateGroupMembership ();

			Util.AddEntry (_conn, _viewDialog, userDN, attrList, true);

			newUserDialog.HideAll ();
		}
	}
}
