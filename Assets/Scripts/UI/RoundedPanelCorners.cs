using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Clips only this Image's geometry. Keeps its original sprite, UVs, material,
/// children, masks and rectangular input area unchanged.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class RoundedPanelCorners : BaseMeshEffect
{
    [Min(0)] public float radius = 36f;
    const int StepsPerCorner = 8;
    readonly List<UIVertex> source = new List<UIVertex>();
    readonly List<UIVertex> result = new List<UIVertex>();
    readonly List<Vector2> boundary = new List<Vector2>(36);
    List<UIVertex> polygon = new List<UIVertex>(40);
    List<UIVertex> scratch = new List<UIVertex>(40);

    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive() || radius <= 0 || helper.currentVertCount == 0) return;
        Rect rect = graphic.rectTransform.rect;
        float r = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
        if (r <= 0) return;

        boundary.Clear();
        // Counter-clockwise outline, retaining all existing sprite UVs by interpolation.
        AddCorner(new Vector2(rect.xMax - r, rect.yMax - r), r, 0);
        AddCorner(new Vector2(rect.xMin + r, rect.yMax - r), r, 90);
        AddCorner(new Vector2(rect.xMin + r, rect.yMin + r), r, 180);
        AddCorner(new Vector2(rect.xMax - r, rect.yMin + r), r, 270);
        source.Clear(); result.Clear();
        helper.GetUIVertexStream(source);
        for (int triangle = 0; triangle + 2 < source.Count; triangle += 3)
        {
            polygon.Clear();
            polygon.Add(source[triangle]);
            polygon.Add(source[triangle + 1]);
            polygon.Add(source[triangle + 2]);
            for (int edge = 0; edge < boundary.Count && polygon.Count > 0; edge++)
            {
                Vector2 a = boundary[edge], b = boundary[(edge + 1) % boundary.Count];
                scratch.Clear();
                UIVertex previous = polygon[polygon.Count - 1];
                float previousSide = Side(a, b, previous.position);
                foreach (var current in polygon)
                {
                    float currentSide = Side(a, b, current.position);
                    bool currentInside = currentSide >= 0, previousInside = previousSide >= 0;
                    if (currentInside != previousInside)
                    {
                        float t = previousSide / (previousSide - currentSide);
                        scratch.Add(Interpolate(previous, current, t));
                    }
                    if (currentInside) scratch.Add(current);
                    previous = current; previousSide = currentSide;
                }
                var swap = polygon; polygon = scratch; scratch = swap;
            }
            for (int i = 1; i + 1 < polygon.Count; i++)
            {
                result.Add(polygon[0]); result.Add(polygon[i]); result.Add(polygon[i + 1]);
            }
        }
        helper.Clear();
        helper.AddUIVertexTriangleStream(result);
    }

    void AddCorner(Vector2 center, float r, float startDegrees)
    {
        for (int i = 0; i <= StepsPerCorner; i++)
        {
            float angle = (startDegrees + 90f * i / StepsPerCorner) * Mathf.Deg2Rad;
            boundary.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
        }
    }

    static float Side(Vector2 a, Vector2 b, Vector3 point) =>
        (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);

    static UIVertex Interpolate(UIVertex a, UIVertex b, float t)
    {
        a.position = Vector3.LerpUnclamped(a.position, b.position, t);
        a.color = Color32.LerpUnclamped(a.color, b.color, t);
        a.uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, t);
        a.uv1 = Vector4.LerpUnclamped(a.uv1, b.uv1, t);
        a.uv2 = Vector4.LerpUnclamped(a.uv2, b.uv2, t);
        a.uv3 = Vector4.LerpUnclamped(a.uv3, b.uv3, t);
        a.normal = Vector3.LerpUnclamped(a.normal, b.normal, t);
        a.tangent = Vector4.LerpUnclamped(a.tangent, b.tangent, t);
        return a;
    }
}
