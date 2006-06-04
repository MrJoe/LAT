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

				Logger.Log.Debug (e.ToString());
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

				Logger.Log.Debug (e.ToString());
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

	public abstract class AttributeViewPlugin
	{
		public AttributeViewPlugin ()
		{
		}
		
		public abstract void Init ();
		
		public abstract void OnActivate (string attributeData);
		public abstract void OnActivate (byte[] attributeData);

		public abstract void GetData (out string userData);
		public abstract void GetData (out byte[] userData);	
		
		public abstract string AttributeName { get; }
		public abstract string[] Authors { get; }		
		public abstract string Copyright { get; }
		public abstract string Description { get; }		
		public abstract string Name { get; }
		public abstract string Version { get; }		
	}
	
	public class PluginManager
	{
		string pluginDirectory;
		string pluginStateDirectory;		
		
		ArrayList viewPluginList;
		ArrayList attrPluginList;

		FileSystemWatcher sysPluginWatch;
		FileSystemWatcher usrPluginWatch;
	
		public PluginManager ()
		{		
			viewPluginList = new ArrayList ();
			attrPluginList = new ArrayList ();
			
			string homeDir = Path.Combine (Environment.GetEnvironmentVariable("HOME"), ".lat");
			DirectoryInfo di = new DirectoryInfo (homeDir);
			if (!di.Exists)
				di.Create ();

			// Load any plugins in sys dir
			DirectoryInfo dir = new System.IO.DirectoryInfo (Defines.SYS_PLUGIN_DIR);
			if (dir.Exists)
				foreach (FileInfo f in dir.GetFiles("*.dll"))
					LoadPluginsFromFile (f.FullName);
			
			// Load any plugins in home dir
			pluginStateDirectory = homeDir;
			pluginDirectory = Path.Combine (homeDir, "plugins");

			dir = new System.IO.DirectoryInfo (pluginDirectory);
			if (dir.Exists)
				foreach (FileInfo f in dir.GetFiles("*.dll"))
					LoadPluginsFromFile (f.FullName);

			foreach (ViewPlugin vp in viewPluginList) {
				string fileName = vp.GetType() + ".state";
				vp.Deserialize (Path.Combine (pluginStateDirectory, fileName));
			}

			// Watch for any plugins to be added/removed
			try {
					
				sysPluginWatch = new FileSystemWatcher (Defines.SYS_PLUGIN_DIR, "*.dll");
				sysPluginWatch.Created += OnPluginCreated;
				sysPluginWatch.Deleted += OnPluginDeleted;
				sysPluginWatch.EnableRaisingEvents = true;
			
			} catch (Exception e) {			
				Logger.Log.Debug ("Plugin system watch error: {0}", e);			
			}

			try {
			
				usrPluginWatch = new FileSystemWatcher (pluginDirectory, "*.dll");
				usrPluginWatch.Created += OnPluginCreated;
				usrPluginWatch.Deleted += OnPluginDeleted;
				usrPluginWatch.EnableRaisingEvents = true;
			
			} catch (Exception e) {			
				Logger.Log.Debug ("Plugin user dir watch error: {0}", e);			
			}
		}

		void OnPluginCreated (object sender, FileSystemEventArgs args)
		{
			Logger.Log.Debug ("New plugin found: {0}", Path.GetFileName (args.FullPath));			
			LoadPluginsFromFile (args.FullPath);
		}
		
		void OnPluginDeleted (object sender, FileSystemEventArgs args)
		{
			// FIXME: remmove plugin
//			Logger.Log.Debug ("Plugin deleted: {0}", Path.GetFileName (args.FullPath));			
		}

		void LoadPluginsFromFile (string fileName)
		{
			Assembly asm = Assembly.LoadFrom (fileName);
				
			Type [] types = asm.GetTypes ();
			foreach (Type type in types) {
				if (type.IsSubclassOf (typeof (ViewPlugin))) {						
					ViewPlugin plugin = (ViewPlugin) Activator.CreateInstance (type);						
					if (plugin == null)
						continue;
						
					viewPluginList.Add (plugin);
					Logger.Log.Debug ("Loaded plugin: {0}", type.FullName);
					
				} else if (type.IsSubclassOf (typeof (AttributeViewPlugin))) {
					AttributeViewPlugin plugin = (AttributeViewPlugin) Activator.CreateInstance (type);						
					if (plugin == null)
						continue;
						
					attrPluginList.Add (plugin);
					Logger.Log.Debug ("Loaded plugin: {0}", type.FullName);				
				}
			}		
		}

		public ViewPlugin FindServerView (string name)
		{
			foreach (ViewPlugin vp in viewPluginList)
				if (vp.Name == name)
						return vp;
						
			return null;
		}

		public AttributeViewPlugin FindAttibuteView (string name)
		{
			foreach (AttributeViewPlugin avp in attrPluginList)
				if (avp.AttributeName == name)
						return avp;
						
			return null;
		}

		public void SavePluginsState ()
		{
			foreach (ViewPlugin vp in viewPluginList) {
				string fileName = vp.GetType() + ".state";
				vp.Serialize (Path.Combine (pluginStateDirectory, fileName));
			}
		}

		public ViewPlugin[] ServerViewPlugins
		{
			get { return (ViewPlugin[]) viewPluginList.ToArray (typeof (ViewPlugin)); }
		}
		
		public AttributeViewPlugin[] AttributeViewPlugins
		{
			get { return (AttributeViewPlugin[]) attrPluginList.ToArray (typeof (AttributeViewPlugin)); }
		}
	}
}
