using System.Windows;
using System.Windows.Media;
using HakoMark.Core;

namespace HakoMark.Drawing;

public static class CurvedArrow
{
    // Local X follows the drag. Rotating this geometry keeps its tip exactly
    // under the pointer for every direction, including a backwards drag.
    public static Geometry Create(ArrowAnnotation arrow)
    {
        var dx=arrow.End.X-arrow.Start.X; var dy=arrow.End.Y-arrow.Start.Y;
        var length=Math.Sqrt(dx*dx+dy*dy);
        if(length<1)return Geometry.Empty;
        // Trace of the supplied reference, not a bent straight-arrow ribbon.
        // Preserve its proportions with a similarity transform (no stretching).
        const double rx=327, ry=-218;
        var referenceLength=Math.Sqrt(rx*rx+ry*ry);
        Point P(double x,double y) {
            var px=x-162; var py=y-482;
            return new Point((px*rx+py*ry)*length/(referenceLength*referenceLength),
                (-px*ry+py*rx)*length/(referenceLength*referenceLength));
        }
        var geometry=new StreamGeometry();
        using(var c=geometry.Open())
        {
            c.BeginFigure(P(162,482),true,true);
            c.BezierTo(P(135.9,332.8),P(243.2,208.9),P(352,213),true,true);
            c.LineTo(P(352,168),true,true);
            c.BezierTo(P(352,161),P(357,159),P(362,163),true,true);
            c.LineTo(P(486,258),true,true);
            c.BezierTo(P(488,260),P(489,262),P(489,264),true,true);
            c.BezierTo(P(489,266),P(488,268),P(486,270),true,true);
            c.LineTo(P(362,367),true,true);
            c.BezierTo(P(357,371),P(352,368),P(352,362),true,true);
            c.LineTo(P(352,316),true,true);
            c.BezierTo(P(267,316),P(188.8,381),P(165,480),true,true);
            c.BezierTo(P(164,485),P(162,486),P(162,482),true,true);
        }
        geometry.Transform=new MatrixTransform(dx/length,dy/length,-dy/length,dx/length,arrow.Start.X,arrow.Start.Y);
        geometry.Freeze(); return geometry;
    }
}
