using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimWithoutOccupancyGrid : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int width = 200;
    public int height = 200;
    public int numAgents = 6000;
    public float sensorDistance = 9.0f;
    public float sensorAngle = 0.125f * Mathf.PI;
    public float stepSize = 1.0f;
    public float rotationAngle = 0.25f * Mathf.PI;
    public float depositionAmount = 5.0f;
    public float decayFactor = 0.1f;

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
            Vector2 initialPosition = new Vector2(Random.Range(0, width), Random.Range(0, height));

            Agent agent = new Agent()
            {
                position = initialPosition,
                angle = Random.Range(0, 2 * Mathf.PI)
            };
            agents.Add(agent);
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

        // Calculate new position
        Vector2 newPosition = agent.position + new Vector2(Mathf.Cos(agent.angle), Mathf.Sin(agent.angle)) * stepSize;

        // Apply periodic boundary conditions
        newPosition.x = Mathf.Repeat(newPosition.x, width);
        newPosition.y = Mathf.Repeat(newPosition.y, height);

        // Simple collision detection
        bool canMove = true;
        foreach (var otherAgent in agents)
        {
            if (otherAgent != agent && Vector2.Distance(newPosition, otherAgent.position) < 1f)
            {
                canMove = false;
                break;
            }
        }

        if (canMove)
        {
            agent.position = newPosition;
            DepositTrail(agent);
        }
        else
        {
            // If movement is blocked, randomly reorient the agent
            agent.angle = Random.Range(0, 2 * Mathf.PI);
            // No trail deposition in this case
        }
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
