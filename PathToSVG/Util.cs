using SkiaSharp;
using System.Numerics;
using Types;

#nullable enable

namespace Utils
{
    static class Util
    {
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

        public static CoordinateSystem GetProjection(this Path3D path3D, View view, bool preferLongestLine)
        {
            IList<Line3D> lines = path3D.Pieces.Where(x => x is Line3D).Select(x => (Line3D)x).ToList();

            var worldForward = new Vector3(1, 0, 0);
            var worldUp = new Vector3(0, 0, 1);


            var projectionX = worldForward;
            var projectionY = worldUp;



            if (lines.Count > 0)
            {
                //Determine first projection axis (X)

                var longestIndex = 0;

                if (preferLongestLine)
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        var currentLine = lines[i];
                        var currentLongest = lines[longestIndex];
                        if (Vector3.Distance(currentLine.End, currentLine.Start) > Vector3.Distance(currentLongest.End, currentLongest.Start))
                        {
                            longestIndex = i;
                        }
                    }
                }

                var longest = lines[longestIndex];
                projectionX = Vector3.Normalize(longest.End - longest.Start);

                if(lines.Count > 1)
                {
                    //Find most suitable second projection axis by looking at adjacent lines first
                    var currentIndexOffset = 1;

                    var nextIndex = longestIndex + currentIndexOffset;
                    var prevIndex = longestIndex - currentIndexOffset;

                    var eps = 1e-6f;

                    while (nextIndex < lines.Count || prevIndex >= 0)
                    {
                        var nextCandidate = nextIndex < lines.Count ? lines[nextIndex] : null;
                        var prevCandidate = prevIndex >= 0 ? lines[prevIndex] : null;

                        
                        if (nextCandidate != null)
                        {
                            var dir = nextCandidate.End - nextCandidate.Start;
                            //Orthonormal basis via Gram-Schmidt
                            var perpendicular = dir - Vector3.Dot(dir, projectionX) * projectionX;
                            if(perpendicular.Length() > eps * dir.Length())
                            {
                                //Truly perpendicular
                                projectionY = Vector3.Normalize(perpendicular);
                                break;
                            }
                        }
                        if(prevCandidate != null)
                        {
                            var dir = prevCandidate.Start - prevCandidate.End;
                            //Orthonormal basis via Gram-Schmidt
                            var perpendicular = dir - Vector3.Dot(dir, projectionX) * projectionX;
                            if (perpendicular.Length() > eps * dir.Length())
                            {
                                //Truly perpendicular
                                projectionY = Vector3.Normalize(perpendicular);
                                break;
                            }
                        }

                        currentIndexOffset++;
                        nextIndex = longestIndex + currentIndexOffset;
                        prevIndex = longestIndex - currentIndexOffset;
                    }

                }
            }

            var projectionZ = Vector3.Cross(projectionY, projectionX); // no need to normalize, already unit length

            return view switch
            {
                View.Front => new CoordinateSystem(AxisX: projectionX, AxisY: projectionY, AxisZ: projectionZ),
                View.Side => new CoordinateSystem(AxisX: projectionZ, AxisY: projectionY, AxisZ: projectionX), //flip x and z
                View.Top => new CoordinateSystem(AxisX: projectionX, AxisY: projectionZ, AxisZ: projectionY), //flip y and z
                _ => throw new NotImplementedException()
            };
        }

        public static ImagePath ToImagePath(this Path3D path3D, View view, bool preferLongestLine, float degreesPerSample)
        {
            var eps = 1e-6f;

            var pathRadius = path3D.Diameter * 0.5f;

            var projection = path3D.GetProjection(view, preferLongestLine);

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

                    IList<Vector3> imageBounds = [];
                    imageBounds.Add(imageStart);
                    imageBounds.Add(imageEnd);

                    if (pieceIdx != 0 && path3D.Pieces[pieceIdx - 1] is Arc3D startArc)
                    {
                        var startArcCoreRadius = Vector3.Distance(startArc.Start, startArc.Center);
                        var startArcOuterRadius = startArcCoreRadius + pathRadius;
                        var startArcWidth = GetArcWidth(startArcOuterRadius, startArc.SweepDeg);

                        outerLen += startArcWidth;
                        var elongatedStart3D = line3D.Start - dir3D * startArcWidth;
                        imageBounds.Add(elongatedStart3D.Project(projection));
                    }

                    if (pieceIdx != path3D.Pieces.Count - 1 && path3D.Pieces[pieceIdx + 1] is Arc3D endArc)
                    {
                        var endArcCoreRadius = Vector3.Distance(endArc.Start, endArc.Center);
                        var endArcOuterRadius = endArcCoreRadius + pathRadius;
                        var endArcWidth = GetArcWidth(endArcOuterRadius, endArc.SweepDeg);

                        outerLen += endArcWidth;
                        var elongatedEnd3D = line3D.End + dir3D * endArcWidth;
                        imageBounds.Add(elongatedEnd3D.Project(projection));
                    }

                    var imageStart2D = new Vector2(imageStart.X, imageStart.Y);
                    var imageEnd2D = new Vector2(imageEnd.X, imageEnd.Y);
                    if (Vector2.Distance(imageStart2D, imageEnd2D) > eps)
                    {
                        var imageDir = Vector3.Normalize(imageEnd - imageStart);
                        var imageOrthoDir = new Vector2(-imageDir.Y, imageDir.X);
                        var outerTransform = new Vector3(imageOrthoDir.X, imageOrthoDir.Y, 0) * path3D.Diameter * 0.5f;

                        //Note that this is a simplified heuristic to add "fake" outer bounds to the 3D cylinder, solely based on 2D transofmrations (i.e. will not affect depth (Z) bounds)
                        var outerBoundsTop = imageBounds.Select(x => x + outerTransform).ToList();
                        var outerBoundsBottom = imageBounds.Select(x => x - outerTransform).ToList();
                        imageBounds = [.. imageBounds, .. outerBoundsTop, .. outerBoundsBottom];
                    }

                    var imageLine = new ImageLine(Line3D: line3D, ImageStart: imageStart, ImageEnd: imageEnd, ImageBounds: imageBounds, StraightLen3D: straightLen, OuterLen3D: outerLen);
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

                    var currentDegrees = degreesPerSample;
                    while (currentDegrees < Math.Abs(arc3D.SweepDeg))
                    {
                        var sweepSampleCoreVec = sweepStartCoreVec.RotateAround(arc3D.Axis, arc3D.SweepDeg > 0 ? -currentDegrees : currentDegrees);
                        var sweepSampleDir = Vector3.Normalize(sweepSampleCoreVec);
                        var sweepSampleOuterVec = sweepSampleCoreVec + sweepSampleDir * pathRadius;
                        var sweepSampleInnerVec = sweepSampleCoreVec - sweepSampleDir * pathRadius;

                        sweepVecSamples.Add(sweepSampleCoreVec);
                        sweepVecBounds.Add(sweepSampleOuterVec);
                        sweepVecBounds.Add(sweepSampleInnerVec);
                        currentDegrees += degreesPerSample;
                    }

                    var sweepEndCoreVec = arc3D.End - arc3D.Center;
                    var sweepEndDir = Vector3.Normalize(sweepEndCoreVec);
                    var sweepEndOuterVec = sweepEndCoreVec + sweepEndDir * pathRadius;
                    var sweepEndInnerVec = sweepStartCoreVec - sweepStartDir * pathRadius;

                    sweepVecSamples.Add(sweepEndCoreVec);
                    sweepVecBounds.Add(sweepEndOuterVec);
                    sweepVecBounds.Add(sweepEndInnerVec);

                    var imageSweepSamples = sweepVecSamples.Select(x => arc3D.Center + x).Select(x => x.Project(projection)).ToList();
                    var imageBounds = sweepVecBounds.Select(x => arc3D.Center + x).Select(x => x.Project(projection)).ToList();

                    var imageArc = new ImageArc(Arc3D: arc3D, ImageCenter: imageCenter, ImageSweepSamples: imageSweepSamples, ImageBounds: imageBounds);
                    imagePieces.Add(imageArc);
                }
            }

            return new ImagePath(Pieces: imagePieces, Diameter: path3D.Diameter);
        }

        public static Bounds GetBounds(this ImagePath path)
        {
            var pathBounds = path.Pieces.SelectMany(x => x.ImageBounds).ToList();
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
    }
}