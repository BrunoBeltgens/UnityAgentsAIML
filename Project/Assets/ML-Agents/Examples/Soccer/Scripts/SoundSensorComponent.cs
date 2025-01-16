using System;
using UnityEngine;
using System.Collections.Generic;
namespace ML_Agents.Examples.Soccer.Scripts
{
    [RequireComponent(typeof(SoundSensor))]
    public class SoundSensorComponent : MonoBehaviour
    {
        [Header("Sound Sensor Settings")]
        [Tooltip("Radius within which the sound can be heard")]
        public float hearingRadius = 7.0f;
        [Tooltip("How long is a sound remembered for")]
        public float soundDuration = 1.0f;
        private SoundSensor soundSensor;

        private void Awake()
        {
            soundSensor = GetComponent<SoundSensor>();
            if (soundSensor == null)
            {
                soundSensor = gameObject.AddComponent<SoundSensor>();
            }
            soundSensor.hearingRadius = hearingRadius;
        }

        private void Start()
        {
            Debug.Log($"Sound sensor initialised with hearing radius {hearingRadius}");
        }

        public void HearSound(Vector3 position)
        {
            soundSensor.hearSound(position);
        }

        public void ResetSensor()
        {
            soundSensor.Reset();
        }
    }

}
