using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MNetNumerics = MathNet.Numerics.LinearAlgebra;

public static class VectorUtils
{
    public static MNetNumerics.Vector<float> ConvertUnityVec3ToNumerics(Vector3 vec)
    {
        return MNetNumerics.Vector<float>.Build.Dense(new float[] { vec.x, vec.y, vec.z });
    }

    public static Vector3 ConvertNumericsVec3ToUnity(MNetNumerics.Vector<float> vec)
    {
        return new Vector3(vec.At(0), vec.At(1), vec.At(2));
    }

    public static MNetNumerics.Vector<float> CrossProduct(MNetNumerics.Vector<float> a,
        MNetNumerics.Vector<float> b)
    {
        var aX = MNetNumerics.Matrix<float>.Build.Dense(3, 3);
        aX.SetColumn(0, new float[] { 0, a.At(2), -a.At(1) });
        aX.SetColumn(1, new float[] { -a.At(2), 0, -a.At(0) });
        aX.SetColumn(2, new float[] { a.At(1), -a.At(0), 0 });
        return aX * b;
    }
}
