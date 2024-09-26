using UnityEngine;
using System.Collections.Generic;

public class SlimeSimulationQuadTextureMethod : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int width = 512;
    public int height = 512;
    public int numAgents = 1000;
    public float sensorDistance = 5.0f;
    public float sensorAngle = 0.25f * Mathf.PI;
    public float stepSize = 1.0f;
    public float rotationAngle = 0.1f * Mathf.PI;
    public float depositionAmount = 1.0f;
    public float decayFactor = 0.99f;

    public MeshRenderer trailMapRenderer;

    private Texture2D trailTexture;
    private List<Agent> agents;
    private Color[] trailMap;

    void Start()
    {
        trailTexture = new Texture2D(width, height);
        trailTexture.filterMode = FilterMode.Point;
        trailMap = new Color[width * height];

        agents = new List<Agent>();
        for (int i = 0; i < numAgents; i++)
        {
            agents.Add(new Agent()
            {
                position = new Vector2(Random.Range(0, width), Random.Range(0, height)),
                angle = Random.Range(0, 2 * Mathf.PI)
            });
        }

        trailMapRenderer.material.mainTexture = trailTexture;
    }

    void Update()
    {
        // Update agents
        foreach (var agent in agents)
        {
            UpdateAgent(agent);
        }

        // Update trail map texture
        trailTexture.SetPixels(trailMap);
        trailTexture.Apply();

        // Apply diffusion and decay
        ApplyDiffusionAndDecay();
    }

    void UpdateAgent(Agent agent)
    {
        // Sense the environment
        float frontSensor = Sense(agent, 0);
        float leftSensor = Sense(agent, sensorAngle);
        float rightSensor = Sense(agent, -sensorAngle);

        // Decide rotation
        if (frontSensor > leftSensor && frontSensor > rightSensor)
        {
            // Keep going straight
        }
        else if (leftSensor > rightSensor)
        {
            agent.angle += rotationAngle;
        }
        else
        {
            agent.angle -= rotationAngle;
        }

        // Move forward
        agent.position += new Vector2(Mathf.Cos(agent.angle), Mathf.Sin(agent.angle)) * stepSize;

        // Ensure the agent stays within bounds
        agent.position.x = Mathf.Repeat(agent.position.x, width);
        agent.position.y = Mathf.Repeat(agent.position.y, height);

        // Deposit trail
        DepositTrail(agent);
    }

    float Sense(Agent agent, float angleOffset)
    {
        Vector2 sensorPos = agent.position + new Vector2(Mathf.Cos(agent.angle + angleOffset), Mathf.Sin(agent.angle + angleOffset)) * sensorDistance;
        int x = Mathf.FloorToInt(sensorPos.x);
        int y = Mathf.FloorToInt(sensorPos.y);
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return trailMap[x + y * width].r;
    }

    void DepositTrail(Agent agent)
    {
        int x = Mathf.FloorToInt(agent.position.x);
        int y = Mathf.FloorToInt(agent.position.y);
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        Color currentColor = trailMap[x + y * width];
        currentColor.r += depositionAmount;
        trailMap[x + y * width] = currentColor;
    }

    void ApplyDiffusionAndDecay()
    {
        Color[] newTrailMap = new Color[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = trailMap[x + y * width];
                color *= decayFactor;

                // Apply simple diffusion
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = Mathf.Clamp(x + dx, 0, width - 1);
                        int ny = Mathf.Clamp(y + dy, 0, height - 1);
                        newTrailMap[nx + ny * width] += color * (1.0f / 9.0f);
                    }
                }
            }
        }

        trailMap = newTrailMap;
    }

    public class Agent
    {
        public Vector2 position;
        public float angle;
    }
}
                                                