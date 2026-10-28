using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HalconDotNet;

namespace WOVE.Inspection
{
    public static class Scratch
    {
        public static int CheckScratch(HObject ho_image,rect2 rect2,ScratchParam scratchParam, rect2[] disROIs,out contour[] contours)
        {
            contours = new contour[0];
            int result = -999;
            if (ho_image == null) return -999;
            double row = rect2._row;
            double col = rect2._col;
            double phi = rect2._phi;
            double len1 = rect2._len1;
            double len2 = rect2._len2;
            HOperatorSet.GenEmptyObj(out HObject ho_rectangle);
            HOperatorSet.GenEmptyObj(out HObject ho_imageReduced);
            HOperatorSet.GenEmptyObj(out HObject ho_imageMean);
            HOperatorSet.GenEmptyObj(out HObject ho_dynimage);
            HOperatorSet.GenEmptyObj(out HObject ho_connectImage);
            HOperatorSet.GenEmptyObj(out HObject ho_seleced1);
            HOperatorSet.GenEmptyObj(out HObject ho_closinged);
            HOperatorSet.GenEmptyObj(out HObject ho_selected2);
            HOperatorSet.GenEmptyObj(out HObject ho_contour);
            HOperatorSet.GenEmptyObj(out HObject ho_pingbi);
            HOperatorSet.GenEmptyObj(out HObject ho_diffregion);
            try
            {
                HOperatorSet.GenRectangle2(out ho_rectangle, row, col, phi, len1, len2);

                if (disROIs.Length > 0)
                {
                    for (int i = 0; i < disROIs.Length; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_disroi);
                        HOperatorSet.GenRectangle2(out ho_disroi, disROIs[i]._row, disROIs[i]._col, disROIs[i]._phi, disROIs[i]._len1, disROIs[i]._len2);
                        HOperatorSet.ConcatObj(ho_pingbi, ho_disroi, out ho_pingbi);
                        ho_disroi.Dispose();
                    }
                }
                if (ho_pingbi != null)
                {
                    HOperatorSet.Difference(ho_rectangle, ho_pingbi, out ho_diffregion);
                    HOperatorSet.ReduceDomain(ho_image, ho_diffregion, out ho_imageReduced);
                }
                else
                {
                    HOperatorSet.ReduceDomain(ho_image, ho_rectangle, out ho_imageReduced);
                }
               
                HOperatorSet.MeanImage(ho_imageReduced, out ho_imageMean, scratchParam.imgMean, scratchParam.imgMean);
                HOperatorSet.DynThreshold(ho_imageReduced, ho_imageMean, out ho_dynimage, scratchParam.thresh, scratchParam.lightDark);
                HOperatorSet.Connection(ho_dynimage, out ho_connectImage);
                string[] feartures = new string[2] { "area", "contlength" };
                int[] minValues = new int[2] { 100, 100 };
                int[] maxValues = new int[2] { 999999, 999999 };
                HOperatorSet.SelectShape(ho_connectImage, out ho_seleced1, feartures, "and", minValues, maxValues);
                HOperatorSet.ClosingCircle(ho_seleced1, out ho_closinged, 5.5);
                int[] minParam = new int[2] { scratchParam.minArea, scratchParam.minLength };
                int[] maxParam = new int[2] { scratchParam.maxArea, scratchParam.maxLenght };
                HOperatorSet.SelectShape(ho_closinged, out ho_selected2, feartures, "and", minParam, maxParam);
                HOperatorSet.GenContourRegionXld(ho_selected2, out ho_contour, "border");

                HOperatorSet.CountObj(ho_contour, out HTuple hv_num);
                contours = new contour[hv_num];
                for (int i = 0; i < hv_num; i++)
                {
                    HOperatorSet.GenEmptyObj(out HObject ho_selected3);
                    HOperatorSet.SelectObj(ho_contour, out  ho_selected3, i + 1);
                    HOperatorSet.GetContourXld(ho_selected3, out HTuple hv_row, out HTuple hv_col);
                    ho_selected3.Dispose();
                    contours[i].rows = hv_row;
                    contours[i].cols = hv_col;
                }
                result = 0;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                ho_rectangle.Dispose();
                ho_imageReduced.Dispose();
                ho_imageMean.Dispose();
                ho_dynimage.Dispose();
                ho_connectImage.Dispose();
                ho_seleced1.Dispose();
                ho_closinged.Dispose();
                ho_selected2.Dispose();
                ho_contour.Dispose();                
                ho_pingbi.Dispose();
                ho_diffregion.Dispose();
            }
            return result;
        }
    }

    public struct ScratchParam
    {
        public int imgMean;
        public int thresh;
        public string lightDark;
        public int  minArea;
        public int  maxArea;
        public int  minLength;
        public int  maxLenght;
    }
}
