// 
// lat - ViewManager.cs
// Author: Loren Bandiera
// Copyright 2006 MMG Security, Inc.
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
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Gtk;
using Novell.Directory.Ldap;

namespace lat {

	[Serializable]
	public struct ViewPluginConfig
	{
		public string[] ColumnNames;
		public string[] ColumnAttributes;
		public string DefaultNewContainer;
		public string Filter;
		public string SearchBase;
	}

	public abstract class ViewPlugin
	{
		protected ViewPluginConfig config;
		
		public ViewPlugin ()
		{
		}
	
		// Methods
		public void Deserialize (string stateFileName)
		{
			try {

				Stream stream = File.OpenRead (stateFileName);

				IFormatter formatter = new BinaryFormatter();
				this.config = (ViewPluginConfig) formatter.Deserialize (stream);
				stream.Close ();

			} catch (Exception e) {

				Logger.Log.Debug ("ViewPlugin.Deserialize: {0}", e.Message);
			}
		}
		
		public void Serialize (string stateFileName)
		{
			try {

				Stream stream = File.OpenWrite (stateFileName);
			
				IFormatter formatter = new BinaryFormatter ();
				formatter.Serialize (stream, this.config); 
				stream.Close ();

			} catch (Exception e) {

				Logger.Log.Debug ("ViewPlugin.Serialize: {0}", e.Message);
			}
		}
		
		public abstract void Init ();
		public abstract void OnAddEntry (LdapServer server);
		public abstract void OnEditEntry (LdapServer server, LdapEntry le);
		public abstract void OnPopupShow (Menu popup);
			
		// Properties		
		public string[] ColumnAttributes 
		{ 
			get { return config.ColumnAttributes; }
			set { config.ColumnAttributes = value; }
		}
		
		public string[] ColumnNames 
		{
			get { return config.ColumnNames; }
			set { config.ColumnNames = value; } 
		}
		
		public string DefaultNewContainer
		{
			get { return config.DefaultNewContainer; }
			set { config.DefaultNewContainer = value; }
		}
		
		public string Filter
		{
			get { return config.Filter; }
			set { config.Filter = value; }
		}

		public string SearchBase
		{
			get { return config.SearchBase; }
			set { config.SearchBase = value; }
		}
		
		public abstract string[] Authors { get; }		
		public abstract string Copyright { get; }
		public abstract string Description { get; }		
		public abstract string Name { get; }
		public abstract string Version { get; }
		public abstract Gdk.Pixbuf Icon { get; }		
	}
	
	public class ViewPluginManager
	{
		string pluginDirectory;
		string pluginStateDirectory;		
		ArrayList pluginList;
//		FileSystemWatcher sysPluginWatch;
//		FileSystemWatcher usrPluginWatch;
	
		public ViewPluginManager ()
		{		
			pluginList = new ArrayList ();
			
			string homeDir = Path.Combine (Environment.GetEnvironmentVariable("HOME"), ".lat");
			DirectoryInfo di = new DirectoryInfo (homeDir);
			if (!di.Exists)
				di.Create ();
				
			pluginStateDirectory = homeDir;
			pluginDirectory = Path.Combine (homeDir, "plugins");

			DirectoryInfo dir = new System.IO.DirectoryInfo (pluginDirectory);
			foreach (FileInfo f in dir.GetFiles("*.dll")) {
				
				Assembly asm = Assembly.LoadFrom (f.FullName);
				
				Type [] types = asm.GetTypes ();
				foreach (Type type in types) {
					if (type.IsSubclassOf (typeof (ViewPlugin))) {						
						ViewPlugin plugin = (ViewPlugin) Activator.CreateInstance (type);						
						if (plugin == null)
							continue;
						
						pluginList.Add (plugin);
						Logger.Log.Debug ("Loaded plugin: {0}", type.FullName);
					}
				}
   			}

			foreach (ViewPlugin vp in pluginList) {
				string fileName = vp.GetType() + ".state";
				vp.Deserialize (Path.Combine (pluginStateDirectory, fileName));
			}

			
//			try {
//			
//				usrPluginWatch = new FileSystemWatcher (pluginDirectory, "*.dll");
//				usrPluginWatch.Created += OnPluginCreated;
//				usrPluginWatch.Changed += OnPluginChanged;
//				usrPluginWatch.Deleted += OnPluginDeleted;
//				usrPluginWatch.Renamed += OnPluginRenamed;
//				usrPluginWatch.EnableRaisingEvents = true;
			
//				sysPluginWatch = new FileSystemWatcher (Defines.SYS_PLUGIN_DIR, "*.dll");
//				sysPluginWatch.Created += OnPluginCreated;
//				sysPluginWatch.Deleted += OnPluginDeleted;
//				sysPluginWatch.EnableRaisingEvents = true;
			
//			} catch (Exception e) {
			
//				Console.WriteLine (e);
			
//			}
		}

		public ViewPlugin Find (string name)
		{
			foreach (ViewPlugin vp in pluginList)
				if (vp.Name == name)
						return vp;
						
			return null;
		}

		public void SavePluginsState ()
		{
			foreach (ViewPlugin vp in pluginList) {
				string fileName = vp.GetType() + ".state";
				vp.Serialize (Path.Combine (pluginStateDirectory, fileName));
			}
		}

		public ViewPlugin[] Plugins
		{
			get { return (ViewPlugin[]) pluginList.ToArray (typeof (ViewPlugin)); }
		}
		
//		void OnPluginCreated (object o, FileSystemEventArgs args)
//		{
//			Console.WriteLine ("args.FullPath: {0}", args.FullPath);
//		}
	}
}
