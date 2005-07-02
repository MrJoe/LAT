// 
// lat - AboutDialog.cs
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
	public class AboutDialog
	{
		private static string[] _author = { "Loren Bandiera" };
		private static string[] _docs = { "Loren Bandiera" };
		private static string _translators = "Pablo Borges (pt_BR)";
		private static string _desc = "LDAP Administration Tool";
		private static string _copy = "Copyright 2005 MMG Security Inc.";

		public AboutDialog ()
		{
// FIXME: Need a way to specify the author
//			Gtk.AboutDialog ab = new Gtk.AboutDialog ();
//			ab.Copyright = _copy;
//			ab.Comments = _desc;
//			ab.Name = Defines.PACKAGE;
//			ab.TranslatorCredits = _translators;
//			ab.Version = Defines.VERSION;

			Gnome.About ab = new Gnome.About (
				Defines.PACKAGE,
				Defines.VERSION,
				_copy,
				_desc,
				_author,
				_docs, 
				_translators, 
				null);

			ab.Show ();
		}

		public static void Show ()
		{
			new AboutDialog ();
		}
	}
}
