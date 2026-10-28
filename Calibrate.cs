using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HalconDotNet;

namespace WOVE.Common
{
    public class Calibrate
    {
        
        public void GenHomMat(double[]px,double []py,double[]qx,double []qy,out HTuple HomMat)
        {
            HOperatorSet.VectorToHomMat2d(px, py, qx, qy, out  HomMat);
            this.HomMat = HomMat;
            HOperatorSet.HomMat2dToAffinePar(HomMat, out HTuple sx, out HTuple sy, out HTuple phi, out HTuple theta, out HTuple tx, out HTuple ty);
            this.x_axis_scale = sx;
            this.y_axis_scale = sy;

        }

        /// <summary>
        /// 像素坐标转实际轴坐标偏移值
        /// </summary>
        /// <param name="HomMat">仿射变换矩阵</param>
        /// <param name="x">像素X坐标</param>
        /// <param name="y">像素Y坐标</param>
        /// <param name="ox">像素参考原点坐标X</param>
        /// <param name="oy">像素参考原点坐标Y</param>
        /// <param name="off_x">实际轴坐标X偏移值</param>
        /// <param name="off_y">实际轴坐标Y偏移值</param>
        public void PixToReal(HTuple HomMat, double []x,double[] y,double ox,double oy,out double[]off_x,out double[] off_y)
        {
            off_x = new double[0];
            off_y = new double[0];
            if (HomMat == null) return;
            HOperatorSet.AffineTransPoint2d(HomMat, x, y, out HTuple qx, out HTuple qy);
            HOperatorSet.AffineTransPoint2d(HomMat, ox, oy, out HTuple oqx, out HTuple oqy);
            off_x = oqx-qx;
            off_y = oqy-qy;
        }

        /// <summary>
        /// 实际轴坐标偏移值转图像像素坐标
        /// </summary>
        /// <param name="HomMat">仿射变换矩阵</param>
        /// <param name="qx">实际X轴偏移值</param>
        /// <param name="qy">实际Y轴偏移值</param>
        /// <param name="ox">图像参考原点X</param>
        /// <param name="oy">图像参考原点Y</param>
        /// <param name="px">转换后像素坐标X</param>
        /// <param name="py">转换后像素坐标Y</param>
        public void RealToPix(HTuple HomMat,double[]qx,double []qy, double ox, double oy,out double []px,out double[]py)
        {
            px = new double[0];
            py = new double[0];
            if (HomMat == null) return;
            HOperatorSet.HomMat2dInvert(HomMat,out HTuple homMat2DInvert);
            HOperatorSet.AffineTransPoint2d(HomMat, ox, oy, out HTuple real_cx,out HTuple real_cy);
            HTuple real_x = real_cx-qx;
            HTuple real_y = real_cy-qy;
            HOperatorSet.AffineTransPoint2d(homMat2DInvert, real_x, real_y, out HTuple hv_px, out HTuple hv_py);
            px = hv_px;
            py = hv_py;
        }

        /// <summary>
        /// 轴与轴之间的坐标轴转换
        /// </summary>
        /// <param name="HomMat">仿射变换矩阵</param>
        /// <param name="sx">原坐标系X坐标</param>
        /// <param name="sy">原坐标系Y坐标</param>
        /// <param name="nx">新坐标系X坐标</param>
        /// <param name="ny">新坐标系Y坐标</param>
        public void PointTrans(HTuple HomMat, double[] sx, double[] sy, out double[] nx, out double[] ny)
        {
            nx = new double[0];
            ny = new double[0];
            if (HomMat == null) return;
            HOperatorSet.AffineTransPoint2d(HomMat, sx, sy, out HTuple qx, out HTuple qy);
            nx = qx;
            ny = qy;
        }

        /// <summary>
        /// 轴与轴的坐标逆向转换
        /// </summary>
        /// <param name="HomMat">仿射变换矩阵</param>
        /// <param name="nx">新坐标系X轴坐标</param>
        /// <param name="ny">新坐标系Y轴坐标</param>
        /// <param name="sx">原坐标系X轴坐标</param>
        /// <param name="sy">原坐标系Y轴坐标</param>
        public void InvertTrans(HTuple HomMat, double[] nx, double[] ny, out double[] sx, out double[] sy)
        {
            sx = new double[0];
            sy = new double[0];
            if (HomMat == null) return;
            HOperatorSet.HomMat2dInvert(HomMat, out HTuple homMat2DInvert);
            HOperatorSet.AffineTransPoint2d(homMat2DInvert, nx, ny, out HTuple hv_sx, out HTuple hv_sy);
            sx = hv_sx;
            sy = hv_sy;
        }

        /// <summary>
        /// 单轴标定，取两个点计算比例
        /// </summary>
        /// <param name="startPoint">图像上起始点坐标</param>
        /// <param name="endPoint">图像上终点坐标</param>
        /// <param name="axisStart">运动轴起点坐标</param>
        /// <param name="axisEnd">运动轴终点坐标</param>
        /// <param name="scale">像素精度</param>
        public void SingleAxisTrans(Point startPoint,Point endPoint,double axisStart,double axisEnd,out double scale)
        {
            double diff_x = endPoint.x- startPoint.x ;
            double diff_y =  endPoint.y- startPoint.y ;
            double axis_diff = axisEnd - axisStart;
            if (Math.Abs(diff_x)>Math.Abs(diff_y))
            {
                scale = axis_diff / diff_x;
                this.x_axis_scale = scale;
            }
            else
            {
                scale = axis_diff / diff_y;
                this.y_axis_scale = scale;
            }
            
        }
        public HTuple HomMat
        {
            get;set;
        }

        public double x_axis_scale
        {
            get;set;
        }

        public double y_axis_scale
        {
            get; set;
        }
    }
}
