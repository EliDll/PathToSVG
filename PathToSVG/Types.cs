using System.Numerics;

#nullable enable

namespace Types
{
    record PathPiece3D();

    record Line3D(Vector3 Start, Vector3 End) : PathPiece3D();

    record Arc3D(Vector3 Start, Vector3 End, Vector3 Center, Vector3 Axis, float SweepDeg) : PathPiece3D();

    record Path3D(IList<PathPiece3D> Pieces, float Diameter);

    enum View
    {
        Front,
        Side,
        Top
    }

    record CoordinateSystem(Vector3 AxisX, Vector3 AxisY, Vector3 AxisZ);

    record ImagePiece(IList<Vector3> ImageBounds);

    record ImageLine(Line3D Line3D, Vector3 ImageStart, Vector3 ImageEnd, IList<Vector3> ImageBounds, float StraightLen3D, float OuterLen3D) : ImagePiece(ImageBounds);

    record ImageArc(Arc3D Arc3D, Vector3 ImageCenter, IList<Vector3> ImageSweepSamples, IList<Vector3> ImageBounds) : ImagePiece(ImageBounds);

    record ImagePath(IList<ImagePiece> Pieces, float Diameter);

    record Margin(float Left, float Top, float Right, float Bottom);

    record Bounds(Vector3 Min, Vector3 Max, Vector3 Range, Vector3 Center);
}