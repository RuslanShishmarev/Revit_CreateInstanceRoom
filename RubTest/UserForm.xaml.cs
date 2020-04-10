using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI.Selection;
using ComboBox = System.Windows.Controls.ComboBox;
using System.Diagnostics;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.Attributes;

namespace InstanceRoom
{
    /// <summary>
    /// Логика взаимодействия для UserForm.xaml
    /// </summary>
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public partial class UserForm : Window
    {
        Room _room;
        //Curves room
        CurveArray _curveArray;
        Document _doc;
        UIApplication _uiapp;
        UIDocument _uidoc;
        //all families
        List<Family> _families;
        Double perimetr;
        //all new points
        List<XYZ> points;
        Family family;
        Double step;
        Double dis;
        Double offset;
        Level level;
        public UserForm(Room element, UIApplication uiapp, List<Family> families, CurveArray curveArray, Double leng)
        {
            InitializeComponent();
            _room = element;
            perimetr = leng;
            _uiapp = uiapp;
            _uidoc = uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;
            _families = families;
            _curveArray = curveArray;
            select_view();
        }
        public void select_view()
        {
            Room room = _room;
            level = _room.Level;
            listView.Items.Add("Имя помещения: " + room.Name);
            listView.Items.Add("Уровень: " + level.Name);
            listView.Items.Add("Периметр: " + Math.Round(perimetr/30.48, 2) + " м");
            listView.Items.Add("Высота помещения: " + room.get_Parameter(BuiltInParameter.ROOM_HEIGHT).AsValueString() + " мм");
            cmbFamily.ItemsSource = _families;
            cmbFamily.DisplayMemberPath = "Name";
        }

        private void cmbFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            Debug.Assert(null != cb,"expected a combo box");
            Debug.Assert(cb == cmbFamily, "what combo box are you, then?");

            Family f = cb.SelectedItem as Family;
            family = f;
            List<FamilySymbol> symbols = new List<FamilySymbol>();

            ISet<ElementId> familySymbolIds = f.GetFamilySymbolIds();
            foreach (ElementId f_symb_id in familySymbolIds)
            {
                FamilySymbol familySymbol = f.Document.GetElement(f_symb_id) as FamilySymbol;
                // Get family symbol
                symbols.Add(familySymbol);
            }
            cmbType.ItemsSource = symbols;
            cmbType.DisplayMemberPath = "Name";
        }
        public FamilySymbol Type_f
        {
            get
            {
                return cmbType.SelectedItem as FamilySymbol;
            }
        }
        public List<XYZ> poin_list
        {
            get
            {
                return points;
            }
        }
        List<FamilyInstance> new_elem_list = new List<FamilyInstance>();
        //find element by name
        Element FindElement(Document doc, Type targetType, string targetName)
        {
            return new FilteredElementCollector(doc)
              .OfClass(targetType)
              .FirstOrDefault<Element>(
                e => e.Name.Equals(targetName));
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Type_f != null)
            {
                if (stepValue.Text.Length < 2)
                {
                    step = 1000;
                }
                else
                {
                    step = Convert.ToDouble(stepValue.Text.Replace(".", ","));
                }
                if (disValue.Text.Length < 2)
                {
                    dis = 1000;
                }
                else
                {
                    dis = Convert.ToDouble(disValue.Text.Replace(".", ","));
                }
                if (offsetValue.Text.Length < 2)
                {
                    offset = 0;
                }
                else
                {
                    offset = Convert.ToDouble(offsetValue.Text.Replace(".", ","));
                }
                    //Get poits
                    AllPoint allPoint = new AllPoint();
                    points = allPoint.CreatePlacePoints(_room, _curveArray, step / 304.8, dis / 304.8, offset / 304.8);
                    StructuralType st = StructuralType.NonStructural;
                    Autodesk.Revit.Creation.Document creation_doc = _doc.Create;
                    using (Transaction t = new Transaction(_doc))
                    {
                        t.Start("CreateInstances");
                        foreach (XYZ p in points)
                        {
                            if (!Type_f.IsActive)
                            {
                                Type_f.Activate();
                            }
                            FamilyInstance new_el = creation_doc.NewFamilyInstance(p, Type_f, level, st);
                            new_elem_list.Add(new_el);
                        }
                        t.Commit();
                    }
                    MessageBox.Show("Готово!");
            }
            else
                MessageBox.Show("Укажите тип элемента");
        }
        List<XYZ> check_points = new List<XYZ>();
        List<Double> dis_list = new List<double>();
        List<String> dis_list_str = new List<string>();
        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (new_elem_list.Count != 0)
            {
                //get the check points
                foreach (XYZ xyz in points)
                {
                    foreach (Curve curve in _curveArray)
                    {
                        XYZ w_S_p = curve.GetEndPoint(0);
                        XYZ w_E_p = curve.GetEndPoint(1);
                        // calculate the distance between the point and room lines
                        Double dis_l = (Math.Abs((w_E_p.Y - w_S_p.Y) * xyz.X -
                            (w_E_p.X - w_S_p.X) * xyz.Y + w_E_p.X * w_S_p.Y - w_E_p.Y * w_S_p.X) /
                            Math.Sqrt(Math.Pow(w_E_p.Y - w_S_p.Y, 2) + Math.Pow(w_E_p.X - w_S_p.X, 2))) * 304.8;
                        dis_list.Add(dis_l);
                        dis_list_str.Add(dis_l.ToString());
                        if (Math.Round(dis_l,0) < Math.Round(dis, 0))
                            check_points.Add(xyz);
                    }
                }
                if (check_points.Count() != 0)
                {
                    //check instances by the check point list
                    List<String> id_list_str = new List<string>();
                    List<ElementId> id_list = new List<ElementId>();
                    List<ElementId> un_id_list = new List<ElementId>();
                    foreach (FamilyInstance el in new_elem_list)
                    {
                        Location L = el.Location;
                        LocationPoint L_p = L as LocationPoint;
                        XYZ p = L_p.Point;
                        foreach (XYZ ch_p in check_points)
                        {
                            if (p.X == ch_p.X && p.Y == ch_p.Y)
                                id_list.Add(el.Id);
                                id_list_str.Add(el.Id.ToString());
                        }
                    }

                    _uidoc.Selection.SetElementIds(id_list);
                    foreach (ElementId eid in id_list)
                    {
                        if (un_id_list.Contains(eid) == false)
                            un_id_list.Add(eid);
                    }
                    //MessageBox.Show(string.Join(Environment.NewLine, un_id_list));
                }
                else
                    MessageBox.Show("Все хорошо!");
            }
            else
                MessageBox.Show("Нет элементов");
        } 
    }
}
