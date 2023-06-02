using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using View = Autodesk.Revit.DB.View;

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
			string centralFilePath = GetEagleEyeDirectory(doc);
			//string centralFilePath = @"J:\Drawings UITVOER\Pet UIT 30-39\Uit33\3375-CHAI AREE\Phase 3_ 220309Brev3-OC\-3- Drawings\3 - Eagle Eye layouts\";
			string filePath1 = $@"{centralFilePath}WithIncubators";
			string filePath2 = $@"{centralFilePath}WithoutIncubators";
			List<ElementId> collectedFamilies = new List<ElementId>();

			switch (AddinForm.EventFlag)
			{
				case EventRaised.Event1:
					
					ViewPlan viewPlan = GetViewPlanByName(doc, viewPlanName);
					if (viewPlan == null)
					{
						TaskDialog.Show("Eagle eye layouts", "The default view plan <5.5 - Eagle Eye> didn't found. Please select one view from the upcoming list", TaskDialogCommonButtons.Ok, TaskDialogResult.Ok);

						List<ViewPlan> allViewPlans = GetAllViewPlans(doc);
						List<string> allViewPlanNames = new List<string>();
						foreach (ViewPlan view in allViewPlans)
						{
							allViewPlanNames.Add(view.Name);
						}

						SelectViewPlan selectViewPlanForm = new SelectViewPlan(allViewPlanNames);
						DialogResult result = selectViewPlanForm.ShowDialog();

						if (result == DialogResult.OK)
						{
							viewPlanName = SelectViewPlan.SelectedViewPlanName;
							viewPlan = GetViewPlanByName(doc, viewPlanName);
						}
						else
						{
							TaskDialog.Show("Eagle eye layouts", "Process canceled.", TaskDialogCommonButtons.Ok, TaskDialogResult.Ok);
							return;
						}
						   
					}
					
					//Collect incubators
					collectedFamilies = CollectFamilyInstances(doc, "XSTR");

					//Check their visibility
					if (IsAnyElementHiddenInView(viewPlan, collectedFamilies))
					{
						TaskDialog dialog = new TaskDialog("Eagle eye layouts")
						{
							MainIcon = TaskDialogIcon.TaskDialogIconInformation,
							MainInstruction = "There are hidden incubators",
							MainContent = "Do you want to unhide them for the image that includes the incubators?",
							CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
							DefaultButton = TaskDialogResult.No
						};

						TaskDialogResult result = dialog.Show();
						if (result == TaskDialogResult.Yes)
						{
							UnhideElementsInPlan(doc,viewPlan, collectedFamilies);
						}
					}
						
					//Export with incubators
					ExportViewPlanToImage(doc, viewPlan, filePath1);
					//Export without incubators
					HideElementsInPlan(doc, viewPlan, collectedFamilies);
					ExportViewPlanToImage(doc, viewPlan, filePath2);
					UnhideElementsInPlan(doc,viewPlan, collectedFamilies);

					TaskDialog.Show("Eagle eye layouts", "Export ready.\nThe export folder location will pop up.", TaskDialogCommonButtons.Ok, TaskDialogResult.Ok);
					Process.Start(centralFilePath);

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

		public List<ViewPlan> GetAllViewPlans(Document doc)
		{
			// Retrieve all ViewPlans in the document
			List<ViewPlan> allViewPlans = new List<ViewPlan>();
			FilteredElementCollector collector = new FilteredElementCollector(doc);
			ICollection<Element> viewPlans = collector.OfClass(typeof(ViewPlan)).ToElements();

			// Find the ViewPlan with the given name
			foreach (Element elem in viewPlans)
			{
				allViewPlans.Add((ViewPlan)elem);
			}

			return allViewPlans;
		}


		public void ExportViewPlanToImage(Document doc, ViewPlan viewPlan, string filePath)
		{
			// Set up the ImageExportOptions
			ImageExportOptions imageExportOptions = new ImageExportOptions
			{
				ExportRange = ExportRange.SetOfViews,
				FilePath = filePath,
				FitDirection = FitDirectionType.Horizontal,
				HLRandWFViewsFileType = ImageFileType.PNG,
				ShadowViewsFileType = ImageFileType.PNG,
				PixelSize = 10000
			};

			// Add the ViewPlan to the set of views to export
			imageExportOptions.SetViewsAndSheets(new List<ElementId>() { viewPlan.Id });

			// Export the image
			doc.ExportImage(imageExportOptions);
		}

		public List<ElementId> CollectFamilyInstances(Document doc, string familyNameStart)
		{
			// Retrieve all FamilyInstance elements in the document
			FilteredElementCollector collector = new FilteredElementCollector(doc);
			ICollection<Element> familyInstances = collector.OfClass(typeof(FamilyInstance)).ToElements();

			// Create a list to store the Ids of the matching elements
			List<ElementId> ids = new List<ElementId>();

			// Loop through all FamilyInstance elements
			foreach (Element elem in familyInstances)
			{
				FamilyInstance fi = elem as FamilyInstance;
				if (fi != null && fi.Symbol.Family.Name.StartsWith(familyNameStart))
				{
					// If the FamilyInstance's family name starts with the specified string, add it to the list
					ids.Add(fi.Id);
				}
			}

			return ids;
		}

		public void HideElementsInPlan(Document doc, ViewPlan viewPlan, List<ElementId> idsToHide)
		{
			// Hide the elements in the view
			using (Transaction t = new Transaction(doc, "Hide Elements"))
			{
				t.Start();
				viewPlan.HideElements(idsToHide);
				t.Commit();
			}
		}

		public void UnhideElementsInPlan(Document doc, ViewPlan viewPlan, List<ElementId> idsToUnhide)
		{
			// Unhide the elements in the view
			using (Transaction t = new Transaction(doc, "Unhide Elements"))
			{
				t.Start();
				viewPlan.UnhideElements(idsToUnhide);
				t.Commit();
			}
		}

		public string GetEagleEyeDirectory(Document doc)
		{
			// Get the ModelPath object for the central model
			ModelPath centralModelPath = doc.GetWorksharingCentralModelPath();

			// Convert the ModelPath object to a string
			string centralFilePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(centralModelPath);

			// Find the last occurrence of the "\" character
			int lastSlashIndex = centralFilePath.LastIndexOf("\\");

			// Find the second last occurrence of the "\" character
			int secondLastSlashIndex = centralFilePath.LastIndexOf("\\", lastSlashIndex - 1);

			// Get the part of the path before the second last "\"
			string parentDirectoryPath = centralFilePath.Substring(0, secondLastSlashIndex);
			
			return parentDirectoryPath + @"\3 - Eagle Eye layouts\";
		}

		public bool IsAnyElementHiddenInView(View view, List<ElementId> elementIds)
		{
			Document doc = view.Document;

			foreach (ElementId id in elementIds)
			{
				Element el = doc.GetElement(id);

				// If the element is not visible in the view, return true
				if (!view.IsElementVisibleInView(el))
				{
					return true;
				}
			}

			// If none of the elements are hidden, return false
			return false;
		}

	}
	
}


namespace EagleEyeLayouts
{
	public static class ViewExtensions
	{
		public static bool IsElementVisibleInView(this View view, Element el)
		{
			if (view == null)
			{
				throw new ArgumentNullException(nameof(view));
			}

			if (el == null)
			{
				throw new ArgumentNullException(nameof(el));
			}

			// Obtain the element's document
			Document doc = el.Document;

			ElementId elId = el.Id;

			// Create a FilterRule that searches for an element matching the given Id
			FilterRule idRule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.ID_PARAM), elId);
			var idFilter = new ElementParameterFilter(idRule);

			// Use an ElementCategoryFilter to speed up the search, as ElementParameterFilter is a slow filter
			Category cat = el.Category;
			var catFilter = new ElementCategoryFilter(cat.Id);

			// Use the constructor of FilteredElementCollector that accepts a view id as a parameter to only search that view
			// Also use the WhereElementIsNotElementType filter to eliminate element types
			FilteredElementCollector collector =
				new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType().WherePasses(catFilter).WherePasses(idFilter);

			// If the collector contains any items, then we know that the element is visible in the given view
			return collector.Any();
		}
	}
}
