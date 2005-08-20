// 
// lat - SIDDialog.cs
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

namespace lat 
{
	public class SIDDialog
	{
		[Glade.Widget] Gtk.Dialog sidDialog;
		[Glade.Widget] Gtk.Entry sidEntry;
		[Glade.Widget] Gtk.Button okButton;
		[Glade.Widget] Gtk.Button cancelButton;

		private Glade.XML ui;

		private string _sid = "";

		public SIDDialog (Gtk.Window parent)
		{
			ui = new Glade.XML (null, "lat.glade", "sidDialog", null);
			ui.Autoconnect (this);

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);
		}

		public void Run ()
		{
			sidDialog.Run ();
			sidDialog.Destroy ();
		}

		public string SID
		{
			get { return _sid; }
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			_sid = sidEntry.Text;
			sidDialog.HideAll ();
		}
	
		private void OnCancelClicked (object o, EventArgs args)
		{
			sidDialog.HideAll ();
		}
	}
}
