using System.Collections.Generic;
using UnityEngine;

// Put this on the MenuCamera in the MainMenu scene.
// Create empty objects along the route you want (hallway, rooms...) and drag them into "points" in order.
// The camera glides through them on a smooth curve, looks ahead along the path, and has a slight handheld sway.
public class MenuCameraPath : MonoBehaviour
{
    [Header("Route")]
    public Transform[] points;
    public bool loop = true;           // loop back to the first point (best if the last point is near the first)
    public float speed = 0.6f;         // metres per second (slow looks more cinematic)
    public float lookAhead = 3f;       // metres ahead on the path that the camera looks toward
    public float turnSmoothing = 2f;   // higher = snappier turns

    [Header("Handheld feel")]
    public float swayAngle = 0.8f;     // degrees of slow rotation drift
    public float swaySpeed = 0.5f;
    public float bobHeight = 0.02f;    // tiny up/down movement

    readonly List<Vector3> samples = new List<Vector3>();
    readonly List<float> cumulative = new List<float>();
    float total;
    float distance;
    Quaternion currentRot = Quaternion.identity;

    void Start()
    {
        BuildPath();
        if (samples.Count < 2 || total < 0.1f)
        {
            Debug.LogWarning("MenuCameraPath needs at least 2 points that are not in the same place.");
            enabled = false;
            return;
        }

        transform.position = Evaluate(0f);
        currentRot = LookAt(Evaluate(0f), Evaluate(lookAhead));
        transform.rotation = currentRot;
    }

    void Update()
    {
        distance += speed * Time.deltaTime;

        Vector3 pos = Evaluate(distance);
        Vector3 target = Evaluate(distance + lookAhead);

        Quaternion want = LookAt(pos, target);
        currentRot = Quaternion.Slerp(currentRot, want, Time.deltaTime * turnSmoothing);

        float t = Time.time * swaySpeed;
        Quaternion sway = Quaternion.Euler(
            Mathf.Sin(t * 1.3f) * swayAngle,
            Mathf.Sin(t * 0.9f + 1f) * swayAngle,
            Mathf.Sin(t * 0.7f) * swayAngle * 0.5f);

        transform.position = pos + Vector3.up * (Mathf.Sin(t * 2.1f) * bobHeight);
        transform.rotation = currentRot * sway;
    }

    Quaternion LookAt(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        return d.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(d) : currentRot;
    }

    // ---------- path building ----------

    void BuildPath()
    {
        samples.Clear();
        cumulative.Clear();
        total = 0f;
        if (points == null || points.Length < 2) return;

        int n = points.Length;
        int segments = loop ? n : n - 1;
        const int steps = 20;

        for (int i = 0; i < segments; i++)
        {
            Vector3 a = P(i - 1), b = P(i), c = P(i + 1), d = P(i + 2);
            for (int s = 0; s < steps; s++)
                samples.Add(CatmullRom(a, b, c, d, s / (float)steps));
        }
        samples.Add(loop ? samples[0] : P(n - 1));

        cumulative.Add(0f);
        for (int i = 1; i < samples.Count; i++)
            cumulative.Add(cumulative[i - 1] + Vector3.Distance(samples[i - 1], samples[i]));
        total = cumulative[cumulative.Count - 1];
    }

    Vector3 P(int i)
    {
        int n = points.Length;
        i = loop ? ((i % n) + n) % n : Mathf.Clamp(i, 0, n - 1);
        return points[i].position;
    }

    static Vector3 CatmullRom(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        return 0.5f * ((2f * b)
                     + (-a + c) * t
                     + (2f * a - 5f * b + 4f * c - d) * t * t
                     + (-a + 3f * b - 3f * c + d) * t * t * t);
    }

    // position at a given distance along the path (constant speed, even if points are unevenly spaced)
    Vector3 Evaluate(float dist)
    {
        dist = loop ? Mathf.Repeat(dist, total) : Mathf.Clamp(dist, 0f, total);

        int idx = cumulative.BinarySearch(dist);
        if (idx < 0) idx = ~idx;
        if (idx <= 0) return samples[0];
        if (idx >= samples.Count) return samples[samples.Count - 1];

        float seg = cumulative[idx] - cumulative[idx - 1];
        float t = seg > 0.0001f ? (dist - cumulative[idx - 1]) / seg : 0f;
        return Vector3.Lerp(samples[idx - 1], samples[idx], t);
    }

    // shows the route in the Scene view
    void OnDrawGizmos()
    {
        if (points == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            Gizmos.DrawSphere(points[i].position, 0.15f);

            int next = i + 1 < points.Length ? i + 1 : (loop ? 0 : -1);
            if (next >= 0 && points[next] != null)
                Gizmos.DrawLine(points[i].position, points[next].position);
        }
    }
}
