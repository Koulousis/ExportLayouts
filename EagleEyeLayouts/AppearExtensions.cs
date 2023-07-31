using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace EagleEyeLayouts
{
	public static class AppearExtensions
	{
		public static bool IsAnyElementHiddenInView(View view, List<ElementId> elementIds)
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
		
		public static void HideElementsInPlan(Document doc, ViewPlan viewPlan, List<ElementId> idsToHide)
		{
			// Hide the elements in the view
			using (Transaction t = new Transaction(doc, "Hide Elements"))
			{
				t.Start();
				viewPlan.HideElements(idsToHide);
				t.Commit();
			}
		}

		public static void UnhideElementsInPlan(Document doc, ViewPlan viewPlan, List<ElementId> idsToUnhide)
		{
			// Unhide the elements in the view
			using (Transaction t = new Transaction(doc, "Unhide Elements"))
			{
				t.Start();
				viewPlan.UnhideElements(idsToUnhide);
				t.Commit();
			}
		}
	}
}
