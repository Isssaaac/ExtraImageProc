using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HalconDotNet;

namespace WOVE.BeadInspection
{
    public class Bead
    {        
        public bool ApplyBeadInspection(HObject ho_image, BeadParam beadParam, Point[] points, out Curve[] leftbase,
           out Curve[] rightbase, out Curve[] leftcurve, out Curve[] rightcurve, out Curve[] errorcurve, out Curve[] errintersc, out string[] errtype)
        {
            HObject ho_beadcontour;
            HObject ho_paralleContours1, ho_paralleContours2;
            HObject ho_leftcontour, ho_rightcontour;
            HObject ho_errorsegment, ho_intersc;
           
            HTuple beadmodel = null; HTuple errorType = null;
            HTuple rows = null; HTuple cols = null; HTuple weight = null;
            HTuple paramname = null; HTuple paramvalue = null;
            HTuple hv_targetThickness = null; HTuple hv_thicknessTolerance = null; HTuple hv_positionTolerance = null; HTuple hv_polarity = null;
            HTuple hv_width = null;

            beadmodel = new HTuple(); errorType = new HTuple();
            rows = new HTuple(); cols = new HTuple(); weight = new HTuple();
            paramname = new HTuple(); paramvalue = new HTuple();
            hv_targetThickness = new HTuple(); hv_thicknessTolerance = new HTuple(); hv_positionTolerance = new HTuple(); hv_polarity = new HTuple();
            hv_width = new HTuple();

            paramname[0] = "sigma";
            paramname[1] = "threshold";
            paramvalue[0] = beadParam.sigma;
            paramvalue[1] = beadParam.threshold;

            hv_targetThickness = beadParam.targetThickness;
            hv_thicknessTolerance = beadParam.thicknessTolerance;
            hv_positionTolerance = beadParam.positionTolerance;
            hv_polarity = beadParam.polarity;
            hv_width = beadParam.targetThickness + 30;

            leftbase = new Curve[0];
            rightbase = new Curve[0];
            leftcurve = new Curve[0];
            rightcurve = new Curve[0];
            errorcurve = new Curve[0];
            errintersc = new Curve[0];
            errtype = new string[0];

            bool result = false;

            HOperatorSet.GenEmptyObj(out ho_beadcontour);
            HOperatorSet.GenEmptyObj(out ho_paralleContours1);
            HOperatorSet.GenEmptyObj(out ho_paralleContours2);
            HOperatorSet.GenEmptyObj(out ho_leftcontour);
            HOperatorSet.GenEmptyObj(out ho_rightcontour);
            HOperatorSet.GenEmptyObj(out ho_errorsegment);
            HOperatorSet.GenEmptyObj(out ho_intersc);

            for (int i = 0; i < points.Length; i++)
            {
                rows[i] = points[i].y;
                cols[i] = points[i].x;
                weight[i] = 15;
            }
            try
            {
                ho_beadcontour.Dispose();
                HOperatorSet.GenContourNurbsXld(out ho_beadcontour, rows, cols, "auto", weight, 3, 1, 5);

                HOperatorSet.CreateBeadInspectionModel(ho_beadcontour, hv_targetThickness, hv_thicknessTolerance, hv_positionTolerance
                    , hv_polarity, paramname, paramvalue, out beadmodel);
                ho_paralleContours1.Dispose();
                ho_paralleContours2.Dispose();
                HOperatorSet.GenParallelContourXld(ho_beadcontour, out ho_paralleContours1, "regression_normal", hv_targetThickness * 0.5);
                HOperatorSet.GenParallelContourXld(ho_beadcontour, out ho_paralleContours2, "regression_normal", hv_targetThickness * -0.5);
                ho_beadcontour.Dispose();
                GetContourPoint(ho_paralleContours1, ref leftbase);
                GetContourPoint(ho_paralleContours2, ref rightbase);
                ho_paralleContours1.Dispose();
                ho_paralleContours2.Dispose();

                ho_leftcontour.Dispose();
                ho_rightcontour.Dispose();
                ho_errorsegment.Dispose();
                HOperatorSet.ApplyBeadInspectionModel(ho_image, out ho_leftcontour,
                    out ho_rightcontour, out ho_errorsegment, beadmodel, out errorType);
                HOperatorSet.ClearBeadInspectionModel(beadmodel);
                beadmodel.UnpinTuple();

                if (errorType.Length > 0)
                {
                    errtype = errorType.SArr;
                    ho_intersc.Dispose();
                    GenDisplaySegments(ho_errorsegment, ho_leftcontour, ho_rightcontour, hv_width, out ho_intersc);
                    GetContourPoint(ho_errorsegment, ref errorcurve);
                    GetContourPoint(ho_intersc, ref errintersc);
                    errorType.UnpinTuple();
                }
                GetContourPoint(ho_leftcontour, ref leftcurve);
                GetContourPoint(ho_rightcontour, ref rightcurve);

                result = true;
            }
            catch
            {
                result = false;
            }
            finally
            {
                ho_beadcontour.Dispose();
                ho_errorsegment.Dispose();
                ho_intersc.Dispose();
                ho_leftcontour.Dispose();
                ho_paralleContours1.Dispose();
                ho_paralleContours2.Dispose();
                ho_rightcontour.Dispose();
                ho_image.Dispose();
            }
            return result;
        }

        public bool ApplyBeadInspection(HObject ho_image, BeadParam beadParam, Point[] points,
             out HObject ho_leftbase, out HObject ho_rightbase, out HObject ho_leftcurve, out HObject ho_rightcurve,
             out HObject ho_errcurve, out HObject ho_errinter, out HTuple errorType)
        {
            bool result = false;

            HObject ho_beadcontour;

            HTuple beadmodel = null; errorType = null;
            HTuple rows = null; HTuple cols = null; HTuple weight = null;
            HTuple paramname = null; HTuple paramvalue = null;
            HTuple hv_targetThickness = null; HTuple hv_thicknessTolerance = null; HTuple hv_positionTolerance = null; HTuple hv_polarity = null;
            HTuple hv_width = null;

            beadmodel = new HTuple(); errorType = new HTuple();
            rows = new HTuple(); cols = new HTuple(); weight = new HTuple();
            paramname = new HTuple(); paramvalue = new HTuple();
            hv_targetThickness = new HTuple(); hv_thicknessTolerance = new HTuple(); hv_positionTolerance = new HTuple(); hv_polarity = new HTuple();
            hv_width = new HTuple();

            paramname[0] = "sigma";
            paramname[1] = "threshold";
            paramvalue[0] = beadParam.sigma;
            paramvalue[1] = beadParam.threshold;

            hv_targetThickness = beadParam.targetThickness;
            hv_thicknessTolerance = beadParam.thicknessTolerance;
            hv_positionTolerance = beadParam.positionTolerance;
            hv_polarity = beadParam.polarity;
            hv_width = beadParam.targetThickness + 30;

            HOperatorSet.GenEmptyObj(out ho_beadcontour);
            HOperatorSet.GenEmptyObj(out ho_leftbase);
            HOperatorSet.GenEmptyObj(out ho_rightbase);
            HOperatorSet.GenEmptyObj(out ho_leftcurve);
            HOperatorSet.GenEmptyObj(out ho_rightcurve);
            HOperatorSet.GenEmptyObj(out ho_errcurve);
            HOperatorSet.GenEmptyObj(out ho_errinter);

            for (int i = 0; i < points.Length; i++)
            {
                rows[i] = points[i].y;
                cols[i] = points[i].x;
                weight[i] = 15;
            }
            try
            {
                ho_beadcontour.Dispose();
                HOperatorSet.GenContourNurbsXld(out ho_beadcontour, rows, cols, "auto", weight, 3, 1, 5);

                HOperatorSet.CreateBeadInspectionModel(ho_beadcontour, hv_targetThickness, hv_thicknessTolerance, hv_positionTolerance
                    , hv_polarity, paramname, paramvalue, out beadmodel);
                ho_leftbase.Dispose();
                ho_rightbase.Dispose();
                HOperatorSet.GenParallelContourXld(ho_beadcontour, out ho_leftbase, "regression_normal", hv_targetThickness * 0.5);
                HOperatorSet.GenParallelContourXld(ho_beadcontour, out ho_rightbase, "regression_normal", hv_targetThickness * -0.5);
                ho_beadcontour.Dispose();

                ho_leftcurve.Dispose();
                ho_rightcurve.Dispose();
                ho_errcurve.Dispose();
                HOperatorSet.ApplyBeadInspectionModel(ho_image, out ho_leftcurve,
                    out ho_rightcurve, out ho_errcurve, beadmodel, out errorType);
                HOperatorSet.ClearBeadInspectionModel(beadmodel);
                beadmodel.UnpinTuple();
                if (errorType.Length > 0)
                {
                    ho_errinter.Dispose();
                    GenDisplaySegments(ho_errcurve, ho_leftcurve, ho_rightcurve, hv_width, out ho_errinter);
                    errorType.UnpinTuple();
                }

                result = true;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        public bool ApplyBeadInspection(HObject ho_image, BeadParam beadParam, Point[] points, out string str)
        {
            bool result = false;
            str = "";
            HObject ho_beadcontour;

            HTuple beadmodel = null, errorType = null;
            HTuple rows = null; HTuple cols = null; HTuple weight = null;
            HTuple paramname = null; HTuple paramvalue = null;
            HTuple hv_targetThickness = null; HTuple hv_thicknessTolerance = null; HTuple hv_positionTolerance = null; HTuple hv_polarity = null;
            HTuple hv_width = null;

            beadmodel = new HTuple();
            rows = new HTuple(); cols = new HTuple(); weight = new HTuple();
            paramname = new HTuple(); paramvalue = new HTuple();
            hv_targetThickness = new HTuple(); hv_thicknessTolerance = new HTuple(); hv_positionTolerance = new HTuple(); hv_polarity = new HTuple();
            hv_width = new HTuple(); errorType = new HTuple();

            paramname[0] = "sigma";
            paramname[1] = "threshold";
            paramvalue[0] = beadParam.sigma;
            paramvalue[1] = beadParam.threshold;

            hv_targetThickness = beadParam.targetThickness;
            hv_thicknessTolerance = beadParam.thicknessTolerance;
            hv_positionTolerance = beadParam.positionTolerance;
            hv_polarity = beadParam.polarity;
            hv_width = beadParam.targetThickness + 30;

            HObject ho_leftcurve;
            HObject ho_rightcurve;
            HObject ho_errcurve;
            HObject ho_errinter;
            HOperatorSet.GenEmptyObj(out ho_beadcontour);
            HOperatorSet.GenEmptyObj(out ho_leftcurve);
            HOperatorSet.GenEmptyObj(out ho_rightcurve);
            HOperatorSet.GenEmptyObj(out ho_errcurve);
            HOperatorSet.GenEmptyObj(out ho_errinter);
            for (int i = 0; i < points.Length; i++)
            {
                rows[i] = points[i].y;
                cols[i] = points[i].x;
                weight[i] = 15;
            }
            try
            {
                ho_beadcontour.Dispose();
                HOperatorSet.GenContourNurbsXld(out ho_beadcontour, rows, cols, "auto", weight, 3, 1, 5);

                HOperatorSet.CreateBeadInspectionModel(ho_beadcontour, hv_targetThickness, hv_thicknessTolerance, hv_positionTolerance
                    , hv_polarity, paramname, paramvalue, out beadmodel);

                // HOperatorSet.GenParallelContourXld(ho_beadcontour, out ho_leftbase, "regression_normal", hv_targetThickness * 0.5);
                //HOperatorSet.GenParallelContourXld(ho_beadcontour, out ho_rightbase, "regression_normal", hv_targetThickness * -0.5);
                ho_beadcontour.Dispose();

                ho_leftcurve.Dispose();
                ho_rightcurve.Dispose();
                ho_errcurve.Dispose();
                HOperatorSet.ApplyBeadInspectionModel(ho_image, out ho_leftcurve,
                    out ho_rightcurve, out ho_errcurve, beadmodel, out errorType);
                HOperatorSet.ClearBeadInspectionModel(beadmodel);
                str = Distance(ho_leftcurve, ho_rightcurve);
                beadmodel.UnpinTuple();
                if (errorType.Length > 0)
                {
                    ho_errinter.Dispose();
                    //GenDisplaySegments(ho_errcurve, ho_leftcurve, ho_rightcurve, hv_width, out ho_errinter);
                    str = str + ";" + ErrorCurve(ho_errcurve, errorType, beadParam.targetThickness);

                    errorType.UnpinTuple();
                }
                result = true;
            }
            catch
            {
                result = false;
            }
            finally
            {
                ho_image.Dispose();
                ho_leftcurve.Dispose();
                ho_rightcurve.Dispose();
                ho_errcurve.Dispose();
                ho_errinter.Dispose();
            }

            return result;
        }

        public string ErrorCurve(HObject ho_errcurve, HTuple errorType, double beadwidth)
        {
            string str = "";
            HTuple errrow = new HTuple(), errcol = new HTuple();
            HTuple lefttop_x = new HTuple(), lefttop_y = new HTuple();
            HTuple rightbottom_x = new HTuple(), rightbottom_y = new HTuple();
            HTuple hv_InterNum = new HTuple();
            HObject ho_selectEc, ho_selectEcInter;
            HOperatorSet.GenEmptyObj(out ho_selectEc);
            //HOperatorSet.GenEmptyObj(out ho_selectEcInter);
            int NobeadLenght = 0;
            string sss = "";
            for (int i = 0; i < errorType.Length; i++)
            {
                ho_selectEc.Dispose();
                HOperatorSet.SelectObj(ho_errcurve, out ho_selectEc, i + 1);
                HOperatorSet.GetContourXld(ho_selectEc, out errrow, out errcol);
                ho_selectEc.Dispose();
                HOperatorSet.TupleMin(errcol, out lefttop_x);
                HOperatorSet.TupleMin(errrow, out lefttop_y);
                HOperatorSet.TupleMax(errcol, out rightbottom_x);
                HOperatorSet.TupleMax(errrow, out rightbottom_y);
                if (errorType[i] == "no bead")
                {
                    NobeadLenght = errrow.Length;
                }
                //ERROR1:断胶长度,ROI
                str = str + sss + "ERROR:" + string.Format("{0},{1},{2},{3},{4}", NobeadLenght.ToString(),
                    ((double)lefttop_x - beadwidth).ToString("f2"), ((double)lefttop_y - beadwidth).ToString("f2"),
                    ((double)rightbottom_x + beadwidth).ToString("f2"),
                    ((double)rightbottom_y + beadwidth).ToString("f2"));
                sss = ";";
            }

            //HOperatorSet.CountObj(ho_errinter, out hv_InterNum);
            //for (int i = 0; i < hv_InterNum; i++)
            //{
            //    ho_selectEcInter.Dispose();
            //    HOperatorSet.SelectObj(ho_errinter, out ho_selectEcInter, i + 1);
            //    HOperatorSet.GetContourXld(ho_selectEcInter, out interrow, out intercol);
            //    ho_selectEcInter.Dispose();
            //    HOperatorSet.TupleConcat(hv_ErrRecRow, interrow,out hv_ErrRecRow);
            //    HOperatorSet.TupleConcat(hv_ErrRecCol, intercol, out hv_ErrRecCol);
            //    //double[] rows = interrow.DArr;
            //    //double[] cols = intercol.DArr;
            //    //str =str+ ";" + string.Join(",", rows.Select(p =>
            //    //Math.Round(p, 2, MidpointRounding.AwayFromZero))) +";"+ string.Join(";", cols.Select(p =>
            //    //Math.Round(p, 2, MidpointRounding.AwayFromZero)));
            //}
            //string.Format("{0},{1},{2},{3}", ((double)hv_ErrRecCol.TupleMin()).ToString("f2"),
            //    ((double)hv_ErrRecRow.TupleMin()).ToString("f2"), ((double)hv_ErrRecCol.TupleMax()).ToString("f2"),
            //    ((double)hv_ErrRecRow.TupleMax()).ToString("f2"));
            return str;
        }

        public string Distance(HObject ho_leftcurve, HObject ho_rightcurve)
        {
            string str = "";
            HTuple hv_count = new HTuple();
            HTuple hv_rowL = new HTuple(), hv_colL = new HTuple();
            HTuple hv_rowR = new HTuple(), hv_colR = new HTuple();
            HTuple dist = new HTuple(), sqrt = new HTuple();
            HTuple hv_ROW = new HTuple(), hv_COL = new HTuple();
            HObject ho_selectL;
            HObject ho_selectR;
            HOperatorSet.GenEmptyObj(out ho_selectL);
            HOperatorSet.GenEmptyObj(out ho_selectR);
            HOperatorSet.CountObj(ho_leftcurve, out hv_count);
            for (int i = 0; i < hv_count; i++)
            {
                ho_selectL.Dispose();
                ho_selectR.Dispose();
                HOperatorSet.SelectObj(ho_leftcurve, out ho_selectL, i + 1);
                HOperatorSet.SelectObj(ho_rightcurve, out ho_selectR, i + 1);
                HOperatorSet.GetContourXld(ho_selectL, out hv_rowL, out hv_colL);
                HOperatorSet.GetContourXld(ho_selectR, out hv_rowR, out hv_colR);
                ho_selectL.Dispose();
                ho_selectR.Dispose();
                HOperatorSet.TupleSqrt(((hv_rowL - hv_rowR) * (hv_rowL - hv_rowR) +
                    (hv_colL - hv_colR) * (hv_colL - hv_colR)), out sqrt);
                HOperatorSet.TupleConcat(dist, sqrt, out dist);
                HOperatorSet.TupleConcat(hv_ROW, hv_rowL, out hv_ROW);
                HOperatorSet.TupleConcat(hv_ROW, hv_rowR, out hv_ROW);
                HOperatorSet.TupleConcat(hv_COL, hv_colL, out hv_COL);
                HOperatorSet.TupleConcat(hv_COL, hv_colR, out hv_COL);
            }
            str = "Result:" + string.Format("{0},{1},{2},{3},{4},{5}", ((double)dist.TupleMax()).ToString("f2"),
                ((double)dist.TupleMin()).ToString("f2"), ((double)hv_COL.TupleMin()).ToString("f2"),
                ((double)hv_ROW.TupleMin()).ToString("f2"), ((double)hv_COL.TupleMax()).ToString("f2"),
                ((double)hv_ROW.TupleMax()).ToString("f2"));

            return str;
        }

        private void GetContourPoint(HObject contour, ref Curve[] curves)
        {
            HTuple hv_num = new HTuple();
            HOperatorSet.CountObj(contour, out hv_num);
            curves = new Curve[hv_num];
            HTuple row = null; HTuple col = null;
            HTuple index = null;
            row = new HTuple(); col = new HTuple();
            index = new HTuple();
            HObject ho_empty;
            HOperatorSet.GenEmptyObj(out ho_empty);
            for (int i = 0; i < curves.Length; i++)
            {
                index = i + 1;
                ho_empty.Dispose();
                HOperatorSet.SelectObj(contour, out ho_empty, index);
                HOperatorSet.GetContourXld(ho_empty, out row, out col);
                ho_empty.Dispose();
                curves[i].points = new Point[row.Length];
                for (int j = 0; j < row.Length; j++)
                {
                    curves[i].points[j].x = col.TupleSelect(j);
                    curves[i].points[j].y = row.TupleSelect(j);
                }
            }

            ho_empty.Dispose();

        }

        private void GenDisplaySegments(HObject ho_TooThinSegments, HObject ho_LeftContour, HObject ho_RightContour, int width, out HObject ho_ContoursIntersection)
        {
            HObject ho_ParallelContours1, ho_ParallelContours2;
            HObject ho_Contour, ho_Contour1;

            HOperatorSet.GenEmptyObj(out ho_ContoursIntersection);
            HOperatorSet.GenEmptyObj(out ho_ParallelContours1);
            HOperatorSet.GenEmptyObj(out ho_ParallelContours2);
            HOperatorSet.GenEmptyObj(out ho_Contour);
            HOperatorSet.GenEmptyObj(out ho_Contour1);
            try
            {

                ho_ParallelContours1.Dispose();
                HOperatorSet.GenParallelContourXld(ho_TooThinSegments, out ho_ParallelContours1, "regression_normal", width);
                ho_ParallelContours2.Dispose();
                HOperatorSet.GenParallelContourXld(ho_TooThinSegments, out ho_ParallelContours2, "regression_normal", -width);
                ho_Contour.Dispose();
                CloseParallels(ho_ParallelContours1, ho_ParallelContours2, out ho_Contour);
                ho_Contour1.Dispose();
                CloseParallels(ho_LeftContour, ho_RightContour, out ho_Contour1);
                ho_ContoursIntersection.Dispose();
                HOperatorSet.IntersectionClosedContoursXld(ho_Contour, ho_Contour1, out ho_ContoursIntersection);
                ho_ParallelContours1.Dispose();
                ho_ParallelContours2.Dispose();
                ho_Contour.Dispose();
                ho_Contour1.Dispose();

                return;
            }
            catch
            {
                ho_ParallelContours1.Dispose();
                ho_ParallelContours2.Dispose();
                ho_Contour.Dispose();
                ho_Contour1.Dispose();
            }
        }

        private void CloseParallels(HObject ho_ParallelContours1, HObject ho_ParallelContours2, out HObject ho_Contour)
        {
            HObject ho_Parallel1 = null, ho_Parallel2 = null;
            HObject ho_ClosedContour = null;

            HTuple hv_Number = new HTuple(), hv_Index = new HTuple(), hv_Row1 = new HTuple();
            HTuple hv_Col1 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Col2 = new HTuple();

            HOperatorSet.GenEmptyObj(out ho_Contour);
            HOperatorSet.GenEmptyObj(out ho_Parallel1);
            HOperatorSet.GenEmptyObj(out ho_Parallel2);
            HOperatorSet.GenEmptyObj(out ho_ClosedContour);
            try
            {
                HOperatorSet.CountObj(ho_ParallelContours1, out hv_Number);
                ho_Contour.Dispose();
                HOperatorSet.GenEmptyObj(out ho_Contour);
                HTuple end_val2 = hv_Number;
                HTuple step_val2 = 1;
                for (hv_Index = 1; hv_Index.Continue(end_val2, step_val2); hv_Index = hv_Index.TupleAdd(step_val2))
                {
                    ho_Parallel1.Dispose();
                    HOperatorSet.SelectObj(ho_ParallelContours1, out ho_Parallel1, hv_Index);
                    ho_Parallel2.Dispose();
                    HOperatorSet.SelectObj(ho_ParallelContours2, out ho_Parallel2, hv_Index);
                    HOperatorSet.GetContourXld(ho_Parallel1, out hv_Row1, out hv_Col1);
                    HOperatorSet.GetContourXld(ho_Parallel2, out hv_Row2, out hv_Col2);
                    ho_Parallel1.Dispose();
                    ho_Parallel2.Dispose();
                    ho_ClosedContour.Dispose();
                    HOperatorSet.GenContourPolygonXld(out ho_ClosedContour, ((hv_Row1.TupleConcat(
                        hv_Row2.TupleInverse()))).TupleConcat(hv_Row1.TupleSelect(0)), ((hv_Col1.TupleConcat(
                        hv_Col2.TupleInverse()))).TupleConcat(hv_Col1.TupleSelect(0)));

                    HObject ExpTmpOutVar_0;
                    HOperatorSet.ConcatObj(ho_Contour, ho_ClosedContour, out ExpTmpOutVar_0);
                    ho_Contour.Dispose();
                    ho_Contour = ExpTmpOutVar_0;
                }

            }
            catch
            {

            }
            finally
            {
                ho_Parallel1.Dispose();
                ho_Parallel2.Dispose();
                ho_ClosedContour.Dispose();
            }
            return;

        }

    }
    public struct BeadParam
    {
        //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string polarity;
        public int targetThickness;
        public int thicknessTolerance;
        public int positionTolerance;
        public double sigma;
        public double threshold;
        public BeadParam(string _polarity, int _targetThickness, int _thicknessTolerance, int _positionTolerance, double _sigma, double _threshold)
        {
            polarity = _polarity;
            targetThickness = _targetThickness;
            thicknessTolerance = _thicknessTolerance;
            positionTolerance = _positionTolerance;
            sigma = _sigma;
            threshold = _threshold;
        }
    }

    public struct Result
    {
        public double width_max;
        public double width_min;
        public double lefttop_x;
        public double lefttop_y;
        public double rightbottom_x;
        public double rightbottom_y;
        public double nobeadlength;
    }
}
