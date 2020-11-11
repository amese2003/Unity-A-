using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line
{
    const float verticalLineGradient = 1e5f;

    float gradient;
    float y_intercept;
    Vector2 pointOnLine_1;
    Vector2 pointOnLine_2;

    float gradientPerpendicular;

    bool approachSide;

    public Line(Vector2 _pointOnLine, Vector2 _pointPerpendicularToLine)
    {
        float dx = _pointOnLine.x - _pointPerpendicularToLine.x;
        float dy = _pointOnLine.y - _pointPerpendicularToLine.y;

        if (dx == 0)
            gradientPerpendicular = verticalLineGradient;
        else
            gradientPerpendicular = dy / dx;

        if (gradientPerpendicular == 0)
            gradient = verticalLineGradient;
        else
            gradient = -1 / gradientPerpendicular;

        y_intercept = _pointOnLine.y - gradient * _pointOnLine.x;

        pointOnLine_1 = _pointOnLine;
        pointOnLine_2 = _pointOnLine + new Vector2(1, gradient);

        approachSide = false;
        approachSide = GetSide(_pointPerpendicularToLine);
    }

    bool GetSide(Vector2 pos)
    {
        return (pos.x - pointOnLine_1.x) * (pointOnLine_2.y - pointOnLine_1.y) > (pos.y - pointOnLine_1.y) * (pointOnLine_2.x - pointOnLine_1.x);
    }

    public bool HasCrossedLine(Vector2 p)
    {
        return GetSide(p) != approachSide;
    }

    public float DistanceFromPoint(Vector2 p)
    {
        float yInterceptPerpendicular = p.y - gradientPerpendicular * p.x;
        float interceptX = (yInterceptPerpendicular - y_intercept) / (gradient - gradientPerpendicular);
        float interceptY = gradient * interceptX + y_intercept;

        return Vector2.Distance(p, new Vector2(interceptX, interceptY));
    }

    public void DrawWithGizmos(float length)
    {
        Vector3 lineDir = new Vector3(1, 0, gradient).normalized;
        Vector3 lineCenter = new Vector3(pointOnLine_1.x, 0, pointOnLine_1.y) + Vector3.up;
        Gizmos.DrawLine(lineCenter - lineDir * length / 2f, lineCenter + lineDir * length / 2f);

    }

}
