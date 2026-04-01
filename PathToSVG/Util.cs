using SkiaSharp;
using System.Numerics;
using Types;

#nullable enable

namespace Utils
{
    static class Util
    {
        public static SKPoint ToSKPoint(this Vector3 pt)
        {
            return new SKPoint((float)pt.X, (float)pt.Y);
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
    }
}