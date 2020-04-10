using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InstanceRoom
{
    class AllPoint
    {
        List<XYZ> roomXYZ = new List<XYZ>();
        List<XYZ> placeXYZ = new List<XYZ>();
        List<XYZ> startXYZ = new List<XYZ>();
        List<Double> xList = new List<double>();
        List<Double> startxList = new List<double>();
        List<Double> yList = new List<double>();
        List<Double> xy_sum = new List<double>();
        XYZ s_p;

        List<Double> lengthList = new List<double>();
        List<XYZ> result_placeXYZ = new List<XYZ>();
        public List<XYZ> CreatePlacePoints(Room room, CurveArray _curveArray, Double step, Double dis, Double offset)
        {
            //get the BoundarySegments
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
            IList<IList<Autodesk.Revit.DB.BoundarySegment>> segArray = room.GetBoundarySegments(opt);
            CurveArray curveArray = new CurveArray();
            // Iterate to gather the curve objects
            foreach (Curve curve in _curveArray)
            {
                lengthList.Add(curve.Length);
                Double X = curve.GetEndPoint(0).X;
                xList.Add(X);
                Double Y = curve.GetEndPoint(0).Y;
                yList.Add(Y);
                Double Z = curve.GetEndPoint(0).Z;
                XYZ cor = new XYZ(X, Y, Z);
                roomXYZ.Add(cor);
                xy_sum.Add(X - Y);
            }               
            
            //Get the start point
            List<XYZ> minp = new List<XYZ>();
            List<Double> xminp = new List<Double>();
            yList.Sort();
            foreach (XYZ p in roomXYZ)
            {
                if (p.Y == yList[0] || p.Y == yList[1])
                    minp.Add(p);
                xminp.Add(p.X);
            }
            foreach (XYZ p in minp)
            {
                if (p.X == xminp.Min())
                    s_p = p;
                break;
            }
            int col = Convert.ToInt32(lengthList.Max() / step);
            XYZ f_cor = new XYZ(s_p.X + dis, s_p.Y + dis, 0);
            //create the place points
            for (int i = 0; i < col; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    XYZ sec_p = new XYZ(f_cor.X + step * i, f_cor.Y + step * j, offset);
                    placeXYZ.Add(sec_p);
                }
            }
            //check points
            foreach(XYZ p_p in placeXYZ)
            {
                if (room.IsPointInRoom(p_p) == true)
                    result_placeXYZ.Add(p_p);
            }
            return result_placeXYZ;
        }
    }
}
