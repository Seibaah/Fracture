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
}
