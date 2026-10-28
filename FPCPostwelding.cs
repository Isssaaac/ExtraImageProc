using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HalconDotNet;

namespace WOVE.Inspection
{
    public class FPCPostwelding
    {
        /// <summary>
        /// 获取ROI范围内斑点面积
        /// </summary>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="imgptr">图像指针</param>
        /// <param name="_param">方法参数</param>
        /// <param name="_result">输出结果</param>
        /// <returns></returns>
        public bool GetCenter(int width, int height, UInt64 imgptr, GetCenterParam _param, out GetCenterResult _result)
        {
            bool result = false;
            HObject ho_image;
            HObject ho_rect;
            HObject ho_imagereduced;
            HObject ho_region;
            HObject ho_selectedregion;
            HObject ho_regionunion;
            HObject ho_connected;
            HOperatorSet.GenEmptyObj(out ho_image);
            HOperatorSet.GenEmptyObj(out ho_rect);
            HOperatorSet.GenEmptyObj(out ho_imagereduced);
            HOperatorSet.GenEmptyObj(out ho_region);
            HOperatorSet.GenEmptyObj(out ho_selectedregion);
            HOperatorSet.GenEmptyObj(out ho_regionunion);
            HOperatorSet.GenEmptyObj(out ho_connected);
            HTuple area = new HTuple(), row = new HTuple(), col = new HTuple();
            ho_image.Dispose();
            try
            {
                HOperatorSet.GenImage1(out ho_image, "byte", width, height, imgptr);
                ho_rect.Dispose();
                HOperatorSet.GenRectangle2(out ho_rect, _param.y, _param.x, _param.angle, _param.length1, _param.length2);
                ho_imagereduced.Dispose();
                HOperatorSet.ReduceDomain(ho_image, ho_rect, out ho_imagereduced);
                ho_region.Dispose();
                HOperatorSet.Threshold(ho_imagereduced, out ho_region, _param.minGray, _param.maxGray);
                ho_connected.Dispose();
                HOperatorSet.Connection(ho_region, out ho_connected);
                ho_selectedregion.Dispose();
                HOperatorSet.SelectShape(ho_connected, out ho_selectedregion, "area", "and", _param.minArea, _param.maxArea);
                ho_regionunion.Dispose();
                HOperatorSet.Union1(ho_selectedregion, out ho_regionunion);
                HOperatorSet.AreaCenter(ho_regionunion, out area, out row, out col);
                _result.area = area.D;
                _result.row = row.D;
                _result.col = col.D;
                result = true;
            }
            catch (Exception ex)
            {
                _result.area = 0;
                _result.row = 0;
                _result.col = 0;
                result = false;
                throw ex;
            }
            finally
            {
                ho_connected.Dispose();
                ho_image.Dispose();
                ho_imagereduced.Dispose();
                ho_rect.Dispose();
                ho_region.Dispose();
                ho_regionunion.Dispose();
                ho_selectedregion.Dispose();
            }

            return result;
        }
    }

    public struct GetCenterParam
    {
        public double x;
        public double y;
        public double length1;
        public double length2;
        public double angle;
        public double minGray;
        public double maxGray;
        public double minArea;
        public double maxArea;
    }

    public struct GetCenterResult
    {
        public double area;
        public double row;
        public double col;
    }
}
