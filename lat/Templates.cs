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
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Soap;

namespace lat
{
	[Serializable]
	public class Template
	{
		private string _name;
		private ArrayList _objClasses;
		private NameValueCollection _attributes;

		public Template (string name)
		{
			_name = name;
			_objClasses = new ArrayList ();
			_attributes = new NameValueCollection ();
		}

		public void AddClass (string name)
		{
			_objClasses.Add (name);
		}

		public void AddAttribute (string attrName, string attrValue)
		{
			_attributes.Add (attrName, attrValue);
		}

		public string[] GetAttributeDefaultValues (string attrName)
		{
			return _attributes.GetValues (attrName); 
		}

		public string Name
		{
			get { return _name; }
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
			
			_configFile = Path.Combine (tmp, "templates.xml");

			DirectoryInfo di = new DirectoryInfo (tmp);
			if (!di.Exists)
			{
				di.Create ();
			}
		}

		public void Add (Template t)
		{
			_templates.Add (t);
		}

		public void Delete (string name)
		{
			foreach (Template t in _templates)
			{
				if (t.Name.Equals (name))
				{
					_templates.Remove (t);
					break;
				}
			}
		}

		public Template Lookup (string name)
		{
			Template retVal = null;

			foreach (Template t in _templates)
			{
				if (t.Name.Equals (name))
				{
					retVal = t;
					break;
				}
			}

			return retVal;
		}

		public void Load ()
		{
			try
			{
				Stream stream = File.OpenRead (_configFile);

				IFormatter formatter = new SoapFormatter();
				_templates = (ArrayList) formatter.Deserialize (stream);
				stream.Close ();

				Logger.Log.Debug ("Load templates count: " + _templates.Count);
			} 
			catch {}
		}
		
		public void Save ()
		{	
			try
			{
				Logger.Log.Debug ("Save scanners count: " + _templates.Count);

				Stream stream = File.OpenWrite (_configFile);
			
				IFormatter formatter = new SoapFormatter ();
				formatter.Serialize (stream, _templates); 
				stream.Close ();
			}
			catch {}
		}
	}
}
