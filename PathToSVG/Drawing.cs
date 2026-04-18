using SkiaSharp;
using System.Numerics;

#nullable enable

namespace PathToSVG
{
    static class Drawing
    {
        public static byte[] DrawToBytes(Path3D path, View view, Anchor anchor, DisplaySettings settings)
        {
            var debugBounds = false;
            var drawBackdrop = false;

            var degreesPerSample = 5.0f;

            var imagePath = path.ToImagePath(view, anchor, degreesPerSample);

            var originalBounds = imagePath.GetBounds();

            switch (settings.HandleOverlaps)
            {
                case HandleOverlaps.Shift:
                    {
                        //Determine preliminary path stroke width (based on unshifted path image dimensions) to apply appropriate shifting
                        var unshiftedMaxImageRange = Math.Max(originalBounds.Range.Y, originalBounds.Range.X);
                        var unshiftedPathStrokeWidth = imagePath.Diameter;
                        if (settings.FixedStrokeWidthPercentage != null)
                        {
                            int unshiftedPathStrokeWidthPercentage = settings.FixedStrokeWidthPercentage.Value.Clamp(min: 0, max: 100);
                            var unshiftedPathStrokeWidthPercent = unshiftedPathStrokeWidthPercentage / 100.0f;
                            unshiftedPathStrokeWidth = unshiftedMaxImageRange * unshiftedPathStrokeWidthPercent;
                        }

                        imagePath = imagePath.ApplyOverlapCorrection(shiftBy: unshiftedPathStrokeWidth * 2.0f);
                        break;
                    }

                case HandleOverlaps.Ignore:
                default:
                    //Do nothing
                    break;
            }

            var bounds = imagePath.GetBounds();

            var white = new SKColor(255, 255, 255);
            var grey = new SKColor(100, 100, 130);
            var blueishGrey = new SKColor(50, 50, 80);
            var blue = new SKColor(0, 150, 220);
            var yellow = new SKColor(255, 255, 0);
            var red = new SKColor(255, 0, 0);


            SKColor totalDimensionsAxisColor;
            if (settings.TotalDimensionsAxisColorHEX == null || !SKColor.TryParse(settings.TotalDimensionsAxisColorHEX, out totalDimensionsAxisColor)) totalDimensionsAxisColor = blue;

            SKColor basePathColor;
            if (settings.BasePathColorHEX == null || !SKColor.TryParse(settings.BasePathColorHEX, out basePathColor)) basePathColor = blueishGrey;

            SKColor highlightPathColor;
            if (settings.HighlightPathColorHEX == null || !SKColor.TryParse(settings.HighlightPathColorHEX, out highlightPathColor)) highlightPathColor = red;

            SKColor angleLabelColor;
            if (settings.AngleLabelColorHEX == null || !SKColor.TryParse(settings.AngleLabelColorHEX, out angleLabelColor)) angleLabelColor = blue;

            SKColor lengthLabelColor;
            if (settings.LengthLabelColorHEX == null || !SKColor.TryParse(settings.LengthLabelColorHEX, out lengthLabelColor)) lengthLabelColor = blue;

            var maxImageRange = Math.Max(bounds.Range.Y, bounds.Range.X);

            int labelFontSizePercentage = (settings.FontSizePercentage ?? 5).Clamp(min: 0, max: 100);
            var labelFontSizePercent = labelFontSizePercentage / 100.0f;
            var labelFontSize = maxImageRange * labelFontSizePercent;
            using var labelFont = new SKFont
            {
                Size = labelFontSize,
            };
            using var angleLabelPaint = new SKPaint
            {
                Color = angleLabelColor,
            };
            using var lengthLabelPaint = new SKPaint
            {
                Color = lengthLabelColor,
            };

            var pathStrokeWidth = imagePath.Diameter;
            if (settings.FixedStrokeWidthPercentage != null)
            {
                int pathStrokeWidthPercentage = settings.FixedStrokeWidthPercentage.Value.Clamp(min: 0, max: 100);
                var pathStrokeWidthPercent = pathStrokeWidthPercentage / 100.0f;
                pathStrokeWidth = maxImageRange * pathStrokeWidthPercent;
            }
            using var basePathPaint = new SKPaint
            {
                Color = basePathColor,
                StrokeWidth = pathStrokeWidth,
                StrokeCap = SKStrokeCap.Round,
            };
            using var selectedPathPaint = new SKPaint
            {
                Color = highlightPathColor,
                StrokeWidth = pathStrokeWidth,
                StrokeCap = SKStrokeCap.Round,
            };
            using var dependentPathPaint = new SKPaint
            {
                Color = highlightPathColor.WithAlpha(120),
                StrokeWidth = pathStrokeWidth,
                StrokeCap = SKStrokeCap.Round,
            };

            int totalDimensionsAxisPercentage = 1;
            var totalDimensionsAxisPercent = totalDimensionsAxisPercentage / 100.0f;
            var totalDimensionsAxisStrokeWidth = maxImageRange * totalDimensionsAxisPercent;
            using var totalDimensionsAxisPaint = new SKPaint
            {
                Color = totalDimensionsAxisColor,
                StrokeWidth = totalDimensionsAxisStrokeWidth,
            };

            using var backgroundPaint = new SKPaint
            {
                Color = white,
            };
            using var marginPaint = new SKPaint
            {
                Color = yellow,
            };
            using var markerPaint = new SKPaint
            {
                Color = red,
            };

            var rangeTolerance = 1.1f;
            var pathHasXRange = bounds.Range.X > imagePath.Diameter * rangeTolerance;
            var pathHasYRange = bounds.Range.Y > imagePath.Diameter * rangeTolerance;
            var pathHasZRange = bounds.Range.Z > imagePath.Diameter * rangeTolerance;

            var baseMargin = totalDimensionsAxisPaint.StrokeWidth;
            var topMarginWithAxis = labelFont.Size + totalDimensionsAxisPaint.StrokeWidth * 2;
            var rightMarginWithAxis = totalDimensionsAxisPaint.StrokeWidth * 6;
            var bottomMarginWithAxis = labelFont.Size + totalDimensionsAxisPaint.StrokeWidth * 6;

            var defaultMargin = new Margin(
                    Left: baseMargin,
                    Top: baseMargin,
                    Right: baseMargin,
                    Bottom: baseMargin
                    );

            var margin = settings.DisplayMeasurements ?
                settings.DisplayTotalDimensions switch
                {
                    DisplayTotalDimensions.Always => new Margin(
                        Left: baseMargin,
                        Top: topMarginWithAxis,
                        Right: rightMarginWithAxis,
                        Bottom: bottomMarginWithAxis
                        ),
                    DisplayTotalDimensions.Auto => new Margin(
                        Left: baseMargin,
                        Top: pathHasYRange ? topMarginWithAxis : baseMargin,
                        Right: pathHasYRange ? rightMarginWithAxis : baseMargin,
                        Bottom: pathHasXRange ? bottomMarginWithAxis : baseMargin
                        ),
                    DisplayTotalDimensions.Never => defaultMargin,
                    _ => defaultMargin
                } : defaultMargin; //No measurements means no special margin, as DisplayTotalDimensions mode is irrelevant

            var canvasRect = new SKRect(0, 0, bounds.Range.X + margin.Left + margin.Right, bounds.Range.Y + margin.Top + margin.Bottom);

            using (var stream = new MemoryStream())
            {
                using (var canvas = SKSvgCanvas.Create(canvasRect, stream))
                {

                    var fullWidth = margin.Left + bounds.Range.X + margin.Right;
                    var fullHeight = margin.Top + bounds.Range.Y + margin.Bottom;

                    #region Draw Backdrop
                    if (drawBackdrop)
                    {
                        canvas.DrawRect(canvasRect, backgroundPaint);
                        canvas.DrawRect(0, 0, fullWidth, margin.Top, marginPaint);
                        canvas.DrawRect(0, fullHeight - margin.Bottom, fullWidth, margin.Bottom, marginPaint);
                        canvas.DrawRect(0, 0, margin.Left, fullHeight, marginPaint);
                        canvas.DrawRect(fullWidth - margin.Right, 0, margin.Right, fullHeight, marginPaint);
                    }
                    #endregion

                    #region Draw ImagePath (Base Layer)
                    for (int pieceIdx = 0; pieceIdx < imagePath.Pieces.Count; pieceIdx++)
                    {
                        var piece = imagePath.Pieces[pieceIdx];
                        if (piece is ImageArc arc)
                        {
                            for (int sampleIdx = 0; sampleIdx < arc.ImageSweepSamples.Count - 1; sampleIdx++)
                            {
                                var start = arc.ImageSweepSamples[sampleIdx];
                                var end = arc.ImageSweepSamples[sampleIdx + 1];
                                canvas.DrawLine(start.ToSKPoint(bounds, margin), end.ToSKPoint(bounds, margin), basePathPaint);
                            }
                        }
                        else if (piece is ImageLine line)
                        {
                            canvas.DrawLine(line.ImageStart.ToSKPoint(bounds, margin), line.ImageEnd.ToSKPoint(bounds, margin), basePathPaint);
                        }
                    }
                    #endregion

                    #region Draw Dependent ImagePath (Mid Layer)
                    for (int pieceIdx = 0; pieceIdx < imagePath.Pieces.Count; pieceIdx++)
                    {
                        var piece = imagePath.Pieces[pieceIdx];
                        if (piece is ImageArc arc && arc.Arc3D.IsDependent)
                        {
                            for (int sampleIdx = 0; sampleIdx < arc.ImageSweepSamples.Count - 1; sampleIdx++)
                            {
                                var start = arc.ImageSweepSamples[sampleIdx];
                                var end = arc.ImageSweepSamples[sampleIdx + 1];
                                canvas.DrawLine(start.ToSKPoint(bounds, margin), end.ToSKPoint(bounds, margin), dependentPathPaint);
                            }
                        }
                        else if (piece is ImageLine line && line.Line3D.IsDependent)
                        {
                            canvas.DrawLine(line.ImageStart.ToSKPoint(bounds, margin), line.ImageEnd.ToSKPoint(bounds, margin), dependentPathPaint);
                        }
                    }
                    #endregion

                    #region Draw Selected ImagePath (Top Layer)
                    for (int pieceIdx = 0; pieceIdx < imagePath.Pieces.Count; pieceIdx++)
                    {
                        var piece = imagePath.Pieces[pieceIdx];
                        if (piece is ImageArc arc && arc.Arc3D.IsSelected)
                        {
                            for (int sampleIdx = 0; sampleIdx < arc.ImageSweepSamples.Count - 1; sampleIdx++)
                            {
                                var start = arc.ImageSweepSamples[sampleIdx];
                                var end = arc.ImageSweepSamples[sampleIdx + 1];
                                canvas.DrawLine(start.ToSKPoint(bounds, margin), end.ToSKPoint(bounds, margin), selectedPathPaint);
                            }
                        }
                        else if (piece is ImageLine line && line.Line3D.IsSelected)
                        {
                            canvas.DrawLine(line.ImageStart.ToSKPoint(bounds, margin), line.ImageEnd.ToSKPoint(bounds, margin), selectedPathPaint);
                        }
                    }
                    #endregion

                    #region Draw ImagePath Bounds (Debug)
                    if (debugBounds)
                    {
                        foreach (var piece in imagePath.Pieces)
                        {
                            if (piece is ImageLine line)
                            {
                                IList<Vector3> outerPts = [.. line.ImageStartBounds, .. line.ImageEndBounds];
                                foreach (var outerPt in outerPts)
                                {
                                    var pt = outerPt.ToSKPoint(bounds, margin);
                                    canvas.DrawCircle(pt, radius: 1, markerPaint);
                                }
                            }
                            else if (piece is ImageArc arc)
                            {
                                foreach (var outerPt in arc.ImageSampleBounds)
                                {
                                    var pt = outerPt.ToSKPoint(bounds, margin);
                                    canvas.DrawCircle(pt, radius: 1, markerPaint);
                                }
                            }
                        }
                    }
                    #endregion

                    #region Draw ImagePath Labels
                    //Text labels at the end

                    var lengthSuffix = settings.LengthUnitSuffix ?? "";

                    if (settings.DisplayMeasurements)
                    {
                        for (int pieceIdx = 0; pieceIdx < imagePath.Pieces.Count; pieceIdx++)
                        {
                            var currentPiece = imagePath.Pieces[pieceIdx];
                            if (currentPiece is ImageLine currentLine)
                            {
                                //Do not draw straight segment label if equal to total (X) length
                                if (imagePath.Pieces.Count > 1)
                                {
                                    var imageCenter = currentLine.ImageStart + (currentLine.ImageEnd - currentLine.ImageStart) * 0.5f;

                                    var outerLengthString = (currentLine.OuterLen3D * (float)settings.MillimetresToDisplayLengthUnit).ToFormattedString(settings.DecimalSeparator, settings.FractionDigits);
                                    var straightLengthString = (currentLine.StraightLen3D * (float)settings.MillimetresToDisplayLengthUnit).ToFormattedString(settings.DecimalSeparator, settings.FractionDigits);

                                    var straightLengthIsDifferent = currentLine.OuterLen3D != currentLine.StraightLen3D; //Note that we can do an exact comparison here, since if equal, both variables point to the same exact value

                                    var lineString = straightLengthIsDifferent && settings.DisplayStraightLengths ?
                                        $"{outerLengthString}({straightLengthString}){lengthSuffix}"
                                        : $"{outerLengthString}{lengthSuffix}";

                                    var distToLeft = Math.Abs(imageCenter.X - bounds.Min.X);
                                    var distToRight = Math.Abs(imageCenter.X - bounds.Max.X);
                                    var distToCenterWidth = Math.Abs(imageCenter.X - bounds.Center.X);

                                    var distToTop = Math.Abs(imageCenter.Y - bounds.Max.Y);
                                    var distToBottom = Math.Abs(imageCenter.Y - bounds.Min.Y);
                                    var distToCenterHeight = Math.Abs(imageCenter.Y - bounds.Center.Y);

                                    var textAlign = distToLeft < distToCenterWidth ? SKTextAlign.Left : (distToRight < distToCenterWidth ? SKTextAlign.Right : SKTextAlign.Center);
                                    canvas.DrawText(lineString, imageCenter.ToSKPoint(bounds, margin), textAlign, labelFont, lengthLabelPaint);
                                }

                                if (pieceIdx < imagePath.Pieces.Count - 1)
                                {
                                    var nextPiece = imagePath.Pieces[pieceIdx + 1];
                                    //If two lines directly after each other, display virtual angle label
                                    if (nextPiece is ImageLine nextLine && nextLine.Line3D.IsValidLine3D() && currentLine.Line3D.IsValidLine3D())
                                    {
                                        var currentReverseDir = currentLine.Line3D.Start - currentLine.Line3D.End;
                                        var nextDir = nextLine.Line3D.End - nextLine.Line3D.Start;
                                        var sweepDeg = Util.AngleBetweenDeg(currentReverseDir, nextDir);

                                        if (settings.HideAnglesModuloDeg == null || !sweepDeg.IsMultipleModulo(settings.HideAnglesModuloDeg.Value))
                                        {
                                            var currentReverseImageDir = Vector2.Normalize((currentLine.ImageStart - currentLine.ImageEnd).GetImage2DComponents());
                                            var nextImageDir = Vector2.Normalize((nextLine.ImageEnd - nextLine.ImageStart).GetImage2DComponents());

                                            var offset = (currentReverseImageDir * 0.5f + nextImageDir * 0.5f) * labelFont.Size * 3.0f;

                                            var angleString = $"{sweepDeg.ToFormattedString(settings.DecimalSeparator, settings.FractionDigits)}°";
                                            canvas.DrawText(angleString, currentLine.ImageEnd.ToSKPoint(bounds, margin) + new SKPoint(0, labelFont.Size * 0.5f) + new SKPoint(offset.X, -offset.Y), SKTextAlign.Center, labelFont, angleLabelPaint);
                                        }
                                    }
                                }
                            }
                            else if (currentPiece is ImageArc currentArc)
                            {
                                var sweepDeg = Math.Abs(currentArc.Arc3D.SweepDeg);

                                if (sweepDeg > 360 || settings.HideAnglesModuloDeg == null || !sweepDeg.IsMultipleModulo(settings.HideAnglesModuloDeg.Value))
                                {
                                    var angleString = $"{sweepDeg.ToFormattedString(settings.DecimalSeparator, settings.FractionDigits)}°";
                                    canvas.DrawText(angleString, currentArc.ImageCenter.ToSKPoint(bounds, margin) + new SKPoint(0, labelFont.Size * 0.5f), SKTextAlign.Center, labelFont, angleLabelPaint);
                                }
                            }
                        }
                    }
                    #endregion

                    #region Draw Measurements
                    if (settings.DisplayMeasurements)
                    {
                        var halfStrokeWidth = totalDimensionsAxisPaint.StrokeWidth * 0.5f;
                        var capWidth = totalDimensionsAxisPaint.StrokeWidth * 4f;
                        var halfCapWidth = capWidth * 0.5f;

                        var xLineHeight = margin.Top + bounds.Range.Y + halfCapWidth + totalDimensionsAxisPaint.StrokeWidth;
                        var yLineWidth = margin.Left + bounds.Range.X + halfCapWidth + totalDimensionsAxisPaint.StrokeWidth;

                        var displayX = settings.DisplayTotalDimensions == DisplayTotalDimensions.Always ||
                            (settings.DisplayTotalDimensions == DisplayTotalDimensions.Auto && pathHasXRange);

                        var displayY = settings.DisplayTotalDimensions == DisplayTotalDimensions.Always ||
                            (settings.DisplayTotalDimensions == DisplayTotalDimensions.Auto && pathHasYRange);

                        var displayZ = settings.DisplayTotalDimensions == DisplayTotalDimensions.Always ||
                            (settings.DisplayTotalDimensions == DisplayTotalDimensions.Auto && pathHasZRange);

                        if (displayX)
                        {
                            var rangeXStart = new SKPoint(margin.Left, xLineHeight);
                            var rangeXEnd = new SKPoint(margin.Left + bounds.Range.X, xLineHeight);
                            canvas.DrawLine(rangeXStart, rangeXEnd, totalDimensionsAxisPaint);
                            canvas.DrawLine(rangeXStart + new SKPoint(halfStrokeWidth, halfCapWidth), rangeXStart + new SKPoint(halfStrokeWidth, -halfCapWidth), totalDimensionsAxisPaint);
                            canvas.DrawLine(rangeXEnd + new SKPoint(-halfStrokeWidth, halfCapWidth), rangeXEnd + new SKPoint(-halfStrokeWidth, -halfCapWidth), totalDimensionsAxisPaint);

                            var rangeXString = (originalBounds.Range.X * (float)settings.MillimetresToDisplayLengthUnit).ToFormattedString(settings.DecimalSeparator, settings.FractionDigits);

                            canvas.DrawText($"{rangeXString}{lengthSuffix}", bounds.Range.X * 0.5f + margin.Left, fullHeight - totalDimensionsAxisPaint.StrokeWidth, SKTextAlign.Center, labelFont, totalDimensionsAxisPaint);
                        }

                        if (displayY)
                        {
                            var rangeYStart = new SKPoint(yLineWidth, margin.Top);
                            var rangeYEnd = new SKPoint(yLineWidth, margin.Top + bounds.Range.Y);
                            canvas.DrawLine(rangeYStart, rangeYEnd, totalDimensionsAxisPaint);
                            canvas.DrawLine(rangeYStart + new SKPoint(halfCapWidth, halfStrokeWidth), rangeYStart + new SKPoint(-halfCapWidth, halfStrokeWidth), totalDimensionsAxisPaint);
                            canvas.DrawLine(rangeYEnd + new SKPoint(halfCapWidth, -halfStrokeWidth), rangeYEnd + new SKPoint(-halfCapWidth, -halfStrokeWidth), totalDimensionsAxisPaint);

                            var rangeYString = (originalBounds.Range.Y * (float)settings.MillimetresToDisplayLengthUnit).ToFormattedString(settings.DecimalSeparator, settings.FractionDigits);

                            canvas.DrawText($"{rangeYString}{lengthSuffix}", fullWidth - totalDimensionsAxisPaint.StrokeWidth, labelFont.Size + totalDimensionsAxisPaint.StrokeWidth, SKTextAlign.Right, labelFont, totalDimensionsAxisPaint);
                        }

                        if (displayZ)
                        {
                            var markerCenter = new SKPoint(yLineWidth, xLineHeight);
                            canvas.DrawCircle(markerCenter, radius: halfCapWidth, totalDimensionsAxisPaint);
                            canvas.DrawCircle(markerCenter, radius: halfCapWidth - totalDimensionsAxisPaint.StrokeWidth, basePathPaint);

                            var rangeZString = (originalBounds.Range.Z * (float)settings.MillimetresToDisplayLengthUnit).ToFormattedString(settings.DecimalSeparator, settings.FractionDigits);

                            canvas.DrawText($"{rangeZString}{lengthSuffix}", fullWidth - totalDimensionsAxisPaint.StrokeWidth, fullHeight - totalDimensionsAxisPaint.StrokeWidth, SKTextAlign.Right, labelFont, totalDimensionsAxisPaint);
                        }
                    }
                    #endregion
                }

                //Important: Only return stream after disposing canvas (SVG closing tag)
                return stream.ToArray();
            }

        }
    }
}