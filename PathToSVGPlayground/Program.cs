using PathToSVG;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;

var len = 300;
var arcRadius = 30;

var rightVec = new Vector3(1, 0, 0);
var leftVec = rightVec * -1;
var upVec = new Vector3(0, 0, 1);
var downVec = upVec * -1;
var inVec = new Vector3(0, 1, 0);
var outVec = inVec * -1;

Path3D test3D = new(Pieces: [
    //new Line3D(new(-len,0,len), new(0,0,0)),
    new Line3D(new(0,0,0), new(len-arcRadius,0,0)),
    new Arc3D(Start: new(len-arcRadius,0,0), End: new(len, 0, arcRadius), Center: new(len-arcRadius, 0, arcRadius), Axis: outVec, SweepDeg: 90),
    new Line3D(new(len,0,arcRadius), new(len,0,len- arcRadius)),
    new Arc3D(Start: new(len,0,len- arcRadius), End: new(len, arcRadius, len), Center: new(len, arcRadius, len-arcRadius), Axis: leftVec, SweepDeg: 90),
    new Line3D(new(len,arcRadius,len), new(len,len-arcRadius,len)),
    new Arc3D(Start: new(len,len - arcRadius,len), End: new(len, len, len-arcRadius), Center: new(len, len-arcRadius, len-arcRadius), Axis: leftVec, SweepDeg: 90),
    new Line3D(new(len,len,len-arcRadius), new(len,len,arcRadius)),
    new Arc3D(Start: new(len,len, arcRadius), End: new(len+arcRadius, len, 0), Center: new(len+arcRadius, len, arcRadius), Axis: outVec, SweepDeg: 90),
    new Line3D(new(len+arcRadius, len, 0), new(len*2,len,0)),
],
Diameter: 8
);

Path3D testCage = new(Pieces: [
    new Line3D(new(len*0.75f, 0, len*0.75f), new(len, 0, len)),
    new Line3D( new(len, 0, len), new(len, 0, arcRadius)),
    new Arc3D(Start: new(len,0,arcRadius), End:  new(len-arcRadius, 0, 0), Center: new(len-arcRadius, 0, arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D( new(len-arcRadius, 0, 0), new(-arcRadius, 0, 0)),
    new Arc3D(Start: new(-arcRadius, 0, 0), End:  new(-arcRadius*2, 0, arcRadius), Center: new(-arcRadius, 0, arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D( new(-arcRadius*2, 0, arcRadius), new(-arcRadius*2, 0, len-arcRadius)),
    new Arc3D(Start:   new(-arcRadius*2, 0, len-arcRadius), End:  new(0, 0, len-arcRadius), Center: new(-arcRadius, 0, len-arcRadius), Axis: outVec, SweepDeg: -180),
    new Line3D( new(0, 0, len-arcRadius), new(0, 0, len-arcRadius*3)),
    new Arc3D(Start: new(0, 0, len-arcRadius*3), End:   new(arcRadius, 0, len-arcRadius*4), Center: new(arcRadius, 0,  len-arcRadius*3), Axis: outVec, SweepDeg: 90),
    new Line3D( new(arcRadius, 0, len-arcRadius*4), new(arcRadius*3, 0, len-arcRadius*4)),
],
Diameter: 10
);

Path3D testOverlap = new(Pieces: [
    new Line3D(new(0,0,len-len*0.3f), new(0,0,len-arcRadius)), //Start overlap
    new Arc3D(Start: new(0,0,len-arcRadius), End: new(arcRadius,0,len), Center: new(arcRadius, 0, len-arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D(new(arcRadius,0,len), new(len-arcRadius, 0, len)),
    new Arc3D(Start: new(len-arcRadius, 0, len), End: new(len, 0, len-arcRadius), Center: new(len-arcRadius, 0, len-arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D(new(len, 0, len-arcRadius), new(len, 0, arcRadius)),
    new Arc3D(Start: new(len, 0, arcRadius), End: new(len-arcRadius, 0, 0), Center: new(len-arcRadius, 0, arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D(new(len-arcRadius, 0, 0), new(arcRadius, 0, 0)),
    new Arc3D(Start: new(arcRadius, 0, 0), End: new(0,0,arcRadius), Center: new(arcRadius, 0, arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D(new(0,0,arcRadius), new(0,0,len-arcRadius)),
    new Arc3D(Start: new(0,0,len-arcRadius), End: new(arcRadius,0,len), Center: new(arcRadius, 0, len-arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D(new(arcRadius,0,len), new(len-arcRadius, 0, len)),
    new Arc3D(Start: new(len-arcRadius, 0, len), End: new(len, 0, len-arcRadius), Center: new(len-arcRadius, 0, len-arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D(new(len, 0, len-arcRadius), new(len, 0, arcRadius)),
    new Arc3D(Start: new(len, 0, arcRadius), End: new(len-arcRadius, 0, 0), Center: new(len-arcRadius, 0, arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D(new(len-arcRadius, 0, 0), new(arcRadius, 0, 0)),
    new Arc3D(Start: new(arcRadius, 0, 0), End: new(0,0,arcRadius), Center: new(arcRadius, 0, arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D(new(0,0,arcRadius), new(0,0,len-arcRadius)),
    new Arc3D(Start: new(0,0,len-arcRadius), End: new(arcRadius,0,len), Center: new(arcRadius, 0, len-arcRadius), Axis: outVec, SweepDeg: -90),
    new Line3D(new(arcRadius,0,len), new(len*0.3f,0,len))//End overlap
],
Diameter: 10
);

Path3D testOddAngles = new(Pieces: [
    new Line3D(new(0,0,len*1.5f), new(len,0,0)),
    new Line3D(new(len,0,0),new(len*3,0,0)),
    new Line3D(new(len*3,0,0),new(len*4,0,len))
],
Diameter: 12
);

Path3D testStraight = new(Pieces: [
    new Line3D(new(0,0,0), new(len*5,0,0)),
    //new Line3D(new(len*5,0,0), new(len*10,0,0)),
],
Diameter: 16
);

var path = testOverlap;

var view = View.Front;

var anchor = Anchor.LongestCollinearGroup;

var settings = new DisplaySettings
{
    LengthLabelColorHEX = "#2196f3",
    AngleLabelColorHEX = "#f44336",
    BasePathColorHEX = "#455a64",
    HighlightPathColorHEX = "#ff9800",
    TotalDimensionsAxisColorHEX = "#2196f3",
    DecimalSeparator = ".",
    DisplayStraightLengths = true,
    DisplayTotalDimensions = DisplayTotalDimensions.Auto,
    FixedStrokeWidthPercentage = 2,
    FontSizePercentage = 5,
    FractionDigits = 1,
    HandleOverlaps = HandleOverlaps.Shift,
    HideAnglesModuloDeg = 90,
    LengthUnitSuffix = "cm",
    MillimetresToDisplayLengthUnit = 0.1,
    DisplayMeasurements = true
};

var timer = new Stopwatch();

timer.Start();
var svgContent = Drawing.DrawToBytes(path, view, anchor, settings);
timer.Stop();

TimeSpan timeTaken = timer.Elapsed;

Console.WriteLine($"Drawn in: {timeTaken.TotalMilliseconds} ms");

string outputPath = "./output.svg";
File.WriteAllBytes(outputPath, svgContent);

Console.WriteLine($"SVG written to: {Path.GetFullPath(outputPath)}");