// 
// lat - CustomViewManager.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
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

namespace lat
{
	public struct CustomViewData
	{
		public string Name;
		public string Filter;
		public string Base;
		public string Cols;

		public CustomViewData (string name, string filter, string searchBase, string cols)
		{
			Name = name;
			Filter = filter;
			Base = searchBase;
			Cols = cols;
		}
	}

	public class CustomViewManager 
	{
		private string _configFile;
		private Hashtable _views;

		public CustomViewManager ()
		{
			_views = new Hashtable ();
		
			string dir = Environment.GetEnvironmentVariable("HOME");
			string tmp = Path.Combine (dir, ".lat");
			
			_configFile = Path.Combine (tmp, "views.xml");

			DirectoryInfo di = new DirectoryInfo (tmp);
			if (!di.Exists)
			{
				di.Create ();
			}

			FileInfo fi = new FileInfo (_configFile);
			if (!fi.Exists)
			{
				return;
			}
			
			loadViews ();		
		}
		
		public string[] getViewNames ()
		{
			string[] retVal = new string [_views.Count];
			int count = 0;
			
			foreach (string s in _views.Keys)
			{
				CustomViewData cvd = (CustomViewData) _views[s];
				retVal [count] = cvd.Name;
				
				count++;
			}
			
			return retVal;
		}
		
		public CustomViewData Lookup (string name)
		{
			return (CustomViewData) _views[name];
		}
		
		public void addView (CustomViewData cvd)
		{
			_views.Add (cvd.Name, cvd);
		}
		
		public void updateView (CustomViewData cvd)
		{
			_views[cvd.Name] = cvd;
		}
		
		public void deleteView (string name)
		{
			_views.Remove (name);
		}

		public void reloadViews ()
		{
			_views.Clear ();
			loadViews ();
		}
		
		public void loadViews ()
		{
			try
			{
				XmlTextReader r = new XmlTextReader (_configFile);
				
				while (r.Read()) 
				{				
					if (r.Name == "view") 
					{								
						CustomViewData cvd = new CustomViewData (
							r.GetAttribute ("name"),
							r.GetAttribute ("filter"),
							r.GetAttribute ("base"),
							r.GetAttribute ("cols"));
				
						_views.Add (cvd.Name, cvd);				
					} 
			 	}
			 			
			 	r.Close ();
			}
			catch (Exception e)
			{
				Console.WriteLine (e.Message);
			}
		}
		
		public void saveViews ()	
		{	
			XmlTextWriter writer = new XmlTextWriter(_configFile,
						System.Text.Encoding.UTF8);
						
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument(false);
			
			writer.WriteStartElement("views");
			
			foreach (string name in _views.Keys) 
			{		     			 	
				CustomViewData cvd = (CustomViewData) _views[name];
		 	
				writer.WriteStartElement("view", null);
			 		
				writer.WriteAttributeString ("name", cvd.Name);
				writer.WriteAttributeString ("filter", cvd.Filter);
				writer.WriteAttributeString ("base", cvd.Base);
				writer.WriteAttributeString ("cols", cvd.Cols);
				
	        		writer.WriteEndElement();
			}
	    		
			writer.WriteEndElement();
			
			writer.Flush();
			writer.Close();	
		}
		
	}
}
