using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using static ImageProcessLib.D3.ExtraDll;

namespace ImageProcessLib.D3
{
    public class DefectDetect
    {//E:\\t1\\六面检测修改版20230702\\data\\3Ddebug\\

        public static HObject ChangeToImage(int[] data, int width, int height)
        {
            //    HObject image = null;

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var psrc = handle.AddrOfPinnedObject();
            try
            {
                HOperatorSet.GenImage1(out var image, "int4", width, height, psrc);
                psrc = IntPtr.Zero;
                handle.Free();
                return image;
            }
            catch
            {


            }
            psrc = IntPtr.Zero;
            handle.Free();
            return null;
        }

        public static HObject ChangeToImage(int[,] data)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var psrc = handle.AddrOfPinnedObject();
            try
            {
                HOperatorSet.GenImage1(out var image, "int4", data.GetLength(0), data.GetLength(1), psrc);
                psrc = IntPtr.Zero;
                handle.Free();
                return image;
            }
            catch
            {


            }
            psrc = IntPtr.Zero;
            handle.Free();
            return null;
        }
        public static void WriteImageTiff(HObject image, string filepath)
        {
            HOperatorSet.WriteImage(image, "tiff", 0, filepath);
        }

        private static void ReadECD(string file, out byte[] data, out int width, out int height, out double _xinterval, out double _yinterval)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                byte[] head = new byte[10240];
                fs.Read(head, 0, head.Length);
                width = BitConverter.ToInt32(head, 4);
                height = BitConverter.ToInt32(head, 8);
                _xinterval = BitConverter.ToDouble(head, 16);
                _yinterval = BitConverter.ToDouble(head, 24);
                data = new byte[width * height * 4];
                fs.Read(data, 0, data.Length);
            }
        }
        public static void ReadECD(string file, out int[] data, out int width, out int height, out double _xinterval, out double _yinterval)
        {
            ReadECD(file, out byte[] src, out width, out height, out _xinterval, out _yinterval);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var psrc = handle.AddrOfPinnedObject();
            data = new int[src.Length / 4];
            var dsthandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var pdst = dsthandle.AddrOfPinnedObject();
            MemCopy(pdst, psrc, src.Length);
            psrc = IntPtr.Zero;
            pdst = IntPtr.Zero;
            handle.Free();
            dsthandle.Free();
            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalMilliseconds);
        }
        public static int[] ReadECD(string file, out Head head)
        {
            ReadECD(file, out int[] data, out var width, out var height, out var _xinterval, out var _yinterval);
            head = new Head();
            head.row = height;
            head.col = width;
            head._xInterval = _xinterval;
            head._yInterval = _yinterval;
            return data;
        }

        public static void ECDToImage(string file, out HObject image, out double _xinterval, out double _yinterval)
        {
            ReadECD(file, out byte[] data, out var width, out var height, out _xinterval, out _yinterval);
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var psrc = handle.AddrOfPinnedObject();
            image = null;
            try
            {
                HOperatorSet.GenImage1(out image, "int4", width, height, psrc);
                //HOperatorSet.WriteImage(image, new HTuple("himg"), "", "test");
            }
            catch
            {
            }
            psrc = IntPtr.Zero;
            handle.Free();
        }

        public static void HalconToLv(UInt64 LVImage, HObject Image, int lineWidth, int width, int height)
        {
            if (Image == null) return;
            HTuple type, xwidth, yheight, ptr;
            HOperatorSet.GetImagePointer1(Image, out ptr, out type, out xwidth, out yheight);
            if (ptr.Type == HTupleType.EMPTY) return;
            if (xwidth != width || yheight != height)
            {
                throw new Exception("not Equal Image size ！");
            }
            IntPtr pimg = new IntPtr((long)LVImage);
            for (int i = 0; i < height; i++)
            {
                IntPtr psrc = new IntPtr(ptr.L + i * xwidth.I);
                IntPtr pdst = pimg + i * lineWidth;
                MemCopy(pdst, psrc, xwidth);
            }
        }

        public static void GetFrameCountours3(HObject image, out HObject validImage, out HObject frameContour, PolygonROI roi, out double[] x, out double[] y)
        {
            x = null;
            y = null;
            validImage = image;
            frameContour = null;
            if (image == null) return;
            HObject reduceImage = image;
            if (roi != null)
            {
                roi.GenRegion(out var roiregion);
                HOperatorSet.ReduceDomain(image, roiregion, out reduceImage);
                roiregion.Dispose();
            }

            HOperatorSet.Threshold(reduceImage, out var region, FromReal(-10), FromReal(10));
            HOperatorSet.OpeningCircle(region, out region, 20);
            HOperatorSet.Connection(region, out var regions);
            HOperatorSet.SelectShapeStd(regions, out regions, "max_area", 1);
            HOperatorSet.ReduceDomain(reduceImage, regions, out validImage);

            HOperatorSet.GenContourRegionXld(regions, out var contours, "border");

            if (roi.margin.AllSame())
            {
                HOperatorSet.ShapeTransXld(contours, out contours, "convex");
                HOperatorSet.GenParallelContourXld(contours, out contours, "regression_normal", -roi.margin.left);
                HOperatorSet.GenRegionContourXld(contours, out regions, "filled");
                HOperatorSet.OpeningCircle(regions, out regions, 3.5);
                HOperatorSet.Connection(regions, out regions);
                HOperatorSet.SelectShapeStd(regions, out regions, "max_area", 1);
            }
            else
            {

                HOperatorSet.SmallestRectangle1(regions, out var row1, out var col1, out var row2, out var col2);
                var r1 = row1 + roi.margin.top;
                var c1 = col1 + roi.margin.left;
                var r2 = row2 - roi.margin.bottom;
                var c2 = col2 - roi.margin.right;
                HOperatorSet.VectorToHomMat2d(new double[] { row1.D, row1.D, row2.D, row2.D }, new double[] { col1.D, col2.D, col2, col1.D }
                , new double[] { r1.D, r1.D, r2.D, r2.D }, new double[] { c1.D, c2.D, c2.D, c1.D }, out var hommat2d);
                HOperatorSet.AffineTransRegion(regions, out regions, hommat2d, "nearest_neighbor");
            }
            FitRectPoly(image, regions, out regions, out var frameinfo);
            HOperatorSet.GenContourRegionXld(regions, out frameContour, "border");
            HOperatorSet.GetContourXld(frameContour, out var row, out var col);

            //HOperatorSet.SegmentContoursXld(frameContour,out var split,"lines_circles",5,4,)


            x = col.DArr;
            y = row.DArr;

            region.Dispose();
            regions.Dispose();

            contours.Dispose();
            if (reduceImage != image)
            {
                reduceImage.Dispose();
            }
        }


        public static void GetFrameCountours(HObject image, out HObject domainRegion, out HObject renderRegion, out HObject frameRegion, PolygonROI roi, out FrameInfo frameinfo)
        {
            HOperatorSet.GenEmptyObj(out frameRegion);
            HOperatorSet.GenEmptyObj(out domainRegion);
            HOperatorSet.GenEmptyObj(out renderRegion);
            frameinfo = new FrameInfo();
            if (image == null) return;
            HObject reduceImage = image;
            if (roi?.x != null && roi?.y != null)
            {
                roi.GenRegion(out var roiregion);
                //这里的roi是可以包含整个电芯面的roi，获取到的是包含电芯面并且包含一部分黑边的定义域
                HOperatorSet.ReduceDomain(image, roiregion, out reduceImage);
                roiregion.Dispose();
            }
            else
                return;
            //  Stopwatch sw = new Stopwatch();
            //   sw.Start();
            //这里的roi是找到有效的电芯数据
            HOperatorSet.Threshold(reduceImage, out domainRegion, FromReal(-10), FromReal(10));
            HOperatorSet.ReduceDomain(reduceImage, domainRegion, out var validImage);
            HOperatorSet.GrayRangeRect(validImage, out var imageresult, 3, 3); //3*3内最大的灰度减去最小的灰度，如果超过0.5的就出来
            var depth = roi.FrameDepth == 0 ? 0.5 : roi.FrameDepth;
            HOperatorSet.Threshold(imageresult, out var fregion, 0, FromReal(depth));
            HOperatorSet.OpeningCircle(fregion, out var openingcircle, 5);
            HOperatorSet.Connection(openingcircle, out var regions);
            HOperatorSet.SelectShapeStd(regions, out var selectregions, "max_area", 1);
            
            HOperatorSet.Intersection(domainRegion, selectregions, out renderRegion);

            FitRectPoly(image, selectregions, out frameRegion, out frameinfo, true, roi.roundedCornersize);
            //maskROI是屏蔽区域
            if (roi.maskROI != null && roi.bmaskTrans && frameinfo.area > 0)
            {
                //Stopwatch sw = new Stopwatch();
                //sw.Start();
                HOperatorSet.GenEmptyRegion(out var maskregion);
                for (int i = 0; i < roi.maskROI?.Length; i++)
                {
                    roi.maskROI[i].GenRegion(out var mregion);
                    HOperatorSet.ConcatObj(maskregion, mregion, out var temp);
                    mregion.Dispose();
                    maskregion.Dispose();
                    maskregion = temp;
                }
                // Console.WriteLine(frameinfo.ToString());
                HOperatorSet.Union1(maskregion, out var maskregions);
                if (roi.rowbase?.Length == 4 && roi.colbase?.Length == 4 && frameinfo.area > 0)
                {
                    //HOperatorSet.VectorAngleToRigid(roi.rowbase, roi.colbase, anglesrc, frameinfo.cy, frameinfo.cx, angledst, out var homat);
                    HOperatorSet.VectorToHomMat2d(roi.rowbase, roi.colbase, frameinfo.y, frameinfo.x, out var homat);
                    HOperatorSet.AffineTransRegion(maskregions, out maskregions, homat, "nearest_neighbor");
                }
                HOperatorSet.Difference(domainRegion, maskregions, out domainRegion);
                HOperatorSet.Difference(renderRegion, maskregions, out renderRegion);

                //HOperatorSet.AreaCenter(domainRegion, out var area, out var a, out var b);

                //frameRegion = maskregions;
                //return;

                validImage.Dispose();
                maskregion.Dispose();
                maskregions.Dispose();
                //sw.Stop();
                //Console.WriteLine(sw.Elapsed.TotalMilliseconds);
            }

            openingcircle.Dispose();
            selectregions.Dispose();

            fregion.Dispose();
            imageresult.Dispose();
            regions.Dispose();
            if (reduceImage != image)
            {
                reduceImage.Dispose();
            }
            //sw.Stop();
            // Console.WriteLine(sw.Elapsed.TotalMilliseconds);
        }

        public static void GetImageSize(HObject image, out int width, out int height)
        {
            HOperatorSet.GetImageSize(image, out var w, out var h);
            width = w.I;
            height = h.I;
        }

       
        public static void TransToPlane(HObject image, HObject region, HObject refregion, out HObject imagePlane, out HTuple meanvalue)
        {

            meanvalue = 0.0;
            // Stopwatch sw = new Stopwatch();
            //  sw.Start();
            HOperatorSet.ReduceDomain(image, region, out imagePlane);
            HOperatorSet.CopyImage(imagePlane, out imagePlane);
            // sw.Stop();
            // Console.WriteLine(sw.Elapsed.TotalMilliseconds);
            HOperatorSet.GetImageSize(image, out var width, out var height);
            HOperatorSet.SmallestRectangle1(refregion, out var row1, out var col1, out var row2, out var col2);
            var ystep = Math.Max((row2 - row1) / 10, 2);
            var xstep = Math.Max((col2 - col1) / 10, 2);

            // testobj = region;
            HOperatorSet.GenGridRegion(out var grid, ystep, xstep, "points", width, height);
            // 生成全图点阵 → 与 region 求交 → 再与 refregion 求交
            // 最终 grid 只保留同时落在 region 和 refregion 内的稀疏采样点
            HOperatorSet.Intersection(region, grid, out grid);
            HOperatorSet.Intersection(grid, refregion, out grid);
            HOperatorSet.GetRegionPoints(grid, out var gridrow, out var gridcol);

            if (gridrow.Length > 10)
            {
                HOperatorSet.GetGrayval(image, gridrow, gridcol, out var gridgray);
                gridgray = gridgray / fscale;
                var mean = gridgray.TupleMean();
                meanvalue = mean * fscale;
                HOperatorSet.VectorToHomMat3d("similarity", gridrow, gridcol, gridgray, gridrow, gridcol, HTuple.TupleGenConst(gridgray.Length, mean), out var hommat3d);


                HOperatorSet.GetRegionPoints(region, out var rows, out var columns);
                // testobj = regiondilation; return;

                HOperatorSet.GetGrayval(image, rows, columns, out var allgrays);
                allgrays = allgrays / fscale;
                HOperatorSet.AffineTransPoint3d(hommat3d, rows, columns, allgrays, out var affrows, out var affcols, out var newgrays);
                newgrays = newgrays * fscale;
                HOperatorSet.SetGrayval(imagePlane, rows, columns, newgrays);
            }
            grid.Dispose();
            //regiondilation.Dispose();
        }

        public static void DetectPlane_Radius(HObject image, HObject region, HObject refregion, PolygonROI roi, HTuple mean, out Contours[] contours, out FFTResult[] results, out double[] CircleRadius)
        {
            results = new FFTResult[0];
            contours = new Contours[0];
            CircleRadius = new double[0];
            if (!roi.bEnablePlane) return;
            if (image == null || region == null || refregion == null)
                return;
            HOperatorSet.Intersection(region, refregion, out var reduceregion);
            HOperatorSet.ReduceDomain(image, reduceregion, out HObject imagereduced);
            HOperatorSet.Threshold(imagereduced, out var validregion, mean - FromReal(roi.checksize.planedepth), mean + FromReal(roi.checksize.planedepth));
            HOperatorSet.Difference(reduceregion, validregion, out validregion);

            HOperatorSet.Connection(validregion, out validregion);
            var minsize = roi.checksize.planedefectArea / roi.checksize._xInterval / roi.checksize._yInterval;
            minsize = Math.Max(1, minsize);
            HOperatorSet.SelectShape(validregion, out validregion, "area", "and", minsize, 10000 * 10000);
            HOperatorSet.AreaCenter(validregion, out var area, out _, out _);
            HOperatorSet.SmallestRectangle1(validregion, out var row1, out var col1, out var row2, out var col2);
            HOperatorSet.GenContourRegionXld(validregion, out var conxld, "border");

            int len = validregion.CountObj();
            results = new FFTResult[len];
            contours = new Contours[len];
            for (int i = 0; i < len; i++)
            {
                HOperatorSet.MinMaxGray(validregion[i + 1], imagereduced, 1, out var mingray, out var maxgray, out var range);
                HOperatorSet.GetContourXld(conxld[i + 1], out var crow, out var ccol);

                HOperatorSet.SmallestCircleXld(conxld[i + 1], out var circlerow, out var circlecol, out var circlxradius);
                CircleRadius[i] = circlxradius;

                contours[i] = new Contours(ccol.DArr, crow.DArr);
                var h = ToReal((int)(maxgray - mean).D);
                var d = ToReal((int)(mingray - mean).D);
                results[i].depth = Math.Abs(h) > Math.Abs(d) ? h : d;
                results[i].width = (col2[i] - col1[i]) * roi.checksize._xInterval;
                results[i].height = (row2[i] - row1[i]) * roi.checksize._yInterval;
                results[i].area = area[i] * roi.checksize._xInterval * roi.checksize._yInterval;
                results[i].MaxZ = ToReal((int)maxgray.D);
                results[i].MinZ = ToReal((int)mingray.D);
            }

            conxld.Dispose();
            reduceregion.Dispose();
            imagereduced.Dispose();
            validregion.Dispose();
        }


        public static void DetectPlane(HObject image, HObject region, HObject refregion, PolygonROI roi, HTuple mean, out Contours[] contours, out FFTResult[] results)
        {
            results = new FFTResult[0];
            contours = new Contours[0];
            if (!roi.bEnablePlane) return;
            if (image == null || region == null || refregion == null)
                return;
            HOperatorSet.Intersection(region, refregion, out var reduceregion);
            HOperatorSet.ReduceDomain(image, reduceregion, out HObject imagereduced);
            HOperatorSet.Threshold(imagereduced, out var validregion, mean - FromReal(roi.checksize.planedepth), mean + FromReal(roi.checksize.planedepth));
            HOperatorSet.Difference(reduceregion, validregion, out validregion);

            HOperatorSet.Connection(validregion, out validregion);
            var minsize = roi.checksize.planedefectArea / roi.checksize._xInterval / roi.checksize._yInterval;
            minsize = Math.Max(1, minsize);
            HOperatorSet.SelectShape(validregion, out validregion, "area", "and", minsize, 10000 * 10000);
            HOperatorSet.AreaCenter(validregion, out var area, out _, out _);
            HOperatorSet.SmallestRectangle1(validregion, out var row1, out var col1, out var row2, out var col2);
            HOperatorSet.GenContourRegionXld(validregion, out var conxld, "border");

            int len = validregion.CountObj();
            results = new FFTResult[len];
            contours = new Contours[len];
            for (int i = 0; i < len; i++)
            {
                HOperatorSet.MinMaxGray(validregion[i + 1], imagereduced, 1, out var mingray, out var maxgray, out var range);
                HOperatorSet.GetContourXld(conxld[i + 1], out var crow, out var ccol);

                HOperatorSet.SmallestCircleXld(conxld[i + 1], out var circlerow, out var circlecol, out var circleradius);


                contours[i] = new Contours(ccol.DArr, crow.DArr);
                var h = ToReal((int)(maxgray - mean).D);
                var d = ToReal((int)(mingray - mean).D);
                results[i].depth = Math.Abs(h) > Math.Abs(d) ? h : d;
                results[i].width = (col2[i] - col1[i]) * roi.checksize._xInterval;
                results[i].height = (row2[i] - row1[i]) * roi.checksize._yInterval;
                results[i].area = area[i] * roi.checksize._xInterval * roi.checksize._yInterval;
                results[i].MaxZ = ToReal((int)maxgray.D);
                results[i].MinZ = ToReal((int)mingray.D);
            }

            conxld.Dispose();
            reduceregion.Dispose();
            imagereduced.Dispose();
            validregion.Dispose();
        }


     

        static readonly double fscale = 10000.0;
        public static int maxSearchNumber = 5;
        public static void DetectGridPlane(HObject image, HObject region, HObject refregion, PolygonROI roi, HTuple mean, out Contours[] contours, out FFTResult[] results)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            results = new FFTResult[0];
            contours = new Contours[0];

            HOperatorSet.GetImageSize(image, out var width, out var height);
            HOperatorSet.SmallestRectangle1(refregion, out var row1, out var col1, out var row2, out var col2);
            var ystep = (int)new double[] { (row2 - row1) / 10, 5 / roi.checksize._yInterval, 100 }.Max();
            var xstep = (int)new double[] { (col2 - col1) / 10, 5 / roi.checksize._xInterval, 100 }.Max();
            HOperatorSet.GenGridRegion(out var grid, ystep, xstep, "lines", width, height);
            HOperatorSet.Difference(refregion, grid, out var diffs);
            HOperatorSet.Intersection(region, diffs, out diffs);

            HOperatorSet.Connection(diffs, out diffs);
            int len = diffs.CountObj();
            HOperatorSet.SmallestRectangle1(diffs, out var srow1, out var scol1, out var srow2, out var scol2);

            HOperatorSet.GenEmptyRegion(out var tempregion);
            for (int i = 0; i < len; i++)
            {
                var swidth = scol2 - scol1;
                var sheight = srow2 - srow1;
                var wdial = swidth[i] >= xstep / 2 ? 0.0 : 2;
                var hdia = sheight[i] >= xstep / 2 ? 0.0 : 2;
                if (wdial != 0 && hdia != 0)
                {
                    HOperatorSet.DilationRectangle1(diffs[i + 1], out var dillation, wdial, hdia);
                    HOperatorSet.ConcatObj(tempregion, dillation, out tempregion);
                    dillation.Dispose();
                }
            }
            HOperatorSet.Union2(diffs, tempregion, out diffs);
            tempregion.Dispose();
            HOperatorSet.Intersection(region, diffs, out diffs);
            HOperatorSet.Intersection(diffs, refregion, out diffs);
            HOperatorSet.Connection(diffs, out diffs);
            HOperatorSet.MinMaxGray(diffs, image, 0, out var min, out var max, out var range);

            len = diffs.CountObj();
            //HOperatorSet.
            var median = (min + max) / 2.0;

            var dep = roi.checksize.Searchdepth;
            
            HOperatorSet.GenEmptyRegion(out var resultregion);
            for (int i = 0; i < len; i++)
            {
                if (range[i] > FromReal(dep))
                    HOperatorSet.Union2(resultregion, diffs[i + 1], out resultregion);
            }
            //  HOperatorSet.DilationRectangle1(resultregion, out resultregion, 1.5, 1.5);
            HOperatorSet.Intersection(region, resultregion, out resultregion);
            HOperatorSet.Connection(resultregion, out resultregion);
            grid.Dispose();
            diffs.Dispose();
          
            len = resultregion.CountObj();
            if (len <= 0)
            {
                resultregion.Dispose();
                return;
            }

            List<Contours> Conlist = new List<Contours>();
            List<FFTResult> fftlist = new List<FFTResult>();


            HOperatorSet.CopyImage(image, out HObject dupImage);
            xstep = Math.Max(xstep / 20, 2);
            ystep = Math.Max(ystep / 20, 2);
            HOperatorSet.GenGridRegion(out var pointsgrid, ystep, xstep, "points", width, height);
            for (int i = 0; i < len; i++)
            {
                var select = resultregion[i + 1];
                HOperatorSet.Intersection(select, pointsgrid, out var gridselect);
                HOperatorSet.GetRegionPoints(gridselect, out var gridrow, out var gridcol);

                if (gridrow.Length < 5)
                {
                    gridselect.Dispose();
                    continue;
                }

                HOperatorSet.GetGrayval(image, gridrow, gridcol, out var gridgray);
                gridgray = gridgray / fscale;
                var hmean = gridgray.TupleMean();
                HOperatorSet.VectorToHomMat3d("similarity", gridrow, gridcol, gridgray, gridrow, gridcol, HTuple.TupleGenConst(gridgray.Length, hmean), out var hommat3d);
                HOperatorSet.GetRegionPoints(select, out var rows, out var columns);
                HOperatorSet.GetGrayval(image, rows, columns, out var allgrays);
                allgrays = allgrays / fscale;
                HOperatorSet.AffineTransPoint3d(hommat3d, rows, columns, allgrays, out var affrows, out var affcols, out var newgrays);
                newgrays = newgrays * fscale;
                HOperatorSet.SetGrayval(dupImage, rows, columns, newgrays);

                var dh = FromReal(dep) * 1;
                HOperatorSet.ReduceDomain(dupImage, select, out var reduced);
                HOperatorSet.Threshold(reduced, out var reducedregion, hmean * fscale - dh, hmean * fscale + dh);
                HOperatorSet.Difference(select, reducedregion, out var dif);
                HOperatorSet.ClosingCircle(dif, out dif, 3.5);
                HOperatorSet.Connection(dif, out dif);
                HOperatorSet.ShapeTrans(dif, out dif, "convex");
                HOperatorSet.Intersection(select, dif, out dif);
                HOperatorSet.Connection(dif, out dif);


                GetRegionResult(reduced, dif, roi, hmean * fscale, out var fcontour, out var fresult);
                Conlist.AddRange(fcontour);
                fftlist.AddRange(fresult);
                //testobj = dif;
                //return;
                gridselect.Dispose();
                reduced.Dispose();


                //  select.Dispose();
                reducedregion.Dispose();
                dif.Dispose();
                if (fftlist.Count > maxSearchNumber) break;
            }
            resultregion.Dispose();
            dupImage.Dispose();
            pointsgrid.Dispose();
            contours = Conlist.ToArray();
            results = fftlist.ToArray();
            sw.Stop();
            Console.WriteLine("plane:{0}", sw.Elapsed.TotalMilliseconds);
            return;
        }

        string GetObjectType(HObject obj)
        {
            if (obj == null || !obj.IsInitialized())
                return "empty";

            try
            {
                HOperatorSet.GetImageType(obj, out _);
                return "image";
            }
            catch { }

            try
            {
                HOperatorSet.AreaCenter(obj, out _, out _, out _);
                return "region";
            }
            catch { }

            try
            {
                HOperatorSet.GetContourXld(obj, out _, out _);
                return "xld";
            }
            catch { }

            return "unknown";
        }

        public static void DetectGridPlaneIterator(HObject image, HObject region, HObject refregion, PolygonROI roi, HTuple mean, out Contours[] contours, out FFTResult[] results)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            results = new FFTResult[0];
            contours = new Contours[0];
            HOperatorSet.GetImageSize(image, out var width, out var height);
            //得到外接矩形
            HOperatorSet.SmallestRectangle1(refregion, out var row1, out var col1, out var row2, out var col2);
            HOperatorSet.Intersection(refregion, region, out var validregion);
            var yrectstep = (int)new double[] { (row2 - row1) / 10, 5 / roi.checksize._yInterval, 50 }.Max();
            var xrectstep = (int)new double[] { (col2 - col1) / 10, 5 / roi.checksize._xInterval, 50 }.Max();
            //制造等距的矩形
            HOperatorSet.GenGridRegion(out var grid, yrectstep, xrectstep, "lines", width, height);
            HOperatorSet.Difference(refregion, grid, out var diffs);
            HOperatorSet.Intersection(region, diffs, out diffs);

            // HOperatorSet.Difference(validregion,grid,out diffs);

            HOperatorSet.Connection(diffs, out diffs);
            int len = diffs.CountObj();
            HOperatorSet.SmallestRectangle1(diffs, out var srow1, out var scol1, out var srow2, out var scol2);

           
            HOperatorSet.GenEmptyRegion(out var tempregion);
            for (int i = 0; i < len; i++)
            {
                var swidth = scol2 - scol1;
                var sheight = srow2 - srow1;
                var wdial = swidth[i] >= xrectstep / 2 ? 0.0 : 2;
                var hdia = sheight[i] >= yrectstep / 2 ? 0.0 : 2;
                if (wdial != 0 && hdia != 0)
                {
                    HOperatorSet.DilationRectangle1(diffs[i + 1], out var dillation, wdial, hdia);
                    HOperatorSet.ConcatObj(tempregion, dillation, out tempregion);
                    dillation.Dispose();
                }
            }
            HOperatorSet.Union2(diffs, tempregion, out diffs);
            tempregion.Dispose();
            //HOperatorSet.Intersection(region, diffs, out diffs);
            //HOperatorSet.Intersection(diffs, refregion, out diffs);
            HOperatorSet.Intersection(validregion, diffs, out diffs);
            HOperatorSet.Connection(diffs, out diffs);
            HOperatorSet.MinMaxGray(diffs, image, 0, out var min, out var max, out var range);


            len = diffs.CountObj();
            //HOperatorSet.
            var median = (min + max) / 2.0;
            var dep = roi.checksize.Searchdepth;
          
            HOperatorSet.GenEmptyRegion(out var resultregion);
            for (int i = 0; i < len; i++)
            {
                if (range[i] > FromReal(dep))
                    HOperatorSet.Union2(resultregion, diffs[i + 1], out resultregion);
            }
            //  HOperatorSet.DilationRectangle1(resultregion, out resultregion, 1.5, 1.5);

            HOperatorSet.Intersection(region, resultregion, out resultregion);
            HOperatorSet.Connection(resultregion, out resultregion);


            grid.Dispose();
            diffs.Dispose();
           
            len = resultregion.CountObj();
            if (len <= 0)
            {
                resultregion.Dispose();
                return;
            }

            List<Contours> Conlist = new List<Contours>();
            List<FFTResult> fftlist = new List<FFTResult>();


            HOperatorSet.CopyImage(image, out HObject dupImage);
            var xstep = Math.Max(xrectstep / 20, 2);
            var ystep = Math.Max(yrectstep / 20, 2);
            HOperatorSet.GenGridRegion(out var pointsgrid, ystep, xstep, "points", width, height);
            HOperatorSet.GenEmptyRegion(out var alternateRegion);
         
            for (int i = 0; i < len; i++)
            {
                PlaneConvex(image, resultregion[i + 1], pointsgrid, dupImage, dep, out var selected, out _);
                HOperatorSet.ConcatObj(alternateRegion, selected, out alternateRegion);
                selected.Dispose();
            }

        
            HOperatorSet.Union1(alternateRegion, out alternateRegion);
            HOperatorSet.DilationCircle(alternateRegion, out var dilationalternateRegion, 10);
            HOperatorSet.Connection(dilationalternateRegion, out dilationalternateRegion);
            HOperatorSet.ShapeTrans(dilationalternateRegion, out dilationalternateRegion, "convex");
          
            HOperatorSet.Intersection(validregion, dilationalternateRegion, out dilationalternateRegion);
            HOperatorSet.Connection(dilationalternateRegion, out dilationalternateRegion);


            len = dilationalternateRegion.CountObj();

            for (int i = 0; i < len; i++)
            {
                var select = dilationalternateRegion[i + 1];
                HOperatorSet.DilationRectangle1(select, out var regionDilation, xrectstep, yrectstep);
                HOperatorSet.Intersection(validregion, regionDilation, out regionDilation);

                HOperatorSet.Difference(regionDilation, dilationalternateRegion, out var diff);
                HOperatorSet.Intersection(diff, pointsgrid, out var selectgrid);

                HOperatorSet.Union2(select, diff, out regionDilation);

                PlaneConvex(image, regionDilation, selectgrid, dupImage, dep, out var selected, out var hmean);
                // testobj = select; return;
                //testobj = null;
                if (hmean?.D != 0)
                {
                    HOperatorSet.ClosingCircle(selected, out selected, 3.5);
                    HOperatorSet.Connection(selected, out selected);
                    HOperatorSet.ShapeTrans(selected, out selected, "convex");
                    HOperatorSet.Intersection(regionDilation, selected, out selected);
                    HOperatorSet.Connection(selected, out selected);
                }
                else
                {
                    selected = select;
                    hmean = mean / fscale;
                }
                GetRegionResult(dupImage, selected, roi, hmean * fscale, out var fcontour, out var fresult);

                Conlist.AddRange(fcontour);
                fftlist.AddRange(fresult);
                regionDilation.Dispose();
                selectgrid.Dispose();
                diff.Dispose();
                selected.Dispose();
                if (fftlist.Count > maxSearchNumber) break;
            }

            //testobj = selected;
            //return;
            validregion.Dispose();
            dilationalternateRegion.Dispose();
            resultregion.Dispose();
            dupImage.Dispose();
            pointsgrid.Dispose();
            contours = Conlist.ToArray();
            results = fftlist.ToArray();
            //sw.Stop();
            //Console.WriteLine("plane:{0}", sw.Elapsed.TotalMilliseconds);
            // return;
        }

     
        public static void PlaneConvex(HObject image, HObject region, HObject pointsgrid, HObject dupImage, double depth, out HObject selected, out HTuple mean)
        {
            HOperatorSet.GetImageType(image, out HTuple type);
            string imgType = type.S; // 返回字符串形式的类型名称

            HOperatorSet.GenEmptyRegion(out selected);
            mean = 0;
            var select = region;
            HOperatorSet.Intersection(select, pointsgrid, out var gridselect);
            HOperatorSet.GetRegionPoints(gridselect, out var gridrow, out var gridcol);
            if (gridrow.Length < 5)
            {
                gridselect.Dispose();
                return;
            }
            HOperatorSet.GetGrayval(image, gridrow, gridcol, out var gridgray);
            gridgray = gridgray / fscale;
            var hmean = gridgray.TupleMean();
            mean = hmean;
            HOperatorSet.VectorToHomMat3d("similarity", gridrow, gridcol, gridgray, gridrow, gridcol, HTuple.TupleGenConst(gridgray.Length, hmean), out var hommat3d);
            HOperatorSet.GetRegionPoints(select, out var rows, out var columns);
            HOperatorSet.GetGrayval(image, rows, columns, out var allgrays);
            allgrays = allgrays / fscale;
            HOperatorSet.AffineTransPoint3d(hommat3d, rows, columns, allgrays, out var affrows, out var affcols, out var newgrays);
            newgrays = newgrays * fscale;
            HOperatorSet.SetGrayval(dupImage, rows, columns, newgrays);

            var dh = FromReal(depth) * 1;
            HOperatorSet.ReduceDomain(dupImage, select, out var reduced);
            //把在平均值的0.12(depth)上下限内的区域筛选出来
            HOperatorSet.Threshold(reduced, out var reducedregion, hmean * fscale - dh, hmean * fscale + dh);
            //剩下的就是突兀点？
            HOperatorSet.Difference(select, reducedregion, out selected);
            reducedregion.Dispose();
            reduced.Dispose();
            gridselect.Dispose();
        }
        private static void GetRegionResult(HObject image, HObject resultregion, PolygonROI roi, HTuple meanvalue, out Contours[] contours, out FFTResult[] result)
        {

            var size = roi.checksize;
            contours = new Contours[0];
            result = new FFTResult[0];

            double cwidth = size.minwidth / size._xInterval;
            double cheight = size.minheight / size._yInterval;
            double carea = cwidth * cheight / 2.0;
            HOperatorSet.SelectShape(resultregion, out var selected, new[] { "area", "width", "height" }, "and", new[] { carea, cwidth, cheight }, new[] { 9999 * 9999, 99999, 99999 });
            HOperatorSet.AreaCenter(selected, out var area, out _, out _);
            //理论上row1，和circleRow的长度一样
            HOperatorSet.SmallestRectangle1(selected, out var row1, out var col1, out var row2, out var col2);

            HOperatorSet.SmallestCircle(selected, out var circleRow, out var circleCol, out var circleRadius);

            int len = selected.CountObj();

            List<FFTResult> reslist = new List<FFTResult>();
            List<Contours> conlist = new List<Contours>();
            for (int i = 0; i < len; i++)
            {
                FFTResult res = new FFTResult();               

                HOperatorSet.GetRegionPoints(selected[i + 1], out var prows, out var pcols);
                

                if (prows.Length <= 0) return;
                HOperatorSet.GetGrayval(image, prows, pcols, out var grayval);

                res.width = Math.Abs(col1[i].D - col2[i].D) * size._xInterval;
                res.height = Math.Abs(row1[i].D - row2[i].D) * size._yInterval;

                res.MinZ = ToReal((int)grayval.TupleMax().D);
                res.MaxZ = ToReal((int)grayval.TupleMin().D);
                res.radius = circleRadius[i].D;

                var h = res.MaxZ - ToReal((int)meanvalue.D);
                var d = res.MinZ - ToReal((int)meanvalue.D);
                res.depth = Math.Abs(h) > Math.Abs(d) ? h : d;
                res.area = area[i].D * size._xInterval * size._yInterval;

                if (Math.Abs(res.depth) > Math.Abs(roi.checksize.maxdepth))
                {
                    reslist.Add(res);
                    HOperatorSet.GenContourRegionXld(selected[i + 1], out var cxld, "border");
                    HOperatorSet.GetContourXld(cxld, out var conrow, out var concol);
                    conlist.Add(new Contours(concol.DArr, conrow.DArr));
                    cxld.Dispose();
                }
                contours = conlist.ToArray();
                result = reslist.ToArray();

                //Console.WriteLine(results[i]);
            }
            selected.Dispose();
        }

        //最终的
        public static HObject testobj;
        public static void Detect(HObject image, out HObject ExtractImage, PolygonROI roi, out FrameInfo frameinfo, out Contours FrameContours, out Contours[] DefectionContours, out FFTResult[] result)
        {
            //HOperatorSet.WriteImage(image, "bigtiff", 0, "D:\\_主文件夹\\_1源代码\\C#程序\\ExtraImageProc\\test");

            DefectionContours = new Contours[0];
            FrameContours = null;
            HOperatorSet.GenEmptyObj(out ExtractImage);
            result = new FFTResult[0];
            frameinfo = new FrameInfo();
            if (roi == null || roi.checksize._xInterval == 0 || roi.checksize._yInterval == 0 || roi.checksize.Searchdepth == 0) return;
            try
            {
                
                HOperatorSet.GetImageSize(image, out var width, out var heigt);

                HOperatorSet.GenImageConst(out ExtractImage, "byte", width, heigt);
                //domainregion是待处理的有效高度
                GetFrameCountours(image, out var domainregion, out var renderRegion, out var frameregion, roi, out frameinfo);

                HOperatorSet.ReduceDomain(image, domainregion, out var imagePlane);

                if (frameinfo.area > 0)
                {
                    ScaleRegion(frameregion, out var region, roi.margin, out var xx, out var yy);
                    HOperatorSet.GenContourRegionXld(region, out var conxld, "border");
                    HOperatorSet.GetContourXld(conxld, out var row, out var col);
                    FrameContours = new Contours(col.DArr, row.DArr);
                    HOperatorSet.GenEmptyRegion(out var planregion);
                    //所有检测都是有投影到平面
                    if (roi.bEnablePlane)
                    {
                        ScaleRegion(frameregion, out planregion, roi.margin + roi.extramargin, out var _, out var _);
                        TransToPlane(imagePlane, domainregion, planregion, out var imagePlane2, out var mean);
                        imagePlane.Dispose();
                        imagePlane = imagePlane2;
                        // ScaleRegion(frameregion, out planregion, roi.margin + roi.extramargin, out var _, out var _);
                        //真正的检测函数
                        DetectGridPlaneIterator(imagePlane, domainregion, planregion, roi, mean, out DefectionContours, out result);
                    }
                    //如果没有投影到平面的话，就执行下面的
                    if (result.Length == 0)
                    {
                        //
                        HOperatorSet.Difference(region, planregion, out var checkregion);
                        HOperatorSet.OpeningCircle(checkregion, out checkregion, 3.5);
                        HOperatorSet.DilationCircle(checkregion, out checkregion, 20);
                        HOperatorSet.Intersection(region, checkregion, out checkregion);

                        var listcontours = new List<Contours>();
                        //真正的检测函数
                        Detect(imagePlane, domainregion, out var filter, checkregion, out var contours, roi, out var fftres);
                        for (int j = 0; j < contours.CountObj(); j++)
                        {
                            HOperatorSet.GetContourXld(contours[j + 1], out var frow, out var fcol);
                            listcontours.Add(new Contours(fcol.DArr, frow.DArr));
                        }
                        Distinct(DefectionContours, result, listcontours, fftres, out DefectionContours, out result);
                        contours.Dispose();
                        filter.Dispose();
                        checkregion.Dispose();
                    }

                    planregion.Dispose();

                    HOperatorSet.ReduceDomain(imagePlane, renderRegion, out var renderImage);
                    HOperatorSet.ScaleImageMax(renderImage, out var imagescaled);

                    //将图像imagescaled的domain中灰度像素，喷到ImageDestination图上，并返回结果到ExtractImage中
                    //这个图像是可以输出到labview上显示的，是byte类型的图像
                    HOperatorSet.PaintGray(imagescaled, ExtractImage, out ExtractImage);

                    renderImage.Dispose();
                    imagescaled.Dispose();
                    region.Dispose();
                    conxld.Dispose();
                }
                imagePlane.Dispose();
                renderRegion.Dispose();
                domainregion.Dispose();
                frameregion.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static void Distinct(IEnumerable<Contours> contours1, IEnumerable<FFTResult> fftresult1, IEnumerable<Contours> contours2, IEnumerable<FFTResult> fftresult2, out Contours[] contours, out FFTResult[] fftresult)
        {
            List<Contours> conlist = new List<Contours>();
            List<FFTResult> fftlist = new List<FFTResult>();
            conlist.AddRange(contours1);
            fftlist.AddRange(fftresult1);
            if (contours2.Count() > 0)
            {

                HOperatorSet.GenEmptyRegion(out var region1);
                for (int i = 0; i < contours1.Count(); i++)
                {
                    HOperatorSet.GenRegionPolygon(out var temp, contours1.ElementAt(i).y, contours1.ElementAt(i).x);
                    HOperatorSet.ConcatObj(region1, temp, out region1);
                    temp.Dispose();
                }
                HOperatorSet.Union1(region1, out region1);
                for (int i = 0; i < contours2.Count(); i++)
                {
                    var con = contours2.ElementAt(i);
                    HOperatorSet.GenRegionPolygon(out var temp, con.y, con.x);
                    HOperatorSet.Intersection(region1, temp, out temp);
                    HOperatorSet.AreaCenter(temp, out var area, out _, out _);
                    if (!(area?.D > 0))
                    {
                        conlist.Add(con);
                        fftlist.Add(fftresult2.ElementAt(i));
                    }
                    temp.Dispose();
                }
                region1.Dispose();
            }
            fftresult = fftlist.ToArray();
            contours = conlist.ToArray();

        }
        public static void GetContours(out HObject hcontour, Contours[] contours)
        {
            HOperatorSet.GenEmptyObj(out hcontour);
            for (int i = 0; i < contours.Length; i++)
            {
                if (contours[i] == null) continue;
                HOperatorSet.GenContourPolygonXld(out var tempcontour, contours[i].y, contours[i].x);
                HOperatorSet.ConcatObj(hcontour, tempcontour, out hcontour);
                tempcontour.Dispose();
            }
        }
        public static void GetContours(out HObject hcontour, Contours contours)
        {
            HOperatorSet.GenEmptyObj(out hcontour);
            if (contours == null) return;
            HOperatorSet.GenContourPolygonXld(out var tempcontour, contours.y, contours.x);
            HOperatorSet.ConcatObj(hcontour, tempcontour, out hcontour);
            tempcontour.Dispose();
        }



        public static void ScaleRegion(HObject region, out HObject PolyRegion, ROIMargin margin, out double[] x, out double[] y)
        {
            x = null;
            y = null;
            HOperatorSet.SmallestRectangle1(region, out var row1, out var col1, out var row2, out var col2);
            var r1 = row1 + margin.top;
            var c1 = col1 + margin.left;
            var r2 = row2 - margin.bottom;
            var c2 = col2 - margin.right;
            if (r1 > r2 || c1 > c2)
            {
                HOperatorSet.GenEmptyRegion(out PolyRegion);
                return;
            }
            HOperatorSet.VectorToHomMat2d(new double[] { row1.D, row1.D, row2.D, row2.D }, new double[] { col1.D, col2.D, col2, col1.D }
            , new double[] { r1.D, r1.D, r2.D, r2.D }, new double[] { c1.D, c2.D, c2.D, c1.D }, out var hommat2d);
            HOperatorSet.AffineTransRegion(region, out PolyRegion, hommat2d, "nearest_neighbor");
            HOperatorSet.GenContourRegionXld(PolyRegion, out var contour, "border");
            HOperatorSet.GetContourXld(contour, out var row, out var col);
            contour.Dispose();
            x = col.DArr;
            y = row.DArr;

        }

        public static void SmallRect(HObject region, out HObject newregion)
        {
            HOperatorSet.SmallestRectangle1(region, out var row1, out var col1, out var row2, out var col2);
            var step = (row2 - row1) / 6.0;
            HOperatorSet.GenRectangle1(out var rect, row1 + step, col1, row2 - step, col2);
            HOperatorSet.Intersection(region, rect, out newregion);
            rect.Dispose();
        }


        public static void FilterPoints(HTuple RowRegionRuns, HTuple ColumnBegin, HTuple ColumnEnd, out HTuple rows, out HTuple colbegin, out HTuple colend)
        {
            var group = RowRegionRuns.LArr.Select((x, i) => new Tuple<double, double, double>(x, ColumnBegin[i], ColumnEnd[i]))
                  .GroupBy(x => x.Item1)
                  .Select(s => new Tuple<double, double, double>(s.Key, s.Select(x => x.Item2).Min(), s.Select(x => x.Item3).Max()));
            rows = group.Select(x => x.Item1).ToArray();
            colbegin = group.Select(x => x.Item2).ToArray();
            colend = group.Select(x => x.Item3).ToArray();
        }
        public static void FitRectPoly(HObject image, HObject region, out HObject polyContours, out FrameInfo framinfo, bool roundedConner = true, double cornersize = 40)
        {
            HOperatorSet.GenEmptyRegion(out polyContours);
            framinfo = new FrameInfo();
            if (image == null || region == null) return;
            HOperatorSet.GetImageSize(image, out var width, out var height);
            //  HOperatorSet.ShapeTrans(region, out var transregion, "convex");
            HOperatorSet.FillUp(region, out var transregion);
            HOperatorSet.SmallestRectangle2(transregion, out var row, out var col, out var phi, out var len, out var len2);


            if (width?.Length > 0 && row?.Length > 0 && len.D > 0 && len2.D > 0)
            {
             

                HOperatorSet.VectorAngleToRigid(row, col, phi, height / 2, width / 2, 0, out var homMat2D);
                HOperatorSet.AffineTransRegion(transregion, out var regionaffine, homMat2D, "nearest_neighbor");
                SmallRect(regionaffine, out regionaffine);
                HOperatorSet.GetRegionRuns(regionaffine, out var RowRegionRuns, out var ColumnBegin, out var ColumnEnd);
                FilterPoints(RowRegionRuns, ColumnBegin, ColumnEnd, out RowRegionRuns, out ColumnBegin, out ColumnEnd);



                HOperatorSet.HomMat2dInvert(homMat2D, out var homMat2DInvert);
                HOperatorSet.AffineTransPoint2d(homMat2DInvert, RowRegionRuns, ColumnBegin, out var lrow1, out var lcols1);
                FitLine(lrow1, lcols1, out var rs1, out var cs1, out var re1, out var ce1);




                HOperatorSet.AffineTransPoint2d(homMat2DInvert, RowRegionRuns, ColumnEnd, out var lrow2, out var lcols2);
                FitLine(lrow2, lcols2, out var rs2, out var cs2, out var re2, out var ce2);


             
                HOperatorSet.VectorAngleToRigid(row, col, phi, height / 2, width / 2, 90.0 / 180 * Math.PI, out var homMat2D2);
                HOperatorSet.AffineTransRegion(transregion, out var regionaffine2, homMat2D2, "nearest_neighbor");
                SmallRect(regionaffine2, out regionaffine2);
                HOperatorSet.GetRegionRuns(regionaffine2, out var RowRegionRuns2, out var ColumnBegin2, out var ColumnEnd2);

                FilterPoints(RowRegionRuns2, ColumnBegin2, ColumnEnd2, out RowRegionRuns2, out ColumnBegin2, out ColumnEnd2);



                HOperatorSet.HomMat2dInvert(homMat2D2, out var homMat2DInvert2);

                HOperatorSet.AffineTransPoint2d(homMat2DInvert2, RowRegionRuns2, ColumnBegin2, out var lrow3, out var lcols3);
                FitLine(lrow3, lcols3, out var rs3, out var cs3, out var re3, out var ce3);

                HOperatorSet.AffineTransPoint2d(homMat2DInvert2, RowRegionRuns2, ColumnEnd2, out var lrow4, out var lcols4);
                FitLine(lrow4, lcols4, out var rs4, out var cs4, out var re4, out var ce4);
                // HOperatorSet.GenRegionLine(out testobj, rs1, cs1, re1, ce1);
                //HOperatorSet.GenContourPolygonXld(out testobj, lrow1, lcols1);

                try
                {
                    if (rs1.Length > 0 && rs2.Length > 0 && rs3.Length > 0 && re4.Length > 0)
                    {
                        HOperatorSet.IntersectionLines(rs1, cs1, re1, ce1, rs3, cs3, re3, ce3, out var prow1, out var pcol1, out _);
                        HOperatorSet.IntersectionLines(rs1, cs1, re1, ce1, rs4, cs4, re4, ce4, out var prow2, out var pcol2, out _);
                        HOperatorSet.IntersectionLines(rs2, cs2, re2, ce2, rs4, cs4, re4, ce4, out var prow3, out var pcol3, out _);
                        HOperatorSet.IntersectionLines(rs2, cs2, re2, ce2, rs3, cs3, re3, ce3, out var prow4, out var pcol4, out _);

                        var rowinter = new[] { prow1.D, prow2.D, prow3.D, prow4.D };
                        var colinter = new[] { pcol1.D, pcol2.D, pcol3.D, pcol4.D };

                        HOperatorSet.GenRegionPolygonFilled(out polyContours, rowinter, colinter);
                        HOperatorSet.AreaCenter(polyContours, out var area, out _, out _);
                        framinfo = new FrameInfo(colinter, rowinter, area.D);
                        if (roundedConner == true)
                        {
                            HOperatorSet.GenEmptyRegion(out var circles);
                            for (int i = 0; i < rowinter.Length; i++)
                            {
                                HOperatorSet.GenCircle(out var circle, rowinter[i], colinter[i], cornersize);
                                HOperatorSet.ConcatObj(circles, circle, out circles);
                            }
                            HOperatorSet.Union1(circles, out circles);
                            HOperatorSet.Intersection(region, circles, out var arcregion);
                            HOperatorSet.Intersection(polyContours, arcregion, out arcregion);
                            HOperatorSet.Difference(polyContours, circles, out var diffregion);
                            HOperatorSet.ConcatObj(diffregion, arcregion, out var unionregion);
                            HOperatorSet.Union1(unionregion, out polyContours);
                            HOperatorSet.ShapeTrans(polyContours, out polyContours, "convex");
                            circles.Dispose();
                            arcregion.Dispose();
                            diffregion.Dispose();
                            unionregion.Dispose();
                        }
                    }
                }
                catch (Exception)
                {
                }

                regionaffine.Dispose();
                regionaffine2.Dispose();
            }
            transregion.Dispose();
        }

        public static void FitLine(HTuple rows, HTuple cols, out HTuple rowbegin, out HTuple colbegin, out HTuple rowend, out HTuple colend)
        {
            rowbegin = new HTuple();
            colbegin = new HTuple();
            rowend = new HTuple();
            colend = new HTuple();
            HOperatorSet.GenContourPolygonXld(out var contours, rows, cols);
            if (rows.Length >= 3)
                HOperatorSet.FitLineContourXld(contours, "tukey", -1, 0, 5, 2.0, out rowbegin, out colbegin, out rowend, out colend, out _, out _, out _);
            contours.Dispose();
        }

        public static void GetScaleImage(HObject image, out HObject ScaleImage, HObject framecontour)
        {
            //HOperatorSet.GenRegionContourXld(framecontour, out var region, "filled");
            HOperatorSet.ReduceDomain(image, framecontour, out ScaleImage);

            //HOperatorSet.EdgesSubPix(reduceImage, out var edges, "canny", 1, 10, 20);

            // HOperatorSet.ScaleImageMax(reduceImage, out var nimage);
            //HOperatorSet.GrayHisto(region, nimage,out var absoluteHisto,out var relativeHisto);
            //HOperatorSet.HistoToThresh(relativeHisto, 8, out var min, out var max);
            //HOperatorSet.Threshold(nimage, out region, min,max);
            //  HOperatorSet.EquHistoImage(nimage, out nimage);

            // threshold(Image, Region, MinThresh, MaxThresh)

            //  HOperatorSet.ThresholdSubPix(nimage, out defect, 128);//Real(0.05));
            //  var s = defect.CountObj();
            //  Console.WriteLine(s);

        }

        public static void Detect(HObject image, HObject domainregion, out HObject filter, HObject frameRegion, out HObject contours, PolygonROI roi, out FFTResult[] results)
        {
            CheckSize size = roi.checksize;
            HOperatorSet.GenEmptyObj(out contours);
            // HOperatorSet.ReduceDomain(image, domainregion, out var reduceImage);

            HOperatorSet.Intersection(domainregion, frameRegion, out var validregion);
            HOperatorSet.ReduceDomain(image, validregion, out var reduceImage);

            IteratorRect(reduceImage, validregion, out filter, out var selected, size, 3);

            HOperatorSet.AreaCenter(selected, out var area, out _, out _);
            HOperatorSet.SmallestRectangle1(selected, out var row1, out var col1, out var row2, out var col2);

            // HOperatorSet.Intersection(domainregion, selected,out selected);

            // HOperatorSet.Connection(selected, out selected);

            int len = selected.CountObj();
            //  results = new FFTResult[0];
            //  Stopwatch sw = new Stopwatch();
            //  sw.Start();
            List<FFTResult> reslist = new List<FFTResult>();
            for (int i = 0; i < len; i++)
            {
                FFTResult res = new FFTResult();
                HOperatorSet.Intersection(domainregion, selected[i + 1], out var select);
                HOperatorSet.GetRegionPoints(select, out var prows, out var pcols);
                HOperatorSet.GetGrayval(reduceImage, prows, pcols, out var grayval);

             
                HOperatorSet.DilationCircle(selected[i + 1], out var dilationRegion, 3);
                HOperatorSet.Difference(dilationRegion, selected[i + 1], out var regionDifference);

                HOperatorSet.Intersection(domainregion, regionDifference, out regionDifference);


                HistoImage(image, regionDifference, out var hist, out var grays, out var pecent);
                HistoImage(image, select, out var absoluteHisto1, out var relativeHisto1, out var pecent1);
                select.Dispose();
                dilationRegion.Dispose();
                regionDifference.Dispose();
            
                var maxvalue = hist.Max();
                var index = hist.ToList().FindIndex(x => x == maxvalue);
                var meanvalue = ToReal((int)grays[index]);
               
                var valueindex = relativeHisto1.ToList().FindIndex(x => x > grays[index]);
                double areauprange = 0, arealowrange = 0;
                if (valueindex > 0)
                {
                    areauprange = absoluteHisto1.Skip(valueindex).Sum();
                    arealowrange = area[i] - areauprange;
                }
              
                GetTopGrayMean(absoluteHisto1, relativeHisto1, pecent1, 0.02, 0.05, out var minmeangray);
                GetTopGrayMean(absoluteHisto1, relativeHisto1, pecent1, 0.95, 0.98, out var maxmeangray);

                res.width = Math.Abs(col1[i].D - col2[i].D) * size._xInterval;
                res.height = Math.Abs(row1[i].D - row2[i].D) * size._yInterval;

                res.MinZ = ToReal(grayval.TupleMax().I);
                res.MaxZ = ToReal(grayval.TupleMin().I);


                var h = ToReal((int)maxmeangray) - meanvalue;// results[i].MaxZ - meanvalue;
                var d = ToReal((int)minmeangray) - meanvalue;// results[i].MinZ - meanvalue;
                res.depth = Math.Abs(h) > Math.Abs(d) ? h : d;
                // results[i].depth = results[i].MaxZ - results[i].MinZ;
                if (res.depth > 0 && areauprange > 0)
                {
                    res.area = areauprange * size._xInterval * size._yInterval;
                }
                else if (res.depth < 0 && arealowrange > 0)
                {
                    res.area = arealowrange * size._xInterval * size._yInterval;
                }
                else
                {
                    res.area = area[i].D * size._xInterval * size._yInterval;
                }

                if (Math.Abs(res.depth) > Math.Abs(roi.checksize.maxdepth) && res.area > size.minwidth * size.minheight)
                {
                    reslist.Add(res);
                    HOperatorSet.GenContourRegionXld(selected[i + 1], out var cxld, "border");
                    HOperatorSet.ConcatObj(contours, cxld, out contours);
                    cxld.Dispose();
                }
                if (reslist.Count() >= maxSearchNumber)
                    break;
                //Console.WriteLine(results[i]);
            }

            results = reslist.ToArray();
            //  sw.Stop();
            //  Console.WriteLine(sw.Elapsed.TotalMilliseconds);
            validregion.Dispose();
            selected.Dispose();
            reduceImage.Dispose();
        }

      
        private static void IteratorRect(HObject image, HObject region, out HObject filter, out HObject selectregion, CheckSize size, int maskwidth = 3)
        {
            int[] arr = new int[] { 0 };
            HOperatorSet.Union1(region, out var newregion);
            HOperatorSet.ReduceDomain(image, newregion, out var imageReduce);

            HOperatorSet.GrayRangeRect(imageReduce, out filter, maskwidth, maskwidth);
            HOperatorSet.GrayRangeRect(filter, out filter, maskwidth, maskwidth);
            HOperatorSet.ErosionCircle(newregion, out var erosion, 2);
            HOperatorSet.ReduceDomain(filter, erosion, out filter);



            HOperatorSet.MinMaxGray(filter, filter, 0, out var min, out var max, out var range);
            var v2 = Math.Min(max.D, FromReal(size.Searchdepth)); //size.Searchdepth起始搜索高度/深度
            HOperatorSet.Threshold(filter, out var region1, v2, max.D);
            HOperatorSet.ClosingCircle(region1, out region1, 7);
            HOperatorSet.Connection(region1, out var connectregion);
            HOperatorSet.ShapeTrans(connectregion, out var convexregion, "convex");
            HOperatorSet.Intersection(region, convexregion, out convexregion);
            HOperatorSet.Connection(convexregion, out convexregion);

            double cwidth = size.minwidth / size._xInterval;
            double cheight = size.minheight / size._yInterval;
            double carea = cwidth * cheight / 2.0;
            HOperatorSet.SelectShape(convexregion, out selectregion, new[] { "area", "width", "height" }, "and", new[] { carea, cwidth, cheight }, new[] { 9999 * 9999, 99999, 99999 });


            int next = arr.Where(x => x > maskwidth).FirstOrDefault();

            newregion.Dispose();
            imageReduce.Dispose();
            region1.Dispose();
            connectregion.Dispose();
            convexregion.Dispose();
            erosion.Dispose();
       
        }



        public static bool detect(int[] data, Head head, PolygonROI roi, out HObject contour)
        {
            contour = null;
            //var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            //var psrc = handle.AddrOfPinnedObject();
            //uint resultsize = 0;
            //try
            //{
            //    Circle2D d2 = new Circle2D();
            //    DRect dr = new DRect();

            //    DPoint2D[] points = new DPoint2D[roi.x.Length];
            //    for (int i = 0; i < roi.x.Length; i++)
            //    {
            //        points[i] = new DPoint2D() { _x = roi.x[i], _y = roi.y[i] };
            //    }
            //    Result[] result = new Result[2000];

            //    RATIO_Plane rplane = new RATIO_Plane();
            //    StringBuilder sb = new StringBuilder(255);
            //    DPoint2D[] countours = new DPoint2D[20000];
            // head._xInterval = 0.03;
            // head._yInterval = 0.15;
            //int ret = findFlaw(psrc, ref head, 0.20, -0.20, 0.05, 2, 500, 500, ref d2, ref dr, ref points[0], (uint)points.Length,
            //    (int)findFlaw_method_type.SSZN_Segment_Curve,
            //   ref result[0], ref resultsize, ref rplane, false, sb, true, false, ref countours[0], 0);



            //    HOperatorSet.GenEmptyObj(out var empty);
            //    var rect = result.Take((int)resultsize);


            //    for (int i = 0; i < resultsize; i++)
            //    {
            //        var r = result[i];
            //        Console.WriteLine("area={0},lenth={1},width={2},maxz={3},minz={4},x={5},y={6},rectwidth={7},rectlenth={8}",
            //                r.area, r.lenth, r.width, r.maxZ, r.minZ, r.x, r.y, r.rectWidth, r.rectLenth);
            //    }
            //    var minx = 0;// points.Select(s => s._x).Min();
            //    var maxy = 0;// points.Select(s => s._y).Min();
            //    HOperatorSet.GenRectangle2ContourXld(out contour
            //        , rect.Select(s => s.y + s.rectLenth / 2.0).ToArray()
            //         , rect.Select(s => s.x + s.rectWidth / 2.0).ToArray()
            //         , new double[resultsize]
            //         , rect.Select(s => s.rectWidth / 2.0).ToArray()
            //         , rect.Select(s => s.rectLenth / 2.0).ToArray()
            //         );

            //    //for (int i=0;i<countours.Length;i++)
            //    //{
            //    //    if (countours[i]._x == 0 && countours[i]._y == 0)
            //    //        break;
            //    //    else
            //    //        len++;
            //    //}
            //    // var list= countours.Take(len);
            //    //  HOperatorSet.GenContourPolygonXld(out contour, list.Select(s=>s._y).ToArray(),list.Select(s=>s._x).ToArray());

            //}
            //catch (Exception)
            //{


            //}
            //handle.Free();
            return false;
        }

        public static void GetTopGrayMean(double[] histo, double[] grays, double[] pecent, double start, double end, out double gray)
        {
            gray = 0;
            //10%最大灰度值的平均值
            var indexstart = pecent.ToList().FindIndex(x => x >= start);
            var indexend = pecent.ToList().FindIndex(x => x >= end);
            if (indexend < indexstart || indexstart < 0 || indexend < 0) return;
            int len = indexend - indexstart + 1;
            double sum = 0;
            for (int i = 0; i < len; i++)
                sum += histo[indexstart + i] * grays[indexstart + i];
            var num = histo.Skip(indexstart).Take(len).Sum();
            if (num == 0) return;
            gray = sum / num;
        }

        public static void HistoImage(HObject image, HObject region, out double[] histo, out double[] grays, out double[] pecent)
        {
            histo = new double[256];
            grays = new double[256];
            pecent = new double[256];
            HOperatorSet.MinMaxGray(region, image, 0, out var min, out var max, out var range);
            HOperatorSet.GrayHisto(region, image, out var absoluteHisto, out var relativeHisto);
            histo = absoluteHisto.LArr.Select(x => (double)x).ToArray();
            for (int i = 0; i < absoluteHisto.Length; i++)
            {
                grays[i] = range.D * i / 255.0 + min.D;
                if (i == 0)
                    pecent[i] = relativeHisto[i];
                else
                    pecent[i] = pecent[i - 1] + relativeHisto[i];
            }
            pecent[255] = 1;
        }

        public static void AddImage(HObject[] images, out HObject image)
        {
            image = null;
            if (images?.Length > 0)
            {
                HOperatorSet.GetImageSize(images[0], out var width, out var height);
                HOperatorSet.GetImageType(images[0], out var type);
                HOperatorSet.GenImageConst(out image, type, width, height);
                for (int i = 0; i < images.Length; i++)
                {
                    HOperatorSet.AddImage(image, images[i], out image, 1, 0);
                }
            }

        }


        public static int FromReal(double x)
        {
            return (int)(x * 100000);
        }
        public static double ToReal(int x)
        {
            return (x / 100000.0);
        }
    }

}




