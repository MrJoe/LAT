// 
// lat - TimeDateDialog.cs
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
using Glade;
using System;

namespace lat
{
	public class TimeDateDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog timeDateDialog;
		[Glade.Widget] Gtk.SpinButton hourSpin;
		[Glade.Widget] Gtk.SpinButton minuteSpin;
		[Glade.Widget] Gtk.SpinButton secondSpin;
		[Glade.Widget] Gtk.Calendar calendar;

		private double _time = 0;

		public TimeDateDialog ()
		{
			ui = new Glade.XML (null, "lat.glade", "timeDateDialog", null);
			ui.Autoconnect (this);
			
			timeDateDialog.Run ();
			timeDateDialog.Destroy ();
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			int hour = Convert.ToInt32 (hourSpin.Value);
			int minute = Convert.ToInt32 (minuteSpin.Value);
			int second = Convert.ToInt32 (secondSpin.Value);

			DateTime dt = calendar.GetDate ();

			DateTime userDT = new DateTime (dt.Year, dt.Month, dt.Day, hour, minute, second);
			TimeSpan ts = userDT.Subtract (new DateTime(1970,1,1,0,0,0));

			_time = ts.TotalSeconds;

			timeDateDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			timeDateDialog.HideAll ();
		}

		public double UnixTime
		{
			get { return _time; }
		}
	}
}
