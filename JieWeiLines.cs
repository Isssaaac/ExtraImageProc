using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ImageProcessLib
{
    public class JieWeiLines
    {
        public static bool JWSelectLines(UInt64 LvImage, int LineWidth, int width, int height, double[] RoiPoints, JWLinesParam param, out Contours[] contours)
        {
            if (param == null) param = new JWLinesParam();
            ImageProc.LvToHalcon(LvImage, LineWidth, width, height, 0, out var image);
            HObject ReduceImage = image;
            if (RoiPoints?.Length != 0)
            {
                ImageProc.GetROIPoint(RoiPoints, out var roix, out var roiy);
                HOperatorSet.GenRegionPolygonFilled(out var region, roiy, roix);
                HOperatorSet.ReduceDomain(image, region, out ReduceImage);
                region.Dispose();
            }
            LinePram lineparam = new LinePram()
            {
                completeJunctions = param.completeJunctions,
                maxLineWidth = param.maxLineWidth,
                contrast = param.contrast,
                ligthDark = param.ligthDark,
                extractWidth = param.extractWidth,
                minlineLength = param.minlineLength,
                maxLineLength = param.maxLineLength,
                unionMaxdisAbs = param.unionMaxdisAbs,
                unionMaxdisRel = param.unionMaxdisRel,
                unionMaxShift = param.unionMaxShift,
                unionMaxAngle = param.unionMaxAngle,
                lineMinAngle = param.lineMinAngle,
                lineMaxAngle = param.lineMaxAngle
            };
            var res = ImageProc.SelectLines(ReduceImage, lineparam, out var lineContours, out _, out _);
            contours = lineContours.OrderByDescending(x => x.lenght).Take(2).ToArray();
            if (contours.Length <= 0)
            {
                LineEdgeparam edgesparam = new LineEdgeparam()
                {
                    Alpha = param.Alpha,
                    minContrast = param.minContrast,
                    maxContrast = param.maxContrast,
                    minlineLength = param.minlineLength,
                    maxLineLength = param.maxLineLength,
                    unionMaxdisAbs = param.unionMaxdisAbs,
                    unionMaxdisRel = param.unionMaxdisRel,
                    unionMaxShift = param.unionMaxShift,
                    unionMaxAngle = param.unionMaxAngle,
                    lineMinAngle = param.lineMinAngle,
                    lineMaxAngle = param.lineMaxAngle
                };
                res= ImageProc.SelectLineEdges(ReduceImage, edgesparam, out var edges);
                contours = edges.OrderByDescending(x => x.lenght).Take(2).ToArray();
            }
            image.Dispose();
            ReduceImage.Dispose();
           // GC.Collect();
            return res;
        }
        public class JWLinesParam
        {

            public double maxLineWidth { get; set; } = 10;
            public double[] contrast { get; set; } = new double[] { 50 };
            public bool ligthDark { get; set; } = false;
            public bool extractWidth { get; set; } = true;
            public bool completeJunctions { get; set; } = false;

            public double Alpha { get; set; } = 5;
            public double minContrast { get; set; } = 20;
            public double maxContrast { get; set; } = 45;

            public double minlineLength { get; set; } = 200;
            public double maxLineLength { get; set; } = 100000;

            public double unionMaxdisAbs { get; set; } = 10;

            public double unionMaxdisRel { get; set; } = 10;

            public double unionMaxShift { get; set; } = 10;

            public double unionMaxAngle { get; set; } = 10;

            public double[] lineMinAngle { get; set; }
            public double[] lineMaxAngle { get; set; }
        }
    }

}
