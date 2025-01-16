using System;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;
namespace ML_Agents.Examples.Soccer.Scripts
{
    public class SoundSensor : MonoBehaviour, ISensor
    {
        /**
         * A Struct describing the observation spec, the information passed into the algorithm
         *  - A Vector3, position of the sound
         *  - A float, the intensity of the sound
         */
        private struct SoundHeard
        {
            public Vector3 position;
            public float intensity;
        }
        private ObservationSpec observationSpec;
        private string sensorName = "Sound Sensor";
        public float hearingRadius = 7.0f;
        private List<SoundHeard> collectedSounds = new List<SoundHeard>();
        private List<float> soundTimestamps = new List<float>();
        private float timeSinceLastHeard = 0f;

        [SerializeField]
        private float soundCooldown = 0.5f;
        //private float soundDuration = 10.0f;

        public ObservationSpec GetObservationSpec()

        {
            return observationSpec;
        }

        public void Awake()
        {
            observationSpec = ObservationSpec.Vector(9);
        }

        public void hearSound(Vector3 position, float baseIntensity = 1.0f)
        {
            if (Time.time - timeSinceLastHeard < soundCooldown) return;
            //if it's outside the hearing radius there is no point in considering it
            float range = Vector3.Distance(position, transform.position);
            if (range > hearingRadius) return;
            float intensity = baseIntensity * (1f - range / hearingRadius);
            collectedSounds.Add(new SoundHeard { position = position, intensity = intensity });
            soundTimestamps.Add(Time.time);
            timeSinceLastHeard = Time.time;
            Debug.Log($"Position = {position}, Intensity = {intensity}");
        }

        public int Write(ObservationWriter writer)
        {
            if (collectedSounds.Count > 0)
            {
                var sound = collectedSounds[collectedSounds.Count - 1];
                var direction = (sound.position - transform.position).normalized;
                writer.Add(direction);
                writer.Add(new Vector3(sound.intensity, 0f, 0f) );
                writer.Add(new Vector3(sound.position.x, 0f, sound.position.z));

            }
            else
            {
                writer.Add(Vector3.zero);
                writer.Add(Vector3.zero);
                writer.Add(Vector3.zero);
            }

            return 9;
        }

        public byte[] GetCompressedObservation()
        {
            return null;
        }

        public void Update()
        {
            // // Remove sounds that have exceeded their duration
            // float currentTime = Time.time;
            // for (int i = collectedSounds.Count - 1; i >= 0; i--)
            // {
            //     if (i < soundTimestamps.Count && Math.Abs(currentTime - soundTimestamps[i]) > soundDuration)
            //     {
            //         collectedSounds.RemoveAt(i);
            //         soundTimestamps.RemoveAt(i);
            //     }
            // }
        }

        public void Reset()
        {
            collectedSounds.Clear();
        }

        public CompressionSpec GetCompressionSpec()
        {
            return CompressionSpec.Default();
        }

        public string GetName()
        {
            return sensorName;
        }

        public bool somethingHeard(out Vector3 position)
        {
            position = Vector3.zero;
            if (collectedSounds.Count > 0)
            {
                var sound = collectedSounds[collectedSounds.Count - 1];
                position = sound.position;
                return true;
            }
            return false;
        }
    }
}
