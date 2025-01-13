using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class StrikerAgentAudio : AgentSoccer{
    private AudioSource audioSource;
    private float[] spectrumData;
    private const int SPECTRUM_SIZE = 256;
    
    private float[] leftChannelData;
    private float[] rightChannelData;
    
    [SerializeField] private float directionSensitivity = 1.0f;
    [SerializeField] private float soundThreshold = 0.1f;

    public override void Initialize(){
        base.Initialize();
        audioSource = GetComponent<AudioSource>();
        if(audioSource==null){
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.spatialize = true;
        audioSource.spatialBlend = 1.0f;

        spectrumData = new float[SPECTRUM_SIZE];
        leftChannelData = new float[SPECTRUM_SIZE];
        rightChannelData = new float[SPECTRUM_SIZE];

        AudioListener listener = GetComponent<AudioListener>();
        if(listener == null){
            listener = gameObject.AddComponent<AudioListener>();

        }

    }

private Vector3 ProcessDirectionalSound()
    {
        // Get spectrum data for both channels
        audioSource.GetSpectrumData(leftChannelData, 0, FFTWindow.BlackmanHarris);  // Left channel
        audioSource.GetSpectrumData(rightChannelData, 1, FFTWindow.BlackmanHarris); // Right channel
        
        float leftIntensity = 0f;
        float rightIntensity = 0f;
        
        for (int i = 0; i < SPECTRUM_SIZE; i++)
        {
            leftIntensity += leftChannelData[i];
            rightIntensity += rightChannelData[i];
        }
        
        leftIntensity /= SPECTRUM_SIZE;
        rightIntensity /= SPECTRUM_SIZE;
        
        // process sound above threshold only
        if (leftIntensity < soundThreshold && rightIntensity < soundThreshold)
        {
            return Vector3.zero;
        }
        
        // get direction based on intensity difference
        float direction = (rightIntensity - leftIntensity) * directionSensitivity;
        
        //  direction vector for understanding where sound is coming from
        return new Vector3(direction, 0f, Mathf.Max(leftIntensity, rightIntensity));
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);

        Vector3 soundDirection = ProcessDirectionalSound();
        
        sensor.AddObservation(soundDirection.x); // Left-right direction
        sensor.AddObservation(soundDirection.z); // Sound intensity
        
        for (int i = 0; i < SPECTRUM_SIZE; i += 8) 
        {
            sensor.AddObservation(spectrumData[i]);
        }
    }
}

public class BallAudioProcessor : MonoBehaviour
{
    private AudioSource audioSource;
    private float[] impactData;
    private const int IMPACT_SAMPLE_SIZE = 128;
    
    // another threshold for impact
    [SerializeField] private float impactThreshold = 0.2f;
    [SerializeField] private float minTimeBetweenImpacts = 0.1f;
    
    private float lastImpactTime;
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        impactData = new float[IMPACT_SAMPLE_SIZE];
        lastImpactTime = 0f;
        
        audioSource.spatialize = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f;
    }
    
    private void Update()
    {
        audioSource.GetSpectrumData(impactData, 0, FFTWindow.BlackmanHarris);
        
        if (DetectImpact())
        {
            ProcessImpact();
        }
    }
    
    private bool DetectImpact()
    {
        if (Time.time - lastImpactTime < minTimeBetweenImpacts)
            return false;
            
        float totalIntensity = 0f;
        
        // Look for sudden spikes in audio intensity
        for (int i = 0; i < IMPACT_SAMPLE_SIZE; i++)
        {
            totalIntensity += impactData[i];
        }
        
        float averageIntensity = totalIntensity / IMPACT_SAMPLE_SIZE;
        
        return averageIntensity > impactThreshold;
    }
    
    private void ProcessImpact()
    {
        lastImpactTime = Time.time;
        
        // Analyze the impact frequency distribution to determine impact type
        float lowFreqIntensity = 0f;
        float highFreqIntensity = 0f;
        
        // Split spectrum into low and high frequencies
        for (int i = 0; i < IMPACT_SAMPLE_SIZE; i++)
        {
            if (i < IMPACT_SAMPLE_SIZE / 2)
            {
                lowFreqIntensity += impactData[i];
            }
            else
            {
                highFreqIntensity += impactData[i];
            }
        }
        
        // for impact hardness only (high means harder surface low means softer surface)
        float impactHardness = highFreqIntensity / (lowFreqIntensity + 0.0001f);
        
    }
}