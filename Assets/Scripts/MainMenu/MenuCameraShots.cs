using System.Collections;
using UnityEngine;

// Put this on the MenuCamera in the MainMenu scene (use it INSTEAD of MenuCameraPath).
//
// Each "shot" has just two points: a Start and an End (empty objects placed in the level).
// The camera glides from Start to End. When it arrives, the screen fades to black, the camera jumps to the
// next shot's Start while it is black, and the screen fades back in as the next shot begins.
public class MenuCameraShots : MonoBehaviour
{
    [System.Serializable]
    public class Shot
    {
        [Tooltip("Where the camera begins (its position AND rotation are used)")]
        public Transform start;
        [Tooltip("Where the camera ends (its position AND rotation are used)")]
        public Transform end;
        [Tooltip("Seconds the camera takes to travel from Start to End")]
        public float duration = 10f;
    }

    [Header("Shots (each one = a start and an end)")]
    public Shot[] shots;
    [Tooltip("Pick the next shot at random (never the same one twice in a row) instead of in order")]
    public bool randomOrder = false;

    [Header("Fade (a black image BEHIND the menu buttons)")]
    public CanvasGroup fade;
    public float fadeOutTime = 1f;      // to black, once the camera reaches its destination
    public float blackHoldTime = 0.4f;  // stays black while the camera jumps to the next shot
    public float fadeInTime = 1f;       // back from black as the next shot starts
    [Tooltip("0 = the fade starts exactly when the camera arrives. Raise it to start fading a little earlier.")]
    public float fadeLeadTime = 0f;

    [Header("Movement")]
    [Tooltip("Ease in and out. Untick for a constant speed.")]
    public bool smoothMovement = true;

    [Header("Handheld feel")]
    public float swayAngle = 0.6f;      // degrees of slow rotation drift
    public float swaySpeed = 0.5f;
    public float bobHeight = 0.015f;    // tiny up / down movement

    int current = -1;

    void Start()
    {
        if (ValidCount() == 0)
        {
            Debug.LogWarning("MenuCameraShots: add at least one shot with both a Start and an End.", this);
            enabled = false;
            return;
        }

        if (fade != null)
        {
            fade.blocksRaycasts = false;     // must never block the menu buttons
            fade.interactable = false;
            fade.alpha = 1f;                 // begin black, then fade in on the first shot
        }
        else
        {
            Debug.LogWarning("MenuCameraShots: no Fade assigned, so the shots will cut without fading.", this);
        }

        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        while (true)
        {
            current = NextShot(current);
            Shot s = shots[current];

            // the screen is black here: jump to the start of the new shot
            ApplyPose(s, 0f);
            if (blackHoldTime > 0f) yield return new WaitForSeconds(blackHoldTime);

            float outTime = fade != null ? fadeOutTime : 0f;
            float fadeOutStart = Mathf.Max(0f, s.duration - fadeLeadTime);
            float t = 0f;

            while (true)
            {
                t += Time.deltaTime;

                // travel from start to end, then hold at the end while the fade-out finishes
                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, s.duration));
                ApplyPose(s, smoothMovement ? Mathf.SmoothStep(0f, 1f, k) : k);

                if (fade != null)
                {
                    float fadeIn = fadeInTime > 0f ? 1f - Mathf.Clamp01(t / fadeInTime) : 0f;
                    float fadeOut = outTime > 0f ? Mathf.Clamp01((t - fadeOutStart) / outTime) : (t >= fadeOutStart ? 1f : 0f);
                    fade.alpha = Mathf.Max(fadeIn, fadeOut);
                }

                if (t >= fadeOutStart + outTime) break;
                yield return null;
            }

            if (fade != null) fade.alpha = 1f;
        }
    }

    void ApplyPose(Shot s, float k)
    {
        Vector3 pos = Vector3.Lerp(s.start.position, s.end.position, k);
        Quaternion rot = Quaternion.Slerp(s.start.rotation, s.end.rotation, k);

        float t = Time.time * swaySpeed;
        Quaternion sway = Quaternion.Euler(
            Mathf.Sin(t * 1.3f) * swayAngle,
            Mathf.Sin(t * 0.9f + 1f) * swayAngle,
            Mathf.Sin(t * 0.7f) * swayAngle * 0.5f);

        transform.SetPositionAndRotation(pos + Vector3.up * (Mathf.Sin(t * 2.1f) * bobHeight), rot * sway);
    }

    // ---------- choosing the next shot ----------

    static bool Valid(Shot s)
    {
        return s != null && s.start != null && s.end != null;
    }

    int ValidCount()
    {
        int n = 0;
        if (shots != null)
            foreach (Shot s in shots)
                if (Valid(s)) n++;
        return n;
    }

    int NextShot(int previous)
    {
        int count = shots.Length;
        int valid = ValidCount();

        if (randomOrder && valid > 1)
        {
            int i;
            do { i = Random.Range(0, count); }
            while (i == previous || !Valid(shots[i]));
            return i;
        }

        int n = previous;
        for (int c = 0; c < count; c++)
        {
            n = (n + 1) % count;
            if (Valid(shots[n])) return n;
        }
        return previous;
    }

    // shows every shot in the Scene view: green = start, red = end, with the direction the camera looks
    void OnDrawGizmos()
    {
        if (shots == null) return;

        for (int i = 0; i < shots.Length; i++)
        {
            Shot s = shots[i];
            if (!Valid(s)) continue;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(s.start.position, 0.12f);
            Gizmos.DrawRay(s.start.position, s.start.forward * 0.8f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(s.end.position, 0.12f);
            Gizmos.DrawRay(s.end.position, s.end.forward * 0.8f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(s.start.position, s.end.position);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(s.start.position + Vector3.up * 0.25f, "Shot " + (i + 1) + " start");
            UnityEditor.Handles.Label(s.end.position + Vector3.up * 0.25f, "Shot " + (i + 1) + " end");
#endif
        }
    }
}
