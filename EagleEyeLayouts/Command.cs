using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace EagleEyeLayouts
{
	[Transaction(TransactionMode.ReadOnly)]
	public class Command : IExternalCommand
	{

		public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
		{
			//Access revitEvent through addinForm using formEvent.Raise()
			Event revitEvent = new Event();
			ExternalEvent formEvent = ExternalEvent.Create(revitEvent);

			//Open addin window from where the events will be triggered
			AddinForm addinForm = new AddinForm(commandData, formEvent, revitEvent);
			addinForm.Show();

			return Result.Succeeded;
		}
	}
}
