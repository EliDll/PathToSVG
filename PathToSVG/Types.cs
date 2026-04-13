using System.Numerics;

#nullable enable

namespace PathToSVG
{
    record PathPiece3D(Vector3 Start, Vector3 End, bool IsSelected, bool IsDependent);

    record Line3D(Vector3 Start, Vector3 End, bool IsSelected = false, bool IsDependent = false) : PathPiece3D(Start, End, IsSelected, IsDependent);

    record Arc3D(Vector3 Start, Vector3 End, Vector3 Center, Vector3 Axis, float SweepDeg, bool IsSelected = false, bool IsDependent = false) : PathPiece3D(Start, End, IsSelected, IsDependent);

    record Path3D(IList<PathPiece3D> Pieces, float Diameter);

    enum View
    {
        Front,
        Side,
        Top
    }

    enum Anchor
    {
        FirstLine,
        LongestLine,
        LongestCollinearGroup
    }

    enum OverlapHandling
    {
        Shift3D,
        Ignore
    }

    record CoordinateSystem(Vector3 AxisX, Vector3 AxisY, Vector3 AxisZ);

    record ImagePiece();

    record ImageLine(Line3D Line3D, Vector3 ImageStart, Vector3 ImageEnd, IList<Vector3> ImageStartBounds, IList<Vector3> ImageEndBounds, float StraightLen3D, float OuterLen3D) : ImagePiece();

    record ImageArc(Arc3D Arc3D, Vector3 ImageCenter, IList<Vector3> ImageSweepSamples, IList<Vector3> ImageSampleBounds) : ImagePiece();

    record ImagePath(IList<ImagePiece> Pieces, float Diameter);

    record Margin(float Left, float Top, float Right, float Bottom);

    record Bounds(Vector3 Min, Vector3 Max, Vector3 Range, Vector3 Center);
}