// 
// lat - PasswordAttributeViewer.cs
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

namespace lat {

	public class PassswordAttributeViewPlugin : AttributeViewPlugin
	{
		string passwordData = null;
	
		public PassswordAttributeViewPlugin () : base ()
		{
		}
	
		public override void OnActivate (string attributeData)
		{
			passwordData = null;
			PasswordDialog pd = new PasswordDialog ();

			if (pd.UnixPassword == null)			
				return;

			passwordData = pd.UnixPassword;
		}
		
		public override void OnActivate (byte[] attributeData)
		{
		}
		
		public override ViewerDataType DataType 
		{
			get { return ViewerDataType.String; }
		}	
		
		public override string AttributeName 
		{
			get { return "userPassword"; }
		}	

		public override string StringValue 
		{
			get { return passwordData; }
		}
		
		public override byte[] ByteValue 
		{
			get { return null; }
		}
			
		public override string[] Authors 
		{
			get {
				string[] cols = { "Loren Bandiera" };
				return cols;
			}
		}
		
		public override string Copyright 
		{ 
			get { return "MMG Security, Inc."; } 
		}
		
		public override string Description 
		{ 
			get { return "Password Attribute Viewer"; } 
		}
		
		public override string Name 
		{ 
			get { return "Password Attribute Viewer"; } 
		}
		
		public override string Version 
		{ 
			get { return Defines.VERSION; } 
		}
	}
}