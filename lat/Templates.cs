// 
// lat - Templates.cs
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
using System.Collections;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

namespace lat
{
	[Serializable]
	public class Template
	{
		private string _name;
		private ArrayList _objClasses;
		private Hashtable _attributes;

		public Template (string name)
		{
			_name = name;
			_objClasses = new ArrayList ();
			_attributes = new Hashtable ();
		}

		public void AddClass (ArrayList classes)
		{
			_objClasses.Clear ();
			_objClasses = classes;
		}

		public void AddClass (string name)
		{
			_objClasses.Add (name);
		}

		public void ClearAttributes ()
		{
			_attributes.Clear ();
		}

		public void AddAttribute (string attrName, string attrValue)
		{
			_attributes.Add (attrName, attrValue);
		}

		public string GetAttributeDefaultValue (string attrName)
		{
			return (string) _attributes [attrName]; 
		}

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		public string[] Classes
		{
			get { return (string[]) _objClasses.ToArray (typeof (string)); }
		}
	}

	public class TemplateManager
	{
		private string _configFile;
		private ArrayList _templates;

		public TemplateManager ()
		{
			_templates = new ArrayList ();
		
			string dir = Environment.GetEnvironmentVariable("HOME");
			string tmp = Path.Combine (dir, ".lat");
			
			_configFile = Path.Combine (tmp, "templates.dat");

			DirectoryInfo di = new DirectoryInfo (tmp);
			if (!di.Exists)
				di.Create ();
		}

		public string[] GetTemplateNames ()
		{
			ArrayList tmp = new ArrayList ();

			foreach (Template t in _templates)
				tmp.Add (t.Name);

			return (string[]) tmp.ToArray (typeof (string));
		}

		public void Add (Template t)
		{
			_templates.Add (t);
		}

		public void Update (Template t)
		{
			for (int i = 0; i < _templates.Count; i++) {
				Template p = (Template) _templates[i];
				
				if (t.Name.Equals (p.Name)) {
					_templates[i] = t;
					break;
				}
			}
		}

		public void Delete (string name)
		{
			ArrayList tmp = new ArrayList ();

			foreach (Template t in _templates) {
				if (!t.Name.Equals (name))
					tmp.Add (t);
			}

			_templates.Clear ();
			_templates = tmp;
		}

		public Template Lookup (string name)
		{
			Template retVal = null;

			foreach (Template t in _templates) {
				if (t.Name.Equals (name)) {
					retVal = t;
					break;
				}
			}

			return retVal;
		}

		public void Load ()
		{
			try {

				Stream stream = File.OpenRead (_configFile);

				IFormatter formatter = new BinaryFormatter();
				_templates = (ArrayList) formatter.Deserialize (stream);
				stream.Close ();

				Logger.Log.Debug ("Load templates count: " + _templates.Count);

			} catch (Exception e) {

				Logger.Log.Debug ("TemplateManager.Load: {0}", e.Message);
			}
		}
		
		public void Save ()
		{	
			try {

				Logger.Log.Debug ("Save templates count: " + _templates.Count);

				Stream stream = File.OpenWrite (_configFile);
			
				IFormatter formatter = new BinaryFormatter ();
				formatter.Serialize (stream, _templates); 
				stream.Close ();

			} catch (Exception e) {

				Logger.Log.Debug ("TemplateManager.Save: {0}", e.Message);
			}
		}
	}
}
