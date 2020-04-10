using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Reflection;
using Autodesk.Revit.UI.Selection;

namespace InstanceRoom
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        List<Double> lengthList = new List<double>();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Reference reference = uidoc.Selection.PickObject(ObjectType.Element);
            Room el = (from r in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).Cast<Room>()
                           where r.Id == reference.ElementId
                           select r).First();
            //Get all families in project
            List<Family> families = new List<Family>(new FilteredElementCollector(doc).OfClass(typeof(Family))
                .Cast<Family>().Where<Family>(f => f.FamilyPlacementType == FamilyPlacementType.OneLevelBased));
            
            //get the BoundarySegments
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
            IList<IList<Autodesk.Revit.DB.BoundarySegment>> segArray = el.GetBoundarySegments(opt);
            CurveArray curveArray = new CurveArray();
            // Iterate to gather the curve objects
            foreach (IList<BoundarySegment> bSegments in segArray)
            {
                foreach (BoundarySegment bSegment in bSegments)
                {
                    Curve curve = bSegment.GetCurve();
                    curveArray.Append(curve);
                    lengthList.Add(curve.Length);
                }
            }
            Double leng = lengthList.Sum();
            UserForm docForm1 = new UserForm(el, uiapp, families, curveArray, leng);
            docForm1.ShowDialog();
            return Result.Succeeded;
        }
    }

    public class RubClass : IExternalApplication
    {
        static void AddRibbonPanel (UIControlledApplication application)
        {
            string tabName = "InfoBIM";
            application.CreateRibbonTab(tabName);
            RibbonPanel ribbonPanel = application.CreateRibbonPanel(tabName, "RoomInstance");

            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData button1 = new PushButtonData("RoomInstance", "RoomInstance", thisAssemblyPath, "RubTest.Command");
            button1.ToolTip = "Select the room";

        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            AddRibbonPanel(application);
            return Result.Succeeded;
        }
    }
}
