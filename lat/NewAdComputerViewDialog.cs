// 
// lat - NewAdComputerViewDialog.cs
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
	public class NewAdComputerViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog newAdComputerDialog;
//		[Glade.Widget] Gtk.Label computerNameLabel;
		[Glade.Widget] Gtk.Entry computerNameEntry;
		[Glade.Widget] Gtk.Entry dnsNameEntry;

		private static string[] hostAttrs = { "cn", "dNSHostName" };

		public NewAdComputerViewDialog (lat.Connection conn) : base (conn)
		{
			Init ();

			newAdComputerDialog.Title = "Add Computer";

			newAdComputerDialog.Run ();

			if (missingValues)
			{
				missingValues = false;
				newAdComputerDialog.Run ();				
			}
			else
			{
				newAdComputerDialog.Destroy ();
			}
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "newAdComputerDialog", null);
			ui.Autoconnect (this);

			_viewDialog = newAdComputerDialog;		
		}

		private Hashtable getCurrentHostInfo ()
		{
			Hashtable retVal = new Hashtable ();

			retVal.Add ("cn", computerNameEntry.Text);
			retVal.Add ("dNSHostName", dnsNameEntry.Text);

			return retVal;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			Hashtable chi = getCurrentHostInfo ();

			string[] missing = null;
			string[] objClass = {"top", "computer"};

			if (!checkReqAttrs (objClass, chi, out missing))
			{
				missingAlert (missing);
				return;
			}

			ArrayList attrList = getAttributes (objClass, hostAttrs, chi);

			SelectContainerDialog scd = 
				new SelectContainerDialog (_conn, newAdComputerDialog);

			scd.Title = "Save Computer";
			scd.Message = String.Format ("Where in the directory would\nyou like save the computer\n{0}?", (string)chi["cn"]);

			scd.Run ();

			if (scd.DN == "")
				return;

			string userDN = String.Format ("cn={0},{1}", (string)chi["cn"], scd.DN);

			Util.AddEntry (_conn, _viewDialog, userDN, attrList, true);

			newAdComputerDialog.HideAll ();
		}
	}
}
