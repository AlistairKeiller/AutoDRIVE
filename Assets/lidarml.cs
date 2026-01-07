using Unity.MLAgents.Sensors;
using UnityEngine;

using Unity.Collections;
using Unity.Burst;

[BurstCompile]
public class LidarML : SensorComponent
{
    [SerializeField] string sensorName = "Lidar";
    [SerializeField, Range(1, 1080)] int samples = 1080;
    [SerializeField, Range(0f, 360f)] float theta = 270f;
    [SerializeField, Range(0.1f, 500f)] float range = 20f;
    [SerializeField] LayerMask layerMask = Physics.DefaultRaycastLayers;

    public override ISensor[] CreateSensors() =>
        new[] { new LidarDistanceSensor(sensorName, transform, samples, theta, range, layerMask) };

    void OnDrawGizmosSelected()
    {
        var pos = transform.position;
        var start = -theta * 0.5f;
        var step = samples > 1 ? theta / (samples - 1) : 0f;

        Gizmos.color = Color.red;
        for (var i = 0; i < samples; i++)
        {
            var dir = Quaternion.Euler(0f, start + step * i, 0f) * Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.forward;
            var hitDetected = Physics.Raycast(pos, dir, out var hit, range, layerMask, QueryTriggerInteraction.Ignore);
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
        readonly LayerMask layerMask;
        readonly Vector3[] directions;

        NativeArray<RaycastHit> hitResults;
        NativeArray<RaycastCommand> rayCommands;

        public LidarDistanceSensor(string name, Transform origin, int samples, float theta, float range, LayerMask layerMask)
        {
            this.name = name;
            this.origin = origin;
            this.samples = samples;
            this.theta = theta;
            this.range = range;
            this.layerMask = layerMask;
            directions = new Vector3[this.samples];
            var start = -theta * 0.5f;
            var step = samples > 1 ? theta / (samples - 1) : 0f;
            for (int i = 0; i < this.samples; i++)
                directions[i] = Quaternion.Euler(0f, start + step * i, 0f) * Vector3.forward;
            rayCommands = new NativeArray<RaycastCommand>(this.samples, Allocator.Persistent);
            hitResults = new NativeArray<RaycastHit>(this.samples, Allocator.Persistent);

        }

        public ObservationSpec GetObservationSpec() => ObservationSpec.Vector(samples);
        public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();

        public int Write(ObservationWriter writer)
        {
            for (int i = 0; i < samples; i++)
            {
                writer[i] = hitResults[i].collider != null ? hitResults[i].distance : range;
            }
            return samples;
        }

        public void Update()
        {
            var pos = origin.position;
            var forward = origin.forward;
            var start = -theta * 0.5f;
            var step = samples > 1 ? theta / (samples - 1) : 0f;
            QueryParameters parameters = new QueryParameters { layerMask = layerMask, };

            for (var i = 0; i < samples; i++)
                rayCommands[i] = new RaycastCommand(pos, Quaternion.Euler(0f, origin.eulerAngles.y, 0f) * directions[i], parameters, range);

            var handle = RaycastCommand.ScheduleBatch(rayCommands, hitResults, 32);
            handle.Complete();
        }

        public void Reset() { }
        public string GetName() => name;
        public byte[] GetCompressedObservation() => null;
    }
}