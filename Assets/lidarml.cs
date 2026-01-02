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

    float timer;

    void Awake() => distances = new float[samples];

    void Update()
    {
        CaptureScan();
        StartCoroutine(SendScan());
    }

    void CaptureScan()
    {
        var pos = transform.position;
        var forward = transform.forward;
        var start = -theta * 0.5f;
        var step = samples > 1 ? theta / (samples - 1) : 0f;
        for (var i = 0; i < samples; i++)
        {
            var dir = Quaternion.Euler(0f, start + step * i, 0f) * forward;
            distances[i] = Physics.Raycast(pos, dir, out var hit, range, layers, QueryTriggerInteraction.Ignore)
                ? hit.distance
                : range;
        }
    }

    IEnumerator SendScan()
    {
        var payload = JsonUtility.ToJson(new Request { command = "publish_lidar", data = distances });
        var req = new UnityWebRequest(serverUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }

    void OnDrawGizmosSelected()
    {
        var pos = transform.position;
        var forward = transform.forward;
        var start = -theta * 0.5f;
        var step = samples > 1 ? theta / (samples - 1) : 0f;
        Gizmos.color = Color.red;
        for (var i = 0; i < samples; i++)
        {
            var dir = Quaternion.Euler(0f, start + step * i, 0f) * forward;
            var hitDetected = Physics.Raycast(pos, dir, out var hit, range, layers, QueryTriggerInteraction.Ignore);
            var end = hitDetected ? hit.point : pos + dir * range;
            Gizmos.DrawLine(pos, end);
            if (hitDetected) Gizmos.DrawSphere(end, 0.01f);
        }
    }

    [System.Serializable]
    class Request
    {
        public string command;
        public float[] data;
    }
}
