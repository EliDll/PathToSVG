using System.Numerics;

#nullable enable

namespace Types
{
    record Segment();

    record Line(Vector3 Start, Vector3 End) : Segment();

    record Arc(Vector3 Start, Vector3 End, Vector3 Center, Vector3 Axis, float SweepDeg) : Segment();

    record Path3D(IList<Segment> segments);

    enum View
    {
       Front,
       Side,
       Top
    }
}