using System.Numerics;

namespace PathToSVG
{
    public static class Util
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

        //Checks if a given point lies linearly between two others
        public static bool LinearBetween(this Vector3 target, Vector3 start, Vector3 end)
        {
            var distanceBetween = (end - start).Length();
            var combinedDistance = Vector3.Distance(target, start) + Vector3.Distance(target, end);
            return Math.Abs(combinedDistance - distanceBetween) < FLOAT_EPS;
        }
    }
}