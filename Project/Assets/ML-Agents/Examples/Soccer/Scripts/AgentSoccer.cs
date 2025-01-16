using ML_Agents.Examples.Soccer.Scripts;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    // Note that the detectable tags are different for the blue and purple teams. The order is:
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;

    float m_KickPower;
    float m_BallTouch;  // From curriculum or environment parameters
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    [HideInInspector] public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;
    public GameObject ballObject;

    // We keep a reference to SoundSensor so we can call 'hearSound(...)'
    // But we don't collect observations from it directly here (the SensorComponent handles that).
    private SoundSensor soundSensor;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        // Grab the SoundSensor if present; add it if missing (optional).
        soundSensor = GetComponent<SoundSensor>();
        if (soundSensor == null)
        {
            soundSensor = gameObject.AddComponent<SoundSensor>();
        }

        // Setup existential reward
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            rotSign = -1f;
        }

        // Configure movement speeds based on position
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }

        // Basic environment / agent references
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        base.Initialize();
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Existential reward logic
        if (position == Position.Goalie)
        {
            // Existential bonus for Goalies
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
        }

        // Move agent based on the chosen action
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis   = act[1];
        var rotateAxis  = act[2];

        // Forward/back
        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        // Left/right
        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        // Rotation
        switch (rotateAxis)
        {
            case 1:
                rotateDir = -transform.up;
                break;
            case 2:
                rotateDir = transform.up;
                break;
        }

        // Apply rotation
        transform.Rotate(rotateDir, Time.deltaTime * 100f);

        // Apply movement
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed, ForceMode.VelocityChange);

        // Emit a footstep sound if we're moving
        if (dirToGo != Vector3.zero)
        {
            soundSensor?.hearSound(transform.position, 2.0f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // forward/back
        if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
        if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 2;
        // rotate
        if (Input.GetKey(KeyCode.A)) discreteActionsOut[2] = 1;
        if (Input.GetKey(KeyCode.D)) discreteActionsOut[2] = 2;
        // right/left
        if (Input.GetKey(KeyCode.E)) discreteActionsOut[1] = 1;
        if (Input.GetKey(KeyCode.Q)) discreteActionsOut[1] = 2;
    }

    /// <summary>
    /// Used to provide a "kick" to the ball (and emit a louder sound).
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power;
        }

        if (c.gameObject.CompareTag("ball"))
        {
            // A small reward for contacting the ball
            AddReward(.2f * m_BallTouch);

            // Kick sound
            soundSensor?.hearSound(transform.position, 5.0f);

            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {
        // E.g., from environment parameters
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }
}
