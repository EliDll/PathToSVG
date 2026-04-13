using SkiaSharp;
using System.Numerics;

#nullable enable

namespace PathToSVG
{
    static class Drawing
    {
        public static byte[] DrawToBytes(Path3D path, View view, Anchor anchor, OverlapHandling overlapHandling)
        {
            var degreesPerSample = 5.0f;

            var debugBounds = true;

            var drawBackdrop = false;

            var white = new SKColor(255, 255, 255);
            var grey = new SKColor(100, 100, 130);
            var blueishGrey = new SKColor(50, 50, 80);
            var blue = new SKColor(0, 150, 220);
            var yellow = new SKColor(255, 255, 0);
            var red = new SKColor(255, 0, 0);
            var redTransparent = new SKColor(255, 0, 0, 120);

            var imagePath = path.ToImagePath(view, anchor, degreesPerSample);

            var originalBounds = imagePath.GetBounds();

            if (overlapHandling == OverlapHandling.Shift3D)
            {
                imagePath = imagePath.ApplyOverlapCorrection();
            }

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
                Color = blue,
                StrokeWidth = Math.Max(measureLinesWidhtFraction, measureLinesHeightFraction),
            };

            var rangeTolerance = 1.1f;

            var margin = new Margin(
                Left: measurePaint.StrokeWidth,
                Top: bounds.Range.Y > imagePath.Diameter * rangeTolerance ? textFont.Size + measurePaint.StrokeWidth * 2 : measurePaint.StrokeWidth,
                Right: bounds.Range.Y > imagePath.Diameter * rangeTolerance ? measurePaint.StrokeWidth * 6 : measurePaint.StrokeWidth,
                Bottom: bounds.Range.X > imagePath.Diameter * rangeTolerance ? textFont.Size + measurePaint.StrokeWidth * 6 : measurePaint.StrokeWidth
                );

            var canvasRect = new SKRect(0, 0, bounds.Range.X + margin.Left + margin.Right, bounds.Range.Y + margin.Top + margin.Bottom);

            using (var stream = new MemoryStream())
            {
                using (var canvas = SKSvgCanvas.Create(canvasRect, stream))
                {
                    using var backgroundPaint = new SKPaint
                    {
                        Color = white,
                    };

                    using var marginPaint = new SKPaint
                    {
                        Color = white,
                    };

                    using var basePathPaint = new SKPaint
                    {
                        Color = blueishGrey,
                        StrokeWidth = imagePath.Diameter,
                        StrokeCap = SKStrokeCap.Round,
                    };

                    using var dependentPathPaint = new SKPaint
                    {
                        Color = redTransparent,
                        StrokeWidth = imagePath.Diameter,
                        StrokeCap = SKStrokeCap.Round,
                    };

                    using var selectedPathPaint = new SKPaint
                    {
                        Color = red,
                        StrokeWidth = imagePath.Diameter,
                        StrokeCap = SKStrokeCap.Round,
                    };

                    using var markerPaint = new SKPaint
                    {
                        Color = red,
                    };

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
                        }
                    }
                    #endregion

                    #region Draw ImagePath Labels
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

                    if (bounds.Range.X > imagePath.Diameter * rangeTolerance)
                    {
                        var rangeXStart = new SKPoint(margin.Left, xLineHeight);
                        var rangeXEnd = new SKPoint(margin.Left + bounds.Range.X, xLineHeight);
                        canvas.DrawLine(rangeXStart, rangeXEnd, measurePaint);

                        canvas.DrawLine(rangeXStart + new SKPoint(halfStrokeWidth, halfCapWidth), rangeXStart + new SKPoint(halfStrokeWidth, -halfCapWidth), measurePaint);
                        canvas.DrawLine(rangeXEnd + new SKPoint(-halfStrokeWidth, halfCapWidth), rangeXEnd + new SKPoint(-halfStrokeWidth, -halfCapWidth), measurePaint);

                        canvas.DrawText($"X: {originalBounds.Range.X.ToString("0.#")}", bounds.Range.X * 0.5f + margin.Left, fullHeight - measurePaint.StrokeWidth, SKTextAlign.Center, textFont, measurePaint);
                    }

                    if (bounds.Range.Y > imagePath.Diameter * rangeTolerance)
                    {
                        var rangeYStart = new SKPoint(yLineWidth, margin.Top);
                        var rangeYEnd = new SKPoint(yLineWidth, margin.Top + bounds.Range.Y);
                        canvas.DrawLine(rangeYStart, rangeYEnd, measurePaint);
                        canvas.DrawLine(rangeYStart + new SKPoint(halfCapWidth, halfStrokeWidth), rangeYStart + new SKPoint(-halfCapWidth, halfStrokeWidth), measurePaint);
                        canvas.DrawLine(rangeYEnd + new SKPoint(halfCapWidth, -halfStrokeWidth), rangeYEnd + new SKPoint(-halfCapWidth, -halfStrokeWidth), measurePaint);

                        canvas.DrawText($"Y: {originalBounds.Range.Y.ToString("0.#")}", fullWidth - measurePaint.StrokeWidth, textFont.Size + measurePaint.StrokeWidth, SKTextAlign.Right, textFont, measurePaint);
                    }

                    if (bounds.Range.Z > imagePath.Diameter * rangeTolerance)
                    {
                        var markerCenter = new SKPoint(yLineWidth, xLineHeight);
                        canvas.DrawCircle(markerCenter, radius: halfCapWidth, measurePaint);
                        canvas.DrawCircle(markerCenter, radius: halfCapWidth - measurePaint.StrokeWidth, marginPaint);

                        canvas.DrawText($"Z: {originalBounds.Range.Z.ToString("0.#")}", fullWidth - measurePaint.StrokeWidth, fullHeight - measurePaint.StrokeWidth, SKTextAlign.Right, textFont, measurePaint);
                    }
                    #endregion
                }

                //Important: Only return stream after disposing canvas (SVG closing tag)
                return stream.ToArray();
            }

        }
    }
}