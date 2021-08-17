using System;
using System.Linq;
using Simulator.Map;
using UnityEngine;

public class MapOriginPositionErrorOptimalizer
{
    private readonly MapOrigin _origin;
    private readonly MapOriginReferencePoint[] _points;

    public MapOriginPositionErrorOptimalizer(MapOrigin origin, MapOriginReferencePoint[] points)
    {
        _origin = origin;
        _points = points;
    }

    public void Optimize(int iterations = 100)
    {
        float errorBefore = ErrorForPoints(_points);
        Debug.Log($"Error before optimalization: {errorBefore}");
        for (int i = 0; i <= iterations; i++)
        {
            FindOffset();
            FindScaleAndRotation();
        }
        float errorAfter = ErrorForPoints(_points);
        Debug.Log($"Error before optimalization: {errorAfter}");
        if (errorBefore < errorAfter)
        {
            Debug.LogError("Optimalization failed, try to set better initial values of MapOrigin.");
        }
    }

    private void FindOffset()
    {
        var originalPosition = _origin.transform.position;
        Func<Vector3, float> f = (arg) =>
        {
            _origin.transform.position = originalPosition + arg;
            return ErrorForPoints(_points);
        };
        var diff = FindMinimum(f, Vector3.zero);
        f(diff);
    }

    private void FindScaleAndRotation()
    {
        var originalRot = _origin.transform.eulerAngles;
        var originalScale = _origin.transform.localScale;
        Func<Vector3, float> f = (arg) =>
        {
            _origin.transform.rotation = Quaternion.Euler(originalRot + new Vector3(0, arg.y, 0));
            _origin.transform.localScale = originalScale + new Vector3(arg.x, 0, arg.z);
            return ErrorForPoints(_points);
        };

        var diff = FindMinimum(f, Vector3.zero, delta: 0.001f, step: 0.001f, iterations: 200);
        f(diff);
    }

    private float ErrorForPoints(MapOriginReferencePoint[] points)
    {
        return points.Average(d => ErrorForPoint(d).sqrMagnitude);
    }

    private Vector3 ErrorForPoint(MapOriginReferencePoint p)
    {
        var realPos = _origin.LatLongToPosition(p.latitue, p.longitude);
        var virtualPos = p.transform.position;
        realPos.y = virtualPos.y;
        return virtualPos - realPos;
    }

    private Vector3 FindMinimum(Func<Vector3, float> f, Vector3 initial, float delta = 0.1f, float step = 1f, int iterations = 50)
    {
        var x = initial;
        while (iterations-- > 0)
        {
            var fx = f(x);
            var gradientx = (fx - f(x - new Vector3(delta, 0, 0)));
            var gradienty = (fx - f(x - new Vector3(0, delta, 0)));
            var gradientz = (fx - f(x - new Vector3(0, 0, delta)));
            x -= new Vector3(gradientx, gradienty, gradientz) * step;
        }
        return x;
    }
}