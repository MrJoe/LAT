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

using System;
using Gtk;

namespace lat 
{
	public class AboutDialog
	{
		static string[] _author = { "Loren Bandiera" };
		static string[] _docs = { "Loren Bandiera" };
		static string _translators = "Pablo Borges (pt_BR)\nThomas Constans (fr_FR)";
		static string _desc = "LDAP Administration Tool";
		static string _copy = "Copyright \xa9 2005-2006 MMG Security Inc.";

		public AboutDialog ()
		{
			Gtk.AboutDialog ab = new Gtk.AboutDialog ();
			ab.Authors = _author;
			ab.Comments = _desc;
			ab.Copyright = _copy;
			ab.Documenters = _docs;
			ab.ProgramName = Defines.PACKAGE;
			ab.TranslatorCredits = _translators;
			ab.Version = Defines.VERSION;
			ab.Icon = Global.latIcon;

			ab.Run ();
			ab.Destroy ();
		}
	}
}
