using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LidarML : MonoBehaviour
{
    [SerializeField] string serverUrl = "http://127.0.0.1:9000";
    [SerializeField, Range(1, 1080)] int samples = 1080;
    [SerializeField, Range(0f, 360f)] float theta = 270f;
    [SerializeField, Range(0.1f, 500f)] float range = 50f;
    [SerializeField] LayerMask layers = Physics.DefaultRaycastLayers;

    float[] distances;
    Vector3[] directions;

    void Awake()
    {
        distances = new float[samples];
        directions = new Vector3[samples];
        RebuildDirections();
        StartCoroutine(SendLoop());
    }

    void OnValidate()
    {
        distances = new float[samples];
        directions = new Vector3[samples];
        RebuildDirections();
    }

    void RebuildDirections()
    {
        var forward = Vector3.forward;
        var start = -theta * 0.5f;
        var step = samples > 1 ? theta / (samples - 1) : 0f;
        for (var i = 0; i < samples; i++)
            directions[i] = Quaternion.Euler(0f, start + step * i, 0f) * forward;
    }

    void CaptureScan()
    {
        var pos = transform.position;
        var euler = transform.rotation.eulerAngles;
        var rot = Quaternion.Euler(0f, euler.y, 0f);
        for (var i = 0; i < samples; i++)
        {
            var dir = rot * directions[i];
            distances[i] = Physics.Raycast(pos, dir, out var hit, range, layers, QueryTriggerInteraction.Ignore)
                ? hit.distance
                : range;
        }
    }

    IEnumerator SendLoop()
    {
        var json = new Request();
        while (true)
        {
            CaptureScan();
            json.command = "publish_lidar";
            json.data = distances;
            var payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(json));
            using var req = new UnityWebRequest(serverUrl, "POST")
            {
                uploadHandler = new UploadHandlerRaw(payload),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
        }
    }

    void OnDrawGizmosSelected()
    {
        var pos = transform.position;
        var euler = transform.rotation.eulerAngles;
        var rot = Quaternion.Euler(0f, euler.y, 0f);
        Gizmos.color = Color.red;
        for (var i = 0; i < samples; i++)
        {
            var dir = rot * directions[i];
            var hitDetected = Physics.Raycast(pos, dir, out var hit, range, layers, QueryTriggerInteraction.Ignore);
            var end = hitDetected ? hit.point : pos + dir * range;
            Gizmos.DrawLine(pos, end);
            if (hitDetected) Gizmos.DrawSphere(end, 0.01f);
        }
    }

    [System.Serializable]
    class Request { public string command; public float[] data; }
}
