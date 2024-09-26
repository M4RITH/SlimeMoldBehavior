using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimSteinbockMaze : MonoBehaviour
{
    [System.Serializable]
    public class Obstacle
    {
        public Rect rect;
        public Color color = Color.gray;
    }

    [System.Serializable]
    public class SpawnPoint
    {
        public Vector2 position;
        public float radius = 5f;
        public int numAgents = 1000;
        public Color color = Color.red;
    }

    [Header("Obstacle Settings")]
    public List<Obstacle> obstacles = new List<Obstacle>();

    [Header("Spawn Settings")]
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    public int agentsPerFrame = 10;

    [Header("Stimuli")]
    public List<Stimulus> stimuli = new List<Stimulus>();

    [Header("Simulation Settings")]
    public int width = 200;
    public int height = 200;
    public int numAgents = 6000;
    public float sensorDistance = 9.0f;
    public float sensorSize = 1.0f;
    public float sensorAngle = 0.25f * Mathf.PI;
    public float stepSize = 1.0f;
    public float rotationAngle = 0.25f * Mathf.PI;
    public float depositionAmount = 5.0f;
    public float decayFactor = 0.1f;

    [Header("Pre-Patterning Settings")]
    public float prePatternWeight = 0.05f; // Adjustable in the range 0.01 to 0.1

    [Header("Mesh Settings")]
    public MeshRenderer trailMapRenderer;

    private Texture2D trailTexture;
    private Color[] trailMap;
    private List<Agent> agents;
    private HashSet<Vector2Int> occupiedCells;
    private int lastStimuliCount = 0;
    private int totalAgents = 0;
    private int spawnedAgents = 0;

    void Start()
    {
        trailTexture = new Texture2D(width, height);
        trailTexture.filterMode = FilterMode.Point;
        trailMap = new Color[width * height];

        InitializePrePattern();
        CalculateTotalAgents();
        InitializeAgents();
        DrawObstacles();
        DrawSpawnPoints();

        trailMapRenderer.material.mainTexture = trailTexture;
        StartCoroutine(SpawnAgentsOverTime());
    }

    void InitializePrePattern()
    {
        foreach (var stimulus in stimuli)
        {
            AddStimulusPoint(stimulus.position.x, stimulus.position.y, stimulus.intensity);
        }
    }


    void CalculateTotalAgents()
    {
        totalAgents = 0;
        foreach (var spawnPoint in spawnPoints)
        {
            totalAgents += spawnPoint.numAgents;
        }
    }

    void InitializeAgents()
    {
        agents = new List<Agent>(totalAgents);
        occupiedCells = new HashSet<Vector2Int>();
    }

    void DrawObstacles()
    {
        foreach (var obstacle in obstacles)
        {
            for (int x = (int)obstacle.rect.xMin; x < (int)obstacle.rect.xMax; x++)
            {
                for (int y = (int)obstacle.rect.yMin; y < (int)obstacle.rect.yMax; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        trailMap[x + y * width] = obstacle.color;
                    }
                }
            }
        }
    }

    void DrawSpawnPoints()
    {
        foreach (var spawnPoint in spawnPoints)
        {
            int x = Mathf.FloorToInt(spawnPoint.position.x);
            int y = Mathf.FloorToInt(spawnPoint.position.y);
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                trailMap[x + y * width] = spawnPoint.color;
            }
        }
    }

    IEnumerator SpawnAgentsOverTime()
    {
        while (spawnedAgents < totalAgents)
        {
            for (int i = 0; i < agentsPerFrame && spawnedAgents < totalAgents; i++)
            {
                SpawnAgent();
            }
            yield return new WaitForEndOfFrame();
        }
    }

    void SpawnAgent()
    {
        SpawnPoint selectedSpawnPoint = null;
        int remainingAgents = totalAgents - spawnedAgents;

        // Select a spawn point based on remaining agents
        float randomValue = Random.Range(0, remainingAgents);
        float cumulativeAgents = 0;

        foreach (var spawnPoint in spawnPoints)
        {
            cumulativeAgents += spawnPoint.numAgents;
            if (randomValue < cumulativeAgents)
            {
                selectedSpawnPoint = spawnPoint;
                break;
            }
        }

        if (selectedSpawnPoint == null)
        {
            return; // No valid spawn point found
        }

        Vector2 offset = Random.insideUnitCircle * selectedSpawnPoint.radius;
        Vector2 position = selectedSpawnPoint.position + offset;
        position.x = Mathf.Repeat(position.x, width);
        position.y = Mathf.Repeat(position.y, height);
        Vector2Int gridPos = Vector2Int.FloorToInt(position);

        if (!occupiedCells.Contains(gridPos))
        {
            Agent agent = new Agent()
            {
                position = position,
                angle = Random.Range(0, 2 * Mathf.PI)
            };
            agents.Add(agent);
            occupiedCells.Add(gridPos);
            spawnedAgents++;
        }
    }

    void Update()
    {
        // Update agents
        foreach (var agent in agents)
        {
            UpdateAgent(agent);
        }

        DrawSpawnPoints();

        // Update trail map texture
        trailTexture.SetPixels(trailMap);
        trailTexture.Apply();

        // Check if stimuli have changed
        if (stimuli.Count != lastStimuliCount)
        {
            UpdateStimuli();
            lastStimuliCount = stimuli.Count;
        }

        // Apply diffusion and decay
        ApplyDiffusionAndDecay();
        ReapplyStimuli();
    }

    void UpdateAgent(Agent agent)
    {
        Vector2Int currentGridPos = Vector2Int.FloorToInt(agent.position);

        // Sense the environment including the chemoattractant layer
        float frontSensor = Sense(agent, 0, sensorSize);
        float leftSensor = Sense(agent, sensorAngle, sensorSize);
        float rightSensor = Sense(agent, -sensorAngle, sensorSize);

        if (frontSensor > leftSensor && frontSensor > rightSensor)
        {
            // Keep going straight
        }
        else if (frontSensor < leftSensor && frontSensor < rightSensor)
        {
            // Rotate randomly left or right
            agent.angle += Random.value < 0.5f ? rotationAngle : -rotationAngle;
        }
        else if (leftSensor < rightSensor)
        {
            // Rotate right
            agent.angle -= rotationAngle;
        }
        else if (rightSensor < leftSensor)
        {
            // Rotate left
            agent.angle += rotationAngle;
        }
        else
        {

        }

        Vector2 newPosition = agent.position + new Vector2(Mathf.Cos(agent.angle), Mathf.Sin(agent.angle)) * stepSize;
        newPosition.x = Mathf.Repeat(newPosition.x, width);
        newPosition.y = Mathf.Repeat(newPosition.y, height);

        // Check for obstacle collision
        bool collision = false;
        foreach (var obstacle in obstacles)
        {
            if (obstacle.rect.Contains(newPosition))
            {
                collision = true;
                break;
            }
        }

        if (collision)
        {
            // Reflect the agent
            agent.angle = Random.Range(0, 2 * Mathf.PI);
            newPosition = agent.position; // Stay at the current position
        }

        Vector2Int newGridPos = Vector2Int.FloorToInt(newPosition);

        if (!occupiedCells.Contains(newGridPos) || newGridPos == currentGridPos)
        {
            occupiedCells.Remove(currentGridPos);
            agent.position = newPosition;
            occupiedCells.Add(newGridPos);
            DepositTrail(agent);
        }
        else
        {
            agent.angle = Random.Range(0, 2 * Mathf.PI);
        }
    }

    float Sense(Agent agent, float angleOffset, float sensorSize)
    {
        Vector2 sensorPos = agent.position + new Vector2(Mathf.Cos(agent.angle + angleOffset), Mathf.Sin(agent.angle + angleOffset)) * sensorDistance;
        float minX = Mathf.Max(0, Mathf.FloorToInt(sensorPos.x - sensorSize / 2));
        float maxX = Mathf.Min(width - 1, Mathf.CeilToInt(sensorPos.x + sensorSize / 2));
        float minY = Mathf.Max(0, Mathf.FloorToInt(sensorPos.y - sensorSize / 2));
        float maxY = Mathf.Min(height - 1, Mathf.CeilToInt(sensorPos.y + sensorSize / 2));

        float sensorValue = 0;
        for (int x = (int)minX; x <= (int)maxX; x++)
        {
            for (int y = (int)minY; y <= (int)maxY; y++)
            {
                sensorValue += trailMap[x + y * width].r + trailMap[x + y * width].g;
            }
        }

        return sensorValue;
    }

    void DepositTrail(Agent agent)
    {
        int x = Mathf.FloorToInt(agent.position.x);
        int y = Mathf.FloorToInt(agent.position.y);
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        // Don't deposit trail on any obstacle
        bool onObstacle = false;
        foreach (var obstacle in obstacles)
        {
            if (obstacle.rect.Contains(new Vector2(x, y)))
            {
                onObstacle = true;
                break;
            }
        }

        if (!onObstacle)
        {
            Color currentColor = trailMap[x + y * width];
            currentColor.r += depositionAmount;
            trailMap[x + y * width] = currentColor;
        }
    }

    void ApplyDiffusionAndDecay()
    {
        Color[] newTrailMap = new Color[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                /// Check if current cell is part of any obstacle
                bool isObstacle = false;
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.rect.Contains(new Vector2(x, y)))
                    {
                        newTrailMap[x + y * width] = obstacle.color;
                        isObstacle = true;
                        break;
                    }
                }

                if (isObstacle)
                {
                    continue;
                }

                Color currentColor = trailMap[x + y * width];

                currentColor.r *= decayFactor;
                currentColor.g *= decayFactor;

                // Apply simple diffusion
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = Mathf.Clamp(x + dx, 0, width - 1);
                        int ny = Mathf.Clamp(y + dy, 0, height - 1);

                        // Skip diffusion from obstacle cells
                        bool isNeighborObstacle = false;
                        foreach (var obstacle in obstacles)
                        {
                            if (obstacle.rect.Contains(new Vector2(nx, ny)))
                            {
                                isNeighborObstacle = true;
                                break;
                            }
                        }

                        if (!isNeighborObstacle)
                        {
                            newTrailMap[nx + ny * width] += currentColor * (1.0f / 9.0f);
                        }
                    }
                }
            }
        }

        trailMap = newTrailMap;
        ProjectStimuli();
    }

    void ProjectStimuli()
    {
        foreach (var stimulus in stimuli)
        {
            int x = stimulus.position.x;
            int y = stimulus.position.y;
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                AddStimulusPoint(x, y, stimulus.intensity);

                // Adding a small amount to neighboring cells for a more visible effect
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = Mathf.Clamp(x + dx, 0, width - 1);
                        int ny = Mathf.Clamp(y + dy, 0, height - 1);
                        if (nx != x || ny != y)
                        {
                            trailMap[nx + ny * width].g += stimulus.intensity * 0.5f;
                        }
                    }
                }
            }
        }
    }

    void AddStimulusPoint(int x, int y, float intensity)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            float weightedIntensity = intensity * prePatternWeight;
            trailMap[x + y * width].g = weightedIntensity;
        }
    }

    public void UpdateStimuli()
    {
        // Clear existing stimuli
        for (int i = 0; i < width * height; i++)
        {
            trailMap[i].g = 0;
        }

        // Apply new stimuli
        ReapplyStimuli();
    }

    public void UpdateStimulusIntensity(int index, float newIntensity)
    {
        if (index >= 0 && index < stimuli.Count)
        {
            stimuli[index].intensity = newIntensity;
            UpdateStimuli();
        }
    }

    void ReapplyStimuli()
    {
        foreach (var stimulus in stimuli)
        {
            AddStimulusPoint(stimulus.position.x, stimulus.position.y, stimulus.intensity);
        }
    }

    public class Agent
    {
        public Vector2 position;
        public float angle;
    }
}
