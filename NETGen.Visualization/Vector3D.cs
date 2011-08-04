using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETGen.Visualization
{
    /// <summary>
    /// A simple 3D vector with some static functions
    /// </summary>
    public struct Point3D
    {
        /// <summary>
        /// The x coordinate of this vector
        /// </summary>
        public double X;

        /// <summary>
        /// The y coordinate of this vector
        /// </summary>
        public double Y;

        /// <summary>
        /// The z coordinate of this vector
        /// </summary>
        public double Z;

        /// <summary>
        /// Initialies a 3D vector with given coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Returns a hash code for this position. Note: there are conflicts since three 64bit values need to be mapped to a single 32-bit value!
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (int) (Math.Pow(Math.Pow(X, Y), Z) * 1000d);
        }

        /// <summary>
        /// Returns whether a vector equals an object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is Point3D)
                return this == (Point3D)obj;
            else
                return false;
        }

        /// <summary>
        /// Returns whether two vectors are identical
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Point3D a, Point3D b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        /// <summary>
        /// Returns whether two vectors are different
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Point3D a, Point3D b)
        {
            return a.X != b.X || a.Y != b.Y || a.Z == b.Z;
        }

        /// <summary>
        /// Adds two vectors a and b and returns the result
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point3D operator +(Point3D a, Point3D b)
        {
            return new Point3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        /// <summary>
        /// Subtracts two vectors a and b and returns the results
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point3D operator -(Point3D a, Point3D b)
        {
            return new Point3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
		
		public static Point3D operator *(Point3D a, double scalar)
		{
			return Multiply(a, scalar);
		}
		
		public static Point3D operator *(double scalar, Point3D a)
		{
			return Multiply(a, scalar);
		}
		
		public static Point3D operator /(Point3D a, double scalar)
		{
			return Multiply(a, 1d/scalar);
		}

        /// <summary>
        /// Returns the angle between two vertices with respect to a certain reference axis
        /// </summary>
        /// <param name="v"></param>
        /// <param name="w"></param>
        /// <returns>The angle in radians </returns>
        public static double getAngle(Point3D v, Point3D w)
        {
            return Math.Acos(Point3D.Dot(Point3D.Normalize(v), Point3D.Normalize(w)));
        }

        /// <summary>
        /// Converts an angle in radians [0 - 2Pi) to degrees [0-360°)
        /// </summary>
        /// <param name="rad"></param>
        /// <returns></returns>
        public static double RadiansToDegree(double rad)
        {
            return (180d * rad) / Math.PI;
        }

        /// <summary>
        /// Converts an angle in degrees [0-360°) to radians [0 - 2Pi)
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static double DegreeToRadians(double deg)
        {
            return (deg * 180d) / Math.PI;
        }

        /// <summary>
        /// Normalizes a 3D vector
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Point3D Normalize(Point3D vec)
        {
            double length = Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
            if (length > 0)
            {
                vec.X /= length;
                vec.Y /= length;
                vec.Z /= length;
            }
            return vec;
        }

        /// <summary>
        /// Multiplies a vector with a scalar and returns the result
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Point3D Multiply(Point3D vec, double scalar)
        {
            vec.X *= scalar;
            vec.Y *= scalar;
            vec.Z *= scalar;
            return vec;
        }

        /// <summary>
        /// Returns the distance between two positions
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Distance(Point3D a, Point3D b)
        {
            double dist = Math.Sqrt(Math.Pow(a.X - b.X, 2d) + Math.Pow(a.Y - b.Y, 2d) + Math.Pow(a.Z - b.Z, 2d));
            return dist;
        }

        /// <summary>
        /// Returns the length of this vector
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static double Length(Point3D a)
        {
            return Math.Sqrt(a.X*a.X + a.Y*a.Y + a.Z*a.Z);
        }

        /// <summary>
        /// Returns the dot product of this vector
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Dot(Point3D a, Point3D b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        /// <summary>
        /// Returns the cross product of this vector
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Point3D Cross(Point3D a, Point3D b)
        {
            return new Point3D(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }       
    }
}
