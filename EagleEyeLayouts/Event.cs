using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using EagleEyeLayouts.Extensions;
using EagleEyeLayouts.Collectors;
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
			string filePath1 = $@"{centralFilePath}Project number – Project name – EE-layout with incubators";
			string filePath2 = $@"{centralFilePath}Project number – Project name – EE-layout without incubators";
			List<ElementId> collectedFamilies = new List<ElementId>();

			switch (AddinForm.EventFlag)
			{
				case EventRaised.Event1:
					
					//Get view plan
					ViewPlan viewPlan = AboutView.GetViewPlanByName(doc, viewPlanName);
					if (viewPlan == null)
					{
						TaskDialog.Show("Eagle eye layouts", "The default view plan <5.5 - Eagle Eye> didn't found. Please select one view from the upcoming list", TaskDialogCommonButtons.Ok, TaskDialogResult.Ok);

						List<ViewPlan> allViewPlans = AboutView.GetAllViewPlans(doc);
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
							viewPlan = AboutView.GetViewPlanByName(doc, viewPlanName);
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
					collectedFamilies = WithCondition.CollectFamilyInstances(doc, "XSTR");

					//Check their visibility
					if (AboutVisibility.IsAnyElementHiddenInView(viewPlan, collectedFamilies))
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
							AboutVisibility.UnhideElementsInPlan(doc,viewPlan, collectedFamilies);
						}
					}
						
					//Export with incubators
					AboutView.ExportViewPlanToImage(doc, viewPlan, filePath1);

					//Export without incubators
					AboutVisibility.HideElementsInPlan(doc, viewPlan, collectedFamilies);
					AboutView.ExportViewPlanToImage(doc, viewPlan, filePath2);
					AboutVisibility.UnhideElementsInPlan(doc,viewPlan, collectedFamilies);

					//Change files names
					filePath1 = Directory.GetFiles(centralFilePath, "*.*").FirstOrDefault(file => Path.GetFileName(file).Contains("with incubators"));
					filePath2 = Directory.GetFiles(centralFilePath, "*.*").FirstOrDefault(file => Path.GetFileName(file).Contains("without incubators"));
					string newfilePath1 = $@"{centralFilePath}Project number – Project name – EE-layout with incubators.png";
					string newfilePath2 = $@"{centralFilePath}Project number – Project name – EE-layout without incubators.png";
					File.Move(filePath1, newfilePath1);
					File.Move(filePath2, newfilePath2);

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

