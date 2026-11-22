using System;

namespace Punchy.Core
{
    /// <summary>
    /// Minimal Unity-free 3D vector struct so Punchy.Core executes natively under netstandard2.0 and dotnet test.
    /// </summary>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);
        public static readonly Vec3 Forward = new Vec3(0f, 0f, 1f);

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float SqrMagnitude => (X * X) + (Y * Y) + (Z * Z);

        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        public Vec3 Normalized
        {
            get
            {
                float mag = Magnitude;
                if (mag <= 1e-6f)
                    return Zero;
                return new Vec3(X / mag, Y / mag, Z / mag);
            }
        }

        public static float Dot(Vec3 a, Vec3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        public static float Distance(Vec3 a, Vec3 b) => (a - b).Magnitude;

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vec3 operator -(Vec3 v) => new Vec3(-v.X, -v.Y, -v.Z);

        public static Vec3 operator *(Vec3 v, float scalar) => new Vec3(v.X * scalar, v.Y * scalar, v.Z * scalar);

        public static Vec3 operator *(float scalar, Vec3 v) => new Vec3(v.X * scalar, v.Y * scalar, v.Z * scalar);

        public static Vec3 operator /(Vec3 v, float divisor)
        {
            if (Math.Abs(divisor) <= 1e-8f)
                return Zero;
            return new Vec3(v.X / divisor, v.Y / divisor, v.Z / divisor);
        }

        public bool Equals(Vec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        public override bool Equals(object obj) => obj is Vec3 other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                hash = (hash * 397) ^ Z.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";
    }
}
