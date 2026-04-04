using SkiaSharp;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using Types;
using Utils;
using static System.Net.Mime.MediaTypeNames;

#nullable enable

// Output path for the SVG file
string outputPath = "./output.svg";

var len = 300;
var arcRadius = 30;

var rightVec = new Vector3(1, 0, 0);
var leftVec = rightVec * -1;
var upVec = new Vector3(0, 0, 1);
var downVec = upVec * -1;
var inVec = new Vector3(0, 1, 0);
var outVec = inVec * -1;

Path3D test3D = new(Pieces: [
    new Line3D(new(-len*0.5f,0,len*0.5f), new(0,0,0)),
    new Line3D(new(0,0,0), new(len-arcRadius,0,0)),
    new Arc3D(Start: new(len-arcRadius,0,0), End: new(len, 0, arcRadius), Center: new(len-arcRadius, 0, arcRadius), Axis: inVec, SweepDeg: 90),
    new Line3D(new(len,0,arcRadius), new(len,0,len- arcRadius)),
    new Arc3D(Start: new(len,0,len- arcRadius), End: new(len, arcRadius, len), Center: new(len, arcRadius, len-arcRadius), Axis: rightVec, SweepDeg: 90),
    new Line3D(new(len,arcRadius,len), new(len,len-arcRadius,len)),
    new Arc3D(Start: new(len,len - arcRadius,len), End: new(len, len, len-arcRadius), Center: new(len, len-arcRadius, len-arcRadius), Axis: rightVec, SweepDeg: 90),
    new Line3D(new(len,len,len-arcRadius), new(len,len,arcRadius)),
    new Arc3D(Start: new(len,len, arcRadius), End: new(len+arcRadius, len, 0), Center: new(len+arcRadius, len, arcRadius), Axis: inVec, SweepDeg: 90),
    new Line3D(new(len+arcRadius, len, 0), new(len*2,len,0)),
],
Diameter: 6
);

Path3D testCage = new(Pieces: [
    new Line3D(new(len*0.75f, 0, len*0.75f), new(len, 0, len)),
    new Line3D( new(len, 0, len), new(len, 0, arcRadius)),
    new Arc3D(Start: new(len,0,arcRadius), End:  new(len-arcRadius, 0, 0), Center: new(len-arcRadius, 0, arcRadius), Axis: inVec, SweepDeg: -90),
    new Line3D( new(len-arcRadius, 0, 0), new(-arcRadius, 0, 0)),
    new Arc3D(Start: new(-arcRadius, 0, 0), End:  new(-arcRadius*2, 0, arcRadius), Center: new(-arcRadius, 0, arcRadius), Axis: inVec, SweepDeg: -90),
    new Line3D( new(-arcRadius*2, 0, arcRadius), new(-arcRadius*2, 0, len-arcRadius)),
    new Arc3D(Start:   new(-arcRadius*2, 0, len-arcRadius), End:  new(0, 0, len-arcRadius), Center: new(-arcRadius, 0, len-arcRadius), Axis: inVec, SweepDeg: -180),
    new Line3D( new(0, 0, len-arcRadius), new(0, 0, len-arcRadius*3)),
    new Arc3D(Start: new(0, 0, len-arcRadius*3), End:   new(arcRadius, 0, len-arcRadius*4), Center: new(arcRadius, 0,  len-arcRadius*3), Axis: inVec, SweepDeg: 90),
    new Line3D( new(arcRadius, 0, len-arcRadius*4), new(arcRadius*3, 0, len-arcRadius*4)),
],
Diameter: 8
);

Path3D testOddAngles = new(Pieces: [
    new Line3D(new(0,0,len*1.5f), new(len,0,0)),
    new Line3D(new(len,0,0),new(len*3,0,0)),
    new Line3D(new(len*3,0,0),new(len*4,0,len))
],
Diameter: 8
);

var view = View.Front;

var degreesPerSample = 5.0f;

var preferLongestLine = false;

var debugBounds = true;

var path = test3D;

var imagePath = path.ToImagePath(view, preferLongestLine, degreesPerSample);

var bounds = imagePath.GetBounds();

var textPercentage = 0.03f;
var textWidthFraction = bounds.Range.Y * textPercentage;
var textHeightFraction = bounds.Range.X * textPercentage;

using var textFont = new SKFont
{
    Size = Math.Max(textWidthFraction, textHeightFraction),
};

var measureLinesPercentage = 0.005f;
var measureLinesWidhtFraction = bounds.Range.Y * measureLinesPercentage;
var measureLinesHeightFraction = bounds.Range.X * measureLinesPercentage;

using var measurePaint = new SKPaint
{
    Color = new SKColor(0, 220, 255),
    StrokeWidth = Math.Max(measureLinesWidhtFraction, measureLinesHeightFraction),
};

var margin = new Margin(
    Left: measurePaint.StrokeWidth,
    Top: bounds.Range.Y > imagePath.Diameter ? textFont.Size + measurePaint.StrokeWidth * 2 : measurePaint.StrokeWidth,
    Right: bounds.Range.Y > imagePath.Diameter ? measurePaint.StrokeWidth * 6 : measurePaint.StrokeWidth,
    Bottom: bounds.Range.X > imagePath.Diameter ? textFont.Size + measurePaint.StrokeWidth * 6 : measurePaint.StrokeWidth
    );

var canvasRect = new SKRect(0, 0, bounds.Range.X + margin.Left + margin.Right, bounds.Range.Y + margin.Top + margin.Bottom);

using (var stream = new SKFileWStream(outputPath))
using (var canvas = SKSvgCanvas.Create(canvasRect, stream))
{
    using var backgroundPaint = new SKPaint
    {
        Color = new SKColor(100, 100, 130),
    };

    using var marginPaint = new SKPaint
    {
        Color = new SKColor(50, 50, 80),
    };

    using var linePaint = new SKPaint
    {
        Color = new SKColor(255, 0, 0),
        StrokeWidth = imagePath.Diameter,
        IsAntialias = true,
    };

    using var arcPaint = new SKPaint
    {
        Color = new SKColor(0, 255, 0),
        StrokeWidth = imagePath.Diameter,
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Round,
    };

    using var markerPaint = new SKPaint
    {
        Color = new SKColor(255, 255, 0),
        IsAntialias = true
    };

    var fullWidth = margin.Left + bounds.Range.X + margin.Right;
    var fullHeight = margin.Top + bounds.Range.Y + margin.Bottom;

    #region Draw Margins
    canvas.DrawRect(canvasRect, backgroundPaint);
    canvas.DrawRect(0, 0, fullWidth, margin.Top, marginPaint);
    canvas.DrawRect(0, fullHeight - margin.Bottom, fullWidth, margin.Bottom, marginPaint);
    canvas.DrawRect(0, 0, margin.Left, fullHeight, marginPaint);
    canvas.DrawRect(fullWidth - margin.Right, 0, margin.Right, fullHeight, marginPaint);
    #endregion

    #region Draw ImagePath
    //Arcs first
    for (int pieceIdx = 0; pieceIdx < imagePath.Pieces.Count; pieceIdx++)
    {
        var piece = imagePath.Pieces[pieceIdx];
        if (piece is ImageArc arc)
        {
            for (int sampleIdx = 0; sampleIdx < arc.ImageSweepSamples.Count - 1; sampleIdx++)
            {
                var start = arc.ImageSweepSamples[sampleIdx];
                var end = arc.ImageSweepSamples[sampleIdx + 1];
                canvas.DrawLine(start.ToSKPoint(bounds, margin), end.ToSKPoint(bounds, margin), arcPaint);
            }
        }
    }
    //Then lines
    for (int pieceIdx = 0; pieceIdx < imagePath.Pieces.Count; pieceIdx++)
    {
        var piece = imagePath.Pieces[pieceIdx];
        if (piece is ImageLine line)
        {
            canvas.DrawLine(line.ImageStart.ToSKPoint(bounds, margin), line.ImageEnd.ToSKPoint(bounds, margin), linePaint);
        }
    }
    //Text labels at the end
    for (int pieceIdx = 0; pieceIdx < imagePath.Pieces.Count; pieceIdx++)
    {
        var piece = imagePath.Pieces[pieceIdx];
        if (piece is ImageLine line)
        {
            if (imagePath.Pieces.Count > 1)
            {
                var imageCenter = line.ImageStart + (line.ImageEnd - line.ImageStart) * 0.5f;
                var lineString = line.OuterLen3D != line.StraightLen3D ? $"{line.OuterLen3D.ToString("0.#")} ({line.StraightLen3D.ToString("0.#")})" : $"{line.OuterLen3D.ToString("0.#")}";

                var distToLeft = Math.Abs(imageCenter.X - bounds.Min.X);
                var distToRight = Math.Abs(imageCenter.X - bounds.Max.X);
                var distToCenterWidth = Math.Abs(imageCenter.X - bounds.Center.X);

                var distToTop = Math.Abs(imageCenter.Y - bounds.Max.Y);
                var distToBottom = Math.Abs(imageCenter.Y - bounds.Min.Y);
                var distToCenterHeight = Math.Abs(imageCenter.Y - bounds.Center.Y);

                var heightOffset = distToTop < distToCenterHeight ? textFont.Size : (distToBottom < distToCenterHeight ? 0 : textFont.Size * 0.5f);

                var textAlign = distToLeft < distToCenterWidth ? SKTextAlign.Left : (distToRight < distToCenterWidth ? SKTextAlign.Right : SKTextAlign.Center);
                canvas.DrawText(lineString, imageCenter.ToSKPoint(bounds, margin) + new SKPoint(0, heightOffset), textAlign, textFont, measurePaint);
            }
        }
        else if (piece is ImageArc arc)
        {
            var distToTop = Math.Abs(arc.ImageCenter.Y - bounds.Max.Y);
            var distToBottom = Math.Abs(arc.ImageCenter.Y - bounds.Min.Y);
            var distToCenterHeight = Math.Abs(arc.ImageCenter.Y - bounds.Center.Y);

            var heightOffset = distToTop < distToCenterHeight ? textFont.Size : (distToBottom < distToCenterHeight ? 0 : textFont.Size * 0.5f);

            var angleString = $"{Math.Abs(arc.Arc3D.SweepDeg).ToString("0.#")}°";
            canvas.DrawText(angleString, arc.ImageCenter.ToSKPoint(bounds, margin) + new SKPoint(0, heightOffset), SKTextAlign.Center, textFont, measurePaint);
        }
    }
    #endregion

    #region Draw Measurements
    var halfStrokeWidth = measurePaint.StrokeWidth * 0.5f;
    var capWidth = measurePaint.StrokeWidth * 4f;
    var halfCapWidth = capWidth * 0.5f;

    var xLineHeight = margin.Top + bounds.Range.Y + halfCapWidth + measurePaint.StrokeWidth;
    var yLineWidth = margin.Left + bounds.Range.X + halfCapWidth + measurePaint.StrokeWidth;

    if (bounds.Range.X > imagePath.Diameter)
    {
        var rangeXStart = new SKPoint(margin.Left, xLineHeight);
        var rangeXEnd = new SKPoint(margin.Left + bounds.Range.X, xLineHeight);
        canvas.DrawLine(rangeXStart, rangeXEnd, measurePaint);

        canvas.DrawLine(rangeXStart + new SKPoint(halfStrokeWidth, halfCapWidth), rangeXStart + new SKPoint(halfStrokeWidth, -halfCapWidth), measurePaint);
        canvas.DrawLine(rangeXEnd + new SKPoint(-halfStrokeWidth, halfCapWidth), rangeXEnd + new SKPoint(-halfStrokeWidth, -halfCapWidth), measurePaint);

        canvas.DrawText($"X: {bounds.Range.X.ToString("0.#")}", bounds.Range.X * 0.5f + margin.Left, fullHeight - measurePaint.StrokeWidth, SKTextAlign.Center, textFont, measurePaint);
    }

    if (bounds.Range.Y > imagePath.Diameter)
    {
        var rangeYStart = new SKPoint(yLineWidth, margin.Top);
        var rangeYEnd = new SKPoint(yLineWidth, margin.Top + bounds.Range.Y);
        canvas.DrawLine(rangeYStart, rangeYEnd, measurePaint);
        canvas.DrawLine(rangeYStart + new SKPoint(halfCapWidth, halfStrokeWidth), rangeYStart + new SKPoint(-halfCapWidth, halfStrokeWidth), measurePaint);
        canvas.DrawLine(rangeYEnd + new SKPoint(halfCapWidth, -halfStrokeWidth), rangeYEnd + new SKPoint(-halfCapWidth, -halfStrokeWidth), measurePaint);

        canvas.DrawText($"Y: {bounds.Range.Y.ToString("0.#")}", fullWidth - measurePaint.StrokeWidth, textFont.Size + measurePaint.StrokeWidth, SKTextAlign.Right, textFont, measurePaint);
    }

    if (bounds.Range.Z > imagePath.Diameter)
    {
        var markerCenter = new SKPoint(yLineWidth, xLineHeight);
        canvas.DrawCircle(markerCenter, radius: halfCapWidth, measurePaint);
        canvas.DrawCircle(markerCenter, radius: halfCapWidth - measurePaint.StrokeWidth, marginPaint);

        canvas.DrawText($"Z: {bounds.Range.Z.ToString("0.#")}", fullWidth - measurePaint.StrokeWidth, fullHeight - measurePaint.StrokeWidth, SKTextAlign.Right, textFont, measurePaint);
    }
    #endregion

    #region Draw ImagePath Bounds (Debug)
    if (debugBounds)
    {
        foreach (var outerPt in imagePath.Pieces.SelectMany(x => x.ImageBounds))
        {
            var pt = outerPt.ToSKPoint(bounds, margin);
            canvas.DrawCircle(pt, radius: 1, markerPaint);
        }
    }
    #endregion
}

Console.WriteLine($"SVG written to: {Path.GetFullPath(outputPath)}");