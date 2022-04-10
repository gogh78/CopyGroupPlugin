using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;  //получаем доступ к документу

                GroupPickFilter groupPickFilter = new GroupPickFilter(); //создаем экземпляр класса
                //Определяем запрос на выбор элемента в модели
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");
                Element element = doc.GetElement(reference); //выбрали группу как элемент модели
                Group group = element as Group; //преобразуем выбранный элемент в группу для дальнейшей работы
                XYZ groupCenter = GetElementCenter(group); //центр выбранной группы
                Room room = GetRoomByPoint(doc, groupCenter);
                XYZ roomCenter = GetElementCenter(room);  //центр выбранной комнаты
                XYZ offset = groupCenter - roomCenter;
                

                //Определяем запрос на выбор точки
                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");
                Room selectRoom = GetRoomByPoint(doc, point);
                XYZ selectRoomCenter = GetElementCenter(selectRoom);

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                //doc.Create.PlaceGroup(point, group.GroupType); //вставка группы в выбранную точку
                doc.Create.PlaceGroup(selectRoomCenter + offset, group.GroupType); //вставка группы в центр комнаты

                transaction.Commit();
            }
            catch(Autodesk.Revit.Exceptions.OperationCanceledException)  //обрабатываем исключение при нажатии кнопки отмены
            {
                return Result.Cancelled;
            }
            catch(Exception ex) //обрабатываем исключение других ошибок
            {
                message = ex.Message; //выводим текст ошибки
                return Result.Failed;
            }
            return Result.Succeeded;
        }

        //метод, определяющий центр элемента на основе boundingBox
        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }

        //метод, определяющий принадлежность точки комнате
        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms); //фильтруем по категории комнаты
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room!=null)
                {
                    if(room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }

    }
    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            //сравниваем категории элементов над которым находится курсор с нужным нам
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
