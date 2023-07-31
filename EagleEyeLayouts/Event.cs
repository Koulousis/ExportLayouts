using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
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
					
					//Get view plan
					ViewPlan viewPlan = ViewExtensions.GetViewPlanByName(doc, viewPlanName);
					if (viewPlan == null)
					{
						TaskDialog.Show("Eagle eye layouts", "The default view plan <5.5 - Eagle Eye> didn't found. Please select one view from the upcoming list", TaskDialogCommonButtons.Ok, TaskDialogResult.Ok);

						List<ViewPlan> allViewPlans = ViewExtensions.GetAllViewPlans(doc);
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
							viewPlan = ViewExtensions.GetViewPlanByName(doc, viewPlanName);
						}
						else
						{
							TaskDialog.Show("Eagle eye layouts", "Process canceled.", TaskDialogCommonButtons.Ok, TaskDialogResult.Ok);
							return;
						}
						   
					}
					
					//Tag all rooms
					TagAllRoomsInSpecificView(doc, viewPlan.Name);

					//Collect incubators
					collectedFamilies = CollectFamilyInstances(doc, "XSTR");

					//Check their visibility
					if (AppearExtensions.IsAnyElementHiddenInView(viewPlan, collectedFamilies))
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
							AppearExtensions.UnhideElementsInPlan(doc,viewPlan, collectedFamilies);
						}
					}
						
					//Export with incubators
					ViewExtensions.ExportViewPlanToImage(doc, viewPlan, filePath1);

					//Export without incubators
					AppearExtensions.HideElementsInPlan(doc, viewPlan, collectedFamilies);
					ViewExtensions.ExportViewPlanToImage(doc, viewPlan, filePath2);
					AppearExtensions.UnhideElementsInPlan(doc,viewPlan, collectedFamilies);

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

		public void TagAllRoomsInSpecificView(Document doc, string viewName)
		{
			// Find the desired view by name
			View desiredView = new FilteredElementCollector(doc)
				.OfClass(typeof(View))
				.Cast<View>()
				.FirstOrDefault(v => v.Name.Equals(viewName));

			// If the desired view doesn't exist, return
			if (desiredView == null)
			{
				TaskDialog.Show("Error", $"Could not find view with name {viewName}.");
				return;
			}

			// Get a collection of all rooms in the project
			FilteredElementCollector collector = new FilteredElementCollector(doc);
			ICollection<Element> collection = collector.OfClass(typeof(SpatialElement))
				.OfCategory(BuiltInCategory.OST_Rooms).ToElements();

			// Begin a new transaction
			using (Transaction trans = new Transaction(doc, "Tag All Rooms"))
			{
				trans.Start();

				// Iterate through each room
				foreach (Element e in collection)
				{
					SpatialElement room = e as SpatialElement;

					if (room != null)
					{
						// Get the room's location point
						LocationPoint roomLocation = room.Location as LocationPoint;

						if (roomLocation != null)
						{
							// Create a new UV point from the room's location
							UV pointUV = new UV(roomLocation.Point.X, roomLocation.Point.Y);

							// Create a new room tag at the room's location in the desired view
							RoomTag newTag = doc.Create.NewRoomTag(new LinkElementId(room.Id), pointUV, desiredView.Id);
						}
					}
				}

				// Commit the transaction
				trans.Commit();
			}
		}

	}
	
}

