using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeSimulationBoundaryAgentsReflect : MonoBehaviour
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
    private Dictionary<Vector2Int, Agent> occupancyGrid;

    void Start()
    {
        trailTexture = new Texture2D(width, height);
        trailTexture.filterMode = FilterMode.Point;
        trailMap = new Color[width * height];

        agents = new List<Agent>();
        occupancyGrid = new Dictionary<Vector2Int, Agent>();

        for (int i = 0; i < numAgents; i++)
        {
            Vector2 initialPosition = new Vector2(Random.Range(0, width), Random.Range(0, height));
            Vector2Int gridPos = Vector2Int.FloorToInt(initialPosition);

            if (!occupancyGrid.ContainsKey(gridPos)) // Only add agent if position is not occupied
            {
                Agent agent = new Agent()
                {
                    position = initialPosition,
                    angle = Random.Range(0, 2 * Mathf.PI)
                };
                agents.Add(agent);
                occupancyGrid[gridPos] = agent;
            }
            else
            {
                // If position is occupied, find a new random position
                i--; // Retry this iteration
            }
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
        Vector2Int currentGridPos = Vector2Int.FloorToInt(agent.position);

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
        Vector2 newPosition = agent.position + new Vector2(Mathf.Cos(agent.angle), Mathf.Sin(agent.angle)) * stepSize;

        // Check for boundary conditions 
        if (newPosition.x < 0 || newPosition.x >= width || newPosition.y < 0 || newPosition.y >= height)
        {
            // Reflect the agent back towards the center
            Vector2 directionToCenter = new Vector2(width / 2, height / 2) - agent.position;
            agent.angle = Mathf.Atan2(directionToCenter.y, directionToCenter.x);
        }
        else
        {
            // Move to new position if within bounds
            Vector2Int newGridPos = Vector2Int.FloorToInt(newPosition);

            if (!occupancyGrid.ContainsKey(newGridPos)) // Only move if new position is not occupied
            {
                // Remove agent from current position in the grid
                occupancyGrid.Remove(currentGridPos);

                // Update agent's position
                agent.position = newPosition;

                // Update grid with new position
                occupancyGrid[newGridPos] = agent;

                // Ensure the agent stays within bounds
                agent.position.x = Mathf.Repeat(agent.position.x, width);
                agent.position.y = Mathf.Repeat(agent.position.y, height);

                // Deposit trail
                DepositTrail(agent);
            }
            else
            {
                // If movement is blocked, randomly reorient the agent
                agent.angle = Random.Range(0, 2 * Mathf.PI);
            }
        }

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
