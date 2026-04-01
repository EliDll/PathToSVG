using SkiaSharp;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using Types;
using Utils;

#nullable enable

var len = 300;
var arcRadius = 30;
var pathWidth = 4;
var margin = pathWidth * 4;

var rightVec = new Vector3(1, 0, 0);
var leftVec = rightVec * -1;
var upVec = new Vector3(0, 0, 1);
var downVec = upVec * -1;
var inVec = new Vector3(0, 1, 0);
var outVec = inVec * -1;

Path3D path = new([
    new Line(new(0,0,0), new(len-arcRadius,0,0)),
    new Arc(Start: new(len-arcRadius,0,0), End: new(len, 0, arcRadius), Center: new(len-arcRadius, 0, arcRadius), Axis: inVec, SweepDeg: 90),
    new Line(new(len,0,arcRadius), new(len,0,len- arcRadius)),
    new Arc(Start: new(len,0,len- arcRadius), End: new(len, arcRadius, len), Center: new(len, arcRadius, len-arcRadius), Axis: rightVec, SweepDeg: 90),
    new Line(new(len,arcRadius,len), new(len,len-arcRadius,len)),
    new Arc(Start: new(len,len - arcRadius,len), End: new(len, len, len-arcRadius), Center: new(len, len-arcRadius, len-arcRadius), Axis: rightVec, SweepDeg: 470),
    new Line(new(len,len,len-arcRadius), new(len,len,arcRadius)),
    new Arc(Start: new(len,len, arcRadius), End: new(len+arcRadius, len, 0), Center: new(len+arcRadius, len, arcRadius), Axis: inVec, SweepDeg: 90),
    new Line(new(len+arcRadius, len, 0), new(len*2,len,0)),
]);


// Output path for the SVG file
string outputPath = "./output.svg";

// Define canvas size
float width = (float)len * 3 + margin * 2;
float height = (float)len * 3 + margin * 2;

// Create an SVG canvas using SKSvgCanvas
using (var stream = new SKFileWStream(outputPath))
using (var canvas = SKSvgCanvas.Create(new SKRect(0, 0, width, height), stream))
{
    using var bgPaint = new SKPaint
    {
        Color = new SKColor(30, 30, 60),
        Style = SKPaintStyle.Fill
    };
    canvas.DrawRect(0, 0, width, height, bgPaint);

    IList<Line> lines = path.segments.Where(x => x is Line).Select(x => (Line)x).ToList();

    var lineDirs = lines.Select(x => x.End - x.Start).ToList();
    var projectionXAxis = lineDirs.Count > 0 ? Vector3.Normalize(lineDirs[0]) : new Vector3(1, 0, 0); //world forward as fallback

    var eps = 1e-6f;

    //Orthonormal basis via Gram-Schmidt
    var projectionYAxisCandidates = lineDirs.Select(x => Vector3.Normalize(x - Vector3.Dot(x, projectionXAxis) * projectionXAxis))
        .Where(x => x.Length() > eps).ToList();

    var projectionYAxis = projectionYAxisCandidates.Count > 0 ? projectionYAxisCandidates[0] : new Vector3(0, 0, 1); //world up as fallback
    var projectionZAxis = Vector3.Cross(projectionYAxis, projectionXAxis); // no need to normalize, already unit length

    var view = View.Front;

    Vector3 Project(Vector3 p)
    {
        return view switch
        {
            View.Front => new Vector3(
            Vector3.Dot(p, projectionXAxis) + margin,
            height - Vector3.Dot(p, projectionYAxis) - margin,
            Vector3.Dot(p, projectionZAxis)
            ),
            View.Side => new Vector3(
            Vector3.Dot(p, projectionZAxis) + margin,
            height - Vector3.Dot(p, projectionYAxis) - margin,
            Vector3.Dot(p, projectionXAxis)
            ),
            View.Top => new Vector3(
            Vector3.Dot(p, projectionXAxis) + margin,
            height - Vector3.Dot(p, projectionZAxis) - margin,
            Vector3.Dot(p, projectionYAxis)
            ),
            _ => throw new NotImplementedException()
        };
    }

    using var red = new SKPaint
    {
        Color = new SKColor(255, 0, 0),
        StrokeWidth = pathWidth,
        IsAntialias = true
    };

    using var green = new SKPaint
    {
        Color = new SKColor(0, 255, 0),
        StrokeWidth = pathWidth,
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Round,
    };

    using var blue = new SKPaint
    {
        Color = new SKColor(0, 100, 255),
        StrokeWidth = pathWidth,
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Round,
    };

    using var backdropPaint = new SKPaint
    {
        Color = new SKColor(50, 50, 50),
        StrokeWidth = pathWidth,
        IsAntialias = true
    };

    using var angleFont = new SKFont
    {
        Size = pathWidth * 4
    };

    IList<Vector3> bounds = [];

    for (int segIdx = 0; segIdx < path.segments.Count; segIdx++)
    {
        var segment = path.segments[segIdx];
        if (segment is Line line)
        {
            var start = Project(line.Start);
            var end = Project(line.End);
            canvas.DrawLine(start.ToSKPoint(), end.ToSKPoint(), red);

            var lineLen = Vector3.Distance(line.End, line.Start);
            var lineDir = Vector3.Normalize(line.End - line.Start);
            var imageDir = Vector3.Normalize(end - start);
            var mid = Project(line.Start + lineDir * (lineLen * 0.5f)).ToSKPoint();

            bounds.Add(start);
            bounds.Add(end);

            var totalLen = lineLen;
            if (segIdx != 0 && path.segments[segIdx - 1] is Arc startArc)
            {
                var outerRadius = Vector3.Distance(startArc.Start, startArc.Center) + (pathWidth * 0.5f);
                var arcWidth = Util.GetArcWidth(outerRadius, startArc.SweepDeg);
                totalLen += arcWidth;

                bounds.Add(start - imageDir * arcWidth);
            }
            if (segIdx != path.segments.Count - 1 && path.segments[segIdx + 1] is Arc endArc)
            {
                var outerRadius = Vector3.Distance(endArc.Start, endArc.Center) + (pathWidth * 0.5f);
                var arcWidth = Util.GetArcWidth(outerRadius, endArc.SweepDeg);
                totalLen += arcWidth;

                bounds.Add(end + imageDir * arcWidth);
            }

            var lineString = totalLen != lineLen ? $"{totalLen} ({lineLen})" : $"{lineLen}";
            canvas.DrawText(lineString, mid.X, mid.Y, SKTextAlign.Center, angleFont, green);
        }
        else if (segment is Arc arc)
        {
            var sweepStartVec = arc.Start - arc.Center;
            var sweepEndVec = arc.End - arc.Center;

            IList<Vector3> sweepVecSamples = [sweepStartVec];
            var sampleDegrees = 5.0f;
            var currentDegrees = sampleDegrees;

            while (currentDegrees < arc.SweepDeg)
            {
                var sampleSweepVec = sweepStartVec.RotateAround(arc.Axis, -currentDegrees);
                sweepVecSamples.Add(sampleSweepVec);
                currentDegrees += sampleDegrees;
            }
            sweepVecSamples.Add(sweepEndVec);

            for (int i = 0; i < sweepVecSamples.Count - 1; i++)
            {
                var startVec = sweepVecSamples[i];
                var endVec = sweepVecSamples[i + 1];

                var start = Project(arc.Center + startVec);
                var end = Project(arc.Center + endVec);
                canvas.DrawLine(start.ToSKPoint(), end.ToSKPoint(), green);

                var startDir = Vector3.Normalize(startVec);
                var endDir = Vector3.Normalize(endVec);

                //Add outer diameter samples to bounds
                bounds.Add(Project(arc.Center + startVec + startDir * (pathWidth * 0.5f)));
                bounds.Add(Project(arc.Center + endVec + endDir * (pathWidth * 0.5f)));
            }

            var center = Project(arc.Center);

            canvas.DrawCircle(center.X, center.Y, pathWidth, red);

            var angleString = $"{arc.SweepDeg}°";
            canvas.DrawText(angleString, center.X, center.Y + angleFont.Size * 0.5f, SKTextAlign.Center, angleFont, green);
        }
    }

    var boundsX = bounds.Select(b => b.X).DefaultIfEmpty(0);
    var maxX = boundsX.Max();
    var minX = boundsX.Min();
    var rangeX = maxX - minX;
    var midX = minX + rangeX * 0.5f;

    var boundsY = bounds.Select(b => b.Y).DefaultIfEmpty(0);
    var maxY = boundsY.Max();
    var minY = boundsY.Min();
    var rangeY = maxY - minY;
    var midY = minY + rangeY * 0.5f;

    var boundsZ = bounds.Select(b => b.Z).DefaultIfEmpty(0);
    var maxZ = boundsZ.Max();
    var minZ = boundsZ.Min();
    var rangeZ = maxZ - minZ;
    var midZ = maxZ + rangeZ * 0.5f;

    var inset = blue.StrokeWidth * 3;

    if(rangeX > pathWidth)
    {
        canvas.DrawText($"X: {rangeX}", midX, maxY, SKTextAlign.Center, angleFont, blue);
        canvas.DrawLine(new SKPoint(minX, maxY + inset), new SKPoint(maxX, maxY + inset), blue);
    }

    if(rangeY > pathWidth)
    {
        canvas.DrawText($"Y: {rangeY}", maxX - angleFont.Size * 3, midY, SKTextAlign.Left, angleFont, blue);
        canvas.DrawLine(new SKPoint(maxX + inset, minY), new SKPoint(maxX + inset, maxY), blue);
    }

    if(rangeZ > pathWidth)
    {
        canvas.DrawText($"Z: {rangeZ}", maxX - angleFont.Size * 3, maxY, SKTextAlign.Left, angleFont, blue);
        canvas.DrawCircle(maxX + inset, maxY + inset, pathWidth, blue);
    }



    //canvas.ClipRect(new SKRect(left: minX, top: minY, right: maxX, bottom: maxY));


}

Console.WriteLine($"SVG written to: {Path.GetFullPath(outputPath)}");