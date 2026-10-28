using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HalconDotNet;

namespace WOVE.CellInspection
{
    public class CCellInspection
    {
        //public CCellInspection()
        //{
        //    AppDomain.CurrentDomain.AssemblyResolve+=new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        //}

        /// <summary>
        /// 错误代码：
        /// </summary>
        public int CheckCellPost(HObject ho_image, rect retract, out double width, out double height, out rect2 rect2, out double[] areas, out contour[] contours)
        {
            int result = -999;
            width = 0; height = 0;
            areas = new double[0];
            contours = new contour[0];
            rect2 = new rect2(0, 0, 0, 0, 0);

            double row, col, len1, len2, phi;
            HTuple hv_row, hv_col, hv_len1, hv_len2, hv_phi;
            HTuple hv_areas;

            if (ho_image == null) return -998;

            HOperatorSet.GenEmptyObj(out HObject ho_threshregion);
            HOperatorSet.GenEmptyObj(out HObject ho_boundregion);
            HOperatorSet.GenEmptyObj(out HObject ho_pingbiregion);
            HOperatorSet.GenEmptyObj(out HObject ho_diffregion);
            HOperatorSet.GenEmptyObj(out HObject ho_edges);
            HOperatorSet.GenEmptyObj(out HObject ho_selectedge);
            HOperatorSet.GenEmptyObj(out HObject ho_maxxld);
            HOperatorSet.GenEmptyObj(out HObject ho_rectangle);
            HOperatorSet.GenEmptyObj(out HObject ho_imgdomain);
            HOperatorSet.GenEmptyObj(out HObject ho_fstthresh);
            HOperatorSet.GenEmptyObj(out HObject ho_connobj);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation);
            HOperatorSet.GenEmptyObj(out HObject ho_contours);

            try
            {
                HOperatorSet.FastThreshold(ho_image, out ho_threshregion, 128, 255, 10);
                HOperatorSet.Boundary(ho_threshregion, out ho_boundregion, "inner");
                HOperatorSet.GenContourRegionXld(ho_boundregion, out ho_edges, "border");
                HOperatorSet.SelectContoursXld(ho_edges, out ho_selectedge, "contour_length", 500, 99999, -0.5, 0.5);
                HOperatorSet.LengthXld(ho_selectedge, out HTuple hv_length);
                HOperatorSet.TupleFind(hv_length, hv_length.TupleMax(), out HTuple hv_xldmaxindex);
                HOperatorSet.SelectObj(ho_selectedge, out ho_maxxld, hv_xldmaxindex);
                HOperatorSet.FitRectangle2ContourXld(ho_maxxld, "tukey", -1, 0, 0, 3, 2, out hv_row,
                    out hv_col, out hv_phi, out hv_len1, out hv_len2, out HTuple hv_pointorder);
                if (hv_len1 != null)
                {
                    if (hv_len1.Length == 1)
                    {
                        row = hv_row;
                        col = hv_col;
                        len1 = hv_len1;
                        len2 = hv_len2;
                        phi = hv_phi;
                    }
                    else
                    {
                        HOperatorSet.TupleFind(hv_len1, hv_len1.TupleMax(), out HTuple hv_maxindex);
                        row = hv_row[hv_maxindex];
                        col = hv_col[hv_maxindex];
                        len1 = hv_len1[hv_maxindex];
                        len2 = hv_len2[hv_maxindex];
                        phi = hv_phi[hv_maxindex];
                    }
                    width = len1 * 2;
                    height = len2 * 2;
                    HOperatorSet.GenRectangle2(out ho_pingbiregion, row, col, phi, len1 - 25, len2 - 35);
                    row = row + (retract.rb_y - retract.lt_y) / 2;
                    col = col + (retract.rb_x - retract.lt_x) / 2;
                    len1 = len1 - (retract.rb_x + retract.lt_x) / 2;
                    if (phi <= Math.PI / 4 && phi > -Math.PI / 4)
                    {
                        len1 = len1 - (retract.rb_x + retract.lt_x) / 2;
                        len2 = len2 - (retract.rb_y + retract.lt_y) / 2;
                    }
                    else
                    {
                        len1 = len1 - (retract.rb_y + retract.lt_y) / 2;
                        len2 = len2 - (retract.rb_x + retract.lt_x) / 2;
                    }
                    result = -997;
                    HOperatorSet.GenRectangle2(out ho_rectangle, row, col, phi, len1, len2);
                    HOperatorSet.Difference(ho_rectangle, ho_pingbiregion, out ho_diffregion);
                    HOperatorSet.ReduceDomain(ho_image, ho_diffregion, out ho_imgdomain);
                    HOperatorSet.Threshold(ho_imgdomain, out ho_fstthresh, 0, 50);
                    result = -996;
                    if (ho_fstthresh != null)
                    {
                        HOperatorSet.Connection(ho_fstthresh, out ho_connobj);
                        HOperatorSet.AreaCenter(ho_connobj, out hv_areas, out HTuple hv_rows, out HTuple hv_cols);
                        HOperatorSet.DilationCircle(ho_connobj, out ho_regionDilation, 3.5);
                        HOperatorSet.GenContourRegionXld(ho_regionDilation, out ho_contours, "border");
                        result = -995;
                        areas = new double[hv_areas.Length];
                        areas = hv_areas;
                        result = -994;
                        HOperatorSet.CountObj(ho_contours, out HTuple hv_num);
                        contours = new contour[hv_num];
                        result = -993;
                        for (int i = 0; i < hv_num; i++)
                        {
                            HOperatorSet.GenEmptyObj(out HObject ho_indexContour);
                            HOperatorSet.SelectObj(ho_contours, out ho_indexContour, i + 1);
                            result = -992;
                            HOperatorSet.GetContourXld(ho_indexContour, out HTuple hv_conRows, out HTuple hv_conCols);
                            contours[i].rows = new double[hv_conRows.Length];
                            contours[i].rows = hv_conRows;
                            contours[i].cols = new double[hv_conCols.Length];
                            contours[i].cols = hv_conCols;
                            ho_indexContour.Dispose();
                        }
                    }

                }
                result = 0;
            }
            catch (Exception)
            {

            }
            finally
            {
                ho_boundregion.Dispose();
                ho_pingbiregion.Dispose();
                ho_threshregion.Dispose();
                ho_edges.Dispose();
                ho_maxxld.Dispose();
                ho_selectedge.Dispose();
                ho_rectangle.Dispose();
                ho_diffregion.Dispose();
                ho_connobj.Dispose();
                ho_contours.Dispose();
                ho_fstthresh.Dispose();
                ho_imgdomain.Dispose();
                ho_regionDilation.Dispose();
            }
            
            return result;
        }

        public int FitCircle(HObject ho_image, thresh thresh, out circle circle)
        {
            int result = -999;
            circle = new circle(0, 0, 0, 0, 0);
            double row, col, radius, startphi, endphi;
            HOperatorSet.GenEmptyObj(out HObject ho_threshregion);
            HOperatorSet.GenEmptyObj(out HObject ho_boundregion);
            HOperatorSet.GenEmptyObj(out HObject ho_edges);
            HOperatorSet.GenEmptyObj(out HObject ho_selectedge);
            try
            {
                if (thresh._minGray == 0 && thresh._maxGray == 0)
                {
                    HOperatorSet.GetImageSize(ho_image, out HTuple hv_imgwidth, out HTuple hv_imgheight);
                    HOperatorSet.GenRectangle1(out HObject ho_grayrect, 0, 0, hv_imgheight, hv_imgwidth);
                    HOperatorSet.GrayHistoRange(ho_grayrect, ho_image, 0, 255, 256, out HTuple hv_histo, out HTuple hv_binsize);
                    HOperatorSet.TupleReplace(hv_histo, 0, 0, out HTuple hv_replaced);
                    ho_grayrect.Dispose();
                    int countGrayMax = hv_replaced.TupleMax();
                    int grayMax = hv_replaced.TupleFind(countGrayMax);
                    thresh._maxGray = grayMax + 60;
                    if (thresh._maxGray > 255) thresh._maxGray = 255;
                    thresh._minGray = grayMax - 60;
                    if (thresh._minGray < 10) thresh._minGray = 10;
                }
                HOperatorSet.FastThreshold(ho_image, out ho_threshregion, thresh._minGray, thresh._maxGray, 10);
                HOperatorSet.Boundary(ho_threshregion, out ho_boundregion, "inner");
                result = -998;
                HOperatorSet.GenContourRegionXld(ho_boundregion, out ho_edges, "border");
                HOperatorSet.SelectContoursXld(ho_edges, out ho_selectedge, "contour_length", 500, 99999, -0.5, 0.5);
                HOperatorSet.FitCircleContourXld(ho_selectedge, "algebraic", -1, 0, 0, 3, 2, out HTuple hv_row, out HTuple hv_col,
                    out HTuple hv_radiusk, out HTuple hv_startPhi, out HTuple hv_endPhi, out HTuple hv_pointOrder);
                result = -990;
                if (hv_radiusk != null)
                {
                    HOperatorSet.TupleFind(hv_radiusk, hv_radiusk.TupleMax(), out HTuple hv_maxindex);
                    row = hv_row[hv_maxindex];
                    col = hv_col[hv_maxindex];
                    radius = hv_radiusk[hv_maxindex];
                    startphi = hv_startPhi[hv_maxindex];
                    endphi = hv_endPhi[hv_maxindex];
                    circle = new circle(col, row, radius, startphi, endphi);
                    result = 0;
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                ho_boundregion.Dispose();
                ho_edges.Dispose();
                ho_selectedge.Dispose();
                ho_threshregion.Dispose();
            }
            return result;
        }

        public int FitEllipse(HObject ho_image, thresh thresh, out ellipse ellipse)
        {
            int result = -999;
            ellipse = new ellipse(0, 0, 0, 0, 0);
            double row, col, phi, radius1, radius2, startphi, endphi;
            HOperatorSet.GenEmptyObj(out HObject ho_threshregion);
            HOperatorSet.GenEmptyObj(out HObject ho_boundregion);
            HOperatorSet.GenEmptyObj(out HObject ho_edges);
            HOperatorSet.GenEmptyObj(out HObject ho_selectedge);
            try
            {
                if (thresh._minGray == 0 && thresh._maxGray == 0)
                {
                    HOperatorSet.GetImageSize(ho_image, out HTuple hv_imgwidth, out HTuple hv_imgheight);
                    HOperatorSet.GenRectangle1(out HObject ho_grayrect, 0, 0, hv_imgheight, hv_imgwidth);
                    HOperatorSet.GrayHistoRange(ho_grayrect, ho_image, 0, 255, 256, out HTuple hv_histo, out HTuple hv_binsize);
                    HOperatorSet.TupleReplace(hv_histo, 0, 0, out HTuple hv_replaced);
                    ho_grayrect.Dispose();
                    int countGrayMax = hv_replaced.TupleMax();
                    int grayMax = hv_replaced.TupleFind(countGrayMax);
                    thresh._maxGray = grayMax + 60;
                    if (thresh._maxGray > 255) thresh._maxGray = 255;
                    thresh._minGray = grayMax - 60;
                    if (thresh._minGray < 10) thresh._minGray = 10;
                }
                HOperatorSet.FastThreshold(ho_image, out ho_threshregion, thresh._minGray, thresh._maxGray, 10);
                HOperatorSet.Boundary(ho_threshregion, out ho_boundregion, "inner");
                result = -998;
                HOperatorSet.GenContourRegionXld(ho_boundregion, out ho_edges, "border");
                HOperatorSet.SelectContoursXld(ho_edges, out ho_selectedge, "contour_length", 500, 99999, -0.5, 0.5);
                HOperatorSet.FitEllipseContourXld(ho_selectedge, "fitzgibbon", -1, 0, 0, 200, 3, 2, out HTuple hv_row, out HTuple hv_col,
                    out HTuple hv_phi, out HTuple hv_radius1, out HTuple hv_radius2, out HTuple hv_startPhi, out HTuple hv_endPhi, out HTuple hv_pointOrder);
                result = -990;
                if (hv_radius1 != null)
                {
                    HOperatorSet.TupleFind(hv_radius1, hv_radius1.TupleMax(), out HTuple hv_maxindex);
                    row = hv_row[hv_maxindex];
                    col = hv_col[hv_maxindex];
                    phi = hv_phi[hv_maxindex];
                    radius1 = hv_radius1[hv_maxindex];
                    radius2 = hv_radius2[hv_maxindex];
                    startphi = hv_startPhi[hv_maxindex];
                    endphi = hv_endPhi[hv_maxindex];
                    ellipse = new ellipse(col, row, phi, radius1, radius2);
                    result = 0;
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                ho_boundregion.Dispose();
                ho_edges.Dispose();
                ho_selectedge.Dispose();
                ho_threshregion.Dispose();
            }
            return result;
        }
        public int FitRectangle(HObject ho_image, thresh thresh, rect retract, out rect2 rect)
        {
            int result = -999;
            rect = new rect2(0, 0, 0, 0, 0);
            double row, col, len1, len2, phi;
            HTuple hv_row, hv_col, hv_len1, hv_len2, hv_phi;
            HOperatorSet.GenEmptyObj(out HObject ho_threshregion);
            HOperatorSet.GenEmptyObj(out HObject ho_boundregion);
            HOperatorSet.GenEmptyObj(out HObject ho_edges);
            HOperatorSet.GenEmptyObj(out HObject ho_selectedge);
            HOperatorSet.GenEmptyObj(out HObject ho_maxxld);
            try
            {
                if (thresh._minGray == 0 && thresh._maxGray == 0)
                {
                    HOperatorSet.GetImageSize(ho_image, out HTuple hv_imgwidth, out HTuple hv_imgheight);
                    HOperatorSet.GenRectangle1(out HObject ho_grayrect, 0, 0, hv_imgheight, hv_imgwidth);
                    HOperatorSet.GrayHistoRange(ho_grayrect, ho_image, 0, 255, 256, out HTuple hv_histo, out HTuple hv_binsize);
                    HOperatorSet.TupleReplace(hv_histo, 0, 0, out HTuple hv_replaced);
                    ho_grayrect.Dispose();
                    int countGrayMax = hv_replaced.TupleMax();
                    int grayMax = hv_replaced.TupleFind(countGrayMax);
                    thresh._maxGray = grayMax + 60;
                    if (thresh._maxGray > 255) thresh._maxGray = 255;
                    thresh._minGray = grayMax - 60;
                    if (thresh._minGray < 10) thresh._minGray = 10;
                }
                HOperatorSet.FastThreshold(ho_image, out ho_threshregion, thresh._minGray, thresh._maxGray, 10);
                HOperatorSet.Boundary(ho_threshregion, out ho_boundregion, "inner");
                result = -998;
                HOperatorSet.GenContourRegionXld(ho_boundregion, out ho_edges, "border");
                HOperatorSet.SelectContoursXld(ho_edges, out ho_selectedge, "contour_length", 500, 99999, -0.5, 0.5);
                HOperatorSet.LengthXld(ho_selectedge, out HTuple hv_length);
                HOperatorSet.TupleFind(hv_length, hv_length.TupleMax(), out HTuple hv_xldmaxindex);
                HOperatorSet.SelectObj(ho_selectedge, out ho_maxxld, hv_xldmaxindex + 1);
                HOperatorSet.FitRectangle2ContourXld(ho_maxxld, "tukey", -1, 0, 0, 3, 2, out hv_row,
                    out hv_col, out hv_phi, out hv_len1, out hv_len2, out HTuple hv_pointorder);
                result = -997;
                if (hv_len1 != null)
                {
                    HOperatorSet.TupleFind(hv_len1, hv_len1.TupleMax(), out HTuple hv_maxindex);
                    row = hv_row[hv_maxindex];
                    col = hv_col[hv_maxindex];
                    len1 = hv_len1[hv_maxindex];
                    len2 = hv_len2[hv_maxindex];
                    phi = hv_phi[hv_maxindex];
                    row = row + (retract.rb_y - retract.lt_y) / 2;
                    col = col + (retract.rb_x - retract.lt_x) / 2;
                    if (phi <= Math.PI / 4 && phi > -Math.PI / 4)
                    {
                        len1 = len1 - (retract.rb_x + retract.lt_x) / 2;
                        len2 = len2 - (retract.rb_y + retract.lt_y) / 2;
                    }
                    else
                    {
                        len1 = len1 - (retract.rb_y + retract.lt_y) / 2;
                        len2 = len2 - (retract.rb_x + retract.lt_x) / 2;
                    }
                    rect = new rect2(row, col, len1, len2, phi);
                    result = 0;
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                ho_maxxld.Dispose();
                ho_boundregion.Dispose();
                ho_edges.Dispose();
                ho_selectedge.Dispose();
                ho_threshregion.Dispose();
            }
            return result;
        }

        //滁州3线六面检测极柱面检测专用，屏蔽区域写在程序中了
        public int CountObjecgs(HObject ho_image, rect2 ROI, rect retract, out contour checkroi, out double[] areas, out contour[] contours)
        {
            int result = -999;
            checkroi = new contour();
            areas = new double[0];
            contours = new contour[0];

            double row, col, phi, len1, len2;
            row = ROI._row;
            col = ROI._col;
            phi = ROI._phi;
            len1 = ROI._len1;
            len2 = ROI._len2;
            HTuple hv_areas;
            HOperatorSet.GenEmptyObj(out HObject ho_pingbiregion);
            HOperatorSet.GenEmptyObj(out HObject ho_rectangle);
            HOperatorSet.GenEmptyObj(out HObject ho_diffregion);
            HOperatorSet.GenEmptyObj(out HObject ho_imgdomain);
            HOperatorSet.GenEmptyObj(out HObject ho_fstthresh);
            HOperatorSet.GenEmptyObj(out HObject ho_connobj);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation);
            HOperatorSet.GenEmptyObj(out HObject ho_contours);
            HOperatorSet.GenEmptyObj(out HObject ho_regionErosion);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation1);
            HOperatorSet.GenEmptyObj(out HObject ho_contour);

            try
            {
                HOperatorSet.GenEllipse(out ho_pingbiregion, row + 1, col, phi, 90, 20);
                //HOperatorSet.GenRectangle2(out ho_pingbiregion, row, col, phi, len1 - 25, len2 - 35);
                row = row + (retract.rb_y - retract.lt_y) / 2;
                col = col + (retract.rb_x - retract.lt_x) / 2;
                if (phi <= Math.PI / 4 && phi > -Math.PI / 4)
                {
                    len1 = len1 - (retract.rb_x + retract.lt_x) / 2;
                    len2 = len2 - (retract.rb_y + retract.lt_y) / 2;
                }
                else
                {
                    len1 = len1 - (retract.rb_y + retract.lt_y) / 2;
                    len2 = len2 - (retract.rb_x + retract.lt_x) / 2;
                }

                result = -997;
                HOperatorSet.GenRectangle2(out ho_rectangle, row, col, phi, len1, len2);
                HOperatorSet.ErosionCircle(ho_rectangle, out ho_regionErosion, 15.5);
                HOperatorSet.DilationCircle(ho_regionErosion, out ho_regionDilation1, 15.5);
                HOperatorSet.GenContourRegionXld(ho_regionDilation1, out ho_contour, "border");
                HOperatorSet.GetContourXld(ho_contour, out HTuple hv_outcontourrow, out HTuple hv_outcontourcol);
                checkroi.rows = new double[hv_outcontourrow.Length];
                checkroi.rows = hv_outcontourrow;
                checkroi.cols = new double[hv_outcontourcol.Length];
                checkroi.cols = hv_outcontourcol;
                HOperatorSet.Difference(ho_regionDilation1, ho_pingbiregion, out ho_diffregion);
                HOperatorSet.ReduceDomain(ho_image, ho_diffregion, out ho_imgdomain);
                HOperatorSet.Threshold(ho_imgdomain, out ho_fstthresh, 0, 50);
                result = 0;
                if (ho_fstthresh != null)
                {
                    HOperatorSet.Connection(ho_fstthresh, out ho_connobj);
                    HOperatorSet.AreaCenter(ho_connobj, out hv_areas, out HTuple hv_rows, out HTuple hv_cols);
                    HOperatorSet.DilationCircle(ho_connobj, out ho_regionDilation, 3.5);
                    HOperatorSet.GenContourRegionXld(ho_regionDilation, out ho_contours, "border");
                    result = -995;
                    areas = new double[hv_areas.Length];
                    areas = hv_areas;
                    result = -994;
                    HOperatorSet.CountObj(ho_contours, out HTuple hv_num);
                    contours = new contour[hv_num];
                    result = -993;
                    for (int i = 0; i < hv_num; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_indexContour);
                        HOperatorSet.SelectObj(ho_contours, out ho_indexContour, i + 1);
                        result = -992;
                        HOperatorSet.GetContourXld(ho_indexContour, out HTuple hv_conRows, out HTuple hv_conCols);
                        contours[i].rows = new double[hv_conRows.Length];
                        contours[i].rows = hv_conRows;
                        contours[i].cols = new double[hv_conCols.Length];
                        contours[i].cols = hv_conCols;
                        ho_indexContour.Dispose();
                    }
                    result = 0;
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                ho_contour.Dispose();
                ho_connobj.Dispose();
                ho_contours.Dispose();
                ho_diffregion.Dispose();
                ho_fstthresh.Dispose();
                ho_imgdomain.Dispose();
                ho_pingbiregion.Dispose();
                ho_rectangle.Dispose();
                ho_regionDilation.Dispose();
                ho_regionErosion.Dispose();
                ho_regionDilation1.Dispose();
            }

            return result;
        }

        //检测矩形区域内缺陷，屏蔽区域为矩形2
        public int CountObjecgs(HObject ho_image, rect2 ROI, rect retract, rect2[] disROIs, out contour checkroi, out double[] areas, out contour[] contours, double rounded)
        {
            int result = -999;
            checkroi = new contour();
            areas = new double[0];
            contours = new contour[0];

            double row, col, phi, len1, len2;
            row = ROI._row;
            col = ROI._col;
            phi = ROI._phi;
            len1 = ROI._len1;
            len2 = ROI._len2;
            HTuple hv_areas;
            HOperatorSet.GenEmptyObj(out HObject ho_pingbiregion);
            HOperatorSet.GenEmptyObj(out HObject ho_rectangle);
            HOperatorSet.GenEmptyObj(out HObject ho_diffregion);
            HOperatorSet.GenEmptyObj(out HObject ho_imgdomain);
            HOperatorSet.GenEmptyObj(out HObject ho_fstthresh);
            HOperatorSet.GenEmptyObj(out HObject ho_connobj);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation);
            HOperatorSet.GenEmptyObj(out HObject ho_contours);
            HOperatorSet.GenEmptyObj(out HObject ho_regionErosion);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation1);
            HOperatorSet.GenEmptyObj(out HObject ho_contour);

            try
            {
                if (disROIs.Length > 0)
                {
                    for (int i = 0; i < disROIs.Length; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_disroi);
                        HOperatorSet.GenRectangle2(out ho_disroi, disROIs[i]._row, disROIs[i]._col, disROIs[i]._phi, disROIs[i]._len1, disROIs[i]._len2);
                        HOperatorSet.ConcatObj(ho_pingbiregion, ho_disroi, out ho_pingbiregion);
                        ho_disroi.Dispose();
                    }
                }

                row = row + (retract.rb_y - retract.lt_y) / 2;
                col = col + (retract.rb_x - retract.lt_x) / 2;
                if (phi <= Math.PI / 4 && phi > -Math.PI / 4)
                {
                    len1 = len1 - (retract.rb_x + retract.lt_x) / 2;
                    len2 = len2 - (retract.rb_y + retract.lt_y) / 2;
                }
                else
                {
                    len1 = len1 - (retract.rb_y + retract.lt_y) / 2;
                    len2 = len2 - (retract.rb_x + retract.lt_x) / 2;
                }
                result = -997;
                HOperatorSet.GenRectangle2(out ho_rectangle, row, col, phi, len1, len2);
                HOperatorSet.ErosionCircle(ho_rectangle, out ho_regionErosion, rounded);
                HOperatorSet.DilationCircle(ho_regionErosion, out ho_regionDilation1, rounded);
                HOperatorSet.GenContourRegionXld(ho_regionDilation1, out ho_contour, "border");
                HOperatorSet.GetContourXld(ho_contour, out HTuple hv_outcontourrow, out HTuple hv_outcontourcol);
                checkroi.rows = new double[hv_outcontourrow.Length];
                checkroi.rows = hv_outcontourrow;
                checkroi.cols = new double[hv_outcontourcol.Length];
                checkroi.cols = hv_outcontourcol;
                if (ho_pingbiregion != null)
                {
                    HOperatorSet.Difference(ho_regionDilation1, ho_pingbiregion, out ho_diffregion);
                    HOperatorSet.ReduceDomain(ho_image, ho_diffregion, out ho_imgdomain);
                }
                else
                {
                    HOperatorSet.ReduceDomain(ho_image, ho_regionDilation1, out ho_imgdomain);
                }
                HOperatorSet.Threshold(ho_imgdomain, out ho_fstthresh, 0, 50);
                result = 0;
                if (ho_fstthresh != null)
                {
                    HOperatorSet.Connection(ho_fstthresh, out ho_connobj);
                    HOperatorSet.AreaCenter(ho_connobj, out hv_areas, out HTuple hv_rows, out HTuple hv_cols);
                    HOperatorSet.DilationCircle(ho_connobj, out ho_regionDilation, 3.5);
                    HOperatorSet.GenContourRegionXld(ho_regionDilation, out ho_contours, "border");
                    result = -995;
                    areas = new double[hv_areas.Length];
                    areas = hv_areas;
                    result = -994;
                    HOperatorSet.CountObj(ho_contours, out HTuple hv_num);
                    contours = new contour[hv_num];
                    result = -993;
                    for (int i = 0; i < hv_num; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_indexContour);
                        HOperatorSet.SelectObj(ho_contours, out ho_indexContour, i + 1);
                        result = -992;
                        HOperatorSet.GetContourXld(ho_indexContour, out HTuple hv_conRows, out HTuple hv_conCols);
                        contours[i].rows = new double[hv_conRows.Length];
                        contours[i].rows = hv_conRows;
                        contours[i].cols = new double[hv_conCols.Length];
                        contours[i].cols = hv_conCols;
                        ho_indexContour.Dispose();
                    }
                    result = 0;
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                ho_contour.Dispose();
                ho_connobj.Dispose();
                ho_contours.Dispose();
                ho_diffregion.Dispose();
                ho_fstthresh.Dispose();
                ho_imgdomain.Dispose();
                ho_pingbiregion.Dispose();
                ho_rectangle.Dispose();
                ho_regionDilation.Dispose();
                ho_regionErosion.Dispose();
                ho_regionDilation1.Dispose();
            }

            return result;
        }

        //检测矩形区域内缺陷，屏蔽区域为椭圆
        public int CountObjecgs(HObject ho_image, rect2 ROI, rect retract, ellipse[] disROIs, out contour checkroi, out double[] areas, out contour[] contours, double rounded)
        {
            int result = -999;
            checkroi = new contour();
            areas = new double[0];
            contours = new contour[0];

            double row, col, phi, len1, len2;
            row = ROI._row;
            col = ROI._col;
            phi = ROI._phi;
            len1 = ROI._len1;
            len2 = ROI._len2;
            HTuple hv_areas;
            HOperatorSet.GenEmptyObj(out HObject ho_pingbiregion);
            HOperatorSet.GenEmptyObj(out HObject ho_rectangle);
            HOperatorSet.GenEmptyObj(out HObject ho_diffregion);
            HOperatorSet.GenEmptyObj(out HObject ho_imgdomain);
            HOperatorSet.GenEmptyObj(out HObject ho_fstthresh);
            HOperatorSet.GenEmptyObj(out HObject ho_connobj);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation);
            HOperatorSet.GenEmptyObj(out HObject ho_contours);
            HOperatorSet.GenEmptyObj(out HObject ho_regionErosion);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation1);
            HOperatorSet.GenEmptyObj(out HObject ho_contour);

            try
            {
                if (disROIs.Length > 0)
                {
                    for (int i = 0; i < disROIs.Length; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_disroi);
                        HOperatorSet.GenEllipse(out ho_disroi, disROIs[i]._y, disROIs[i]._x, disROIs[i]._phi, disROIs[i]._radius1, disROIs[i]._radius2);
                        HOperatorSet.ConcatObj(ho_pingbiregion, ho_disroi, out ho_pingbiregion);
                        ho_disroi.Dispose();
                    }
                }

                row = row + (retract.rb_y - retract.lt_y) / 2;
                col = col + (retract.rb_x - retract.lt_x) / 2;
                if (phi <= Math.PI / 4 && phi > -Math.PI / 4)
                {
                    len1 = len1 - (retract.rb_x + retract.lt_x) / 2;
                    len2 = len2 - (retract.rb_y + retract.lt_y) / 2;
                }
                else
                {
                    len1 = len1 - (retract.rb_y + retract.lt_y) / 2;
                    len2 = len2 - (retract.rb_x + retract.lt_x) / 2;
                }
                result = -997;
                HOperatorSet.GenRectangle2(out ho_rectangle, row, col, phi, len1, len2);
                HOperatorSet.ErosionCircle(ho_rectangle, out ho_regionErosion, rounded);
                HOperatorSet.DilationCircle(ho_regionErosion, out ho_regionDilation1, rounded);
                HOperatorSet.GenContourRegionXld(ho_regionDilation1, out ho_contour, "border");
                HOperatorSet.GetContourXld(ho_contour, out HTuple hv_outcontourrow, out HTuple hv_outcontourcol);
                checkroi.rows = new double[hv_outcontourrow.Length];
                checkroi.rows = hv_outcontourrow;
                checkroi.cols = new double[hv_outcontourcol.Length];
                checkroi.cols = hv_outcontourcol;
                if (ho_pingbiregion != null)
                {
                    HOperatorSet.Difference(ho_regionDilation1, ho_pingbiregion, out ho_diffregion);
                    HOperatorSet.ReduceDomain(ho_image, ho_diffregion, out ho_imgdomain);
                }
                else
                {
                    HOperatorSet.ReduceDomain(ho_image, ho_regionDilation1, out ho_imgdomain);
                }
                HOperatorSet.Threshold(ho_imgdomain, out ho_fstthresh, 0, 50);
                result = 0;
                if (ho_fstthresh != null)
                {
                    HOperatorSet.Connection(ho_fstthresh, out ho_connobj);
                    HOperatorSet.AreaCenter(ho_connobj, out hv_areas, out HTuple hv_rows, out HTuple hv_cols);
                    HOperatorSet.DilationCircle(ho_connobj, out ho_regionDilation, 3.5);
                    HOperatorSet.GenContourRegionXld(ho_regionDilation, out ho_contours, "border");
                    result = -995;
                    areas = new double[hv_areas.Length];
                    areas = hv_areas;
                    result = -994;
                    HOperatorSet.CountObj(ho_contours, out HTuple hv_num);
                    contours = new contour[hv_num];
                    result = -993;
                    for (int i = 0; i < hv_num; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_indexContour);
                        HOperatorSet.SelectObj(ho_contours, out ho_indexContour, i + 1);
                        result = -992;
                        HOperatorSet.GetContourXld(ho_indexContour, out HTuple hv_conRows, out HTuple hv_conCols);
                        contours[i].rows = new double[hv_conRows.Length];
                        contours[i].rows = hv_conRows;
                        contours[i].cols = new double[hv_conCols.Length];
                        contours[i].cols = hv_conCols;
                        ho_indexContour.Dispose();
                    }
                    result = 0;
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                ho_contour.Dispose();
                ho_connobj.Dispose();
                ho_contours.Dispose();
                ho_diffregion.Dispose();
                ho_fstthresh.Dispose();
                ho_imgdomain.Dispose();
                ho_pingbiregion.Dispose();
                ho_rectangle.Dispose();
                ho_regionDilation.Dispose();
                ho_regionErosion.Dispose();
                ho_regionDilation1.Dispose();
            }

            return result;
        }

        //检测矩形区域内缺陷，屏蔽区域为矩形+椭圆
        public int CountObjecgs(HObject ho_image, rect2 ROI, rect retract, rect2[] disRect, ellipse[] disEllipse, out contour checkroi, out double[] areas, out contour[] contours, double rounded)
        {
            int result = -999;
            checkroi = new contour();
            areas = new double[0];
            contours = new contour[0];

            double row, col, phi, len1, len2;
            row = ROI._row;
            col = ROI._col;
            phi = ROI._phi;
            len1 = ROI._len1;
            len2 = ROI._len2;
            HTuple hv_areas;
            HOperatorSet.GenEmptyObj(out HObject ho_pingbiregion);
            HOperatorSet.GenEmptyObj(out HObject ho_rectangle);
            HOperatorSet.GenEmptyObj(out HObject ho_diffregion);
            HOperatorSet.GenEmptyObj(out HObject ho_imgdomain);
            HOperatorSet.GenEmptyObj(out HObject ho_fstthresh);
            HOperatorSet.GenEmptyObj(out HObject ho_connobj);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation);
            HOperatorSet.GenEmptyObj(out HObject ho_contours);
            HOperatorSet.GenEmptyObj(out HObject ho_regionErosion);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation1);
            HOperatorSet.GenEmptyObj(out HObject ho_contour);

            try
            {
                if (disRect.Length > 0)
                {
                    for (int i = 0; i < disRect.Length; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_disrect);
                        HOperatorSet.GenRectangle2(out ho_disrect, disRect[i]._row, disRect[i]._col, disRect[i]._phi, disRect[i]._len1, disRect[i]._len2);
                        HOperatorSet.ConcatObj(ho_pingbiregion, ho_disrect, out ho_pingbiregion);
                        ho_disrect.Dispose();
                    }
                }
                if (disEllipse.Length > 0)
                {
                    for (int i = 0; i < disEllipse.Length; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_disell);
                        HOperatorSet.GenEllipse(out ho_disell, disEllipse[i]._y, disEllipse[i]._x, disEllipse[i]._phi, disEllipse[i]._radius1, disEllipse[i]._radius2);
                        HOperatorSet.ConcatObj(ho_pingbiregion, ho_disell, out ho_pingbiregion);
                        ho_disell.Dispose();
                    }
                }

                row = row + (retract.rb_y - retract.lt_y) / 2;
                col = col + (retract.rb_x - retract.lt_x) / 2;
                if (phi <= Math.PI / 4 && phi > -Math.PI / 4)
                {
                    len1 = len1 - (retract.rb_x + retract.lt_x) / 2;
                    len2 = len2 - (retract.rb_y + retract.lt_y) / 2;
                }
                else
                {
                    len1 = len1 - (retract.rb_y + retract.lt_y) / 2;
                    len2 = len2 - (retract.rb_x + retract.lt_x) / 2;
                }
                result = -997;
                HOperatorSet.GenRectangle2(out ho_rectangle, row, col, phi, len1, len2);
                HOperatorSet.ErosionCircle(ho_rectangle, out ho_regionErosion, rounded);
                HOperatorSet.DilationCircle(ho_regionErosion, out ho_regionDilation1, rounded);
                HOperatorSet.GenContourRegionXld(ho_regionDilation1, out ho_contour, "border");
                HOperatorSet.GetContourXld(ho_contour, out HTuple hv_outcontourrow, out HTuple hv_outcontourcol);
                checkroi.rows = new double[hv_outcontourrow.Length];
                checkroi.rows = hv_outcontourrow;
                checkroi.cols = new double[hv_outcontourcol.Length];
                checkroi.cols = hv_outcontourcol;
                if (ho_pingbiregion != null)
                {
                    HOperatorSet.Difference(ho_regionDilation1, ho_pingbiregion, out ho_diffregion);
                    HOperatorSet.ReduceDomain(ho_image, ho_diffregion, out ho_imgdomain);
                }
                else
                {
                    HOperatorSet.ReduceDomain(ho_image, ho_regionDilation1, out ho_imgdomain);
                }
                HOperatorSet.Threshold(ho_imgdomain, out ho_fstthresh, 0, 50);
                result = 0;
                if (ho_fstthresh != null)
                {
                    HOperatorSet.Connection(ho_fstthresh, out ho_connobj);
                    HOperatorSet.AreaCenter(ho_connobj, out hv_areas, out HTuple hv_rows, out HTuple hv_cols);
                    HOperatorSet.DilationCircle(ho_connobj, out ho_regionDilation, 3.5);
                    HOperatorSet.GenContourRegionXld(ho_regionDilation, out ho_contours, "border");
                    result = -995;
                    areas = new double[hv_areas.Length];
                    areas = hv_areas;
                    result = -994;
                    HOperatorSet.CountObj(ho_contours, out HTuple hv_num);
                    contours = new contour[hv_num];
                    result = -993;
                    for (int i = 0; i < hv_num; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_indexContour);
                        HOperatorSet.SelectObj(ho_contours, out ho_indexContour, i + 1);
                        result = -992;
                        HOperatorSet.GetContourXld(ho_indexContour, out HTuple hv_conRows, out HTuple hv_conCols);
                        contours[i].rows = new double[hv_conRows.Length];
                        contours[i].rows = hv_conRows;
                        contours[i].cols = new double[hv_conCols.Length];
                        contours[i].cols = hv_conCols;
                        ho_indexContour.Dispose();
                    }
                    result = 0;
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                ho_contour.Dispose();
                ho_connobj.Dispose();
                ho_contours.Dispose();
                ho_diffregion.Dispose();
                ho_fstthresh.Dispose();
                ho_imgdomain.Dispose();
                ho_pingbiregion.Dispose();
                ho_rectangle.Dispose();
                ho_regionDilation.Dispose();
                ho_regionErosion.Dispose();
                ho_regionDilation1.Dispose();
            }

            return result;
        }

        //检测椭圆区域内缺陷，屏蔽区域为矩形+椭圆，缩进为半径
        public int CountObjecgs(HObject ho_image, ellipse ROI, double retract, rect2[] disRect, ellipse[] disEllipse, out contour checkroi, out double[] areas, out contour[] contours)
        {
            int result = -999;
            checkroi = new contour();
            areas = new double[0];
            contours = new contour[0];

            double x, y, phi, radius1, radius2;
            x = ROI._x;
            y = ROI._y;
            phi = ROI._phi;
            radius1 = ROI._radius1;
            radius2 = ROI._radius2;
            HTuple hv_areas;
            HOperatorSet.GenEmptyObj(out HObject ho_pingbiregion);
            HOperatorSet.GenEmptyObj(out HObject ho_ellipse);
            HOperatorSet.GenEmptyObj(out HObject ho_diffregion);
            HOperatorSet.GenEmptyObj(out HObject ho_imgdomain);
            HOperatorSet.GenEmptyObj(out HObject ho_fstthresh);
            HOperatorSet.GenEmptyObj(out HObject ho_connobj);
            HOperatorSet.GenEmptyObj(out HObject ho_regionDilation);
            HOperatorSet.GenEmptyObj(out HObject ho_contours);
            HOperatorSet.GenEmptyObj(out HObject ho_regionErosion);

            HOperatorSet.GenEmptyObj(out HObject ho_contour);

            try
            {
                if (disRect.Length > 0)
                {
                    for (int i = 0; i < disRect.Length; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_disrect);
                        HOperatorSet.GenRectangle2(out ho_disrect, disRect[i]._row, disRect[i]._col, disRect[i]._phi, disRect[i]._len1, disRect[i]._len2);
                        HOperatorSet.ConcatObj(ho_pingbiregion, ho_disrect, out ho_pingbiregion);
                        ho_disrect.Dispose();
                    }
                }
                if (disEllipse.Length > 0)
                {
                    for (int i = 0; i < disEllipse.Length; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_disell);
                        HOperatorSet.GenEllipse(out ho_disell, disEllipse[i]._y, disEllipse[i]._x, disEllipse[i]._phi, disEllipse[i]._radius1, disEllipse[i]._radius2);
                        HOperatorSet.ConcatObj(ho_pingbiregion, ho_disell, out ho_pingbiregion);
                        ho_disell.Dispose();
                    }
                }

                result = -997;
                HOperatorSet.GenEllipse(out ho_ellipse, y, x, phi, radius1, radius2);
                HOperatorSet.ErosionCircle(ho_ellipse, out ho_regionErosion, retract);
                HOperatorSet.GenContourRegionXld(ho_regionErosion, out ho_contour, "border");
                HOperatorSet.GetContourXld(ho_contour, out HTuple hv_outcontourrow, out HTuple hv_outcontourcol);
                checkroi.rows = new double[hv_outcontourrow.Length];
                checkroi.rows = hv_outcontourrow;
                checkroi.cols = new double[hv_outcontourcol.Length];
                checkroi.cols = hv_outcontourcol;
                if (ho_pingbiregion != null)
                {
                    HOperatorSet.Difference(ho_regionErosion, ho_pingbiregion, out ho_diffregion);
                    HOperatorSet.ReduceDomain(ho_image, ho_diffregion, out ho_imgdomain);
                }
                else
                {
                    HOperatorSet.ReduceDomain(ho_image, ho_regionErosion, out ho_imgdomain);
                }
                HOperatorSet.Threshold(ho_imgdomain, out ho_fstthresh, 0, 50);
                result = 0;
                if (ho_fstthresh != null)
                {
                    HOperatorSet.Connection(ho_fstthresh, out ho_connobj);
                    HOperatorSet.AreaCenter(ho_connobj, out hv_areas, out HTuple hv_rows, out HTuple hv_cols);
                    HOperatorSet.DilationCircle(ho_connobj, out ho_regionDilation, 3.5);
                    HOperatorSet.GenContourRegionXld(ho_regionDilation, out ho_contours, "border");
                    result = -995;
                    areas = new double[hv_areas.Length];
                    areas = hv_areas;
                    result = -994;
                    HOperatorSet.CountObj(ho_contours, out HTuple hv_num);
                    contours = new contour[hv_num];
                    result = -993;
                    for (int i = 0; i < hv_num; i++)
                    {
                        HOperatorSet.GenEmptyObj(out HObject ho_indexContour);
                        HOperatorSet.SelectObj(ho_contours, out ho_indexContour, i + 1);
                        result = -992;
                        HOperatorSet.GetContourXld(ho_indexContour, out HTuple hv_conRows, out HTuple hv_conCols);
                        contours[i].rows = new double[hv_conRows.Length];
                        contours[i].rows = hv_conRows;
                        contours[i].cols = new double[hv_conCols.Length];
                        contours[i].cols = hv_conCols;
                        ho_indexContour.Dispose();
                    }
                    result = 0;
                }
            }
            catch (Exception ex )
            {
                throw (ex);
            }
            finally
            {
                ho_contour.Dispose();
                ho_connobj.Dispose();
                ho_contours.Dispose();
                ho_diffregion.Dispose();
                ho_fstthresh.Dispose();
                ho_imgdomain.Dispose();
                ho_pingbiregion.Dispose();
                ho_ellipse.Dispose();
                ho_regionDilation.Dispose();
                ho_regionErosion.Dispose();
            }
            return result;
        }

        public int Fitplane(double[] x, double[] y, double[] z, out PlaneParam planeParam)
        {
            planeParam = new PlaneParam();
            int result = -999;
            HTuple hv_objectModel3D = new HTuple();

            string[] paraName = { "primitive_type", "fitting_algorithm" };
            string[] paraVal = { "plane", "least_squares_tukey" };
            try
            {
                HOperatorSet.GenObjectModel3dFromPoints(x, y, z, out hv_objectModel3D);
                HOperatorSet.FitPrimitivesObjectModel3d(hv_objectModel3D, paraName, paraVal, out HTuple hv_objectModel3DOut);
                result = -998;
                HOperatorSet.GetObjectModel3dParams(hv_objectModel3DOut, "primitive_parameter", out HTuple hv_genParamValue);
                result = -997;

                planeParam.A0 = hv_genParamValue.TupleSelect(0);
                planeParam.A1 = hv_genParamValue.TupleSelect(1);
                planeParam.A2 = hv_genParamValue.TupleSelect(2);
                planeParam.D = hv_genParamValue.TupleSelect(3);
                planeParam.Sprt = Math.Sqrt(planeParam.A1 * planeParam.A1 + planeParam.A2 * planeParam.A2 + planeParam.A0 * planeParam.A0);
                HOperatorSet.ClearObjectModel3d(hv_objectModel3D);
                hv_objectModel3D = null;
                result = 0;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                if (hv_objectModel3D != null)
                {
                    HOperatorSet.ClearObjectModel3d(hv_objectModel3D);
                }
            }
            return result;
        }

    }



    public struct PlaneParam
    {
        public double A0;
        public double A1;
        public double A2;
        public double D;
        public double Sprt;
    }
}
