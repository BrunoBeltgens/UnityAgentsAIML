using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine.PlayerLoop;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
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
    float m_BallTouch;
    public float hearingRadius = 10f;
    public Position position;

    //kick power (force) 2000 is default
    const float k_Power = 200f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;
    public bool shouldPlaySound;
    int agentID;
    private static int nextAgentID = 1;

    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;
    public float ballID;

    EnvironmentParameters m_ResetParams;

    private AudioSource audioSource;
    public AudioClip moveSound;

    public void Update()
    {
        DetectAndRespondToSound();

        
        
    }

    public override void Initialize()
{
    shouldPlaySound = false;

    // Only assign a new agentID if it hasn't been set (i.e., if it’s 0)
    if (agentID == 0)
    {
        agentID = nextAgentID++;
    }

    SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
    if (envController != null)
    {
        m_Existential = 1f / envController.MaxEnvironmentSteps;
    }
    else
    {
        m_Existential = 1f / MaxStep;
    }

    m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
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
    if (position == Position.Goalie)
    {
        m_LateralSpeed = 1.5f;
        m_ForwardSpeed = 1.0f;
    }
    else if (position == Position.Striker)
    {
        m_LateralSpeed = 0.5f;
        m_ForwardSpeed = 1.5f;
    }
    else
    {
        m_LateralSpeed = 0.3f;
        m_ForwardSpeed = 1.0f;
    }
    m_SoccerSettings = FindObjectOfType<SoccerSettings>();
    agentRb = GetComponent<Rigidbody>();
    agentRb.maxAngularVelocity = 500;

    m_ResetParams = Academy.Instance.EnvironmentParameters;

    audioSource = gameObject.AddComponent<AudioSource>();
    audioSource.clip = moveSound;
    audioSource.spatialize = true;
}

public void DetectAndRespondToSound()
{
    GameObject ball = transform.parent.Find("Soccer Ball")?.gameObject;
    if (ball == null) return;

    float distanceToBall = Vector3.Distance(this.transform.position, ball.transform.position);
    if (distanceToBall <= hearingRadius)
    {
        Vector3 directionToSound = (ball.transform.position - transform.position).normalized;
        // Add the sound observation to the agent's state
        if (!audioSource.isPlaying)
        {
            PlayMovementSound();
        }
        // Rotate towards the sound source
        //RotateTowards(directionToSound);
    }
}

private void RotateTowards(Vector3 direction)
{
    if(direction == Vector3.zero)
    {
        return;
    }
    Quaternion lookRotation = Quaternion.LookRotation(direction);
    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 2f);
}
   public void MoveAgent(ActionSegment<int> act)
{
    if (shouldPlaySound)
    {
        DetectAndRespondToSound();
    }

    var dirToGo = Vector3.zero;
    var rotateDir = Vector3.zero;

    m_KickPower = 0f;

    var forwardAxis = act[0];
    var rightAxis = act[1];
    var rotateAxis = act[2];

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

    switch (rightAxis)
    {
        case 1:
            dirToGo = transform.right * m_LateralSpeed;
            break;
        case 2:
            dirToGo = transform.right * -m_LateralSpeed;
            break;
    }

    switch (rotateAxis)
    {
        case 1:
            rotateDir = transform.up * -1f;
            break;
        case 2:
            rotateDir = transform.up * 1f;
            break;
    }

    transform.Rotate(rotateDir, Time.deltaTime * 100f);
    agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed, ForceMode.VelocityChange);

    if (dirToGo != Vector3.zero)
    {
        PlayMovementSound();
    }
}

    private void PlayMovementSound()
    {
        if (!audioSource.isPlaying && shouldPlaySound)
        {
            audioSource.Play();
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (position == Position.Goalie)
        {
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            AddReward(-m_Existential);
        }
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[2] = 2;
        }
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[1] = 2;
        }
    }

    void OnCollisionEnter(Collision c)
    {        
        if (c.gameObject.CompareTag("ball"))
        {
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            shouldPlaySound = true;
            m_KickPower = k_Power;
            
            // Apply force to the ball using m_KickPower
            Rigidbody ballRb = c.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.AddForce(dir * m_KickPower, ForceMode.Impulse);
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }

    public void HearSound(Unity.MLAgents.Sound sound)
    {
        // Handle the sound detection stuff here
        Debug.Log($"Sound heard at {sound.Position} with radius {sound.Radius}");
        shouldPlaySound = true;
    }
}