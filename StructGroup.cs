using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WOVE
{
    //将通用的结构体全部写在此处
     class StructGroup
    {
    }

    public struct rect
    {
        public rect(double x1, double y1, double x2, double y2)
        {
            lt_x = x1;
            lt_y = y1;
            rb_x = x2;
            rb_y = y2;
        }
        public double lt_x;
        public double lt_y;
        public double rb_x;
        public double rb_y;
    }
    public struct rect2
    {
        public rect2(double row, double col, double len1, double len2, double phi)
        {
            _row = row;
            _col = col;
            _len1 = len1;
            _len2 = len2;
            _phi = phi;
        }
        public double _row;
        public double _col;
        public double _len1;
        public double _len2;
        public double _phi;
    }

    public struct contour
    {
        public double[] rows;
        public double[] cols;
    }

    public struct circle
    {
        public circle(double x, double y, double r, double startPhi, double endPhi)
        {
            _x = x;
            _y = y;
            _r = r;
            _startPhi = startPhi;
            _endPhi = endPhi;
        }
        public double _x;
        public double _y;
        public double _r;
        public double _startPhi;
        public double _endPhi;
    }

    public struct ellipse
    {
        public ellipse(double x, double y, double phi, double radius1, double radius2)
        {
            _x = x;
            _y = y;
            _phi = phi;
            _radius1 = radius1;
            _radius2 = radius2;
        }
        public double _x;
        public double _y;
        public double _phi;
        public double _radius1;
        public double _radius2;
    }

    public struct thresh
    {
        public thresh(double minGray, double maxGray)
        {
            _minGray = minGray;
            _maxGray = maxGray;
        }
        public double _minGray;
        public double _maxGray;
    }

    public struct Point
    {
        public double x;
        public double y;
        public Point(double _x, double _y)
        {
            x = _x;
            y = _y;
        }
    }

    public struct Curve
    {
        public Point[] points;
    }
}
