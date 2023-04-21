using System;
using UnityEngine;
using MathNetNumerics = MathNet.Numerics.LinearAlgebra;

public static class VectorUtils
{
    /// <summary>
    /// Converts a Unity.Vector3 to a MathNet.Numerics Dense Vector3
    /// </summary>
    /// <param name="vec">Unity.Vector3 to convert</param>
    /// <returns>Returns a MathNet.Numerics Dense Vector3 with values equal to the passed vector</returns>
    public static MathNetNumerics.Vector<float> ConvertUnityVec3ToMathNetNumericsVec3(Vector3 vec)
    {
        return MathNetNumerics.Vector<float>.Build.Dense(new float[] { vec.x, vec.y, vec.z });
    }

    /// <summary>
    /// Converts a MathNet.Numerics Dense Vector3 to a Unity.Vector3
    /// </summary>
    /// <param name="vec">MathNet.Numerics Dense Vector 3 to convert</param>
    /// <returns>Returns a Unity.Vector3 with values equal to the passed vector</returns>
    public static Vector3 ConvertMathNetNumericsVec3ToUnityVec3(MathNetNumerics.Vector<float> vec)
    {
        if (vec.Count != 3)
        {
            throw new ArgumentException("Vector must be dimension 3");
        }

        return new Vector3(vec.At(0), vec.At(1), vec.At(2));
    }

    /// <summary>
    /// Computes the cross product of 2 MathNet.Numerics vectors in R3
    /// </summary>
    /// <param name="a">MathNet.Numerics Dense Vector 3 a</param>
    /// <param name="b">MathNet.Numerics Dense Vector 3 b</param>
    /// <returns>Cross product a X b</returns>
    public static MathNetNumerics.Vector<float> CrossProduct(MathNetNumerics.Vector<float> a,
        MathNetNumerics.Vector<float> b)
    {
        if (a.Count != 3 || b.Count != 3)
        {
            throw new ArgumentException("Both vectors must be dimension 3");
        }

        var aX = MathNetNumerics.Matrix<float>.Build.Dense(3, 3);
        aX.SetColumn(0, new float[] { 0, a.At(2), -a.At(1) });
        aX.SetColumn(1, new float[] { -a.At(2), 0, -a.At(0) });
        aX.SetColumn(2, new float[] { a.At(1), -a.At(0), 0 });
        return aX * b;
    }
}
