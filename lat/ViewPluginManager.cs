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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Gtk;
using Novell.Directory.Ldap;

namespace lat 
{
	[Serializable]
	public struct ViewPluginConfig
	{
		public string PluginName;
		public string[] ColumnNames;
		public string[] ColumnAttributes;
		public string DefaultNewContainer;
		public string Filter;
		public string SearchBase;
	}

	[Serializable]
	public class PluginConfigCollection : ICollection<ViewPluginConfig>
	{
		Dictionary<string,ViewPluginConfig> pluginConfigs;

		public PluginConfigCollection ()
		{
			pluginConfigs = new Dictionary<string,ViewPluginConfig> ();
		}

		public void Add (ViewPluginConfig vpc)
		{	
			pluginConfigs.Add (vpc.PluginName, vpc);
		}

		public void Clear ()
		{
			pluginConfigs.Clear ();
		}

		public bool Contains (ViewPluginConfig vpc)
		{
			if (pluginConfigs.ContainsValue (vpc))
				return true;

			return false;
		}

		public bool Contains (string name)
		{
			if (pluginConfigs.ContainsKey (name))
				return true;
				
			return false;
		}

		public void CopyTo (ViewPluginConfig[] array, int arrayIndex)
		{
			int count = 0;

			foreach (KeyValuePair<string,ViewPluginConfig> kvp in pluginConfigs) {
				array [arrayIndex + count] = kvp.Value;
				count++;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return pluginConfigs.GetEnumerator ();
		}

		public IEnumerator<ViewPluginConfig> GetEnumerator ()
		{
			return pluginConfigs.Values.GetEnumerator ();
		}

		public void Remove (string name)
		{
			if (pluginConfigs.ContainsKey (name))
				pluginConfigs.Remove (name);
		}

		public bool Remove (ViewPluginConfig vpc)
		{
			return pluginConfigs.Remove (vpc.PluginName);
		}

		public void Update (ViewPluginConfig vpc)
		{	
			pluginConfigs[vpc.PluginName] = vpc;
		} 

		public int Count
		{
			get { return pluginConfigs.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}
		
		public ViewPluginConfig this [string name]
		{
			get { return pluginConfigs [name]; }			
			set { pluginConfigs[name] = value; }
		}
	}

	public abstract class ViewPlugin
	{
		protected ViewPluginConfig config;
		
		public ViewPlugin ()
		{
		}
	
		// Methods		
		public abstract void Init ();
		public abstract void OnAddEntry (Connection conn);
		public abstract void OnEditEntry (Connection conn, LdapEntry le);
		public abstract void OnPopupShow (Menu popup);
			
		// Properties
		public ViewPluginConfig PluginConfiguration
		{			
			get { return config; }
			set { config = value; }
		}
		
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
		
		public abstract string MenuLabel { get; }
		public abstract AccelKey MenuKey { get; }
		public abstract Gdk.Pixbuf Icon { get; }		
	}

	public enum ViewerDataType : int { Binary, String };

	public abstract class AttributeViewPlugin
	{
		public AttributeViewPlugin ()
		{
		}
			
		public abstract void OnActivate (string attributeName, string attributeData);
		public abstract void OnActivate (string attributeName, byte[] attributeData);
	
		public abstract string[] AttributeNames { get; }
		public abstract string StringValue { get; }
		public abstract byte[] ByteValue { get; }

		public abstract ViewerDataType DataType { get; }
				
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
		string pluginStateFile;
		
		List<ViewPlugin> viewPluginList;
		List<AttributeViewPlugin> attrPluginList;
		Dictionary<string,string> viewPluginHash;

		Dictionary<string,PluginConfigCollection> serverViewConfig;

		FileSystemWatcher sysPluginWatch;
		FileSystemWatcher usrPluginWatch;
	
		public PluginManager ()
		{		
			viewPluginList = new List<ViewPlugin> ();
			attrPluginList = new List<AttributeViewPlugin> ();
			viewPluginHash = new Dictionary<string,string> ();
			
			serverViewConfig = new Dictionary<string,PluginConfigCollection> (); 
			
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
			pluginStateFile = Path.Combine (pluginStateDirectory, "plugins.state");
			pluginDirectory = Path.Combine (homeDir, "plugins");

			dir = new System.IO.DirectoryInfo (pluginDirectory);
			if (dir.Exists)
				foreach (FileInfo f in dir.GetFiles("*.dll"))
					LoadPluginsFromFile (f.FullName);

			// Login plugin states (if any)
			Load ();

			// Watch for any plugins to be added/removed
			try {
					
				sysPluginWatch = new FileSystemWatcher (Defines.SYS_PLUGIN_DIR, "*.dll");
				sysPluginWatch.Created += OnPluginCreated;
				sysPluginWatch.Deleted += OnPluginDeleted;
				sysPluginWatch.EnableRaisingEvents = true;
			
			} catch (Exception e) {			
				Log.Debug ("Plugin system watch error: {0}", e);			
			}

			try {
			
				usrPluginWatch = new FileSystemWatcher (pluginDirectory, "*.dll");
				usrPluginWatch.Created += OnPluginCreated;
				usrPluginWatch.Deleted += OnPluginDeleted;
				usrPluginWatch.EnableRaisingEvents = true;
			
			} catch (Exception e) {			
				Log.Debug ("Plugin user dir watch error: {0}", e);			
			}
		}

		void OnPluginCreated (object sender, FileSystemEventArgs args)
		{
			Log.Debug ("New plugin found: {0}", Path.GetFileName (args.FullPath));			
			LoadPluginsFromFile (args.FullPath);
		}
		
		void OnPluginDeleted (object sender, FileSystemEventArgs args)
		{
			// FIXME: remove plugin
			Log.Debug ("Plugin deleted: {0}", Path.GetFileName (args.FullPath));			
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
					viewPluginHash.Add (plugin.MenuLabel, plugin.Name);			
					Log.Debug ("Loaded plugin: {0}", type.FullName);
					
				} else if (type.IsSubclassOf (typeof (AttributeViewPlugin))) {
					AttributeViewPlugin plugin = (AttributeViewPlugin) Activator.CreateInstance (type);						
					if (plugin == null)
						continue;
						
					attrPluginList.Add (plugin);
					Log.Debug ("Loaded plugin: {0}", type.FullName);				
				}
			}		
		}

		public ViewPlugin GetViewPlugin (string pluginName, string configName)
		{
			ViewPlugin retVal = null;

			string labelKey = null;
			if (viewPluginHash.ContainsKey (pluginName))
				labelKey = viewPluginHash [pluginName];

			foreach (ViewPlugin vp in viewPluginList) {		
				if (vp.Name == pluginName || vp.Name == labelKey)
					retVal = vp;
			}

			if (retVal != null && serverViewConfig.ContainsKey (configName)) {
				
				PluginConfigCollection pcc = serverViewConfig [configName];
				if (pcc.Contains (pluginName)) {
					ViewPluginConfig vpc = pcc [pluginName];			
					retVal.PluginConfiguration = vpc;
				}
			}

			return retVal;
		}

		public AttributeViewPlugin FindAttributeView (string name)
		{
			foreach (AttributeViewPlugin avp in attrPluginList)
				if (avp.Name == name)
						return avp;
						
			return null;
		}

		public void Load ()
		{
			if (!File.Exists (pluginStateFile))
				return;
		
			try {

				Stream stream = File.OpenRead (pluginStateFile);

				IFormatter formatter = new BinaryFormatter();
				this.serverViewConfig = (Dictionary<string,PluginConfigCollection>) formatter.Deserialize (stream);
				stream.Close ();

				Log.Debug ("Loaded {0} configs from plugins.state", this.serverViewConfig.Count);

			} catch (Exception e) {
				Log.Error ("Error loading plugin state: {0}", e.Message);
				Log.Debug (e);
			}		
		}

		public void Save ()
		{
			try {
			
				Stream stream = File.OpenWrite (pluginStateFile);
			
				IFormatter formatter = new BinaryFormatter ();
				formatter.Serialize (stream, this.serverViewConfig); 
				stream.Close ();
	
				Log.Debug ("Saved {0} configs to plugins.state", this.serverViewConfig.Count);

			} catch (Exception e) {
				Log.Error ("Error saving plugin state: {0}", e.Message);
				Log.Debug (e);
			}			
		}

		public void SetPluginConfiguration (string connName, ViewPluginConfig config)
		{
			if (serverViewConfig.ContainsKey (connName)) {
			
				PluginConfigCollection pcc = serverViewConfig [connName];
				pcc.Update (config);
			
			} else {
			
				PluginConfigCollection pcc = new PluginConfigCollection ();
				pcc.Add (config);				
				
				serverViewConfig.Add (connName, pcc);
			}				
		}

		public Dictionary<string,PluginConfigCollection> ServerViewConfig
		{
			get { return serverViewConfig; }
			set { serverViewConfig = value; }
		}

		public ViewPlugin[] ServerViewPlugins
		{
			get { return viewPluginList.ToArray (); }
		}
		
		public AttributeViewPlugin[] AttributeViewPlugins
		{
			get { return attrPluginList.ToArray (); }
		}
	}
}
