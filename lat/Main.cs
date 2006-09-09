// 
// lat - Main.cs
// Author: Loren Bandiera
// Copyright 2005-2006 MMG Security, Inc.
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
using System.Text;
using Gtk;
using Gnome;
using lat;

public class Global
{
	public static Gdk.Pixbuf latIcon;
	public static MainWindow Window;
	
	public static ConnectionManager Connections;
	public static PluginManager Plugins;
	public static TemplateManager Templates;

#if ENABLE_NETWORKMANAGER
	public static NetworkDetect Network;
#endif
}

public class LdapAdministrationTool
{
	static void PrintUsage ()
	{
		StringBuilder usage = new StringBuilder ();
		usage.AppendFormat ("{0} {1}\n", Defines.PACKAGE, Defines.VERSION);
		usage.AppendFormat ("Web page: http://dev.mmgsecurity.com/projects/lat/\n");
		usage.AppendFormat ("Copyright 2005-2006 MMG Security, Inc.\n\n");
		usage.AppendFormat ("Usage: {0} [OPTIONS]\n\n", Defines.PACKAGE);
		usage.AppendFormat ("Options:\n");
		usage.AppendFormat ("  -d,  --debug\t\t\tTurn on debugging messages.\n");
		usage.AppendFormat ("  -v,  --version\t\tPrint version and exit.\n");
		usage.AppendFormat ("  -h,  --help\t\t\tPrint this usage message.\n");

		Console.WriteLine (usage.ToString());
	}

	static void PrintVersion ()
	{
		StringBuilder version = new StringBuilder ();
		version.AppendFormat ("{0} {1}\n\n", Defines.PACKAGE, Defines.VERSION);
		version.AppendFormat ("Copyright 2005-2006 MMG Security, Inc.\n");
		version.AppendFormat ("This is free software; see the source for copying conditions. There is NO\n");
		version.AppendFormat ("warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.\n");
		
		Console.WriteLine (version.ToString());
	}

	public static void Main (string[] args)
	{
		int i = 0;
		LogLevel logLevel = LogLevel.Info;

		while (i < args.Length) {

			string arg = args[i];
			++i;

			switch (arg) {

			case "-d":
			case "--debug":
				logLevel = LogLevel.Debug;
				break;

			case "-h":
			case "--help":
				PrintUsage ();
				Environment.Exit (0);
				break;

			case "-v":
			case "--version":
				PrintVersion ();
				Environment.Exit (0);
				break;

			default:
				Console.WriteLine ("Unknown argument '{0}'", arg);
				break;
			}
		}

		Application.Init ();
		
		Log.Initialize (logLevel);		
		Log.Info ("Starting {0} (version {1})", Defines.PACKAGE, Defines.VERSION);

		Util.SetProcessName (Defines.PACKAGE);
		
		if (Util.IsOldConfig())
			Util.UpgradeConfigurationFiles ();
		
		Global.Templates = new TemplateManager ();
		Global.Plugins = new PluginManager ();		
		Global.Connections = new ConnectionManager ();
			
		Mono.Unix.Catalog.Init (Defines.PACKAGE, Defines.LOCALE_DIR);		

		try {
		
			Program program = new Program (Defines.PACKAGE, Defines.VERSION, Modules.UI, args);					
			Global.Window = new MainWindow (program);		
			program.Run ();
		
		} catch (Exception e) {
		
			Log.Debug (e);
			Log.Error ("Error occured: {0}", e.Message);
		}
		
		Global.Templates.Save ();
		Global.Connections.Save ();
		Global.Plugins.Save ();

		Log.Info ("Exiting {0}", Defines.PACKAGE);
	}
}
