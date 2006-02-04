// 
// lat - CertificateDialog.cs
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

namespace lat 
{
	public enum CertDialogResponse { Import, NoImport, Cancel };

	public class CertificateDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog certDialog;
		[Glade.Widget] Gtk.Label certInfoLabel;
		[Glade.Widget] Gtk.RadioButton yesAlwaysRadio;
		[Glade.Widget] Gtk.RadioButton yesSessionRadio;
		[Glade.Widget] Gtk.RadioButton noRadio;

		public CertDialogResponse UserResponse;

		public CertificateDialog (string securityInfo)
		{
			ui = new Glade.XML (null, "lat.glade", "certDialog", null);
			ui.Autoconnect (this);

			certInfoLabel.Text = securityInfo;
			noRadio.Active = true;

			certDialog.Icon = Global.latIcon;
			certDialog.Run ();
			certDialog.Destroy ();
		}

		public void OnOkClicked (object o, EventArgs args) 
		{
			if (yesAlwaysRadio.Active)
				UserResponse = CertDialogResponse.Import;
			else if (yesSessionRadio.Active)
				UserResponse = CertDialogResponse.NoImport;
			else if (noRadio.Active)
				UserResponse = CertDialogResponse.Cancel;
		}
	}
}
