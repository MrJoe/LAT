// 
// lat - HostsViewDialog.cs
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
using System.Collections;
using Novell.Directory.Ldap;

namespace lat
{
	public class HostsViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog hostDialog;
		[Glade.Widget] Gtk.Entry hostNameEntry;
		[Glade.Widget] Gtk.Entry ipEntry;
		[Glade.Widget] Gtk.Entry descriptionEntry;
		[Glade.Widget] Gtk.Image image31;

		private bool _isEdit;
		
		private LdapEntry _le;
		private ArrayList _modList;
		private Hashtable _hi;

		private static string[] hostAttrs = { "cn", "ipHostNumber", "description" };

		public HostsViewDialog (LdapServer ldapServer, string newContainer) : base (ldapServer, newContainer)
		{
			Init ();

			hostDialog.Icon = Global.latIcon;
			hostDialog.Title = "Add Computer";

			hostDialog.Run ();

			while (missingValues || errorOccured) {
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				hostDialog.Run ();				
			}

			hostDialog.Destroy ();
		}

		public HostsViewDialog (LdapServer ldapServer, LdapEntry le) : base (ldapServer, null)
		{
			_le = le;
			_modList = new ArrayList ();

			_isEdit = true;

			Init ();

			server.GetAttributeValuesFromEntry (le, hostAttrs, out _hi);

			string hostName = (string) _hi["cn"];

			hostDialog.Title = hostName + " Properties";

			hostNameEntry.Text = hostName;
			ipEntry.Text = (string) _hi["ipHostNumber"];
			descriptionEntry.Text = (string) _hi["description"];

			hostDialog.Run ();
			hostDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "hostDialog", null);
			ui.Autoconnect (this);

			viewDialog = hostDialog;

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server-48x48.png");
			image31.Pixbuf = pb;
		}

		private Hashtable getCurrentHostInfo ()
		{
			Hashtable retVal = new Hashtable ();

			retVal.Add ("cn", hostNameEntry.Text);
			retVal.Add ("ipHostNumber", ipEntry.Text);
			retVal.Add ("description", descriptionEntry.Text);

			return retVal;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			Hashtable chi = getCurrentHostInfo ();

			string[] missing = null;
			string[] objClass = {"top", "ipHost", "device"};

			if (!checkReqAttrs (objClass, chi, out missing)) {
				missingAlert (missing);
				missingValues = true;

				return;
			}

			if (_isEdit) {
				_modList = getMods (hostAttrs, _hi, chi);

				if (!Util.ModifyEntry (server, viewDialog, _le.DN, _modList, true)) {
					errorOccured = true;
					return;
				}
			} else {

				ArrayList attrList = getAttributes (objClass, hostAttrs, chi);

				SelectContainerDialog scd = 
					new SelectContainerDialog (server, hostDialog);

				scd.Title = "Save Host";
				scd.Message = String.Format ("Where in the directory would\nyou like save the host\n{0}?", (string)chi["cn"]);

				scd.Run ();

				if (scd.DN == "")
					return;

				string userDN = String.Format ("cn={0},{1}", (string)chi["cn"], scd.DN);

				if (!Util.AddEntry (server, viewDialog, userDN, attrList, true)) {
					errorOccured = true;
					return;
				}
			}

			hostDialog.HideAll ();
		}
	}
}
