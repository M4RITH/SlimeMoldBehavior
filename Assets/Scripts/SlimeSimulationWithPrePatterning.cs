using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stimulus
{
    public Vector2Int position;
    public float intensity;
}

public class SlimeSimulationWithPrePatterning : MonoBehaviour
{
    [Header("Stimuli")]
    public List<Stimulus> stimuli = new List<Stimulus>();

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

    [Header("Pre-Patterning Settings")]
    public float prePatternWeight = 0.05f; 

    [Header("Mesh Settings")]
    public MeshRenderer trailMapRenderer;

    private Texture2D trailTexture;
    private Color[] trailMap;
    private List<Agent> agents;
    private HashSet<Vector2Int> occupiedCells;
    private int lastStimuliCount = 0;

    void Start()
    {
        trailTexture = new Texture2D(width, height);
        trailTexture.filterMode = FilterMode.Point;
        trailMap = new Color[width * height];

        InitializePrePattern();
        InitializeAgents();

        trailMapRenderer.material.mainTexture = trailTexture;
    }

    void InitializePrePattern()
    {
        foreach (var stimulus in stimuli)
        {
            AddStimulusPoint(stimulus.position.x, stimulus.position.y, stimulus.intensity);
        }
    }

    void InitializeAgents()
    {
        agents = new List<Agent>(numAgents);
        occupiedCells = new HashSet<Vector2Int>();

        for (int i = 0; i < numAgents; i++)
        {
            Vector2 initialPosition = new Vector2(Random.Range(0, width), Random.Range(0, height));
            Vector2Int gridPos = Vector2Int.FloorToInt(initialPosition);

            if (!occupiedCells.Contains(gridPos))
            {
                Agent agent = new Agent()
                {
                    position = initialPosition,
                    angle = Random.Range(0, 2 * Mathf.PI)
                };
                agents.Add(agent);
                occupiedCells.Add(gridPos);
            }
            else
            {
                i--; // Retry
            }
        }
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
        float frontSensor = Sense(agent, 0);
        float leftSensor = Sense(agent, sensorAngle);
        float rightSensor = Sense(agent, -sensorAngle);

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

    float Sense(Agent agent, float angleOffset)
    {
        Vector2 sensorPos = agent.position + new Vector2(Mathf.Cos(agent.angle + angleOffset), Mathf.Sin(agent.angle + angleOffset)) * sensorDistance;
        int x = Mathf.FloorToInt(sensorPos.x);
        int y = Mathf.FloorToInt(sensorPos.y);
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return trailMap[x + y * width].r + trailMap[x + y * width].g;
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
                        newTrailMap[nx + ny * width] += currentColor * (1.0f / 9.0f);
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
