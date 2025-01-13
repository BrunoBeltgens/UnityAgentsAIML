using UnityEngine;
using System.Collections.Generic;

namespace Unity.MLAgents
{
    public class Sound
    {
        public Vector3 Position { get; private set; }
        public float Radius { get; private set; }

        public Sound(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
    }

    public static class SoundManager
    {
        private static List<Sound> activeSounds = new List<Sound>();

        public static void PlaySound(Sound sound)
        {
            activeSounds.Add(sound);
        }

        public static List<Sound> GetActiveSounds()
        {
            return new List<Sound>(activeSounds);
        }

        public static void ClearSounds()
        {
            activeSounds.Clear();
        }
    }
} 