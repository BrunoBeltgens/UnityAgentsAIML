using UnityEngine;

namespace ML_Agents.Scripts
{
    public class SoundManager
    {
        public static void PlaySound(Sound sound)
        {
            var colliders = Physics.OverlapSphere(sound.Origin, sound.Radius);
            foreach (var collider in colliders)
            {
                if (collider.TryGetComponent<ISoundListener>(out var listener))
                {
                    listener.HearSound(sound);
                }
            }
        }
    }
}
