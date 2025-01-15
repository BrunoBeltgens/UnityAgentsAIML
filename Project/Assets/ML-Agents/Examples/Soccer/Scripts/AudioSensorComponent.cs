using UnityEngine;
using Unity.MLAgents.Sensors;

namespace MLAgents.Soccer
{
    public class AudioSensorComponent : SensorComponent
    {
        [SerializeField] private string sensorName = "AudioSensor";
        [SerializeField] private float hearingRadius;
        
        private AgentSoccer agentSoccer;
        private SoccerEnvController envController;
        private bool isInitialized = false;

        void Start()
        {
            InitializeDependencies();
            hearingRadius = agentSoccer.HearingRadius;
        }

        private void InitializeDependencies()
        {
            if (isInitialized) return;

            // Wait for AgentSoccer to be ready
            agentSoccer = GetComponent<AgentSoccer>();
            if (agentSoccer == null)
            {
                Debug.LogError($"[AudioSensorComponent] No AgentSoccer found on {gameObject.name}");
                return;
            }

            // Find the root SoccerFieldTwos object
            var root = transform.root;
            envController = root.GetComponent<SoccerEnvController>();
            if (envController == null)
            {
                Debug.LogError($"[AudioSensorComponent] No SoccerEnvController found on root object {root.name}");
                return;
            }

            // Wait for ball reference
            if (envController.ball == null)
            {
                Debug.LogError($"[AudioSensorComponent] Ball reference not set in SoccerEnvController");
                return;
            }

            isInitialized = true;
            Debug.Log($"[AudioSensorComponent] Initialized on {gameObject.name} with radius {hearingRadius}");
        }

        public override ISensor[] CreateSensors()
        {
            if (!isInitialized)
            {
                InitializeDependencies();
            }

            if (!isInitialized)
            {
                Debug.LogError($"[AudioSensorComponent] Cannot create sensor - missing dependencies on {gameObject.name}");
                return new ISensor[0];
            }

            return new ISensor[] { new AudioSensor(sensorName, agentSoccer, envController, hearingRadius) };
        }
    }
}