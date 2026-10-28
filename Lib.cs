using System;
using System.Collections.Generic;
using System.Linq;
using HalconDotNet;
//using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

using System.Text;

namespace ImageProcessLib
{
    public class ImageProc
    {
        public enum CalibrationUnit_enum
        {
            IMAQ_UNDEFINED = 0,           //The image does not have a defined unit of measurement.
            IMAQ_ANGSTROM = 1,           //The unit of measure for the image is angstroms.
            IMAQ_MICROMETER = 2,           //The unit of measure for the image is micrometers.
            IMAQ_MILLIMETER = 3,           //The unit of measure for the image is millimeters.
            IMAQ_CENTIMETER = 4,           //The unit of measure for the image is centimeters.
            IMAQ_METER = 5,           //The unit of measure for the image is meters.
            IMAQ_KILOMETER = 6,           //The unit of measure for the image is kilometers.
            IMAQ_MICROINCH = 7,           //The unit of measure for the image is microinches.
            IMAQ_INCH = 8,           //The unit of measure for the image is inches.
            IMAQ_FOOT = 9,           //The unit of measure for the image is feet.
            IMAQ_NAUTICMILE = 10,          //The unit of measure for the image is nautical miles.
            IMAQ_GROUNDMILE = 11,          //The unit of measure for the image is ground miles.
            IMAQ_STEP = 12,          //The unit of measure for the image is steps.
            IMAQ_CALIBRATION_UNIT_SIZE_GUARD = unchecked((int)(0xFFFFFFFF))
        }
        public enum ImageType_enum
        {
            IMAQ_IMAGE_U8 = 0,           //The image type is 8-bit unsigned integer grayscale.
            IMAQ_IMAGE_U16 = 7,           //The image type is 16-bit unsigned integer grayscale.
            IMAQ_IMAGE_I16 = 1,           //The image type is 16-bit signed integer grayscale.
            IMAQ_IMAGE_SGL = 2,           //The image type is 32-bit floating-point grayscale.
            IMAQ_IMAGE_COMPLEX = 3,           //The image type is complex.
            IMAQ_IMAGE_RGB = 4,           //The image type is RGB color.
            IMAQ_IMAGE_HSL = 5,           //The image type is HSL color.
            IMAQ_IMAGE_RGB_U64 = 6,           //The image type is 64-bit unsigned RGB color.
            IMAQ_IMAGE_TYPE_SIZE_GUARD = unchecked((int)0xFFFFFFFF)
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi/*, Pack = 1*/)]
        unsafe public struct ImageInfo_struct
        {
            public CalibrationUnit_enum imageUnit;     //If you set calibration information with imaqSetSimpleCalibrationInfo(), imageUnit is the calibration unit.
            public float stepX;         //If you set calibration information with imaqSetCalibrationInfo(), stepX is the distance in the calibration unit between two pixels in the x direction.
            public float stepY;         //If you set calibration information with imaqSetCalibrationInfo(), stepY is the distance in the calibration unit between two pixels in the y direction.
            public ImageType_enum imageType;     //The type of the image.
            public int xRes;          //The number of columns in the image.
            public int yRes;          //The number of rows in the image.
            public int xOffset;       //If you set mask offset information with imaqSetMaskOffset(), xOffset is the offset of the mask origin in the x direction.
            public int yOffset;       //If you set mask offset information with imaqSetMaskOffset(), yOffset is the offset of the mask origin in the y direction.
            public int border;        //The number of border pixels around the image.
            public int pixelsPerLine; //The number of pixels stored for each line of the image.
            public void* reserved0;     //This element is reserved.
            public void* reserved1;     //This element is reserved.
            public IntPtr imageStart;    //A pointer to pixel (0,0).
        }
        // List<HTuple> modelIDs = new List<HTuple>();
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private static extern void MemCopy(IntPtr dest, IntPtr src, int count);//字节数组到字节数组的拷贝

        [DllImport("nivissvc.dll", EntryPoint = "LV_GetImagePixelPtr_v2", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]

        public static extern int GetImagePtr(IntPtr image, IntPtr pixelPtr, int x, int y, Int16 function, IntPtr borderSize, IntPtr lineWidth, IntPtr pixelSize, IntPtr maxTransferSize, IntPtr err);

        [DllImport("nivissvc.dll", EntryPoint = "LV_GetImageSize", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static unsafe extern int GetImage(void* image, int* x, int* y, void* err);


        [DllImport("nivissvc.dll", EntryPoint = "LV_GetImageSize", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static unsafe extern int GetImage(UInt64 LvImage, out int x, out int y, void* err);


        [DllImport("nivision.dll", EntryPoint = "imaqGetImageSize", CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static unsafe extern int GetImage(uint LvImage, out int x, out int y);

        [DllImport("nivision.dll", EntryPoint = "imaqGetImageInfo", CallingConvention = CallingConvention.StdCall, SetLastError = false)]
        public static unsafe extern int imaqGetImageInfo(uint LvImage, out ImageInfo_struct info);
      
        public static long LvToHalcon2(uint LvImage,out HObject image)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            image = null;
            imaqGetImageInfo(LvImage, out var info);           
           
            IntPtr ptr = info.imageStart;
            if (info.imageType == ImageType_enum.IMAQ_IMAGE_U8)
            {
                HObject BigSizeImge;
                HOperatorSet.GenImage1Rect(out BigSizeImge, ptr, info.xRes, info.yRes, info.pixelsPerLine, 8, 8, "false", 0);
                HOperatorSet.ChangeFormat(BigSizeImge, out image, info.xRes, info.yRes);
                BigSizeImge.Dispose();
            }
            if(info.imageType==ImageType_enum.IMAQ_IMAGE_RGB)
            {
                HObject BigSizeImge;
                HOperatorSet.GenImageInterleaved(out BigSizeImge, ptr, "bgrx", info.pixelsPerLine, info.yRes, 0, "byte", 0, 0, 0, 0, -1, 0);
                HOperatorSet.ChangeFormat(BigSizeImge, out image, info.xRes, info.yRes);
                BigSizeImge.Dispose();
            }           
            sw.Stop();            
            return sw.ElapsedMilliseconds;
        }

        public static void transImage(int image, out int width, out int height, int err/*, out int lineWidth, out int pixp*/)
        {
            IntPtr pwidth = new IntPtr(), pheight = new IntPtr()/*, plinewidth= new IntPtr(), ppixp= new IntPtr()*/;
            width = 0; height = 0;
            unsafe
            {
                MessageBox.Show(image.ToString());

                int w, h;
                int* pw = &w;
                int* ph = &h;
                GetImage((void*)(image), pw, ph, (void*)err);
                MessageBox.Show(w.ToString() + "," + h.ToString());
            }

            MessageBox.Show(pwidth.ToInt32().ToString() + "," + pheight.ToInt32().ToString());
            //  GetImage(image, pwidth, pheight,err);
            width = pwidth.ToInt32();
            height = pheight.ToInt32();
            MessageBox.Show(pwidth.ToInt32().ToString() + "," + pheight.ToInt32().ToString());
            //  GetImagePtr(image, ppixp)
        }

        public static long LvToHalcon(UInt64 LvImage, int lineWidth, int width, int height, int mode, out HObject image)
        {
            image = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            IntPtr ptr = new IntPtr((long)LvImage);
            switch (mode)
            {
                case 0:
                    {
                        HObject BigSizeImge;
                        HOperatorSet.GenImage1Rect(out BigSizeImge, ptr, width, height, lineWidth, 8, 8, "false", 0);
                        HOperatorSet.ChangeFormat(BigSizeImge, out image, width, height);
                        BigSizeImge.Dispose();
                    }
                    break;
                case 1:
                    HOperatorSet.GenImageConst(out image, "byte", width, height);
                    HTuple pimage, type, xwidth, yheight;
                    HOperatorSet.GetImagePointer1(image, out pimage, out type, out xwidth, out yheight);
                    for (int i = 0; i < height; i++)
                    {
                        IntPtr pdst = new IntPtr(pimage.I + i * xwidth);
                        IntPtr psrc = ptr + i * lineWidth;
                        MemCopy(pdst, psrc, xwidth);
                    }
                    break;
                case 3:
                    {
                        HObject BigSizeImge;
                        HOperatorSet.GenImageInterleaved(out BigSizeImge, ptr, "bgrx", lineWidth, height, 0, "byte", 0, 0, 0, 0, -1, 0);
                        HOperatorSet.ChangeFormat(BigSizeImge, out image, width, height);
                        BigSizeImge.Dispose();
                    }
                    break;
                default:
                    break;
            }
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
        public void HalconToLv(UInt64 LVImage, HObject Image, int lineWidth, int width, int height)
        {
            if (Image == null) return;
            HTuple type, xwidth, yheight, ptr;
            HOperatorSet.GetImagePointer1(Image, out ptr, out type, out xwidth, out yheight);
            if (xwidth != width || yheight != height)
            {
                throw new Exception("not Equal Image size ！");
            }
            IntPtr pimg = new IntPtr((long)LVImage);
            for (int i = 0; i < height; i++)
            {
                IntPtr psrc = new IntPtr(ptr.I + i * xwidth);
                IntPtr pdst = pimg + i * lineWidth;
                MemCopy(pdst, psrc, xwidth);
            }
        }



        public MatchResult ShapMatch(UInt64 LvImage, int LineWidth, int width, int height, ModelInfo Model, MatchCondition mc)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            HObject src, ReduceSrc, region;
            HTuple rows = new HTuple(), cols = new HTuple(), angles = new HTuple(), scaleds = new HTuple(), scores = new HTuple(), roi_top = new HTuple(0.0), roi_left = new HTuple(0.0);


            MatchResult result = new MatchResult();

            LvToHalcon(LvImage, LineWidth, width, height, 0, out src);
            ReduceSrc = src;

            if (mc.Roix != null && mc.Roix.Length != 0)
            {
                HTuple t1, t2;
                HOperatorSet.GenRegionPolygonFilled(out region, mc.RoiY, mc.Roix);
                HOperatorSet.ReduceDomain(src, region, out ReduceSrc);
                HOperatorSet.SmallestRectangle1(region, out roi_top, out roi_left, out t1, out t2);
                HOperatorSet.CropDomain(ReduceSrc, out ReduceSrc);
                region.Dispose();
            }
            switch (mc.MatchMode)
            {
                case MatchMode.Normal:
                    FindScaledShapeModel(ReduceSrc, Model, mc, out result);
                    break;
                case MatchMode.Planar:
                    FindPlanarUncalibDeformableModel(ReduceSrc, Model, mc, out result);
                    break;
                case MatchMode.LocalDeformable:
                    HObject imageRectified, vectorField, deformedContours;
                    HOperatorSet.FindLocalDeformableModel(ReduceSrc, out imageRectified, out vectorField, out deformedContours, Model.ModelId, mc.angleStart, mc.angleExtent, mc.scaleRMin, mc.scaleRMax, mc.scaleCMin, mc.scaleCMax, mc.minScore, mc.numMatches, mc.maxOverlap, mc.numLevels, mc.greediness, new HTuple(), new HTuple(), new HTuple(), out scores, out rows, out cols);
                    break;
                default:
                    break;
            }

            result.offSet(roi_top.D, roi_left.D);
            sw.Stop();
            src.Dispose();
            ReduceSrc.Dispose();
            return result;
        }

        public MatchResult ShapMatch_Domain(HObject src, ModelInfo Model, MatchCondition mc)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            HObject ReduceSrc, region;
            HTuple rows = new HTuple(), cols = new HTuple(), angles = new HTuple(), scaleds = new HTuple(), scores = new HTuple(), roi_top = new HTuple(0.0), roi_left = new HTuple(0.0);


            MatchResult result = new MatchResult();

            //LvToHalcon(LvImage, LineWidth, width, height, 0, out src);
            ReduceSrc = src;

            if (mc.Roix != null && mc.Roix.Length != 0)
            {
                HTuple t1, t2;
                HOperatorSet.GenRegionPolygonFilled(out region, mc.RoiY, mc.Roix);
                HOperatorSet.ReduceDomain(src, region, out ReduceSrc);
                HOperatorSet.SmallestRectangle1(region, out roi_top, out roi_left, out t1, out t2);
                HOperatorSet.CropDomain(ReduceSrc, out ReduceSrc);
                region.Dispose();
            }
            switch (mc.MatchMode)
            {
                case MatchMode.Normal:
                    FindScaledShapeModel(ReduceSrc, Model, mc, out result);
                    break;
                case MatchMode.Planar:
                    FindPlanarUncalibDeformableModel(ReduceSrc, Model, mc, out result);
                    break;
                case MatchMode.LocalDeformable:
                    HObject imageRectified, vectorField, deformedContours;
                    HOperatorSet.FindLocalDeformableModel(ReduceSrc, out imageRectified, out vectorField, out deformedContours, Model.ModelId, mc.angleStart, mc.angleExtent, mc.scaleRMin, mc.scaleRMax, mc.scaleCMin, mc.scaleCMax, mc.minScore, mc.numMatches, mc.maxOverlap, mc.numLevels, mc.greediness, new HTuple(), new HTuple(), new HTuple(), out scores, out rows, out cols);
                    break;
                default:
                    break;
            }

            result.offSet(roi_top.D, roi_left.D);
            sw.Stop();
            src.Dispose();
            ReduceSrc.Dispose();
            return result;
        }

        private void FindPlanarUncalibDeformableModel(HObject image, ModelInfo Model, MatchCondition mc, out MatchResult result)
        {

            HTuple scores, Hom2dmat;
            HOperatorSet.FindPlanarUncalibDeformableModel(image, Model.ModelId, mc.angleStart, mc.angleExtent, mc.scaleRMin, mc.scaleRMax, mc.scaleCMin, mc.scaleCMax, mc.minScore, mc.numMatches, mc.maxOverlap, mc.numLevels, mc.greediness, new HTuple(), new HTuple(), out Hom2dmat, out scores);
            result = new MatchResult(Model, Hom2dmat, scores);
        }

        private void FindScaledShapeModel(HObject image, ModelInfo Model, MatchCondition mc, out MatchResult result)
        {
            HTuple rows = new HTuple(), cols = new HTuple(), angles = new HTuple(), scaleds = new HTuple(), scores = new HTuple(), roi_top = new HTuple(0.0), roi_left = new HTuple(0.0);
            if (mc.ScaledEnble == true)
            {
                HOperatorSet.FindScaledShapeModel(image, Model.ModelId, mc.angleStart, mc.angleExtent, mc.ScaleMin, mc.ScaleMax, mc.minScore, mc.numMatches, mc.maxOverlap, mc.subPixel, mc.numLevels, mc.greediness, out rows, out cols, out angles, out scaleds, out scores);
            }
            else
            {
                HOperatorSet.FindShapeModel(image, Model.ModelId, mc.angleStart, mc.angleExtent, mc.minScore, mc.numMatches, mc.maxOverlap, mc.subPixel, mc.numLevels, mc.greediness, out rows, out cols, out angles, out scores);
                scaleds = new HTuple(new double[] { });
                if (scores.Length != 0)
                {
                    foreach (var item in scores.DArr)
                    {
                        scaleds = scaleds.TupleConcat(1.0);
                    }
                }
            }
            result = new MatchResult(rows, cols, angles, scaleds, scores, Model);
        }

        public MatchResult[] ShapMatchs(UInt64 LvImage, int LineWidth, int width, int height, ModelInfo[] Model, MatchCondition mc)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            HObject src, ReduceSrc, region;
            HTuple rows, cols, angles, scaleds, scores, modelIndex, roi_top = new HTuple(0.0), roi_left = new HTuple(0.0);
            LvToHalcon(LvImage, LineWidth, width, height, 0, out src);
            ReduceSrc = src;
            HTuple modelIds = new HTuple(new int[] { });
            for (int i = 0; i < Model.Length; i++)
            {
                modelIds = modelIds.TupleConcat(Model[i].ModelId);
            }
            if (mc.Roix.Length != 0)
            {
                HTuple t1, t2;
                HOperatorSet.GenRegionPolygonFilled(out region, mc.RoiY, mc.Roix);
                HOperatorSet.ReduceDomain(src, region, out ReduceSrc);
                HOperatorSet.SmallestRectangle1(region, out roi_top, out roi_left, out t1, out t2);
                HOperatorSet.CropDomain(ReduceSrc, out ReduceSrc);
                region.Dispose();
            }
            if (mc.ScaledEnble == true)
            {
                HOperatorSet.FindScaledShapeModels(ReduceSrc, modelIds, mc.angleStart, mc.angleExtent, mc.ScaleMin, mc.ScaleMax, mc.minScore, mc.numMatches, mc.maxOverlap, mc.subPixel, mc.numLevels, mc.greediness, out rows, out cols, out angles, out scaleds, out scores, out modelIndex);
            }
            else
            {
                HOperatorSet.FindShapeModels(ReduceSrc, modelIds, mc.angleStart, mc.angleExtent, mc.minScore, mc.numMatches, mc.maxOverlap, mc.subPixel, mc.numLevels, mc.greediness, out rows, out cols, out angles, out scores, out modelIndex);
                scaleds = new HTuple(new double[] { });
                if (scores.Length != 0)
                {
                    foreach (var item in scores.DArr)
                    {
                        scaleds = scaleds.TupleConcat(1.0);
                    }
                }
            }
            if (scores.Length != 0)
            {
                for (int i = 0; i < scores.Length; i++)
                {
                    rows[i].D = rows[i].D + roi_top.D;
                    cols[i].D = cols[i].D + roi_left.D;
                }
            }
            List<MatchResult> res = new List<MatchResult>();
            for (int i = 0; i < rows.Length; i++)
            {
                MatchResult Res = new MatchResult(rows[i].DArr, cols[i].DArr, angles[i].DArr, scaleds[i].DArr, scores[i].DArr, Model[modelIndex[i]]);
                res.Add(Res);
            }
            src.Dispose();
            ReduceSrc.Dispose();
            sw.Stop();
            return res.ToArray();
        }

        public MatchResult[] ShapMatchs_Domain(HObject src, ModelInfo[] Model, MatchCondition mc)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //HObject src, ReduceSrc, region;
            HObject ReduceSrc, region;
            HTuple rows, cols, angles, scaleds, scores, modelIndex, roi_top = new HTuple(0.0), roi_left = new HTuple(0.0);
            //LvToHalcon(LvImage, LineWidth, width, height, 0, out src);
            ReduceSrc = src;
            HTuple modelIds = new HTuple(new int[] { });
            for (int i = 0; i < Model.Length; i++)
            {
                modelIds = modelIds.TupleConcat(Model[i].ModelId);
            }
            if (mc.Roix.Length != 0)
            {
                HTuple t1, t2;
                HOperatorSet.GenRegionPolygonFilled(out region, mc.RoiY, mc.Roix);
                HOperatorSet.ReduceDomain(src, region, out ReduceSrc);
                HOperatorSet.SmallestRectangle1(region, out roi_top, out roi_left, out t1, out t2);
                HOperatorSet.CropDomain(ReduceSrc, out ReduceSrc);
                region.Dispose();
            }
            if (mc.ScaledEnble == true)
            {
                HOperatorSet.FindScaledShapeModels(ReduceSrc, modelIds, mc.angleStart, mc.angleExtent, mc.ScaleMin, mc.ScaleMax, mc.minScore, mc.numMatches, mc.maxOverlap, mc.subPixel, mc.numLevels, mc.greediness, out rows, out cols, out angles, out scaleds, out scores, out modelIndex);
            }
            else
            {
                HOperatorSet.FindShapeModels(ReduceSrc, modelIds, mc.angleStart, mc.angleExtent, mc.minScore, mc.numMatches, mc.maxOverlap, mc.subPixel, mc.numLevels, mc.greediness, out rows, out cols, out angles, out scores, out modelIndex);
                scaleds = new HTuple(new double[] { });
                if (scores.Length != 0)
                {
                    foreach (var item in scores.DArr)
                    {
                        scaleds = scaleds.TupleConcat(1.0);
                    }
                }
            }
            if (scores.Length != 0)
            {
                for (int i = 0; i < scores.Length; i++)
                {
                    rows[i].D = rows[i].D + roi_top.D;
                    cols[i].D = cols[i].D + roi_left.D;
                }
            }
            List<MatchResult> res = new List<MatchResult>();
            for (int i = 0; i < rows.Length; i++)
            {
                MatchResult Res = new MatchResult(rows[i].DArr, cols[i].DArr, angles[i].DArr, scaleds[i].DArr, scores[i].DArr, Model[modelIndex[i]]);
                res.Add(Res);
            }
            src.Dispose();
            ReduceSrc.Dispose();
            sw.Stop();
            return res.ToArray();
        }

        public ModelInfo CreateModel(string file, ShapModelCondition smc)
        {
            HObject Image;
            HTuple width, height, id;
            HOperatorSet.ReadImage(out Image, file);
            HOperatorSet.GetImageSize(Image, out width, out height);
            CreateModel(Image, smc, out id);
            return new ModelInfo(id, width, height, smc);
        }
        public ModelInfo CreateModel(UInt64 LvImage, int LineWidth, int width, int height, double[] RoiPoints, ShapModelCondition smc, out List<List<Contours>> Cont)
        {
            HObject src, ReduceSrc, region;
            HTuple crow, ccol, area, id, top, left, right, bottom;
            try
            {
                //LvToHalcon(LvImage, LineWidth, width, height, 0, out src);
                LvToHalcon(LvImage, LineWidth, width, height, 0, out src);
                ReduceSrc = src;
                HOperatorSet.GenRectangle1(out region, 0, 0, height, width);
                if (RoiPoints.Length != 0)
                {
                    double[] roix = new double[RoiPoints.Length / 2];
                    double[] roiy = new double[RoiPoints.Length / 2];
                    int len = RoiPoints.Length / 2 * 2;
                    for (int i = 0; i < len; i++)
                    {
                        if (i % 2 == 0) roix[i / 2] = RoiPoints[i];
                        else roiy[i / 2] = RoiPoints[i];
                    }
                    HOperatorSet.GenRegionPolygonFilled(out region, roiy, roix);
                    HOperatorSet.ReduceDomain(src, region, out ReduceSrc);
                }
                CreateModel(src, smc, out id);

                HOperatorSet.AreaCenter(region, out area, out crow, out ccol);
                HOperatorSet.SmallestRectangle1(region, out top, out left, out bottom, out right);
                ModelInfo infor = new ModelInfo(id, right.D - left.D, bottom.D - top.D, smc);
                MatchResult match = new MatchResult(crow, ccol, new HTuple(new double[] { 0 }), new HTuple(new double[] { 1 }), new HTuple(new double[] { 0 }), infor);
                Cont = match.GetContours();
                return infor;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private void CreateModel(HObject Image, ShapModelCondition smc, out HTuple id)
        {

            switch (smc.matchmode)
            {
                case MatchMode.Normal:
                    if (smc.scaledEnble)
                    {
                        HOperatorSet.CreateScaledShapeModel(Image, new TurpleConvert(smc.numLevels).Tuple, smc.angleStart, smc.angleExtent, new TurpleConvert(smc.angleStep).Tuple, smc.scaleMin, smc.scaleMax, new TurpleConvert(smc.scaleStep).Tuple, smc.optimization, smc.metric, new int[] { smc.tempmincontrast, smc.tempmaxcontrast, smc.mintuplelen }/*new TurpleConvert(smc.contrast).Tuple*/, new TurpleConvert(smc.minContrast).Tuple, out id);
                    }
                    else
                    {
                        HOperatorSet.CreateShapeModel(Image, new TurpleConvert(smc.numLevels).Tuple, smc.angleStart, smc.angleExtent, smc.angleStep, smc.optimization, smc.metric, new int[] { smc.tempmincontrast, smc.tempmaxcontrast, smc.mintuplelen } /*new TurpleConvert(smc.contrast).Tuple*/, new TurpleConvert(smc.minContrast).Tuple, out id);
                    }
                    HOperatorSet.SetShapeModelParam(id, "timeout", 3000);
                    break;
                case MatchMode.Planar:
                    HOperatorSet.CreatePlanarUncalibDeformableModel(Image, new TurpleConvert(smc.numLevels).Tuple, smc.angleStart, smc.angleExtent, new TurpleConvert(smc.angleStep).Tuple, smc.scaleRMin, smc.scaleRMax, new TurpleConvert(smc.scaleRstep).Tuple, smc.ScaleCMin, smc.scaleCMax, new TurpleConvert(smc.scaleCstep).Tuple, smc.optimization, smc.metric, new int[] { smc.tempmincontrast, smc.tempmaxcontrast } /*new TurpleConvert(smc.contrast).Tuple*/, new TurpleConvert(smc.minContrast).Tuple, new HTuple("min_size"), new HTuple(smc.mintuplelen), out id);
                    break;
                case MatchMode.LocalDeformable:
                    id = new HTuple();
                    break;
                default:
                    id = new HTuple();
                    break;
            }
        }

        public ModelInfo CreateModelDxf(string path, double scale, ShapModelCondition smc, out List<List<Contours>> Cont)
        {
            try
            {
                HObject contours;
                HTuple sts, id;
                HOperatorSet.ReadContourXldDxf(out contours, path, new HTuple(), new HTuple(), out sts);
                HHomMat2D mat = new HHomMat2D();
                mat.HomMat2dIdentity();
                mat = mat.HomMat2dScale(scale, scale, 0, 0);
                HOperatorSet.AffineTransContourXld(contours, out contours, mat);


                switch (smc.matchmode)
                {
                    case MatchMode.Normal:
                        if (smc.scaledEnble)
                        {
                            HOperatorSet.CreateScaledShapeModelXld(contours, new TurpleConvert(smc.numLevels).Tuple, smc.angleStart, smc.angleExtent, new TurpleConvert(smc.angleStep).Tuple, smc.scaleMin, smc.scaleMax, new TurpleConvert(smc.scaleStep).Tuple, new string[] { "none", "no_pregeneration" }, "ignore_local_polarity", new TurpleConvert(smc.minContrast).Tuple, out id);
                        }
                        else
                        {
                            HOperatorSet.CreateShapeModelXld(contours, new TurpleConvert(smc.numLevels).Tuple, smc.angleStart, smc.angleExtent, smc.angleStep, new string[] { "none", "no_pregeneration" }, "ignore_local_polarity", new TurpleConvert(smc.minContrast).Tuple, out id);
                        }
                        HOperatorSet.SetShapeModelParam(id, "timeout", 3000);

                        break;
                    case MatchMode.Planar:
                        HOperatorSet.CreatePlanarUncalibDeformableModelXld(contours, new TurpleConvert(smc.numLevels).Tuple, smc.angleStart,
                            smc.angleExtent, new TurpleConvert(smc.angleStep).Tuple, smc.scaleRMin, smc.scaleRMax,
                            new TurpleConvert(smc.scaleRstep).Tuple, smc.ScaleCMin, smc.scaleCMax, new TurpleConvert(smc.scaleCstep).Tuple,
                            "none", "ignore_color_polarity",
                            new TurpleConvert(smc.minContrast).Tuple, new HTuple(), new HTuple(), out id);

                        break;
                    case MatchMode.LocalDeformable:
                        id = new HTuple();
                        break;
                    default:
                        id = new HTuple();
                        break;
                }
                HTuple crow, ccol, area, top, left, right, bottom;
              
               // HOperatorSet.AreaCenterPointsXld(contours, out area, out crow, out ccol);
               // HOperatorSet.GetContourXld(contours, out crow, out ccol);
                //   HTuple.
                // HOperatorSet.TupleMax(crow, out var max);
                HOperatorSet.SmallestRectangle1Xld(contours, out top, out left, out bottom, out right);

                HOperatorSet.TupleMin(top, out top);
                HOperatorSet.TupleMax(bottom, out bottom);
                HOperatorSet.TupleMin(left, out left);
                HOperatorSet.TupleMax(right, out right);

                crow = (top + bottom) / 2.0;
                ccol = (left + right) / 2.0;


                HOperatorSet.SmallestRectangle1Xld(contours, out top, out left, out bottom, out right);
                ModelInfo infor = new ModelInfo(id, right.D - left.D, bottom.D - top.D, smc);
                MatchResult match = new MatchResult(crow, ccol, new HTuple(new double[] { 0 }), new HTuple(new double[] { 1 }), new HTuple(new double[] { 0 }), infor);
                Cont = match.GetContours();
                return infor;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void disPoseModel(ModelInfo modelinfo)
        {
            switch (modelinfo.smc.matchmode)
            {
                case MatchMode.Normal:
                    HOperatorSet.ClearShapeModel(modelinfo.ModelId);
                    break;
                case MatchMode.Planar:
                    HOperatorSet.ClearDeformableModel(modelinfo.ModelId);
                    break;
                case MatchMode.LocalDeformable:
                    HOperatorSet.ClearDeformableModel(modelinfo.ModelId);
                    break;
                default:
                    break;
            }

        }
        public double[] GetAreaRect(UInt64 LvImage, int LineWidth, int width, int height, int minThresholdGray = 128, int maxThresholdGray = 255)
        {
            HObject src, region, regions, selectregion;
            HTuple areas, row, col, left, top, right, buttom;
            LvToHalcon(LvImage, LineWidth, width, height, 0, out src);
            HOperatorSet.Threshold(src, out region, minThresholdGray, maxThresholdGray);
            HOperatorSet.Connection(region, out regions);
            HOperatorSet.AreaCenter(regions, out areas, out row, out col);
            HOperatorSet.SelectShape(regions, out selectregion, "area", "and", areas.TupleMax(), areas.TupleMax());
            HOperatorSet.SmallestRectangle1(selectregion, out top, out left, out buttom, out right);
            return new double[] { left.D, top.D, right.D, buttom.D };
            //            threshold(src, Region, 220, 255)
            //connection(Region, ConnectedRegions)
            //area_center(ConnectedRegions, Area, Row3, Column3)
            //select_shape(ConnectedRegions, SelectedRegions, 'area', 'and', max(Area), max(Area) + 1)
            //shape_trans(SelectedRegions, RegionTrans, 'rectangle1')
            //dilation_rectangle1(RegionTrans, RegionDilation, 150, 150)

        }

        public MatchResult[] ShapMatchs(string path, ModelInfo[] Model, MatchCondition mc)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            HObject src, ReduceSrc, region;
            HTuple rows, cols, angles, scaleds, scores, modelIndex, roi_top = new HTuple(0.0), roi_left = new HTuple(0.0);
            //LvToHalcon(LvImage, LineWidth, width, height, 0, out src);
            HOperatorSet.ReadImage(out src, path);
            ReduceSrc = src;
            HTuple modelIds = new HTuple(new int[] { });
            for (int i = 0; i < Model.Length; i++)
            {
                modelIds = modelIds.TupleConcat(Model[i].ModelId);
            }
            if (mc.Roix.Length != 0)
            {
                HTuple t1, t2;
                HOperatorSet.GenRegionPolygonFilled(out region, mc.RoiY, mc.Roix);
                HOperatorSet.ReduceDomain(src, region, out ReduceSrc);
                HOperatorSet.SmallestRectangle1(region, out roi_top, out roi_left, out t1, out t2);
                HOperatorSet.CropDomain(ReduceSrc, out ReduceSrc);
                region.Dispose();
            }
            if (mc.ScaledEnble == true)
            {
                HOperatorSet.FindScaledShapeModels(ReduceSrc, modelIds, mc.angleStart, mc.angleExtent, mc.ScaleMin, mc.ScaleMax, mc.minScore, mc.numMatches, mc.maxOverlap, mc.subPixel, mc.numLevels, mc.greediness, out rows, out cols, out angles, out scaleds, out scores, out modelIndex);
            }
            else
            {
                HOperatorSet.FindShapeModels(ReduceSrc, modelIds, mc.angleStart, mc.angleExtent, mc.minScore, mc.numMatches, mc.maxOverlap, mc.subPixel, mc.numLevels, mc.greediness, out rows, out cols, out angles, out scores, out modelIndex);
                scaleds = new HTuple(new double[] { });
                if (scores.Length != 0)
                {
                    foreach (var item in scores.DArr)
                    {
                        scaleds = scaleds.TupleConcat(1.0);
                    }
                }
            }
            if (scores.Length != 0)
            {
                for (int i = 0; i < scores.Length; i++)
                {
                    rows[i].D = rows[i].D + roi_top.D;
                    cols[i].D = cols[i].D + roi_left.D;
                }
            }
            List<MatchResult> res = new List<MatchResult>();
            for (int i = 0; i < rows.Length; i++)
            {
                MatchResult Res = new MatchResult(rows[i].DArr, cols[i].DArr, angles[i].DArr, scaleds[i].DArr, scores[i].DArr, Model[modelIndex[i]]);
                res.Add(Res);
            }
            src.Dispose();
            ReduceSrc.Dispose();
            sw.Stop();
            return res.ToArray();
        }

        public bool BeadInspection(UInt64 LvImage, int LineWidth, int width, int height, double[] ContoureX, double[] ContoureY, int TargetWidth, int WidthTolerance, int PositionTolerance, string Polarity, double sigma, double threshold, out Contours[] left, out Contours[] right, out Contours[] Nobead, out Contours[] toothin, out Contours[] toothick, out Contours[] incorrectPos)
        {
            LvToHalcon(LvImage, LineWidth, width, height, 0, out HObject Image);
            HOperatorSet.GenContourNurbsXld(out HObject counter, ContoureY, ContoureX, "auto", "auto", 3, 1, 20);
            HOperatorSet.CreateBeadInspectionModel(counter, TargetWidth, WidthTolerance, PositionTolerance, Polarity,
                new string[] { "sigma", "threshold" }, new double[] { sigma, threshold }, out HTuple id);
            HOperatorSet.ApplyBeadInspectionModel(Image, out HObject lc, out HObject rc, out HObject ErrSeg, id, out HTuple ErrType);

            left = ContourToXld(lc);
            right = ContourToXld(rc);
            //['no bead','too thin','too thick','incorrect position']  
            List<int> inobead = new List<int>(), itoothin = new List<int>(), itoothick = new List<int>(), iincpos = new List<int>();
            for (int i = 1; i <= ErrType.Length; i++)
            {
                switch (ErrType[i - 1].S)
                {
                    case "no bead":
                        inobead.Add(i);
                        break;
                    case "too thin":
                        itoothin.Add(i);
                        break;
                    case "too thick":
                        itoothick.Add(i);
                        break;
                    case "incorrect position":
                        iincpos.Add(i);
                        break;
                    default:
                        break;
                }
            }

            HOperatorSet.SelectObj(ErrSeg, out HObject nobeadC, inobead.ToArray());
            Nobead = ContourToXld(nobeadC);
            nobeadC.Dispose();


            HOperatorSet.SelectObj(ErrSeg, out HObject toothinC, itoothin.ToArray());
            var c = ContourIntersection(toothinC, lc, rc);
            toothin = ContourToXld(c);
            c.Dispose();
            toothinC.Dispose();

            HOperatorSet.SelectObj(ErrSeg, out HObject toothickC, itoothick.ToArray());
            var c1 = ContourIntersection(toothickC, lc, rc);
            toothick = ContourToXld(c1);
            c1.Dispose();
            toothickC.Dispose();

            HOperatorSet.SelectObj(ErrSeg, out HObject incposC, iincpos.ToArray());
            var c2 = ContourIntersection(incposC, lc, rc);
            incorrectPos = ContourToXld(c2);
            c2.Dispose();
            incposC.Dispose();

            HOperatorSet.ClearBeadInspectionModel(id);

            lc.Dispose();
            rc.Dispose();
            ErrSeg.Dispose();
            counter.Dispose();
            Image.Dispose();
            if (ErrType.Length == 0) return true;
            else return false;
        }
        public bool ColorSelectObject(UInt64 LvImage, int LineWidth, int width, int height, double[] RoiPoints, int[] thesholdMin, int[] thesholdMax, int DilationRadius, out double[] RectX, out double[] RectY)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            HObject image;
            var time = LvToHalcon(LvImage, LineWidth, width, height, 3, out image);
            var ReduceSrc = image;
            if (RoiPoints.Length != 0)
            {
                double[] roix = new double[RoiPoints.Length / 2];
                double[] roiy = new double[RoiPoints.Length / 2];
                int len = RoiPoints.Length / 2 * 2;
                for (int i = 0; i < len; i++)
                {
                    if (i % 2 == 0) roix[i / 2] = RoiPoints[i];
                    else roiy[i / 2] = RoiPoints[i];
                }
                HOperatorSet.GenRegionPolygonFilled(out var region, roiy, roix);
                HOperatorSet.ReduceDomain(image, region, out ReduceSrc);
                region.Dispose();
            }
            HOperatorSet.Decompose3(ReduceSrc, out var R, out var G, out var B);
            HOperatorSet.TransFromRgb(R, G, B, out var H, out var S, out var V, "hsv");
            HOperatorSet.Threshold(H, out var region1, thesholdMin[0], thesholdMax[0]);
            HOperatorSet.Threshold(S, out var region2, thesholdMin[1], thesholdMax[1]);
            HOperatorSet.Intersection(region1, region2, out var region3);
            if (DilationRadius == 0) DilationRadius = 5;
            HOperatorSet.ErosionCircle(region3, out var regionErosion, DilationRadius);
            HOperatorSet.DilationCircle(regionErosion, out var regionDilation, DilationRadius);
            HOperatorSet.Connection(regionDilation, out var connect);
            HOperatorSet.SelectShapeStd(connect, out var selectregion, "max_area", 90);
            HOperatorSet.ShapeTrans(selectregion, out var rect, "rectangle2");
            HOperatorSet.SmallestRectangle2(rect, out var row, out var col, out var phi, out var len11, out var len22);

            float sin = (float)Math.Sin(-phi);
            float cos = (float)Math.Cos(-phi);
            float len1 = (float)len11.D;
            float len2 = (float)len22.D;
            Matrix mat = new Matrix(cos, sin, -sin, cos, (float)col.D, (float)row.D);
            PointF[] points = new PointF[] { new PointF(-len1, len2), new PointF(len1, len2), new PointF(len1, -len2), new PointF(-len1, -len2) };
            mat.TransformPoints(points);

            RectX = points.Select(x => (double)x.X).ToArray();
            RectY = points.Select(x => (double)x.Y).ToArray();
            R.Dispose();
            G.Dispose();
            B.Dispose();
            H.Dispose();
            S.Dispose();
            V.Dispose();
            region1.Dispose();
            region2.Dispose();
            region3.Dispose();
            regionErosion.Dispose();
            regionDilation.Dispose();
            connect.Dispose();
            selectregion.Dispose();
            rect.Dispose();
            ReduceSrc.Dispose();
            image.Dispose();
            return len11.D > 0 && len11.D > 0;
        }


        public bool SelectLines(UInt64 LvImage, int LineWidth, int width, int height, double[] RoiPoints, LinePram param, out Contours[] contours, out Contours[] left, out Contours[] right)
        {

            contours = new Contours[0];
            left = new Contours[0];
            right = new Contours[0];

            if (param == null) param = new LinePram();

            LvToHalcon(LvImage, LineWidth, width, height, 0, out var image);

            HObject ReduceImage = image;
            if (RoiPoints?.Length != 0)
            {
                GetROIPoint(RoiPoints, out var roix, out var roiy);
                HOperatorSet.GenRegionPolygonFilled(out var region, roiy, roix);
                HOperatorSet.ReduceDomain(image, region, out ReduceImage);
                region.Dispose();
            }

            var res = SelectLines(ReduceImage, param, out contours, out left, out right);
            image.Dispose();
            ReduceImage.Dispose();
            return res;
        }

        public bool SelectLineEdges(UInt64 LvImage, int LineWidth, int width, int height, double[] RoiPoints, LineEdgeparam param, out Contours[] contours)
        {
            contours = new Contours[0];
            if (param == null) param = new LineEdgeparam();

            LvToHalcon(LvImage, LineWidth, width, height, 0, out var image);

            HObject ReduceImage = image;
            if (RoiPoints?.Length != 0)
            {
                GetROIPoint(RoiPoints, out var roix, out var roiy);
                HOperatorSet.GenRegionPolygonFilled(out var region, roiy, roix);
                HOperatorSet.ReduceDomain(image, region, out ReduceImage);
                region.Dispose();
            }
            var res = SelectLineEdges(ReduceImage, param, out contours);
            image.Dispose();
            ReduceImage.Dispose();
            return res;
        }

        internal static bool SelectLines(HObject Image, LinePram param, out Contours[] contours, out Contours[] left, out Contours[] right)
        {

            contours = new Contours[0];
            left = new Contours[0];
            right = new Contours[0];

            if (param == null) param = new LinePram();

            CalculateLinesGaussParameters(param.maxLineWidth, param.contrast, out var sigma, out var low, out var high);
            if (param.lineMode == null || param.lineMode == "") param.lineMode = "bar-shaped";
            HOperatorSet.LinesGauss(Image, out var lines, sigma, low, high, (param.ligthDark ? "light" : "dark"), param.extractWidth ? "true" : "false", param.lineMode, param.completeJunctions ? "true" : "false");

            HOperatorSet.SegmentContoursXld(lines, out var segc, "lines", 5, 4, 3);

            HOperatorSet.UnionCollinearContoursXld(segc, out var unionc, param.unionMaxdisAbs, param.unionMaxdisRel, param.unionMaxShift, new HTuple(param.unionMaxAngle).TupleRad(), "attr_keep");

            HOperatorSet.SelectContoursXld(unionc, out var selectedcontours, "contour_length", param.minlineLength, param.maxLineLength, 0, 0);

            HObject cangle = null;
            if (param.lineMinAngle?.Length > 0 && param.lineMaxAngle?.Length > 0)
            {
                HOperatorSet.GenEmptyObj(out cangle);
                for (int i = 0; i < param.lineMinAngle.Length && i < param.lineMaxAngle.Length; i++)
                {
                    HOperatorSet.SelectContoursXld(selectedcontours, out var ctemp, "direction", new HTuple(param.lineMinAngle[i]).TupleRad(), new HTuple(param.lineMaxAngle[i]).TupleRad(), -0.5, 0.5);
                    if (ctemp.CountObj() > 0)
                    {
                        var temp = cangle.ConcatObj(ctemp);
                        cangle.Dispose();
                        cangle = temp;
                    }
                    ctemp.Dispose();
                }
            }
            if (cangle == null) cangle = selectedcontours;
            var len = cangle.CountObj();
            contours = new Contours[len];
            left = new Contours[len];
            right = new Contours[len];

            for (int i = 1; i <= len; i++)
            {
                /*var line = */
                HOperatorSet.SelectObj(cangle, out var line, i);
                HOperatorSet.GetContourXld(line, out var row, out var col);
                HOperatorSet.LengthXld(line, out var length);
                HOperatorSet.GetContourAttribXld(line, "angle", out var angle);
                HOperatorSet.GetContourAttribXld(line, "width_right", out var widthR);
                HOperatorSet.GetContourAttribXld(line, "width_left", out var widthL);

                contours[i - 1] = new Contours(row.DArr, col.DArr, length);

                int count = angle.Length;
                var rr = new double[0];
                var rc = new double[0];
                var lr = new double[0];
                var lc = new double[0];

                HOperatorSet.TupleCos(angle, out var cos);
                HOperatorSet.TupleSin(angle, out var sin);
                rr = row + cos * widthR;
                rc = col + sin * widthR;
                lr = row - cos * widthL;
                lc = col - sin * widthL;

                right[i - 1] = new Contours(rr, rc);
                left[i - 1] = new Contours(lr, lc);

                //line.Dispose();
            }
            selectedcontours.Dispose();
            cangle.Dispose();
            lines.Dispose();
            segc.Dispose();
            unionc.Dispose();
            if (len > 0) return true;
            else return false;
        }

        internal static bool SelectLineEdges(HObject Image, LineEdgeparam param, out Contours[] contours)
        {
            contours = new Contours[0];
            if (param == null) param = new LineEdgeparam();

            HOperatorSet.EdgesSubPix(Image, out var lines, "canny", param.Alpha, param.minContrast, param.maxContrast);
            HOperatorSet.SegmentContoursXld(lines, out var segc, "lines", 5, 4, 3);

            HOperatorSet.UnionCollinearContoursXld(segc, out var unionc, param.unionMaxdisAbs, param.unionMaxdisRel, param.unionMaxShift, new HTuple(param.unionMaxAngle).TupleRad(), "attr_keep");

            HOperatorSet.SelectContoursXld(unionc, out var selectedcontours, "contour_length", param.minlineLength, param.maxLineLength, 0, 0);

            HObject cangle = null;
            if (param.lineMinAngle?.Length > 0 && param.lineMaxAngle?.Length > 0)
            {
                HOperatorSet.GenEmptyObj(out cangle);
                for (int i = 0; i < param.lineMinAngle.Length && i < param.lineMaxAngle.Length; i++)
                {
                    HOperatorSet.SelectContoursXld(selectedcontours, out var ctemp, "direction", new HTuple(param.lineMinAngle[i]).TupleRad(), new HTuple(param.lineMaxAngle[i]).TupleRad(), -0.5, 0.5);
                    if (ctemp.CountObj() > 0)
                    {
                        var temp = cangle.ConcatObj(ctemp);
                        cangle.Dispose();
                        cangle = temp;
                    }
                    ctemp.Dispose();
                }
            }
            if (cangle == null) cangle = selectedcontours;
            var len = cangle.CountObj();
            contours = new Contours[len];
            for (int i = 1; i <= len; i++)
            {
                /*var line = */
                HOperatorSet.SelectObj(cangle, out var line, i);
                HOperatorSet.GetContourXld(line, out var row, out var col);
                HOperatorSet.LengthXld(line, out var length);
                contours[i - 1] = new Contours(row.DArr, col.DArr, length);
            }
            selectedcontours.Dispose();
            cangle.Dispose();
            lines.Dispose();
            segc.Dispose();
            unionc.Dispose();
            if (len > 0) return true;
            else return false;
        }


        private static void CalculateLinesGaussParameters(double maxlinewidth, double[] Contrast, out double Sigma, out double low, out double high)
        {
            high = Contrast[0];
            if (Contrast.Length == 2)
                low = Contrast[1];
            else
                low = high / 3.0;
            if (maxlinewidth < Math.Sqrt(3.0))
            {
                low = low * maxlinewidth / Math.Sqrt(3.0);
                high = high * maxlinewidth / Math.Sqrt(3.0);
                maxlinewidth = Math.Sqrt(3.0);
            }
            var halfwidth = maxlinewidth / 2.0;
            Sigma = halfwidth / Math.Sqrt(3.0);
            var help = -2.0 * halfwidth / (Math.Sqrt(Math.PI * 2) * Math.Pow(Sigma, 3.0)) * Math.Exp(-0.5 * Math.Pow(halfwidth / Sigma, 2.0));
            high = Math.Abs(high * help);
            low = Math.Abs(low * help);
        }

        internal static void GetROIPoint(double[] RoiPoints, out double[] roix, out double[] roiy)
        {
            roix = new double[0];
            roiy = new double[0];
            if (RoiPoints.Length != 0)
            {
                roix = new double[RoiPoints.Length / 2];
                roiy = new double[RoiPoints.Length / 2];
                int len = RoiPoints.Length / 2 * 2;
                for (int i = 0; i < len; i++)
                {
                    if (i % 2 == 0) roix[i / 2] = RoiPoints[i];
                    else roiy[i / 2] = RoiPoints[i];
                }
            }
        }
        private Contours[] ContourToXld(HObject contour)
        {
            HOperatorSet.SegmentContoursXld(contour, out HObject splitc, "lines_circles", 5, 4, 2);
            int count = splitc.CountObj();
            var con = new Contours[count];
            for (int i = 1; i <= count; i++)
            {
                HTuple rows, cols;
                HOperatorSet.SelectObj(splitc, out HObject selectcon, i);
                HOperatorSet.GetContourXld(selectcon, out rows, out cols);
                con[i - 1] = new Contours(rows.DArr, cols.DArr);
            }
            return con;
        }
        private HObject ContourIntersection(HObject counters, HObject leftC, HObject rightC)
        {
            //gen_parallel_contour_xld(TooThinSegments, ParallelContours1, 'regression_normal', 50)
            //gen_parallel_contour_xld(TooThinSegments, ParallelContours2, 'regression_normal', -50)
            //close_parallels(ParallelContours1, ParallelContours2, Contour)
            //close_parallels(LeftContour, RightContour, Contour1)
            //intersection_closed_contours_xld(Contour, Contour1, ContoursIntersection)
            HOperatorSet.GenParallelContourXld(counters, out HObject ParallelContours1, "regression_normal", 50);
            HOperatorSet.GenParallelContourXld(counters, out HObject ParallelContours2, "regression_normal", -50);

            int len = ParallelContours1.CountObj();
            HOperatorSet.GenEmptyObj(out var targc);
            for (int i = 1; i <= len; i++)
            {
                HOperatorSet.SelectObj(ParallelContours1, out HObject p1, i);
                HOperatorSet.SelectObj(ParallelContours2, out HObject p2, i);
                HOperatorSet.GetContourXld(p1, out var row1, out var col1);
                HOperatorSet.GetContourXld(p2, out var row2, out var col2);
                HOperatorSet.GenContourPolygonXld(out var closed, new HTuple(row1, row2.TupleInverse(), row1[0]), new HTuple(col1, col2.TupleInverse(), col1[0]));
                HOperatorSet.ConcatObj(targc, closed, out targc);
                p1.Dispose();
                p2.Dispose();
                closed.Dispose();
            }
            int len2 = leftC.CountObj();
            HOperatorSet.GenEmptyObj(out var targc2);
            for (int i = 1; i <= len2; i++)
            {
                HOperatorSet.SelectObj(leftC, out HObject p1, i);
                HOperatorSet.SelectObj(rightC, out HObject p2, i);
                HOperatorSet.GetContourXld(p1, out var row1, out var col1);
                HOperatorSet.GetContourXld(p2, out var row2, out var col2);
                HOperatorSet.GenContourPolygonXld(out var closed, new HTuple(row1, row2.TupleInverse(), row1[0]), new HTuple(col1, col2.TupleInverse(), col1[0]));
                HOperatorSet.ConcatObj(targc2, closed, out targc2);
                p1.Dispose();
                p2.Dispose();
                closed.Dispose();
            }
            HOperatorSet.IntersectionClosedContoursXld(targc, targc2, out var inters);
            targc.Dispose();
            targc2.Dispose();
            ParallelContours1.Dispose();
            ParallelContours2.Dispose();
            return inters;
        }
    }

    public class log
    {
        public static string fileName { get; set; } = "d:\\log.log";
        public static void WriteFile(string msg)
        {
            return;
            using (StreamWriter sw = new StreamWriter(fileName, true))
            {
                sw.Write($"{DateTime.Now.ToString("yy/MM/dd HH:mm:ss.fff")} {msg}\r\n");
            }
        }
    }

    public class MatchCondition
    {
        public double angleStart = 0;
        public double angleExtent = 0;
        public double minScore = 0.5;
        public int numMatches = 1;
        public double maxOverlap = 0.5;
        public string subPixel = "least_squares";
        public int numLevels = 0;
        public double greediness = 0.7;
        public double ScaleMin = 1;
        public double ScaleMax = 1;
        public bool ScaledEnble;
        public MatchMode MatchMode = MatchMode.Normal;
        public double scaleRMin = 1;
        public double scaleRMax = 1;
        public double scaleCMin = 1;
        public double scaleCMax = 1;


        public double[] Roix
        {
            get
            {
                if (RoiPoints == null) return new double[] { };
                double[] arr = new double[RoiPoints.Length / 2];

                for (int i = 0; i < RoiPoints.Length; i += 2)
                {
                    arr[i / 2] = RoiPoints[i];
                }
                return arr;

            }
        }
        public double[] RoiY
        {
            get
            {
                if (RoiPoints == null) return new double[] { };
                double[] arr = new double[RoiPoints.Length / 2];
                for (int i = 1; i < RoiPoints.Length; i += 2)
                {
                    arr[i / 2] = RoiPoints[i];
                }
                return arr;
            }
        }
        public double[] RoiPoints
        {
            get; set;
        }
    }
    public enum MatchMode
    {
        Normal = 0,
        Planar = 1,
        LocalDeformable = 2

    }
    public class MatchResult
    {
        public double[] rows;
        public double[] cols;
        public double[] angles;
        public double[] scores;
        //

        public double[] Scaleds;
        //

        public double[] Rscales;
        public double[] Cscales;

        public BoundingBox[] boxes;

        private int _size = 0;
        private HHomMat2D[] mat;
        // private HObject countours;
        private ModelInfo _ModelInfo;
        private MatchMode matchmode = MatchMode.Normal;
        public MatchResult(HTuple _rows, HTuple _cols, HTuple _angles, HTuple _scaleds, HTuple _scores, ModelInfo model)
        {

            if (_rows.Length == 0) return;
            rows = _rows.DArr;
            cols = _cols.DArr;
            angles = _angles.DArr;
            Scaleds = _scaleds.DArr;
            Rscales = _scaleds.DArr;
            Cscales = _scaleds.DArr;
            scores = _scores.DArr;
            _size = _rows.DArr.Length;
            mat = new HHomMat2D[_size];
            boxes = new BoundingBox[_size];
            _ModelInfo = model;
            for (int i = 0; i < _size; i++)
            {
                HTuple px, py, qx, qy, crow, ccol;

                mat[i] = new HHomMat2D();
                // HOperatorSet.GetShapeModelOrigin(model.ModelId, out crow, out ccol);
                mat[i].VectorAngleToRigid(0, 0, 0, rows[i], cols[i], angles[i]);
                mat[i] = mat[i].HomMat2dScale(Scaleds[i], Scaleds[i], rows[i], cols[i]);
                py = new double[] { -model.Width / 2, model.Width / 2, model.Width / 2, -model.Width / 2, -model.Width / 2 };
                px = new double[] { -model.Height / 2, -model.Height / 2, model.Height / 2, model.Height / 2, -model.Height / 2 };
                HOperatorSet.AffineTransPoint2d(mat[i], px, py, out qx, out qy);
                boxes[i] = new BoundingBox(qx.DArr, qy.DArr);
            }
        }
        public MatchResult(ModelInfo model, HTuple hom2dmat, HTuple scores)
        {
            if (scores.Length == 0) return;
            _size = scores.Length;
            mat = new HHomMat2D[_size];
            boxes = new BoundingBox[_size];
            matchmode = MatchMode.Planar;
            this.scores = scores.DArr;
            _ModelInfo = model;

            double cw = model.Width / 2.0;
            double ch = model.Height / 2.0;
            HTuple pcol = new double[] { -cw, cw, cw, -cw }, prow = new double[] { -ch, -ch, ch, -ch };
            HTuple r = new double[] { }, c = new double[] { }, a = new double[] { }, rs = new double[] { }, cs = new double[] { };

            for (int i = 0; i < scores.Length; i++)
            {
                HTuple crow, ccol, trow, tcol, tangle;
                mat[i] = new HHomMat2D(hom2dmat.TupleSelectRange(i * 9, i * 9 + 8));
                HOperatorSet.ProjectiveTransPixel(mat[i], 0, 0, out crow, out ccol);
                HOperatorSet.ProjectiveTransPixel(mat[i], prow, pcol, out trow, out tcol);
                HOperatorSet.AngleLl(prow[0].D, pcol[0].D, prow[1].D, pcol[1].D, trow[0].D, tcol[0].D, trow[1].D, tcol[1].D, out tangle);
                r = r.TupleConcat(crow);
                c = c.TupleConcat(ccol);
                a = a.TupleConcat(tangle);
                cs = cs.TupleConcat(Math.Sqrt(Math.Pow(trow[0] - trow[1], 2) + Math.Pow(tcol[0] - tcol[1], 2)) / model.Width);
                rs = rs.TupleConcat(Math.Sqrt(Math.Pow(trow[1] - trow[2], 2) + Math.Pow(tcol[1] - tcol[1], 2)) / model.Height);
                boxes[i] = new BoundingBox(trow.DArr, tcol.DArr);
            }
            rows = r.DArr;
            cols = c.DArr;
            angles = a.DArr;
            Rscales = rs.DArr;
            Cscales = cs.DArr;
            Scaleds = new double[Rscales.Length];
            //Scaleds =rs.DArr.Select(s=>Math.Max(s,cs))       
            for (int i = 0; i < Rscales.Length; i++)
            {
                Scaleds[i] = Math.Max(Rscales[i], Cscales[i]);
            }
        }
        public MatchResult()
        {

        }
        public List<List<Contours>> GetContours()
        {
            List<List<Contours>> MatchContours = new List<List<Contours>>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            if (_ModelInfo == null) return new List<List<Contours>>();
            for (int i = 0; i < _size; i++)
            {
                List<Contours> list = new List<Contours>();
                foreach (var item in _ModelInfo.ModelContours)
                {
                    HTuple qx = new double[] { }, qy = new double[] { };
                    switch (matchmode)
                    {
                        case MatchMode.Normal:
                            HOperatorSet.AffineTransPoint2d(mat[i], item.row, item.col, out qx, out qy);
                            break;
                        case MatchMode.Planar:
                            HOperatorSet.ProjectiveTransPixel(mat[i], item.row, item.col, out qx, out qy);
                            break;
                        case MatchMode.LocalDeformable:
                            break;
                        default:
                            break;
                    }
                    Contours c = new Contours(qx.DArr, qy.DArr);
                    list.Add(c);
                }
                MatchContours.Add(list);
            }
            sw.Stop();
            return MatchContours;
        }

        public void offSet(HTuple r, HTuple c)
        {
            if (Size == 0) return;
            rows = rows.Select(s => s + r.D).ToArray();
            cols = cols.Select(s => s + c.D).ToArray();
            for (int i = 0; i < Size; i++)
            {
                boxes[i].ArrayCol = boxes[i].ArrayCol.Select(s => s + c.D).ToArray();
                boxes[i].ArrayRow = boxes[i].ArrayRow.Select(s => s + r.D).ToArray();
                mat[i] = mat[i].HomMat2dTranslate(r.D, c.D);
            }
        }
        public int Size
        {
            get { return _size; }
        }

    }
    public class ShapModelCondition
    {

        public string numLevels = "auto";
        public double angleStart = 0;
        public double angleExtent = 0;
        public string angleStep = "auto";
        public string optimization = "auto";
        public string metric = "use_polarity";
        public string contrast = "auto";
        public string minContrast = "auto";
        //  public int modelID;
        public bool scaledEnble = false;
        public string scaleStep = "auto";
        public double scaleMax = 1;
        public double scaleMin = 1;
        public double scaleRMax = 1;
        public double scaleRMin = 1;
        public string scaleRstep = "auto";
        public double scaleCMax = 1;
        public double ScaleCMin = 1;
        public string scaleCstep = "auto";
        public MatchMode matchmode = MatchMode.Normal;

        public int tempmincontrast = 20;
        public int tempmaxcontrast = 40;
        public int mintuplelen = 10;

    }
    public class TurpleConvert
    {
        string _str;
        public TurpleConvert(string str)
        {
            _str = str;
            int a;
            double b;
            if (int.TryParse(str, out a))
            {
                Tuple = a;
                return;
            }
            if (double.TryParse(str, out b))
            {
                Tuple = b;
                return;
            }
            Tuple = str;
        }
        public HTuple Tuple;
        public int INT
        {
            get { return int.Parse(_str); }
        }
        public double DOUBLE
        {
            get { return double.Parse(_str); }
        }

    }
    public class Contours : IComparable<Contours>
    {
        public double[] row;
        public double[] col;
        double _lenght = 0;
        public double lenght
        {
            get
            {
                if (_lenght == 0 && row?.Length > 1)
                {
                    HOperatorSet.GenContourPolygonXld(out var contour, row, col);
                    HOperatorSet.LengthXld(contour, out var len);
                    _lenght = len;
                    contour.Dispose();
                }
                return _lenght;
            }

        }
        public Contours(double[] _row, double[] _col, double length = 0)
        {
            row = _row;
            col = _col;
            this._lenght = length;
        }
        public Contours()
        {
            row = new double[0];
            col = new double[0];
        }

        public int CompareTo(Contours other)
        {
            return this.lenght.CompareTo(other.lenght);
        }
        ~Contours()
        {
            this.row = new double[0];
            this.col = new double[0];
        }
    }
    public class ModelInfo
    {
        private ShapModelCondition _smc;
        public ShapModelCondition smc { get { return _smc; } }
        public HTuple ModelId { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        private List<Contours> _ModelContours = new List<Contours>();
        public List<Contours> ModelContours { get { return _ModelContours; } }

        public ModelInfo(HTuple id, HTuple width, HTuple height, ShapModelCondition smc)
        {
            HObject cons = new HObject(), ContoursSplit, selectcon;
            this._smc = smc;
            this.Width = width;
            this.Height = height;
            this.ModelId = id;

            switch (smc.matchmode)
            {
                case MatchMode.Normal:
                    HOperatorSet.GetShapeModelContours(out cons, id, 1);
                    break;
                case MatchMode.Planar:
                    HOperatorSet.GetDeformableModelContours(out cons, id, 1);
                    break;
                case MatchMode.LocalDeformable:
                    break;
                default:
                    break;
            }

            HOperatorSet.SegmentContoursXld(cons, out ContoursSplit, "lines_circles", 5, 4, 2);
            int count = ContoursSplit.CountObj();
            for (int i = 1; i <= count; i++)
            {
                HTuple rows, cols;
                HOperatorSet.SelectObj(ContoursSplit, out selectcon, i);
                HOperatorSet.GetContourXld(selectcon, out rows, out cols);
                Contours c = new Contours(rows.DArr, cols.DArr);
                ModelContours.Add(c);
            }
        }
        ~ModelInfo()
        {
            dispose();
            // MessageBox.Show("disposed!");
        }

        public void dispose()
        {
            try
            {
                switch (smc.matchmode)
                {
                    case MatchMode.Normal:
                        HOperatorSet.ClearShapeModel(this.ModelId);
                        break;
                    case MatchMode.Planar:
                        HOperatorSet.ClearDeformableModel(this.ModelId);
                        break;
                    case MatchMode.LocalDeformable:
                        HOperatorSet.ClearDeformableModel(this.ModelId);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
            }

        }

    }
    public struct Point
    {
        public double x;
        public double y;
        public Point(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }
    public class BoundingBox
    {
        public double[] ArrayCol
        {
            get; set;
        }
        public double[] ArrayRow
        {
            get; set;
        }
        public BoundingBox(double[] row, double[] col)
        {
            ArrayRow = row;
            ArrayCol = col;
        }
    }

    public class LinePram
    {
        public double maxLineWidth { get; set; } = 10;
        public double[] contrast { get; set; } = new double[] { 50 };
        public bool ligthDark { get; set; } = false;
        public bool extractWidth { get; set; } = true;
        public bool completeJunctions { get; set; } = false;

        public double minlineLength { get; set; } = 200;
        public double maxLineLength { get; set; } = 100000;
        public string lineMode { get; set; } = "bar-shaped";

        public double unionMaxdisAbs { get; set; } = 10;

        public double unionMaxdisRel { get; set; } = 10;

        public double unionMaxShift { get; set; } = 10;

        public double unionMaxAngle { get; set; } = 10;

        public double[] lineMinAngle { get; set; }
        public double[] lineMaxAngle { get; set; }
    }

    public class LineEdgeparam
    {
        public double Alpha { get; set; } = 5;
        public double minContrast { get; set; } = 5;
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
