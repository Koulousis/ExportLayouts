using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.UI;

namespace EagleEyeLayouts
{
	public partial class AddinForm : Form
	{
		private ExternalCommandData _commandData;
		private readonly ExternalEvent _formEvent;
		private Event _revitEvent;
		public static EventRaised EventFlag = EventRaised.NoEvent;

		public AddinForm(ExternalCommandData commandData, ExternalEvent formEvent, Event revitEvent)
		{
			InitializeComponent();
			_commandData = commandData;
			_formEvent = formEvent;
			_revitEvent = revitEvent;

			Event1();
		}

		public void Event1()
		{
			EventFlag = EventRaised.Event1;
			_formEvent.Raise();
		}

		public void Event2()
		{
			EventFlag = EventRaised.Event2;
			_formEvent.Raise();
			_revitEvent.OnAfterEventRaised += AfterEvent2;
		}

		public void AfterEvent2()
		{
			TaskDialog.Show("AfterEvent2", "Event2 after raise.");
		}
	}

	public enum EventRaised : byte
	{
		NoEvent,
		Event1,
		Event2
	}

	
}
