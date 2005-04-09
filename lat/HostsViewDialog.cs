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
using GLib;
using Glade;
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
		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private bool _isEdit;
		
		private LdapEntry _le;
		private ArrayList _modList;
		private Hashtable _hi;

		private static string[] hostAttrs = { "cn", "ipHostNumber", "description" };

		public HostsViewDialog (lat.Connection conn) : base (conn)
		{
			Init ();

			hostDialog.Title = "LAT - Add Host";

			hostDialog.Run ();
			hostDialog.Destroy ();
		}

		public HostsViewDialog (lat.Connection conn, LdapEntry le) : base (conn)
		{
			_le = le;
			_modList = new ArrayList ();

			_isEdit = true;

			Init ();

			_hi = getEntryInfo (hostAttrs, le);

			hostDialog.Title = "LAT - Edit Host";

			hostNameEntry.Text = (string) _hi["cn"];
			ipEntry.Text = (string) _hi["ipHostNumber"];
			descriptionEntry.Text = (string) _hi["description"];

			hostDialog.Run ();
			hostDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "hostDialog", null);
			ui.Autoconnect (this);

			_viewDialog = hostDialog;
		
			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			hostDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);
		}

		private Hashtable getCurrentHostInfo ()
		{
			Hashtable retVal = new Hashtable ();

			retVal.Add ("cn", hostNameEntry.Text);
			retVal.Add ("ipHostNumber", ipEntry.Text);
			retVal.Add ("description", descriptionEntry.Text);

			return retVal;
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			Hashtable chi = getCurrentHostInfo ();

			if (_isEdit)
			{
				_modList = getMods (hostAttrs, _hi, chi);

				Util.ModifyEntry (_conn, _viewDialog, _le.DN, _modList);
			}
			else
			{
				string[] missing = null;
				string[] objClass = {"top", "ipHost", "device"};

				if (!checkReqAttrs (objClass, chi, out missing))
				{
					missingAlert (missing);
					return;
				}

				ArrayList attrList = getAttributes (objClass, hostAttrs, chi);

				SelectContainerDialog scd = 
					new SelectContainerDialog (_conn, hostDialog);

				scd.Title = "Save Host";
				scd.Message = String.Format ("Where in the directory would\nyou like save the host\n{0}?", (string)chi["cn"]);

				scd.Run ();

				if (scd.DN == "")
					return;

				string userDN = String.Format ("cn={0},{1}", (string)chi["cn"], scd.DN);

				Util.AddEntry (_conn, _viewDialog, userDN, attrList);
			}

			hostDialog.HideAll ();
		}
	}
}
