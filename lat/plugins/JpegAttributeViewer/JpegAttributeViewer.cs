// 
// lat - JpegAttributeViewer.cs
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
using System.IO;
using Gtk;
using Gdk;

namespace lat {

	public class JpegAttributeViewPlugin : AttributeViewPlugin
	{
		byte[] jpegData = null;
	
		public JpegAttributeViewPlugin () : base ()
		{
		}
	
		public override void OnActivate (string attributeData)
		{
		}
		
		public override void OnActivate (byte[] attributeData)
		{
			jpegData = null;
			
			ViewerDialog vd = new ViewerDialog (attributeData);
			jpegData = vd.RawFileBytes;
		}

		public override ViewerDataType DataType 
		{
			get { return ViewerDataType.Binary; }
		}

		public override string StringValue 
		{
			get { return null; }
		}
		
		public override byte[] ByteValue 
		{
			get { return jpegData; }
		}
		
		public override string AttributeName 
		{
			get { return "jpegPhoto"; }
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
			get { return "JPEG Attribute Viewer"; } 
		}
		
		public override string Name 
		{ 
			get { return "JPEG Attribute Viewer"; } 
		}
		
		public override string Version 
		{ 
			get { return Defines.VERSION; } 
		}
	}
	
	public class ViewerDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog jpegAttributeViewDialog;

		[Glade.Widget] Gtk.Image jpegImage;
		[Glade.Widget] Gtk.Entry filenameEntry;

		byte[] rawBytes = null;

		public ViewerDialog (byte[] imageData) 
		{
			ui = new Glade.XML (null, "dialog.glade", "jpegAttributeViewDialog", null);
			ui.Autoconnect (this);
			
			if (imageData != null) {
				try {
					Gdk.Pixbuf pb = new Gdk.Pixbuf (imageData);
					jpegImage.Pixbuf = pb;
				} catch {}
			}
			
			jpegAttributeViewDialog.Resize (300, 400);
			jpegAttributeViewDialog.Run ();
			jpegAttributeViewDialog.Destroy ();
		}

		public void OnBrowseClicked (object o, EventArgs args)
		{
			FileChooserDialog fcd = new FileChooserDialog (
				"Choose an image",
				Gtk.Stock.Open, 
				null, 
				FileChooserAction.Open);

			fcd.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			fcd.AddButton (Gtk.Stock.Open, ResponseType.Ok);

			fcd.SelectMultiple = false;

			ResponseType response = (ResponseType) fcd.Run();
			if (response == ResponseType.Ok) {
				filenameEntry.Text = fcd.Filename;
				
				try {
					Gdk.Pixbuf pb = new Gdk.Pixbuf (fcd.Filename);
					jpegImage.Pixbuf = pb;
				} catch {}
			} 
		
			fcd.Destroy();		
		}

		static byte[] ReadFile (Stream stream)
		{
			byte[] buffer = new byte[32768];
			using (MemoryStream ms = new MemoryStream())
			{
				while (true)
				{
					int read = stream.Read (buffer, 0, buffer.Length);
					if (read <= 0)
						return ms.ToArray();
					ms.Write (buffer, 0, read);
				}
			}
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			if (filenameEntry.Text == null || filenameEntry.Text == "")
				return;
				
			try {
			
				FileStream fs = File.OpenRead(filenameEntry.Text);
				rawBytes = ReadFile (fs);				
				
			} catch (Exception e) {
				Log.Debug (e.ToString());
			}
		}
		
		public byte[] RawFileBytes
		{
			get { return rawBytes; }
		}
	}
}