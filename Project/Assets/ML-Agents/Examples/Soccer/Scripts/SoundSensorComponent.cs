using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;

namespace ML_Agents.Examples.Soccer.Scripts
{
    [RequireComponent(typeof(SoundSensor))]
    public class SoundSensorComponent : SensorComponent
    {
        [Header("Sound Sensor Settings")]
        [Tooltip("Radius within which the sound can be heard")]
        public float hearingRadius = 7.0f;
        [Tooltip("How long is a sound remembered for")]
        public float soundDuration = 1.0f;
        private SoundSensor soundSensor;

        public override ISensor[] CreateSensors()
        {
            soundSensor = GetComponent<SoundSensor>();
            if (soundSensor == null)
            {
                Debug.LogWarning("Sound Sensor component is missing");
            }
            soundSensor.hearingRadius = hearingRadius;
            return new ISensor[] { soundSensor };
        }
    }

}
