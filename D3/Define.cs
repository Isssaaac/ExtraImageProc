using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageProcessLib.D3
{
    public struct ROIMargin
    {
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }

        public ROIMargin(int margin)
        {
            this.left = margin;
            this.right = margin;
            this.top = margin;
            this.bottom = margin;
        }
        public ROIMargin(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
        public bool AllSame()
        {
            return left == right && top == bottom && left == top;
        }
        public static ROIMargin operator +(ROIMargin a, ROIMargin b)
        {
            return new ROIMargin(a.left + b.left, a.top + b.top, a.right + b.right, a.bottom + b.bottom);
        }
        public static bool operator ==(ROIMargin a, ROIMargin b)
        {
            return a.top == b.top && a.left == b.left && a.right == b.right && a.bottom == b.bottom;
        }
        public static bool operator !=(ROIMargin a, ROIMargin b)
        {
            return !(a == b);
        }
        public static implicit operator ROIMargin(int all)
        {
            return new ROIMargin(all);
        }
        public static implicit operator ROIMargin(int[] arr)
        {
            int[] t = new int[4];
            for (int i = 0; i < t.Length; i++)
            {
                if (arr.Length > i)
                    t[i] = arr[i];
            }
            return new ROIMargin(t[0], t[1], t[2], t[3]);
        }
    }
    public enum ROITYTE
    {
        Polygon = 0,
        Rectangle = 1,
        Ellipse = 2
    }
    public class PolygonROI
    {
        private bool disposedValue;

        public CheckSize checksize = new CheckSize();
        public PolygonROI[] maskROI { get; set; }

        public ROIMargin margin { get; set; }
        public ROIMargin extramargin { get; set; } = new ROIMargin();

        public double left;
        public double right;
        public double top;
        public double bottom;
        public double[] x { get; set; }
        public double[] y { get; set; }

        public ROITYTE roitype { get; set; } = ROITYTE.Polygon;

        public bool bmaskTrans { get; set; }
      
        public double[] rowbase { get; set; }
        public double[] colbase { get; set; }

        public double roundedCornersize { get; set; } = 40;

        public bool bEnablePlane { get; set; } = true;
        public double FrameDepth { get; set; } = 0.5;




        public PolygonROI(double[] x, double[] y)
        {
            this.x = new double[0];
            this.y = new double[0];
            if (x != null && y != null)
            {
                this.x = x;
                this.y = y;
                this.left = x.Min();
                this.right = x.Max();
                this.top = y.Min();
                this.bottom = y.Max();
            }
            this.roitype = ROITYTE.Polygon;
        }
        public PolygonROI(double left, double top, double right, double bottom, ROITYTE roitype)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.roitype = roitype;
            this.x = new[] { left, right, right, left };
            this.y = new[] { top, top, bottom, bottom };
        }
        public PolygonROI(double[] x, double[] y, ROIMargin margin, PolygonROI[] maskroi = null) : this(x, y)
        {
            this.margin = margin;
            this.maskROI = maskroi;
        }

        public void GenRegion(out HObject region)
        {
            if (roitype == ROITYTE.Rectangle)
            {
                HOperatorSet.GenRectangle1(out region, top, left, bottom, right);
            }
            else if (roitype == ROITYTE.Ellipse)
            {
                HOperatorSet.GenEllipse(out region, (top + bottom) / 2.0, (left + right) / 2.0, 0, (right - left) / 2.0, (bottom - top) / 2.0);
            }
            else
            {
                HOperatorSet.GenRegionPolygonFilled(out region, y, x);
            }
        }



        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)                   
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~PolygonROI()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public struct CheckSize
    {
        public double minwidth;
        public double minheight;
        public double Searchdepth;
        public double maxdepth;
        public double _xInterval;
        public double _yInterval;
        public double planedepth;
        public double planedefectArea;

        public CheckSize(double minwidth, double minheight, double searchdepth, double _xInterval, double _yInterval)
        {
            this.minwidth = minwidth;
            this.minheight = minheight;
            this.Searchdepth = searchdepth;
            this.maxdepth = searchdepth;
            this._xInterval = _xInterval;
            this._yInterval = _yInterval;
            this.planedepth = 1;
            this.planedefectArea = 10;
        }
    }

    public struct FrameInfo
    {
        public double[] x;
        public double[] y;
        public double area;
        public FrameInfo(double[] x, double[] y, double area)
        {
            this.x = x;
            this.y = y;
            this.area = area;
            if (x.Length == 4 && y.Length == 4)
            {
                OrderRect(x, y, out var xx, out var yy);
                this.x = xx;
                this.y = yy;
            }
            else
            {
                this.area = 0;
            }
        }
        public void OrderRect(double[] x, double[] y, out double[] xx, out double[] yy)
        {
            var xmin = x.Min();
            var xmax = x.Max();
            var ymin = y.Min();
            var ymax = y.Max();
            var lefttop = x.Select((s, i) => new Tuple<double, int>((s - xmin) * (s - xmin) + (y[i] - ymin) * (y[i] - ymin), i)).OrderBy(m => m.Item1).First().Item2;
            var righttop = x.Select((s, i) => new Tuple<double, int>((s - xmax) * (s - xmax) + (y[i] - ymin) * (y[i] - ymin), i)).OrderBy(m => m.Item1).First().Item2;
            var rightbottom = x.Select((s, i) => new Tuple<double, int>((s - xmax) * (s - xmax) + (y[i] - ymax) * (y[i] - ymax), i)).OrderBy(m => m.Item1).First().Item2;
            var leftbottom = x.Select((s, i) => new Tuple<double, int>((s - xmin) * (s - xmin) + (y[i] - ymax) * (y[i] - ymax), i)).OrderBy(m => m.Item1).First().Item2;

            xx = new double[] { x[lefttop], x[righttop], x[rightbottom], x[leftbottom] };
            yy = new double[] { y[lefttop], y[righttop], y[rightbottom], y[leftbottom] };
        }
        
        public override string ToString()
        {
           return string.Format("[{0}],[{1}]" ,string.Join(",", x) , string.Join(",", y));
        }

    }
    public struct FFTResult
    {
        public double area;
        public double width;
        public double height;
        public double depth;
        public double MinZ;
        public double MaxZ;
        public double radius;
        public FFTResult(double area, double width, double height, double depth, double minz, double maxz,double radius)
        {
            this.area = area;
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.MaxZ = maxz;
            this.MinZ = minz;
            this.radius = radius;
        }
        public override string ToString()
        {
            return string.Format("[area={0},width={1},height={2},depth={3},minz={4},maxz={5},radisu={6}]", area, width, height, depth, MaxZ, MinZ,radius);
        }
    }

    public class Contours
    {
        public double[] x { get; set; }
        public double[] y { get; set; }

        public int count
        {
            get { return x?.Length ?? 0; }
        }
        public Contours(double[] x, double[] y)
        {
            this.x = x;
            this.y = y;
        }
    }



    public class FindLine
    {
        public static void Process(HObject image, HObject rect, out HObject roicontour, FindLineParam pm, out HTuple rows, out HTuple cols, out HTuple rowbegin, out HTuple colbegin, out HTuple rowend, out HTuple colend)
        {
            rowbegin = null;
            colbegin = null;
            rowend = null;
            colend = null;
            roicontour = null;
            rows = null;
            cols = null;
            if (image == null || rect == null) return;
            var line = new RoiLine(rect, pm.OrienTion, pm.lineSpan);
            HTuple metrologyHandle;
            HTuple index;
            HOperatorSet.GetImageSize(image, out var width, out var height);
            HOperatorSet.CreateMetrologyModel(out metrologyHandle);
            try
            {

                HOperatorSet.SetMetrologyModelImageSize(metrologyHandle, width, height);
                HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, line.rowbegin, line.colbegin, line.rowend, line.colend, line.len2, 1, pm.sigma, pm.Amplitude, new HTuple(), new HTuple(), out index);
                var len = Math.Min(Math.Max(1.0, pm.lineSpan), line.lenght / 3);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "measure_distance", len);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "measure_transition", MetrologyM.GetTransitionString(pm.transition));
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "measure_select", MetrologyM.GetSelectString(pm.SelectPos));
                HOperatorSet.ApplyMetrologyModel(image, metrologyHandle);
                HTuple nr, nc, dist;
                HOperatorSet.GetMetrologyObjectMeasures(out roicontour, metrologyHandle, index, MetrologyM.GetTransitionString(pm.transition), out rows, out cols);
                if (rows.Length > 2)
                {
                    HOperatorSet.GenContourPolygonXld(out var contours, rows, cols);
                    HOperatorSet.FitLineContourXld(contours, "tukey", -1, 0, 5, 2.0, out rowbegin, out colbegin, out rowend, out colend, out nr, out nc, out dist);
                    contours.Dispose();
                }

            }
            catch (Exception)
            {


            }
            HOperatorSet.ClearMetrologyModel(metrologyHandle);
        }


        public static void FineRect(HObject image, HObject rect, out HObject roicontour, FindLineParam pm, out HTuple rows, out HTuple cols, out HTuple rowbegin, out HTuple colbegin, out HTuple rowend, out HTuple colend)
        {
            rowbegin = null;
            colbegin = null;
            rowend = null;
            colend = null;
            roicontour = null;
            rows = null;
            cols = null;
            if (image == null || rect == null) return;
            var line = new RoiLine(rect, pm.OrienTion, pm.lineSpan);
            HTuple metrologyHandle;
            HTuple index;
            HOperatorSet.GetImageSize(image, out var width, out var height);
            HOperatorSet.CreateMetrologyModel(out metrologyHandle);
            try
            {

                HOperatorSet.SetMetrologyModelImageSize(metrologyHandle, width, height);
                HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, line.rowbegin, line.colbegin, line.rowend, line.colend, line.len2, 1, pm.sigma, pm.Amplitude, new HTuple(), new HTuple(), out index);
                //HOperatorSet.AddMetrologyObjectRectangle2Measure(metrologyHandle,)

                var len = Math.Min(Math.Max(1.0, pm.lineSpan), line.lenght / 3);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "measure_distance", len);
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "measure_transition", MetrologyM.GetTransitionString(pm.transition));
                HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "measure_select", MetrologyM.GetSelectString(pm.SelectPos));
                HOperatorSet.ApplyMetrologyModel(image, metrologyHandle);
                HTuple nr, nc, dist;
                HOperatorSet.GetMetrologyObjectMeasures(out roicontour, metrologyHandle, index, MetrologyM.GetTransitionString(pm.transition), out rows, out cols);
                if (rows.Length > 2)
                {
                    HOperatorSet.GenContourPolygonXld(out var contours, rows, cols);
                    HOperatorSet.FitLineContourXld(contours, "tukey", -1, 0, 5, 2.0, out rowbegin, out colbegin, out rowend, out colend, out nr, out nc, out dist);
                    contours.Dispose();
                }

            }
            catch (Exception)
            {


            }
            HOperatorSet.ClearMetrologyModel(metrologyHandle);
        }



        public enum MetrTransition
        {
            all = 0,
            /// <summary>
            /// 暗到明
            /// </summary>
            positive = 1,
            /// <summary>
            /// 明到暗
            /// </summary>
            negative = 2

        }
        public enum MetrOrientation
        {
            leftright = 0,
            rightleft = 1,
            topbottom = 2,
            bottomtop = 3
        }
        public enum MetrSelectPos
        {
            all = 0,
            first = 1,
            last = 2

        }
        public class FindLineParam
        {
            public int lineSpan = 10;
            public MetrTransition transition;
            public MetrOrientation OrienTion;
            public MetrSelectPos SelectPos;
            public double sigma = 3;
            public int Amplitude = 45;

        }
        public static class MetrologyM
        {
            public static string GetTransitionString(MetrTransition t)
            {
                switch (t)
                {
                    case MetrTransition.all:
                    default:
                        return "all";
                    case MetrTransition.positive:
                        return "positive";
                    case MetrTransition.negative:
                        return "negative";
                }
            }
            public static string GetSelectString(MetrSelectPos s)
            {
                switch (s)
                {
                    case MetrSelectPos.first:
                        return "first";
                    case MetrSelectPos.last:
                        return "last";
                    default:
                        return "all";
                }
            }

        }
        public static class RandomColor
        {
            public static string[] crosscolor { get; } = new string[] { "green", "blue", "#00ff7f", "#ffb529", "#e53019", "#fa7f4f", "#8343b3", "#00ffee", "#ff00ff", "#6959cf", "#ffff00" };


        }

        class RoiLine
        {
            public HTuple rowbegin;
            public HTuple colbegin;
            public HTuple rowend;
            public HTuple colend;
            //   public HTuple phi;
            public HTuple len1;
            public HTuple len2;
            public HTuple lenght;
            [Serializable]
            public struct PointD
            {
                public double X;
                public double Y;
                public PointD(double x, double y)
                {
                    this.X = x;
                    this.Y = y;
                }
                public static PointD operator +(PointD a, PointD b)
                {
                    return new PointD(a.X + b.X, a.Y + b.Y);
                }
                public static PointD operator -(PointD a, PointD b)
                {
                    return new PointD(a.X - b.X, a.Y - b.Y);
                }
                public static PointD operator *(PointD a, double f)
                {
                    return new PointD(a.X * f, a.Y * f);
                }
                public static PointD operator /(PointD a, double f)
                {
                    return new PointD(a.X / f, a.Y / f);
                }

            }

            public RoiLine(HObject roi, MetrOrientation or, int grap)
            {

                if (grap < 3) grap = 3;
                double[] rows, cols;
                var list = get4points(roi, out rows, out cols);
                double l1 = 0, l2 = 0;
                switch (or)
                {
                    case MetrOrientation.leftright:
                        list.Sort((x, y) => x.X.CompareTo(y.X));
                        rowbegin = (list[2].X + list[3].X) / 2;
                        colbegin = (list[2].Y + list[3].Y) / 2;
                        rowend = (list[0].X + list[1].X) / 2;
                        colend = (list[0].Y + list[1].Y) / 2;
                        l2 = Math.Sqrt(Math.Pow(list[2].X - list[3].X, 2) + Math.Pow(list[2].Y - list[3].Y, 2));
                        break;
                    case MetrOrientation.rightleft:
                        list.Sort((x, y) => x.X.CompareTo(y.X));
                        rowbegin = (list[0].X + list[1].X) / 2;
                        colbegin = (list[0].Y + list[1].Y) / 2;
                        rowend = (list[2].X + list[3].X) / 2;
                        colend = (list[2].Y + list[3].Y) / 2;
                        l2 = Math.Sqrt(Math.Pow(list[2].X - list[3].X, 2) + Math.Pow(list[2].Y - list[3].Y, 2));
                        break;
                    case MetrOrientation.bottomtop:
                        list.Sort((x, y) => x.Y.CompareTo(y.Y));
                        rowbegin = (list[2].X + list[3].X) / 2;
                        colbegin = (list[2].Y + list[3].Y) / 2;
                        rowend = (list[0].X + list[1].X) / 2;
                        colend = (list[0].Y + list[1].Y) / 2;
                        l2 = Math.Sqrt(Math.Pow(list[2].X - list[3].X, 2) + Math.Pow(list[2].Y - list[3].Y, 2));
                        break;
                    case MetrOrientation.topbottom:
                        list.Sort((x, y) => x.Y.CompareTo(y.Y));
                        rowbegin = (list[0].X + list[1].X) / 2;
                        colbegin = (list[0].Y + list[1].Y) / 2;
                        rowend = (list[2].X + list[3].X) / 2;
                        colend = (list[2].Y + list[3].Y) / 2;
                        l2 = Math.Sqrt(Math.Pow(list[2].X - list[3].X, 2) + Math.Pow(list[2].Y - list[3].Y, 2));
                        break;
                    default:
                        break;
                }
                l1 = Math.Sqrt(Math.Pow(rowbegin - rowend, 2) + Math.Pow(colbegin - colend, 2));
                this.len2 = l2 / 2;
                this.len1 = l1 / (2 * grap);
                lenght = l1;
            }
            private List<PointD> get4points(HObject roi, out double[] rows, out double[] cols)
            {
                HTuple row, col, phi, len1, len2;
                HOperatorSet.SmallestRectangle2(roi, out row, out col, out phi, out len1, out len2);
                HTuple cos = phi.TupleCos();
                HTuple sin = phi.TupleSin();
                double a = -len1 * cos - len2 * sin;
                double b = len1 * sin - len2 * cos;
                //point2
                double c = len1 * cos - len2 * sin;
                double d = -len1 * sin - len2 * cos;
                //point3
                double e = len1 * cos + len2 * sin;
                double f = -len1 * sin + len2 * cos;
                //point4
                double g = -len1 * cos + len2 * sin;
                double h = len1 * sin + len2 * cos;
                cols = new double[] { a, c, e, g } + col;
                rows = new double[] { b, d, f, h } + row;
                List<PointD> list = new List<PointD>();
                for (int i = 0; i < rows.Length; i++)
                {
                    list.Add(new PointD(rows[i], cols[i]));
                }
                return list;
            }
        }
    }
}
