using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessLib.D3
{
    public class ExtraDll
    {
       

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern void MemCopy(IntPtr dest, IntPtr src, int count);//字节数组到字节数组的拷贝



        public enum findFlaw_method_type
        {
            SSZN_Plane = 0,         //基于平面拟合方法。
            SSZN_Curve_2 = 1,        //基于二次方程拟合方法。
            SSZN_Segment_Curve = 2,   //基于分割曲面拟合方法。
            SSZN_MeanFilter = 3,    //基于均值滤波拟合方法。
        };
        public struct Head
        {
            public int row;
            public int col;
            public double _xInterval;
            public double _yInterval;
        }
        public struct Circle2D
        {
            public DPoint2D _center; //圆心
            public double _radius; //半径
        };
        public struct DPoint2D
        {
            public double _x;
            public double _y;
        };
        public struct DRect
        {
            public double _leftX; //左下角X坐标
            public double _leftY; //左下角Y坐标
            public double _width; //矩形宽度
            public double _height; //矩形高度
        };
        public struct Result
        {
            public double area;
            public double lenth;
            public double width;
            public double maxZ;
            public double minZ;
            public double x;
            public double y;
            public double rectWidth;
            public double rectLenth;
        };
        public struct RATIO_Plane
        {
            public double r0;
            public double r1;
            public double r2;
            public double distB;    //used in distance caculating
        };
    }
}
