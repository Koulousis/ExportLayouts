using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace EagleEyeLayouts.Extensions
{
	public static class AboutView
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

		public static ViewPlan GetViewPlanByName(Document doc, string viewPlanName)
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

		public static List<ViewPlan> GetAllViewPlans(Document doc)
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
		
		public static void ExportViewPlanToImage(Document doc, ViewPlan viewPlan, string filePath)
		{
			// Set up the ImageExportOptions
			ImageExportOptions imageExportOptions = new ImageExportOptions
			{
				ExportRange = ExportRange.SetOfViews,
				FilePath = filePath,
				FitDirection = FitDirectionType.Horizontal,
				HLRandWFViewsFileType = ImageFileType.PNG,
				ShadowViewsFileType = ImageFileType.PNG,
				PixelSize = 10000,
				
			};

			// Add the ViewPlan to the set of views to export
			imageExportOptions.SetViewsAndSheets(new List<ElementId>() { viewPlan.Id });

			// Export the image
			doc.ExportImage(imageExportOptions);
		}
	}
}