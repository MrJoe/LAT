// 
// lat - SearchBuilderDialog.cs
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

using Gtk;
using GLib;
using Glade;
using System;
using System.Collections;

namespace lat 
{
	public struct SearchCriteria
	{
		public HBox hbox;
		public Gtk.Entry attrEntry;
		public Combo critCombo;
		public Gtk.Entry valEntry;
		public Combo boolCombo;

		public SearchCriteria (HBox aHbox, Gtk.Entry attr, Combo op, Gtk.Entry val, Combo bc)
		{
			hbox = aHbox;
			attrEntry = attr;
			critCombo = op;
			valEntry = val;
			boolCombo = bc;
		}
	}
	
	public class SearchBuilderDialog
	{
		[Glade.Widget] Gtk.Dialog searchBuilderDialog;
		[Glade.Widget] HBox opHbox;
		[Glade.Widget] Gtk.Entry attributeEntry;
		[Glade.Widget] Gtk.Entry valueEntry;
		[Glade.Widget] VBox critVbox;
		[Glade.Widget] Button addButton;
		[Glade.Widget] Button removeButton;
		[Glade.Widget] Button okButton;
		[Glade.Widget] Button cancelButton;

		private Glade.XML ui;
		private Combo opComboBox;
		private Combo firstCritCombo;
		
		private ArrayList _allCombos;

		private int _numCriteria = 0;
		private Hashtable _critTable;

		private LdapSearch _ls;

		private static string[] ops = { "begins with", "ends with", "equals", "contains", "is present" };
		private static string[] boolOps = { "", "AND", "OR" };

		public SearchBuilderDialog ()
		{
			_allCombos = new ArrayList ();
			_critTable = new Hashtable ();
			_ls = new LdapSearch ();

			ui = new Glade.XML (null, "lat.glade", "searchBuilderDialog", null);
			ui.Autoconnect (this);

			opComboBox = createCombo (ops);
			opHbox.Add (opComboBox);

			addButton.Clicked += new EventHandler (OnAddClicked);
			removeButton.Clicked += new EventHandler (OnRemoveClicked);

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

//			searchBuilderDialog.Resize (350, 400);

			searchBuilderDialog.Run ();
			searchBuilderDialog.Destroy ();
		}

		private static Combo createCombo (string[] list)
		{		
			Combo retVal = new Combo ();
			retVal.PopdownStrings = list;
			retVal.DisableActivate ();
			retVal.Entry.IsEditable = false;
			retVal.Show ();

			return retVal;
		}

		private void toggleBoolCombo (int row)
		{
			string prevKey = "row" + 
				(row - 1).ToString();

			SearchCriteria prevSC = (SearchCriteria)
				_critTable [prevKey];

			prevSC.boolCombo.Sensitive = !prevSC.boolCombo.Sensitive;
		}
		
		private void createCritRow (string attr, string op, string val)
		{
			_numCriteria++;

			HBox hbox = new HBox (false, 0);
			critVbox.PackStart (hbox, true, true, 0);

			Gtk.Entry attrEntry = new Gtk.Entry ();
			attrEntry.Text = attr;
			attrEntry.Show ();
			hbox.PackStart (attrEntry, true, true, 5);

			VBox vbox75 = new VBox (false, 0);
			vbox75.Show ();
			hbox.PackStart (vbox75, true, true, 5);

			Combo critCombo = createCombo (ops);
			critCombo.Entry.Text = op;
			vbox75.PackStart (critCombo, false, true, 16);
			
			Gtk.Entry valEntry = new Gtk.Entry ();
			valEntry.Text = val;
			valEntry.Show ();
			hbox.PackStart (valEntry, true, true, 5);

			VBox vbox76 = new VBox (false, 0);
			vbox76.Show ();
			hbox.PackStart (vbox76, true, true, 5);

			Combo boolCombo = createCombo (boolOps);
			boolCombo.Sensitive = false;
			vbox76.PackStart (boolCombo, false, true, 16);

			if (_numCriteria == 1)
			{
				firstCritCombo = boolCombo;
				firstCritCombo.Entry.Changed += new EventHandler (OnBoolChanged);
			}
			else if (_numCriteria > 1)
			{
				_allCombos.Add (boolCombo);
			}

			SearchCriteria sc = new SearchCriteria (
				hbox, attrEntry, critCombo, valEntry, boolCombo);

			string key = "row" + _numCriteria.ToString ();

			_critTable.Add (key, sc);

			if (_numCriteria > 1)
				toggleBoolCombo (_numCriteria);

			critVbox.ShowAll ();
		}

		private void OnBoolChanged (object o, EventArgs args)
		{
			foreach (Combo c in _allCombos)
			{
				if (c == null)
					continue;

				if (c.Sensitive)
				{
					c.Entry.Text = firstCritCombo.Entry.Text;
				}
			}
		}

		private void OnAddClicked (object o, EventArgs args)
		{
			createCritRow (attributeEntry.Text, 
					opComboBox.Entry.Text, 
					valueEntry.Text);

			attributeEntry.Text = "";
			valueEntry.Text = "";
		}

		private void OnRemoveClicked (object o, EventArgs args)
		{
			string key = "row" + _numCriteria.ToString ();

			SearchCriteria sc = (SearchCriteria) _critTable [key];
			sc.hbox.Destroy ();
			sc.attrEntry.Destroy ();
			sc.critCombo.Destroy ();
			sc.valEntry.Destroy ();
			sc.boolCombo.Destroy ();

			_critTable.Remove (key);

			if (_numCriteria > 1)
				toggleBoolCombo (_numCriteria);

			_numCriteria--;
		}

		private void buildFilter ()
		{
			string boolOp = "";

			foreach (string key in _critTable.Keys)
			{
				SearchCriteria sc = (SearchCriteria) _critTable [key];
			
				_ls.addCondition (
					sc.attrEntry.Text,
					sc.critCombo.Entry.Text,
					sc.valEntry.Text);
		
				if (!sc.boolCombo.Entry.Text.Equals (""))
					boolOp = sc.boolCombo.Entry.Text;
			}

			_ls.addBool (boolOp);
			_ls.endBool ();
		}
		
		private void OnOkClicked (object o, EventArgs args)
		{
			if (!attributeEntry.Text.Equals (""))
			{
				// simple search; only one criteria
				_ls.addCondition (
					attributeEntry.Text,
					opComboBox.Entry.Text,
					valueEntry.Text);
			}
			else
			{
				// complex search
				buildFilter ();
			}

			searchBuilderDialog.HideAll ();
		}
	
		private void OnCancelClicked (object o, EventArgs args)
		{
			searchBuilderDialog.HideAll ();
		}

		public string UserFilter
		{
			get { return _ls.Filter; }
		}
	}
}
