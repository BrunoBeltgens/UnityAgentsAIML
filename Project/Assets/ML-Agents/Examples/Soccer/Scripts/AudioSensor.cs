using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using System.Text;

namespace MLAgents.Soccer
{
    public class AudioSensor : ISensor
    {
        private string sensorName;
        private AgentSoccer agentSoccer;
        private SoccerEnvController envController;
        private float hearingRadius;

        private const int ValuesPerSource = 3;
        private const int NumSources = 4;
        private const int ObservationSize = ValuesPerSource * NumSources;

        public AudioSensor(string name, AgentSoccer agent, SoccerEnvController controller, float radius)
        {
            sensorName = name;
            agentSoccer = agent;
            envController = controller;
            hearingRadius = radius;
            //Debug.Log($"[AudioSensor] Initialized: {name} for agent {agent.name} with radius {radius}");
        }

        public string GetName() => sensorName;

        public ObservationSpec GetObservationSpec()
        {
            return ObservationSpec.Vector(ObservationSize);
        }

        public int Write(ObservationWriter writer)
        {
            if (envController == null || envController.ball == null)
            {
                //Debug.LogError($"[AudioSensor] {sensorName}: Environment controller or ball is null!");
                writer.AddList(new float[ObservationSize]);
                return ObservationSize;
            }

            var observations = new float[ObservationSize];
            int index = 0;
            var observationLog = new StringBuilder();
            observationLog.AppendLine($"\n[AudioSensor] {agentSoccer.name} Observations:");

            // Ball observations
            AddSourceObservation(observations, ref index,
                envController.ball.transform,
                1.0f, "Ball", observationLog);

            int teammateCount = 0;
            int opponentCount = 0;

            // Other agents observations
            foreach (var playerInfo in envController.AgentsList)
            {
                if (playerInfo.Agent == agentSoccer) continue;

                float sourceType = playerInfo.Agent.team == agentSoccer.team ? 2.0f : 3.0f;
                string sourceTypeName = sourceType == 2.0f ? "Teammate" : "Opponent";

                if (sourceType == 2.0f) teammateCount++;
                else opponentCount++;

                AddSourceObservation(observations, ref index,
                    playerInfo.Agent.transform,
                    sourceType,
                    sourceTypeName,
                    observationLog);
            }

            //Debug.Log(observationLog.ToString());
            writer.AddList(observations);
            return ObservationSize;
        }

        private void AddSourceObservation(float[] observations, ref int index, Transform source, float sourceType, string sourceName, StringBuilder log)
        {
            float distance = Vector3.Distance(agentSoccer.transform.position, source.position);

            if (distance > hearingRadius)
            {
                // Zero value for observations outside range
                observations[index++] = 1.0f;
                observations[index++] = 0.0f;
                observations[index++] = sourceType;
                log.AppendLine($"  {sourceName}: dist={distance:F2} (OUT OF RANGE)");
                return;
            }

            float normalizedDistance = distance / hearingRadius;
            float intensity = 0f;
            // For ball, intensity is 1 if moving, 0 if not moving
            if (sourceType == 1.0f)
            {
                var ballRb = envController.ballRb;
                if (ballRb.velocity.magnitude > 0.1f)
                {
                    intensity = 1;
                }
                log.AppendLine($"  Ball: dist={distance:F2}, normDist={normalizedDistance:F2}, intensity={intensity:F2} (moving={intensity > 0})");
            }
            else
            {
                if (agentSoccer.GetComponent<Rigidbody>().velocity.magnitude > 0.1f)
                {
                    intensity = 1;
                    //Debug.Log($"agent is moving with velocity {agentSoccer.GetComponent<Rigidbody>().velocity.magnitude}");
                }
                log.AppendLine($"  {sourceName}: dist={distance:F2}, normDist={normalizedDistance:F2}, intensity={intensity:F2}");
            }

            if (intensity == 1)
            {
                //Debug.Log($"[AudioSensor] {sourceName} is moving");
                observations[index++] = normalizedDistance;
                observations[index++] = intensity;
                observations[index++] = sourceType;
            }
            else
            {
                observations[index++] = 1.0f;
                observations[index++] = 0.0f;
                observations[index++] = sourceType;
            }
        }

        public void Update() { }
        public void Reset() { }
        public byte[] GetCompressedObservation() => null;
        public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();
    }
}