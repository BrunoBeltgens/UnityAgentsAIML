using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using MLAgents.Soccer;

public class SoccerEnvController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent;
        public Vector3 StartingPos;
        //public AudioSource AudioSource;
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    public GameObject ball;
    [HideInInspector]
    public Rigidbody ballRb;

    // Performance metrics
    public int BlueTeamGoals = 0;
    public int PurpleTeamGoals = 0;

    // Possession Time
    public float possessionTime = 0f;
    public float rel_possessionTime = 0f;
    public GameObject lastAgentToTouchBall;
    public string lastTeamToControlBall;

    // To get the average goal accuracy we need the sum and the number of attempts
    public static float BlueTeamGoalAccuracySum = 0.0f;
    public static int BlueTeamGoalAttempts = 0;
    public static float PurpleTeamGoalAccuracySum = 0.0f;
    public static int PurpleTeamGoalAttempts = 0;
    // Blocked Shots
    public static int BlueTeamBlockedShots = 0;
    public static int PurpleTeamBlockedShots = 0;
    // Total Possession Time
    public static float BlueTeamTotalPossessionTime = 0.0f;
    public static float PurpleTeamTotalPossessionTime = 0.0f;

    Vector3 m_BallStartingPos;
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    private SoccerSettings m_SoccerSettings;


    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    private int m_ResetTimer;

    void Start()
    {
        if (ball == null)
        {
            Debug.LogError("[SoccerEnvController] Ball reference not set!");
            return;
        }

        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();
        ballRb = ball.GetComponent<Rigidbody>();
        
        if (ballRb == null)
        {
            Debug.LogError("[SoccerEnvController] Ball must have a Rigidbody component!");
            return;
        }

        m_BallStartingPos = ball.transform.position;
        
        // Add a single AudioListener to the environment controller
        // if (FindObjectOfType<AudioListener>() == null)
        // {
        //     gameObject.AddComponent<AudioListener>();
        // }

        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            if (item.Agent.team == Team.Blue)
            {
                m_BlueAgentGroup.RegisterAgent(item.Agent);
            }
            else
            {
                m_PurpleAgentGroup.RegisterAgent(item.Agent);
            }
        }
        ResetScene();
    }

    void FixedUpdate()
    {
        possessionTime += Time.fixedDeltaTime;
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }


    public void ResetBall()
    {
        var randomPosX = Random.Range(-2.5f, 2.5f);
        var randomPosZ = Random.Range(-2.5f, 2.5f);

        ball.transform.position = m_BallStartingPos + new Vector3(randomPosX, 0f, randomPosZ);
        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

    }

    public void GoalTouched(Team scoredTeam)
    {
        if (scoredTeam == Team.Blue)
        {
            m_BlueAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_PurpleAgentGroup.AddGroupReward(-1);
        }
        else
        {
            m_PurpleAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_BlueAgentGroup.AddGroupReward(-1);
        }
        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();

        if (scoredTeam == Team.Blue)
        {
            BlueTeamGoals++;
        }
        else
        {
            PurpleTeamGoals++;
        }

        ResetScene();

    }


    public void ResetScene()
    {
        m_ResetTimer = 0;

        Debug.Log("____________________________");
        Debug.Log($"Blue Team Goals: {BlueTeamGoals}");
        Debug.Log($"Purple Team Goals: {PurpleTeamGoals}");
        Debug.Log($"Blue Team Shot Accuracy: {BlueTeamGoalAccuracySum / (BlueTeamGoalAttempts == 0 ? 1 : BlueTeamGoalAttempts)}");
        Debug.Log($"Purple Team Shot Accuracy: {PurpleTeamGoalAccuracySum / (PurpleTeamGoalAttempts == 0 ? 1 : PurpleTeamGoalAttempts)}");
        Debug.Log($"Blue Team Blocked Shots: {BlueTeamBlockedShots}");
        Debug.Log($"Purple Team Blocked Shots: {PurpleTeamBlockedShots}");
        Debug.Log($"Blue Team Total Possession Time: {BlueTeamTotalPossessionTime}");
        Debug.Log($"Purple Team Total Possession Time: {PurpleTeamTotalPossessionTime}");

        possessionTime = 0f;
        rel_possessionTime = 0f;
        lastAgentToTouchBall = null;
        lastTeamToControlBall = null;

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = item.Agent.initialPos + new Vector3(randomPosX, 0f, 0f);
            var rot = item.Agent.rotSign * Random.Range(80.0f, 100.0f);
            var newRot = Quaternion.Euler(0, rot, 0);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        //Reset Ball
        ResetBall();
    }

    public void KickBall(Vector3 kickDirection)
    {
    }
}