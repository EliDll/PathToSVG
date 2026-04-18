using SkiaSharp;
using System.Globalization;
using System.Numerics;

#nullable enable

namespace PathToSVG
{
    static class Util
    {
        //Suitable float error term
        public const float FLOAT_EPS = 1e-6f;

        public static int Clamp(this int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static bool IsMultipleModulo(this float value, float mode)
        {
            var modulo = value % mode;
            //Return true if almost zero, or almost mode
            return Math.Abs(modulo) < FLOAT_EPS || Math.Abs(modulo - mode) < FLOAT_EPS;
        }

        public static bool NonZeroLength(this Vector3 vec) => vec.Length() > FLOAT_EPS;

        public static bool NonZeroLength(this Vector2 vec) => vec.Length() > FLOAT_EPS;

        public static bool NonZeroDistance(Vector3 vec1, Vector3 vec2) => Vector3.Distance(vec1, vec2) > FLOAT_EPS;

        public static bool NonZeroDistance(Vector2 vec1, Vector2 vec2) => Vector2.Distance(vec1, vec2) > FLOAT_EPS;

        public static bool AreCollinear(Vector3 vec1, Vector3 vec2)
        {
            var cross = Vector3.Cross(Vector3.Normalize(vec1), Vector3.Normalize(vec2));
            return cross.Length() < FLOAT_EPS;
        }

        public static float AngleBetweenDeg(Vector3 vec1, Vector3 vec2)
        {
            var dot = Vector3.Dot(Vector3.Normalize(vec1), Vector3.Normalize(vec2));
            return RadToDeg(MathF.Acos(dot));
        }

        //Gets viewplane components of Vector3
        //Assumed to be encoded in corresponding coordinate basis
        public static Vector2 GetImage2DComponents(this Vector3 imagePt)
        {
            return new Vector2(imagePt.X, imagePt.Y);
        }

        public static bool IsValidLine3D(this Line3D line3D)
        {
            return NonZeroDistance(line3D.Start, line3D.End);
        }

        public static bool IsValidImageLine(this ImageLine imageLine)
        {
            if (IsValidLine3D(imageLine.Line3D))
            {
                var imageStart2D = imageLine.ImageStart.GetImage2DComponents();
                var imageEnd2D = imageLine.ImageEnd.GetImage2DComponents();
                return NonZeroDistance(imageStart2D, imageEnd2D);
            }
            else
            {
                return false;
            }
        }

        public static bool IsValidArc3D(this Arc3D arc3D)
        {
            var hasRadius = NonZeroDistance(arc3D.Center, arc3D.Start);

            //Either there is a difference between start or end, or it is exact (non-zero) multiple of 360 degrees sweep
            var hasSweep = NonZeroDistance(arc3D.Start, arc3D.End) || (arc3D.SweepDeg > FLOAT_EPS && Math.Abs(arc3D.SweepDeg % 360) < FLOAT_EPS);

            return hasRadius && hasSweep;
        }

        public static bool IsValidImageArc(this ImageArc imageArc)
        {
            if (IsValidArc3D(imageArc.Arc3D))
            {
                var imageCenter2D = imageArc.ImageCenter.GetImage2DComponents();
                //Check if any image sweep sample is different from the projected arc center (i.e. forms a valid projection shape)
                return imageArc.ImageSweepSamples.Any(x => NonZeroDistance(imageCenter2D, x.GetImage2DComponents()));
            }
            else
            {
                return false;
            }
        }

        public static SKPoint ToSKPoint(this Vector3 pt, Bounds bounds, Margin margin)
        {
            var boundsX = pt.X - bounds.Min.X;
            var boundsY = pt.Y - bounds.Min.Y;

            var canvasX = margin.Left + boundsX;
            var canvasY = (bounds.Range.Y + margin.Top) - boundsY; //y points downwards in canvas

            return new SKPoint(canvasX, canvasY);
        }

        public static Vector3 RotateAround(this Vector3 vector, Vector3 axis, float angleDeg)
        {
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), DegToRad(angleDeg));
            return Vector3.Transform(vector, rotation);
        }

        public static float DegToRad(float deg) => deg * ((float)Math.PI / 180.0f);

        public static float RadToDeg(float rad) => rad * (180.0f / (float)Math.PI);

        public static string ToFormattedString(this float angle, string decimalSeparator, int maximumFractionDigits)
        {
            var format = new NumberFormatInfo { NumberDecimalSeparator = decimalSeparator };
            var pattern = "0." + new string('#', maximumFractionDigits);
            return angle.ToString(pattern, format);
        }

        public static float GetArcWidth(float arcRadius, float arcAngleDeg)
        {
            double am = Math.Min(Math.Abs(DegToRad(arcAngleDeg)), Math.PI / 2.0);
            return arcRadius * (float)Math.Tan(am / 2.0);
        }

        //Returns the resulting image point as Vector3(X: viewplane right, Y: viewplane down, Z: depth from viewplane)
        public static Vector3 Project(this Vector3 point3D, CoordinateSystem projection)
        {
            return new Vector3(
                x: Vector3.Dot(point3D, projection.AxisX),
                y: Vector3.Dot(point3D, projection.AxisY),
                z: Vector3.Dot(point3D, projection.AxisZ)
                );
        }

        //Orthonormal basis via Gram-Schmidt
        //Returns null if result is degenerate, i.e. (almost) parallel
        //Otherwise returns a normalized vector perpendicular to the specified axis
        public static Vector3? GetPerpendicularTo(this Vector3 direction, Vector3 axis)
        {
            var normalizedAxis = Vector3.Normalize(axis); //just to be sure
            var normalizedDirection = Vector3.Normalize(direction); //is expected to not be normalized
            var perpendicular = normalizedDirection - Vector3.Dot(normalizedDirection, normalizedAxis) * normalizedAxis;
            if (perpendicular.NonZeroLength())
            {
                return Vector3.Normalize(perpendicular);
            }
            else
            {
                return null;
            }
        }

        public static CoordinateSystem GetProjection(this Path3D path3D, View view, Anchor anchor)
        {
            var worldRight = new Vector3(1, 0, 0);
            var worldUp = new Vector3(0, 0, 1);
            var worldIn = new Vector3(0, 1, 0);

            var projectionX = worldRight;
            var projectionY = worldUp;
            var projectionZ = worldIn;

            int? bestXLineIdx = null;

            switch (anchor)
            {
                case Anchor.FirstLine:
                    {
                        //Choose first (real) line (if it exists)
                        for (int i = 0; i < path3D.Pieces.Count; i++)
                        {
                            if (path3D.Pieces[i] is Line3D line)
                            {
                                if (NonZeroDistance(line.End, line.Start))
                                {
                                    bestXLineIdx = i;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                case Anchor.LongestLine:
                    {
                        //Choose longest (real) line (if it exists)
                        var longestLineLen = FLOAT_EPS;
                        int? longestLineIdx = null;

                        for (int i = 0; i < path3D.Pieces.Count; i++)
                        {
                            if (path3D.Pieces[i] is Line3D line)
                            {
                                var lineLen = Vector3.Distance(line.End, line.Start);
                                if (lineLen > longestLineLen)
                                {
                                    longestLineIdx = i;
                                    longestLineLen = lineLen;
                                }
                            }
                        }

                        bestXLineIdx = longestLineIdx;
                        break;
                    }
                case Anchor.LongestCollinearGroup:
                    {
                        var collinearGroups = new Dictionary<Vector3, IList<(Line3D Line, int Idx)>>();

                        for (int i = 0; i < path3D.Pieces.Count; i++)
                        {
                            var piece = path3D.Pieces[i];
                            var currentKeys = collinearGroups.Keys;
                            if (piece is Line3D line)
                            {
                                var lineDir = line.End - line.Start;
                                if (currentKeys.Any(key => AreCollinear(key, lineDir)))
                                {
                                    //Append to existing group
                                    var targetKey = currentKeys.FirstOrDefault(key => AreCollinear(key, lineDir));
                                    var currentValues = collinearGroups[targetKey];
                                    collinearGroups[targetKey] = [.. currentValues, (Line: line, Idx: i)];
                                }
                                else
                                {
                                    //Create new group
                                    collinearGroups.Add(Vector3.Normalize(lineDir), [(Line: line, Idx: i)]);
                                }
                            }
                        }

                        var longestSpan = FLOAT_EPS;
                        int? longestGroupIdx = null;

                        var groupKeys = collinearGroups.Keys.ToList();

                        for (int i = 0; i < groupKeys.Count; i++)
                        {
                            var key = groupKeys[i];
                            var entries = collinearGroups[key];

                            var commonAxis = key;

                            IList<Vector3> pts = [];
                            foreach (var entry in entries)
                            {
                                pts.Add(entry.Line.Start);
                                pts.Add(entry.Line.End);
                            }

                            var distancesOnAxis = pts.Select(x => Vector3.Dot(x, commonAxis)).DefaultIfEmpty(0).ToList();

                            var min = distancesOnAxis.Min();
                            var max = distancesOnAxis.Max();
                            var spanLen = max - min;

                            if (spanLen > longestSpan)
                            {
                                longestSpan = spanLen;
                                longestGroupIdx = i;
                            }
                        }

                        if (longestGroupIdx is int groupIdx)
                        {
                            //Choose longest line in this group as projectionX
                            var longestGroupKey = groupKeys[groupIdx];

                            var longestGroupEntries = collinearGroups[longestGroupKey];

                            var longestLineLen = FLOAT_EPS;
                            int? longestLineIdx = null;

                            for (int i = 0; i < longestGroupEntries.Count; i++)
                            {
                                var line = longestGroupEntries[i].Line;
                                var lineLen = Vector3.Distance(line.End, line.Start);
                                if (lineLen > longestLineLen)
                                {
                                    longestLineIdx = i;
                                    longestLineLen = lineLen;
                                }
                            }

                            if (longestLineIdx is int lineIdx)
                            {
                                bestXLineIdx = longestGroupEntries[lineIdx].Idx;
                            }
                        }

                        break;
                    }
                default:
                    break;

            }

            if (bestXLineIdx is int idx)
            {
                var bestXLine = (Line3D)path3D.Pieces[idx];

                # region Claude suggestion: projectionX should preferrably be screen right
                var rawDir = Vector3.Normalize(bestXLine.End - bestXLine.Start);

                // Anchor projectionX to worldRight so that reversing the picked
                // line (longest vs first) doesn't mirror the whole projection.
                projectionX = Vector3.Dot(rawDir, worldRight) >= 0 ? rawDir : -rawDir;
                #endregion

                //Attempt to determine other projection axes by simultaneously iterating over previous and next pieces
                var currentIndexOffset = 1;
                var nextIndex = idx + currentIndexOffset;
                var prevIndex = idx - currentIndexOffset;

                while (nextIndex < path3D.Pieces.Count || prevIndex >= 0)
                {
                    var nextCandidate = nextIndex < path3D.Pieces.Count ? path3D.Pieces[nextIndex] : null;
                    if (nextCandidate != null)
                    {
                        if (nextCandidate is Arc3D nextArc)
                        {
                            //Use arc axis (guaranteed to be perpendicular to projectionX) as projectionZ
                            projectionZ = Vector3.Normalize(nextArc.Axis);
                            projectionY = Vector3.Cross(projectionZ, projectionX);
                            break;
                        }
                        else if (nextCandidate is Line3D nextLine)
                        {
                            //Attempt to use line direction to create orthonormal basis (if sufficiently non-parallel)
                            var nextDir = nextLine.End - nextLine.Start;
                            var nextPerpendicular = nextDir.GetPerpendicularTo(projectionX);
                            if (nextPerpendicular is Vector3 newProjectionY)
                            {
                                projectionY = newProjectionY;
                                projectionZ = Vector3.Cross(projectionX, projectionY);
                                break;
                            }
                        }
                    }

                    var prevCandidate = prevIndex >= 0 ? path3D.Pieces[prevIndex] : null;
                    if (prevCandidate != null)
                    {
                        if (prevCandidate is Arc3D prevArc)
                        {
                            //Use arc axis (guaranteed to be perpendicular to projectionX) as projectionZ
                            projectionZ = Vector3.Normalize(prevArc.Axis);
                            projectionY = Vector3.Cross(projectionZ, projectionX);
                            break;
                        }
                        else if (prevCandidate is Line3D prevLine)
                        {
                            //Attempt to use line direction to create orthonormal basis (if sufficiently non-parallel)
                            var prevDir = prevLine.Start - prevLine.End; //Reverse direction for previous lines (results in more intuitive orthonormal basis)
                            var prevPerpendicular = prevDir.GetPerpendicularTo(projectionX);
                            if (prevPerpendicular is Vector3 newProjectionY)
                            {
                                projectionY = newProjectionY;
                                projectionZ = Vector3.Cross(projectionX, projectionY);
                                break;
                            }
                        }
                    }

                    currentIndexOffset++;
                    nextIndex = idx + currentIndexOffset;
                    prevIndex = idx - currentIndexOffset;
                }
            }

            //Check for degenerate basis (if only projectionX was set from path geometry)
            var perpendicularY = projectionY.GetPerpendicularTo(projectionX);
            if (perpendicularY is Vector3 perpendicularProjectionY)
            {
                //Make sure final projectionY is actually perpendicular to projectionX
                projectionY = perpendicularProjectionY;
            }
            else
            {
                //Currently set projectionY (worldUp) is parallel to projectionX, try worldRight
                var newPerpendicularY = worldRight.GetPerpendicularTo(projectionX);
                if (newPerpendicularY == null)
                {
                    //worldRight is also parallel, use worldIn (guranteed to be non-parallel at this point)
                    newPerpendicularY = worldIn.GetPerpendicularTo(projectionX);
                }

                //Should always be the case, see above comment (Still check for robustness)
                if (newPerpendicularY is Vector3 newProjectionY)
                {
                    projectionY = newProjectionY;
                }
            }

            //Make sure projectionZ is still perpendicular at this point
            projectionZ = Vector3.Cross(projectionX, projectionY);

            #region Claude suggestion: projectionY should preferrably be screen up
            // For Front/Side views the natural screen-up is worldUp (Z axis).
            // For Top view the camera looks down worldUp, so screen-up becomes worldRight instead.
            // In each case: if the geometry-derived projectionY points against the hint, flip it
            // (and flip projectionZ accordingly to preserve right-handedness).
            var screenUpHint = view switch
            {
                View.Front => worldUp,
                View.Side => worldUp,
                View.Top => worldIn,
                _ => throw new NotImplementedException()
            };

            // For Front/Side: check projectionY against screen-up hint
            // For Top: check projectionZ, since that becomes AxisY (screen-up) in the Top view
            if (view == View.Top)
            {
                if (Vector3.Dot(projectionZ, worldIn) < 0)
                {
                    projectionZ = -projectionZ;
                }
            }
            else
            {
                if (Vector3.Dot(projectionY, worldUp) < 0)
                {
                    projectionY = -projectionY;
                    projectionZ = -projectionZ;
                }
            }
            #endregion

            #region Claude suggestion: Recompute AxisZ for alternative views to ensure consistent depth transform
            // Now build each view's coordinate system so that AxisZ always points
            // toward the viewer (positive = in front), using a stable cross product.
            return view switch
            {
                // Front: look along -projectionZ (into screen), screen-right = projectionX, screen-up = projectionY
                View.Front => new CoordinateSystem(
                    AxisX: projectionX,
                    AxisY: projectionY,
                    AxisZ: projectionZ),

                // Side: rotate 90° — projectionZ becomes screen-right, projectionX recedes into screen
                // AxisY stays the same (still world-up aligned)
                // AxisZ for depth = cross(AxisX, AxisY) to guarantee right-handed, pointing toward viewer
                View.Side => new CoordinateSystem(
                    AxisX: projectionZ,
                    AxisY: projectionY,
                    AxisZ: Vector3.Cross(projectionZ, projectionY)),  // = -projectionX if basis is orthonormal

                // Top: look down -projectionY, screen-right = projectionX, screen-up = projectionZ (world-forward aligned)
                // AxisZ for depth = cross(AxisX, AxisY) to guarantee right-handed, pointing toward viewer
                View.Top => new CoordinateSystem(
                    AxisX: projectionX,
                    AxisY: projectionZ,
                    AxisZ: Vector3.Cross(projectionX, projectionZ)),  // = -projectionY if basis is orthonormal

                _ => throw new NotImplementedException()
            };
            #endregion
        }

        public static ImagePath ToImagePath(this Path3D path3D, View view, Anchor anchor, float degreesPerSample)
        {
            var pathRadius = path3D.Diameter * 0.5f;

            var projection = path3D.GetProjection(view, anchor);

            IList<ImagePiece> imagePieces = [];

            for (int pieceIdx = 0; pieceIdx < path3D.Pieces.Count; pieceIdx++)
            {
                var piece3D = path3D.Pieces[pieceIdx];
                if (piece3D is Line3D line3D)
                {
                    var imageStart = line3D.Start.Project(projection);
                    var imageEnd = line3D.End.Project(projection);

                    var dir3D = Vector3.Normalize(line3D.End - line3D.Start);

                    var straightLen = Vector3.Distance(line3D.Start, line3D.End);
                    var outerLen = straightLen;

                    IList<Vector3> imageStartBounds = [imageStart];
                    IList<Vector3> imageEndBounds = [imageEnd];

                    if (pieceIdx != 0 && path3D.Pieces[pieceIdx - 1] is Arc3D startArc)
                    {
                        var startArcCoreRadius = Vector3.Distance(startArc.Start, startArc.Center);
                        var startArcOuterRadius = startArcCoreRadius + pathRadius;
                        var startArcWidth = GetArcWidth(startArcOuterRadius, startArc.SweepDeg);
                        outerLen += startArcWidth;

                        //Don't include this in bounds for now, as this tangent length will exceed the total dimensions of the geometry for angles greater than 90 degrees
                        //var elongatedStart3D = line3D.Start - dir3D * startArcWidth;
                        //imageStartBounds.Add(elongatedStart3D.Project(projection));
                    }

                    if (pieceIdx != path3D.Pieces.Count - 1 && path3D.Pieces[pieceIdx + 1] is Arc3D endArc)
                    {
                        var endArcCoreRadius = Vector3.Distance(endArc.Start, endArc.Center);
                        var endArcOuterRadius = endArcCoreRadius + pathRadius;
                        var endArcWidth = GetArcWidth(endArcOuterRadius, endArc.SweepDeg);
                        outerLen += endArcWidth;

                        //Don't include this in bounds for now, as this tangent length will exceed the total dimensions of the geometry for angles greater than 90 degrees
                        //var elongatedEnd3D = line3D.End + dir3D * endArcWidth;
                        //imageEndBounds.Add(elongatedEnd3D.Project(projection));
                    }

                    var imageStart2D = imageStart.GetImage2DComponents();
                    var imageEnd2D = imageEnd.GetImage2DComponents();
                    if (NonZeroDistance(imageStart2D, imageEnd2D))
                    {
                        var imageDir = Vector3.Normalize(imageEnd - imageStart);
                        var imageOrthoDir = new Vector2(-imageDir.Y, imageDir.X);
                        var outerTransform = new Vector3(imageOrthoDir.X, imageOrthoDir.Y, 0) * path3D.Diameter * 0.5f;

                        //Note that this is a simplified heuristic to add "fake" outer bounds to the 3D cylinder, solely based on 2D transofmrations (i.e. will not affect depth (Z) bounds)
                        var outerBoundsStartTop = imageStartBounds.Select(x => x + outerTransform).ToList();
                        var outerBoundsStartBottom = imageStartBounds.Select(x => x - outerTransform).ToList();
                        imageStartBounds = [.. imageStartBounds, .. outerBoundsStartTop, .. outerBoundsStartBottom];

                        var outerBoundsEndTop = imageEndBounds.Select(x => x + outerTransform).ToList();
                        var outerBoundsEndBottom = imageEndBounds.Select(x => x - outerTransform).ToList();
                        imageEndBounds = [.. imageEndBounds, .. outerBoundsEndTop, .. outerBoundsEndBottom];
                    }

                    var imageLine = new ImageLine(
                        Line3D: line3D,
                        ImageStart: imageStart,
                        ImageEnd: imageEnd,
                        ImageStartBounds: imageStartBounds,
                        ImageEndBounds: imageEndBounds,
                        StraightLen3D: straightLen,
                        OuterLen3D: outerLen
                        );
                    imagePieces.Add(imageLine);
                }
                else if (piece3D is Arc3D arc3D)
                {
                    var imageCenter = arc3D.Center.Project(projection);

                    var sweepStartCoreVec = arc3D.Start - arc3D.Center;
                    var sweepStartDir = Vector3.Normalize(sweepStartCoreVec);
                    var sweepStartOuterVec = sweepStartCoreVec + sweepStartDir * pathRadius;
                    var sweepStartInnerVec = sweepStartCoreVec - sweepStartDir * pathRadius;

                    IList<Vector3> sweepVecSamples = [sweepStartCoreVec];
                    IList<Vector3> sweepVecBounds = [sweepStartOuterVec, sweepStartInnerVec];

                    if (degreesPerSample > 0)
                    {
                        var currentDegrees = degreesPerSample;
                        while (currentDegrees < Math.Abs(arc3D.SweepDeg))
                        {
                            var sweepSampleCoreVec = sweepStartCoreVec.RotateAround(arc3D.Axis, arc3D.SweepDeg > 0 ? currentDegrees : -currentDegrees);
                            var sweepSampleDir = Vector3.Normalize(sweepSampleCoreVec);
                            var sweepSampleOuterVec = sweepSampleCoreVec + sweepSampleDir * pathRadius;
                            var sweepSampleInnerVec = sweepSampleCoreVec - sweepSampleDir * pathRadius;

                            sweepVecSamples.Add(sweepSampleCoreVec);
                            sweepVecBounds.Add(sweepSampleOuterVec);
                            sweepVecBounds.Add(sweepSampleInnerVec);
                            currentDegrees += degreesPerSample;
                        }
                    }

                    var sweepEndCoreVec = arc3D.End - arc3D.Center;
                    var sweepEndDir = Vector3.Normalize(sweepEndCoreVec);
                    var sweepEndOuterVec = sweepEndCoreVec + sweepEndDir * pathRadius;
                    var sweepEndInnerVec = sweepEndCoreVec - sweepEndDir * pathRadius;

                    sweepVecSamples.Add(sweepEndCoreVec);
                    sweepVecBounds.Add(sweepEndOuterVec);
                    sweepVecBounds.Add(sweepEndInnerVec);

                    var imageSweepSamples = sweepVecSamples.Select(x => arc3D.Center + x).Select(x => x.Project(projection)).ToList();
                    var imageSampleBounds = sweepVecBounds.Select(x => arc3D.Center + x).Select(x => x.Project(projection)).ToList();

                    var imageArc = new ImageArc(
                        Arc3D: arc3D,
                        ImageCenter: imageCenter,
                        ImageSweepSamples: imageSweepSamples,
                        ImageSampleBounds: imageSampleBounds
                        );
                    imagePieces.Add(imageArc);
                }
            }

            return new ImagePath(Pieces: imagePieces, Diameter: path3D.Diameter);
        }

        public static Bounds GetBounds(this ImagePath path)
        {
            IList<Vector3> pathBounds = [];
            foreach (var piece in path.Pieces)
            {
                if (piece is ImageLine line)
                {
                    pathBounds = pathBounds.Concat(line.ImageStartBounds).Concat(line.ImageEndBounds).ToList();
                }
                else if (piece is ImageArc arc)
                {
                    pathBounds = pathBounds.Concat(arc.ImageSampleBounds).ToList();
                }
            }
            if (pathBounds.Count == 0)
            {
                pathBounds.Add(new Vector3(0, 0, 0));
            }

            var min = new Vector3(
                x: pathBounds.Select(vec => vec.X).Min(),
                y: pathBounds.Select(vec => vec.Y).Min(),
                z: pathBounds.Select(vec => vec.Z).Min()
                );

            var max = new Vector3(
                x: pathBounds.Select(vec => vec.X).Max(),
                y: pathBounds.Select(vec => vec.Y).Max(),
                z: pathBounds.Select(vec => vec.Z).Max()
                );

            var range = new Vector3(
                x: max.X - min.X,
                y: max.Y - min.Y,
                z: max.Z - min.Z
                );

            var center = new Vector3(
                x: min.X + range.X * 0.5f,
                y: min.Y + range.Y * 0.5f,
                z: min.Z + range.Z * 0.5f
                );

            return new Bounds(Min: min, Max: max, Range: range, Center: center);
        }

        public static IList<ImagePiece> GetWithOffset(this IList<ImagePiece> originalPieces, Vector3 offset)
        {
            IList<ImagePiece> modifiedPieces = [];

            foreach (var originalPiece in originalPieces)
            {
                if (originalPiece is ImageLine originalLine)
                {
                    var newLine = originalLine with
                    {
                        ImageEnd = originalLine.ImageEnd + offset,
                        ImageStart = originalLine.ImageStart + offset,
                        ImageStartBounds = originalLine.ImageStartBounds.Select(x => x + offset).ToList(),
                        ImageEndBounds = originalLine.ImageEndBounds.Select(x => x + offset).ToList(),
                    };
                    modifiedPieces.Add(newLine);
                }
                else if (originalPiece is ImageArc originalArc)
                {
                    var newArc = originalArc with
                    {
                        ImageCenter = originalArc.ImageCenter + offset,
                        ImageSweepSamples = originalArc.ImageSweepSamples.Select(x => x + offset).ToList(),
                        ImageSampleBounds = originalArc.ImageSampleBounds.Select(x => x + offset).ToList()
                    };
                    modifiedPieces.Add(newArc);
                }
            }

            return modifiedPieces;
        }

        public static bool LinearBetween(this Vector3 target, Vector3 start, Vector3 end, float? precalculatedDistanceBetween = null)
        {
            var distanceBetween = precalculatedDistanceBetween != null ? precalculatedDistanceBetween.Value : (end - start).Length();
            var combinedDistance = Vector3.Distance(target, start) + Vector3.Distance(target, end);
            return Math.Abs(combinedDistance - distanceBetween) < FLOAT_EPS;
        }

        public static bool AreOverlapping(ImageLine firstLine, ImageLine secondLine)
        {
            var firstDir = firstLine.ImageEnd - firstLine.ImageStart;
            var firstLen = firstDir.Length();
            var secondDir = secondLine.ImageEnd - secondLine.ImageStart;
            var secondLen = secondDir.Length();
            if (AreCollinear(firstDir, secondDir))
            {
                //Check if either end point of one line lies exactly within the other
                return firstLine.ImageStart.LinearBetween(secondLine.ImageEnd, secondLine.ImageStart, precalculatedDistanceBetween: secondLen)
                    || firstLine.ImageEnd.LinearBetween(secondLine.ImageEnd, secondLine.ImageStart, precalculatedDistanceBetween: secondLen)
                    || secondLine.ImageStart.LinearBetween(firstLine.ImageEnd, firstLine.ImageStart, precalculatedDistanceBetween: firstLen)
                    || secondLine.ImageEnd.LinearBetween(firstLine.ImageEnd, firstLine.ImageStart, precalculatedDistanceBetween: firstLen);
            }
            else
            {
                //Do not point in the same direction
                return false;
            }
        }

        public static ImagePath ApplyOverlapCorrection(this ImagePath inputPath, float shiftBy)
        {
            var MAX_SHIFT_ATTEMPTS = 10;

            IList<ImagePiece> finalPieces = [];

            IList<ImagePiece> remainingPieces = inputPath.Pieces;

            while (remainingPieces.Count > 0)
            {
                var currentPiece = remainingPieces.First();
                remainingPieces = remainingPieces.Skip(1).ToList();

                if (currentPiece is ImageLine currentLine)
                {
                    var modifiedLine = currentLine;

                    var remainingLines = remainingPieces.Where(x => x is ImageLine).Select(x => (ImageLine)x).ToList();

                    if (remainingLines.Count > 0)
                    {
                        var currentLineDir = Vector3.Normalize(currentLine.ImageEnd - currentLine.ImageStart);

                        var previousLines = finalPieces.Where(x => x is ImageLine).Select(x => (ImageLine)x).ToList();

                        for (int i = 0; i < MAX_SHIFT_ATTEMPTS; i++)
                        {
                            remainingLines = remainingPieces.Where(x => x is ImageLine).Select(x => (ImageLine)x).ToList();
                            var nextLine = remainingLines.First();

                            if (previousLines.Any(x => AreOverlapping(x, nextLine)))
                            {
                                var shiftOffset = currentLineDir * shiftBy;

                                //Iteratively apply shift offset to current line
                                modifiedLine = modifiedLine with
                                {
                                    ImageEnd = modifiedLine.ImageEnd + shiftOffset,
                                    ImageEndBounds = modifiedLine.ImageEndBounds.Select(x => x + shiftOffset).ToList(),
                                };

                                //Iteratively apply shift offset to all remaining pieces (directly needed in next check)
                                remainingPieces = remainingPieces.GetWithOffset(shiftOffset);
                            }
                        }
                    }

                    finalPieces.Add(modifiedLine);
                }
                else
                {
                    finalPieces.Add(currentPiece);
                }
            }

            IList<ImagePiece> modifiedPieces = [];

            return inputPath with { Pieces = finalPieces };
        }
    }
}