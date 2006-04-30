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
	public static Gnome.Program latProgram;
	public static Gdk.Pixbuf latIcon;
	public static bool Debug = false;
	public static bool VerboseMessages;
	
	public static TemplateManager theTemplateManager;
	public static ViewPluginManager viewPluginManager;
}

public class LdapAdministrationTool
{
	public static void printUsage ()
	{
		string usage = Defines.PACKAGE + " " + Defines.VERSION + "\n" +
			"Web page: http://dev.mmgsecurity.com/projects/lat/\n" +
			"Copyright 2005 MMG Security, Inc.\n\n";

		usage += 
			"Usage: " + Defines.PACKAGE + " [OPTIONS]\n\n" +
			"Options:\n" +
			"  -d,  --debug\t\t\tTurn on debugging messages.\n" +
			"  -v,  --version\t\tPrint version and exit.\n" +
			"  -h,  --help\t\t\tPrint this usage message.\n";

		Console.WriteLine (usage);
	}

	public static void printVersion ()
	{
		string version = Defines.PACKAGE + " " + Defines.VERSION + "\n\n" +
			"Copyright 2005 MMG Security, Inc.\n" +
			"This is free software; see the source for copying conditions. There is NO\n" +
			"warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.\n";

		Console.WriteLine (version);
	}

	public static void Main (string[] args)
	{
		// Parse command-line arguments 			
		int i = 0;

		while (i < args.Length) {

			string arg = args[i];
			++i;

			switch (arg) {

			case "-d":
			case "--debug":
				Global.Debug = true;
				break;

			case "-h":
			case "--help":
				printUsage ();
				Environment.Exit (0);
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

		try {
			Util.SetProcessName (Defines.PACKAGE);
		} catch {}

		Application.Init ();

		Global.latIcon = Gdk.Pixbuf.LoadFromResource ("lat.png");

		Global.theTemplateManager = new TemplateManager ();
		Global.theTemplateManager.Load ();

		Global.viewPluginManager = new ViewPluginManager ();

		Global.latProgram = new Program (Defines.PACKAGE, Defines.VERSION, Modules.UI, args);

		Mono.Unix.Catalog.Init (Defines.PACKAGE, Defines.LOCALE_DIR);

		new ConnectDialog ();	
		Global.latProgram.Run ();

		Global.theTemplateManager.Save ();
		Global.viewPluginManager.SavePluginsState ();

		Logger.Log.Debug ("Exiting {0}", Defines.PACKAGE);
	}
}
