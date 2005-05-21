// 
// lat - Main.cs
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
using Gnome;
using System;
using lat;

public class Global
{
	public static bool Debug = false;
}

public class LdapAdministrationTool
{
	public static void printVersion ()
	{
		Console.WriteLine ("{0} {1}\n", Defines.PACKAGE, Defines.VERSION);
		Console.WriteLine ("Copyright 2005 MMG Security, Inc.");
		Console.WriteLine ("This is free software; see the source for copying conditions. There is NO");
		Console.WriteLine ("warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.\n");
	}

	public static void Main (string[] args)
	{
		Application.Init ();

		// Parse command-line arguments 			
		int i = 0;

		// loop taken from beagled
		while (i < args.Length)
		{
			string arg = args[i];
			++i;

			string next_arg = i < args.Length ? args[i] : null;

			switch (arg)
			{
				case "-d":
				case "--debug":
					Global.Debug = true;
					break;

				case "-v":
				case "--version":
					printVersion ();
					Environment.Exit (0);
					break;

				default:
					Console.WriteLine ("Unknown argument '{0}'", arg);
					break;
			}
		}

		Logger.Log.Debug ("Starting {0} (version {1})", Defines.PACKAGE, Defines.VERSION);

		Program program = new Program (
			Defines.PACKAGE, Defines.VERSION, Modules.UI, args);

		Mono.Posix.Catalog.Init (
			Defines.PACKAGE, 
			Defines.LOCALE_DIR);

		new ConnectDialog ();
		
		program.Run ();

		Logger.Log.Debug ("Exiting {0}", Defines.PACKAGE);
	}
}
