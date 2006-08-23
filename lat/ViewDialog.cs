// 
// lat - ViewDialog.cs
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

using System;
using System.Collections.Generic;
using Gtk;
using Novell.Directory.Ldap;

namespace lat
{
	public class ViewDialog
	{
		protected Connection conn;
		protected Gtk.Dialog viewDialog;
		protected bool missingValues = false;
		protected bool errorOccured = false;
		protected string defaultNewContainer = null;
		
		public ViewDialog (Connection connection, string newContainer)
		{
			conn = connection;
			defaultNewContainer = newContainer;
		}

		public void missingAlert (string[] missing)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString ("You must provide values for the following attributes:\n\n"));

			foreach (string m in missing)
				msg += String.Format ("{0}\n", m);

			HIGMessageDialog dialog = new HIGMessageDialog (
					viewDialog,
					0,
					Gtk.MessageType.Warning,
					Gtk.ButtonsType.Ok,
					"Attributes missing",
					msg);

			dialog.Run ();
			dialog.Destroy ();
		}
	}
}
