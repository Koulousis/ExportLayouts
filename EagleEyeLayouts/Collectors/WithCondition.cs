using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace EagleEyeLayouts.Collectors
{
	public static class WithCondition
	{
		public static List<ElementId> CollectFamilyInstances(Document doc, string familyNameStart)
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

	}
}
