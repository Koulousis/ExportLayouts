using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace EagleEyeLayouts
{
	public class Event : IExternalEventHandler
	{
		public delegate void AfterEventRaised();
		public event AfterEventRaised OnAfterEventRaised;

		public string GetName()
		{
			return "Event";
		}

		public void Execute(UIApplication app)
		{
			UIDocument uiDoc = app.ActiveUIDocument;
			Document doc = uiDoc.Document;
			string viewPlanName = "5.5 - Eagle Eye";
			string filePath = $@"C:\Users\AKoulousis\OneDrive - Petersime NV\Bureaublad\Test\{viewPlanName}";


			switch (AddinForm.EventFlag)
			{
				case EventRaised.Event1:
					ViewPlan viewPlan = GetViewPlanByName(doc, viewPlanName);
					ExportViewPlanToImage(doc, viewPlan, filePath);
					AddinForm.EventFlag = EventRaised.NoEvent;
					break;
				case EventRaised.Event2:
					TaskDialog.Show("Event2", "Event2 Raised.");
					OnAfterEventRaised?.Invoke();
					AddinForm.EventFlag = EventRaised.NoEvent;
					break;
			}
		}

		public ViewPlan GetViewPlanByName(Document doc, string viewPlanName)
		{
			// Retrieve all ViewPlans in the document
			FilteredElementCollector collector = new FilteredElementCollector(doc);
			ICollection<Element> viewPlans = collector.OfClass(typeof(ViewPlan)).ToElements();

			// Find the ViewPlan with the given name
			foreach (Element elem in viewPlans)
			{
				ViewPlan viewPlan = (ViewPlan)elem;
				if (viewPlan != null && viewPlan.Name == viewPlanName)
				{
					return viewPlan;
				}
			}

			// If no ViewPlan with the given name was found, return null
			return null;
		}


		public void ExportViewPlanToImage(Document doc, ViewPlan viewPlan, string filePath)
		{
			// Set up the ImageExportOptions
			ImageExportOptions options = new ImageExportOptions
			{
				FilePath = filePath,
				FitDirection = FitDirectionType.Horizontal,
				HLRandWFViewsFileType = ImageFileType.PNG,
				ShadowViewsFileType = ImageFileType.PNG,
				PixelSize = 10000
			};

			// Add the ViewPlan to the set of views to export
			options.SetViewsAndSheets(new List<ElementId>() { viewPlan.Id });

			// Export the image
			doc.ExportImage(options);
		}
	}
	
}
