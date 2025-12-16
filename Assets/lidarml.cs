using Unity.MLAgents.Sensors;
using UnityEngine;

public class LidarML : SensorComponent
{
    [SerializeField] string sensorName = "Lidar";
    [SerializeField, Range(1, 1080)] int samples = 4;
    [SerializeField, Range(0f, 360f)] float theta = 270f;
    [SerializeField, Range(0.1f, 500f)] float range = 50f;
    [SerializeField] LayerMask layers = Physics.DefaultRaycastLayers;
    LidarDistanceSensor sensor;

    public override ISensor[] CreateSensors() =>
        new[] { sensor = new LidarDistanceSensor(sensorName, transform, samples, theta, range, layers) };

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

    class LidarDistanceSensor : ISensor
    {
        readonly string name;
        readonly Transform origin;
        readonly int samples;
        readonly float theta;
        readonly float range;
        readonly LayerMask layers;
        readonly float[] distances;

        public LidarDistanceSensor(string name, Transform origin, int samples, float theta, float range, LayerMask layers)
        {
            this.name = name;
            this.origin = origin;
            this.samples = Mathf.Max(1, samples);
            this.theta = theta;
            this.range = range;
            this.layers = layers;
            distances = new float[this.samples];
        }

        public ObservationSpec GetObservationSpec() => ObservationSpec.Vector(samples);
        public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();

        public int Write(ObservationWriter writer)
        {
            var pos = origin.position;
            var forward = origin.forward;
            var start = -theta * 0.5f;
            var step = samples > 1 ? theta / (samples - 1) : 0f;

            for (var i = 0; i < samples; i++)
            {
                var dir = Quaternion.Euler(0f, start + step * i, 0f) * forward;
                writer[i] = distances[i] = Physics.Raycast(pos, dir, out var hit, range, layers, QueryTriggerInteraction.Ignore)
                    ? hit.distance
                    : range;
            }

            return samples;
        }

        public void Update() { }
        public void Reset() { }
        public string GetName() => name;
        public byte[] GetCompressedObservation() => null;
    }
}