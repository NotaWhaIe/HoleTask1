using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using System.Security.Cryptography.X509Certificates;
using HoleTasksPlagin;


namespace HoleTask1Plugin
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class HoleTasks1Command : IExternalCommand
    {
        static AddInId addinId = new AddInId(new Guid("f64706dd-e8f6-4cbe-9cc6-a2910be5ad5a"));
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            Document linkDoc = null;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            var selFilterRevitLinkInstance = new RevitLinkInstanceSelectionFilter();
            Reference selRevitLinkInstance = null;
            try
            {
                selRevitLinkInstance = sel.PickObject(ObjectType.Element, selFilterRevitLinkInstance, "Выберите связанный файл!");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            var revitLinkInstance = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Where(li => li.Id == selRevitLinkInstance.ElementId)
                .Cast<RevitLinkInstance>();
            if (revitLinkInstance.Count() == 0)
            {
                TaskDialog.Show("Revit", "Связанный файл не найден!");
                return Result.Cancelled;
            }
            linkDoc = revitLinkInstance.First().GetLinkDocument();
            Transform transform = revitLinkInstance.First().GetTotalTransform();

            var wallsInLinkList = new FilteredElementCollector(linkDoc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .ToList();
            List<Floor> floorsInLinkList = new FilteredElementCollector(linkDoc)
                .OfClass(typeof(Floor))
                .Cast<Floor>()
                .ToList();
            List<Pipe> pipesList = new FilteredElementCollector(doc)
                .OfClass(typeof(Pipe))
                .Cast<Pipe>()
                .ToList();
            List<Duct> ductsList = new FilteredElementCollector(doc)
                .OfClass(typeof(Duct))
                .Cast<Duct>()
                .ToList();

            Solid floorSolid = GetElementSolid(wallsInLinkList[0], revitLinkInstance.First());
            Face faceWithHoles = GetSolidMainFace(floorSolid);
            var getSolidMainFace = GetFaceWithoutHoles(faceWithHoles);
            ///Настроить отладку студии

            ///Обработка геометрии
            ///Обработка геометрии Стен
            ///Обработка геометрии Перекрытий
            //TaskDialog.Show("Поверхность стены и ее ID", faceWithHoles.Id.ToString());
            return Result.Succeeded;
        }
        public Solid GetElementSolid(Element element, RevitLinkInstance revitLinkInstance)
        {
            dynamic geoElement = null;

            Options opt = new Options();
            opt.ComputeReferences = true;
            opt.DetailLevel = ViewDetailLevel.Fine;
            geoElement = element.get_Geometry(opt);///добавить фильтрацию по солидам

            Solid solid = null;


            var linkDoc = revitLinkInstance.GetLinkDocument();
            Transform transform = revitLinkInstance.GetTotalTransform();

            // Get geometry object
            foreach (GeometryObject geoObject in geoElement)
            {
                solid = geoObject as Solid;
                if (null != solid)
                {
                    solid = SolidUtils.CreateTransformed(solid, transform);
                }
            }
            return solid;
        }

        public Face GetSolidMainFace(Solid solid)
        {
            Face faceMaxSquare = null;
            var faces = solid.Faces;
            foreach (Face solidface in faces)
            {
                if (faceMaxSquare == null || faceMaxSquare.Area < solidface.Area)
                    faceMaxSquare = solidface;
            }
            ///Метод возвращает одну из поверхностей с макс Площадью
            ///Найти сторону в которую нужно выдавить поверхность
            return faceMaxSquare;
        }
        public Autodesk.DesignScript.Geometry.Surface GetFaceWithoutHoles(Face faceWithHoles)
        {
            Autodesk.DesignScript.Geometry.Surface faceWithoutHoles = null;
            EdgeArrayArray AllFaceEdges = faceWithHoles.EdgeLoops;
            foreach (EdgeArray AgesOfOneFace in AllFaceEdges)
            {
                var perimeterPoints = new List<Autodesk.DesignScript.Geometry.Point>();
                foreach (Edge item in AgesOfOneFace)
                {
                    var pointStart = item.;
                    var pointStartDynamo = Autodesk.DesignScript.Geometry.Point.ByCoordinates(pointStart.X, pointStart.Y, pointStart.Z);
                    perimeterPoints.Add(pointStartDynamo);
                }
                var face = Autodesk.DesignScript.Geometry.Surface.ByPerimeterPoints(perimeterPoints);
                if (faceWithoutHoles == null || faceWithHoles.Area < faceWithoutHoles.Area)
                    faceWithoutHoles = face;


            }
            return faceWithoutHoles;
        }
    }
}
