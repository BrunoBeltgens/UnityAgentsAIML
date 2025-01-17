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
    [SerializeField] private float hearingRadius = 7.0f;
    public Position position;

    //kick power (force) 2000 is default
    const float k_Power = 10f;
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
    private GameObject ball;
    private const float possessionRewardRate = 0.005f; // Reward per second of possession
    private Vector3 opponentGoalPosition;
    private SoccerEnvController envController;

    EnvironmentParameters m_ResetParams;

    //private AudioSource audioSource;
    //public AudioClip moveSound;

    public float HearingRadius => hearingRadius;

    public void Update()
    {


    }

    public override void Initialize()
    {
        shouldPlaySound = false;
        ball = transform.parent.Find("Soccer Ball")?.gameObject;

        // Only assign a new agentID if it hasn't been set (i.e., if it’s 0)
        if (agentID == 0)
        {
            agentID = nextAgentID++;
        }

        envController = GetComponentInParent<SoccerEnvController>();
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
        opponentGoalPosition = initialPos + new Vector3(rotSign * 25.0f, 0, 0);
    }

    private bool BallIsShot(string agentTag)
    {
        if (ball == null) return false;
        // Debug.DrawLine(transform.position, opponentGoalPosition, Color.red, 2.0f); // Draw a line to the opponent's goal position
        // Debug.Log($"Opponent Goal Position: {opponentGoalPosition}"); // Log the position for debugging

        // Check if the ball's velocity is above a threshold and is moving towards the opponent's goal.
        Vector3 ballVelocity = ball.GetComponent<Rigidbody>().velocity;
        Vector3 directionToGoal = opponentGoalPosition - ball.transform.position;
        float dotProduct = Vector3.Dot(ballVelocity.normalized, directionToGoal.normalized);

        if (agentTag == "blueAgent")
        {
            SoccerEnvController.BlueTeamGoalAccuracySum += dotProduct; // Track goal accuracy for blue team
            SoccerEnvController.BlueTeamGoalAttempts++; // Track number of shots for blue team
        }
        else if (agentTag == "purpleAgent")
        {
            SoccerEnvController.PurpleTeamGoalAccuracySum += dotProduct; // Track goal accuracy for purple team
            SoccerEnvController.PurpleTeamGoalAttempts++; // Track number of shots for purple team
        }
        return ballVelocity.magnitude > 2.0f && dotProduct > 0.8f; // Ball is moving towards the opponent's goal significantly.
    }

    private bool BallIsBlocked()
    {
        if (ball == null) return false;

        // Check if the ball's position is close to the agent, and its velocity is reduced.
        Vector3 ballPosition = ball.transform.position;
        Vector3 agentPosition = transform.position;

        float distance = Vector3.Distance(ballPosition, agentPosition);

        // Assume the ball is blocked if it's near the agent and its velocity is below a threshold.
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        return distance < 1.5f && ballRb.velocity.magnitude < 1.0f;
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        if (shouldPlaySound)
        {
            PlayMovementSound();
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
        if (shouldPlaySound)
        {
            m_KickPower = 200f * m_KickPower;
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
        if (c.gameObject.CompareTag("ball") && (gameObject.CompareTag("blueAgent") || gameObject.CompareTag("purpleAgent")))
        {
            string agentTag = gameObject.tag;
            if (BallIsShot(agentTag))
            {
                AddReward(0.2f);
            }
            if (BallIsBlocked())
            {
                if (gameObject.CompareTag("blueAgent"))
                {
                    SoccerEnvController.BlueTeamBlockedShots++; // Track blocked shots for blue team
                }
                else if (gameObject.CompareTag("purpleAgent"))
                {
                    SoccerEnvController.PurpleTeamBlockedShots++; // Track blocked shots for purple team
                }
                AddReward(0.2f);
            }

            //Debug.Log("Ball touched by agent: " + gameObject.tag);
            if (agentTag != envController.lastTeamToControlBall) // Possession changed
            {
                //Debug.Log($"Possession changed from {envController.lastTeamToControlBall} to {agentTag}");
                // Penalize the previous team for losing possession
                if (envController.lastAgentToTouchBall != null)
                {
                    envController.lastAgentToTouchBall.GetComponent<AgentSoccer>().AddReward(-0.05f); // Penalty
                }
                // Reset possession timer
                if (agentTag == "blueAgent")
                {
                    SoccerEnvController.PurpleTeamTotalPossessionTime += envController.possessionTime;
                }
                else if (agentTag == "purpleAgent")
                {
                    SoccerEnvController.BlueTeamTotalPossessionTime += envController.possessionTime;
                }
                envController.possessionTime = 0f;
                envController.rel_possessionTime = 0f;
            }
            envController.lastAgentToTouchBall = gameObject; // Update to current agent
            envController.lastTeamToControlBall = agentTag;

            AddReward(.2f * m_BallTouch);

            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;

            Rigidbody ballRb = c.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.AddForce(dir * m_KickPower, ForceMode.Impulse);
            }
        }
    }

    void FixedUpdate()
    {

        // Reward for every second of possession
        if (envController.possessionTime - envController.rel_possessionTime >= 1f)
        {
            AddReward(possessionRewardRate); // Reward based on possession duration
            envController.rel_possessionTime = envController.possessionTime; // Reset timer
        }
    }


    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }
}