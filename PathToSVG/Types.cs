using System.Numerics;

namespace PathToSVG
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Start"></param>
    /// <param name="End"></param>
    /// <param name="IsSelected"></param>
    /// <param name="IsDependent"></param>
    public record PathPiece3D(Vector3 Start, Vector3 End, bool IsSelected, bool IsDependent);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Start"></param>
    /// <param name="End"></param>
    /// <param name="IsSelected"></param>
    /// <param name="IsDependent"></param>
    public record Line3D(Vector3 Start, Vector3 End, bool IsSelected = false, bool IsDependent = false) : PathPiece3D(Start, End, IsSelected, IsDependent);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Start"></param>
    /// <param name="End"></param>
    /// <param name="Center"></param>
    /// <param name="Axis"></param>
    /// <param name="SweepDeg"></param>
    /// <param name="IsSelected"></param>
    /// <param name="IsDependent"></param>
    public record Arc3D(Vector3 Start, Vector3 End, Vector3 Center, Vector3 Axis, float SweepDeg, bool IsSelected = false, bool IsDependent = false) : PathPiece3D(Start, End, IsSelected, IsDependent);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Pieces"></param>
    /// <param name="Diameter"></param>
    public record Path3D(IList<PathPiece3D> Pieces, float Diameter);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="AxisX"></param>
    /// <param name="AxisY"></param>
    /// <param name="AxisZ"></param>
    public record CoordinateSystem(Vector3 AxisX, Vector3 AxisY, Vector3 AxisZ);

    /// <summary>
    /// 
    /// </summary>
    public record ImagePiece();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Line3D"></param>
    /// <param name="ImageStart"></param>
    /// <param name="ImageEnd"></param>
    /// <param name="ImageStartBounds"></param>
    /// <param name="ImageEndBounds"></param>
    /// <param name="StraightLen3D"></param>
    /// <param name="OuterLen3D"></param>
    public record ImageLine(Line3D Line3D, Vector3 ImageStart, Vector3 ImageEnd, IList<Vector3> ImageStartBounds, IList<Vector3> ImageEndBounds, float StraightLen3D, float OuterLen3D) : ImagePiece();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Arc3D"></param>
    /// <param name="ImageCenter"></param>
    /// <param name="ImageSweepSamples"></param>
    /// <param name="ImageSampleBounds"></param>
    public record ImageArc(Arc3D Arc3D, Vector3 ImageCenter, IList<Vector3> ImageSweepSamples, IList<Vector3> ImageSampleBounds) : ImagePiece();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Pieces"></param>
    /// <param name="Diameter"></param>
    public record ImagePath(IList<ImagePiece> Pieces, float Diameter);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Left"></param>
    /// <param name="Top"></param>
    /// <param name="Right"></param>
    /// <param name="Bottom"></param>
    public record Margin(float Left, float Top, float Right, float Bottom);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Min"></param>
    /// <param name="Max"></param>
    /// <param name="Range"></param>
    /// <param name="Center"></param>
    public record Bounds(Vector3 Min, Vector3 Max, Vector3 Range, Vector3 Center);

    /// <summary>
    /// 
    /// </summary>
    public enum View
    {
        ///
        Front,
        ///
        Side,
        ///
        Top
    }

    /// <summary>
    /// 
    /// </summary>
    public enum Anchor
    {
        ///
        FirstLine,
        ///
        LongestLine,
        ///
        LongestCollinearGroup
    }

    /// <summary>
    /// 
    /// </summary>
    public enum DisplayTotalDimensions
    {
        ///
        Always,
        ///
        Auto,
        ///
        Never
    }

    /// <summary>
    /// 
    /// </summary>
    public enum HandleOverlaps
    {
        ///
        Shift,
        ///
        Ignore
    }

    /// <summary>
    /// Customization options for the rendered SVG image
    /// </summary>
    public record DisplaySettings
    {
        /// <summary>
        /// Determines if length and angle labels are displayed
        /// </summary>
        public required bool DisplayMeasurements { get; init; }

        /// <summary>
        /// Decimal separator in numeric text labels
        /// </summary>
        public required string DecimalSeparator { get; init; }

        /// <summary>
        /// Number of fraction digits that are displayed in numeric text labels
        /// </summary>
        public required int FractionDigits { get; init; }

        /// <summary>
        /// Conversion factor to apply to internal values (mm) to display them in length text labels
        /// </summary>
        public required double MillimetresToDisplayLengthUnit { get; init; }

        /// <summary>
        /// Optional suffix that is appended to length text labels
        /// If not specified, no suffix will be displayed
        /// </summary>
        public required string? LengthUnitSuffix { get; init; }

        /// <summary>
        /// Determines whether, in addition to outer lengths, straight lengths will also be shown (in brackets)
        /// </summary>
        public required bool DisplayStraightLengths { get; init; }

        /// <summary>
        /// Optional criterion, specified as degrees ]0,360[ for not displaying angle text labels for angles (smaller than 360 degrees) that are an exact multiple of the specified value
        /// Note: Angles greater than 360 degrees (spirals) will always be displayed as text labels, since the drawn path will overlap
        /// </summary>
        public required int? HideAnglesModuloDeg { get; init; }

        /// <summary>
        /// Font size, specified as percentage [0,100] of total image dimensions
        /// </summary>
        public required int? FontSizePercentage { get; init; }

        /// <summary>
        /// Optional stroke width, specified as percentage [0,100] of total image dimensions
        /// If not specified, stroke width is set according to bar diameter 
        /// </summary>
        public required int? FixedStrokeWidthPercentage { get; init; }

        /// <summary>
        /// Specifies when total dimension (width, height, depth) axes will be displayed
        /// If set to Auto, only the axes in which the given bar extends (axis dimension greater than bar diameter) will be shown
        /// </summary>
        public required DisplayTotalDimensions DisplayTotalDimensions { get; init; }

        /// <summary>
        /// Specifies how overlaps in the underlying path geometry should be handled when displaying it
        /// </summary>
        public required HandleOverlaps HandleOverlaps { get; init; }

        /// <summary>
        /// Specifies whether, when straight pieces are highlighted, adjacent arcs are also highlighted up to their tangent
        /// If set to false, only the straight piece itself will be highlighted
        /// </summary>
        public required bool HighlightOuterDimensions { get; init; }

        /// <summary>
        /// HEX Color Code (#RRGGBB) of the drawn base path
        /// </summary>
        public required string? BasePathColorHEX { get; init; }

        /// <summary>
        /// HEX Color Code (#RRGGBB) of the drawn highlighted path
        /// This simultaneously affects the display of highlighted segment indices, as well as highlighted slave segment indices (by blending between this and the base path color)
        /// </summary>
        public required string? HighlightPathColorHEX { get; init; }

        /// <summary>
        /// HEX Color Code (#RRGGBB) of the total dimension axes
        /// </summary>
        public required string? TotalDimensionsAxisColorHEX { get; init; }

        /// <summary>
        /// HEX Color Code (#RRGGBB) of all drawn angle text labels
        /// </summary>
        public required string? AngleLabelColorHEX { get; init; }

        /// <summary>
        /// HEX Color Code (#RRGGBB) of all drawn length text labels
        /// </summary>
        public required string? LengthLabelColorHEX { get; init; }
    }
}