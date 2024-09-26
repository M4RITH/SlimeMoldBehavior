using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlimeSiulationOneAgentPerCell : MonoBehaviour
{
    [Header("UI Settings")]
    public Slider sensorDistanceSlider;
    public Slider stepSizeSlider;
    public Slider decayFactorSlider;
    public InputField numAgentsInput;
    public Text sensorDistanceLabel;
    public Text stepSizeLabel;
    public Text numAgentsLabel;
    public Text decayFactorLabel;

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
    private HashSet<Vector2Int> occupiedCells;

    void Start()
    {
        //UI Elements
        // Initialize the sliders and input fields with current parameter values
        sensorDistanceSlider.value = sensorDistance;
        stepSizeSlider.value = stepSize;
        decayFactorSlider.value = decayFactor;
        numAgentsInput.text = numAgents.ToString();

        // Update the labels to display initial values
        sensorDistanceLabel.text = "Sensor Distance: " + sensorDistance;
        stepSizeLabel.text = "Step Size: " + stepSize;
        decayFactorLabel.text = "DecayFactor: " + decayFactor;
        numAgentsLabel.text = "Number of Agents: " + numAgents;

        // Setup listener for when UI values change
        sensorDistanceSlider.onValueChanged.AddListener(UpdateSensorDistance);
        stepSizeSlider.onValueChanged.AddListener(UpdateStepSize);
        decayFactorSlider.onValueChanged.AddListener(UpdateDecayFactor);
        numAgentsInput.onEndEdit.AddListener(UpdateNumAgents);

        trailTexture = new Texture2D(width, height);
        trailTexture.filterMode = FilterMode.Point;
        trailMap = new Color[width * height];

        agents = new List<Agent>();
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

        trailMapRenderer.material.mainTexture = trailTexture;
    }

    //UI Methods
    public void UpdateSensorDistance(float newValue)
    {
        sensorDistance = newValue;
        sensorDistanceLabel.text = "Sensor Distance: " + sensorDistance;
    }

    public void UpdateStepSize(float newValue)
    {
        stepSize = newValue;
        stepSizeLabel.text = "Step Size: " + stepSize;
    }

    public void UpdateDecayFactor(float newValue)
    {
        decayFactor = newValue;
        decayFactorLabel.text = "DecayFactor: " + stepSize;
    }

    public void UpdateNumAgents(string newValue)
    {
        int parsedValue;
        if (int.TryParse(newValue, out parsedValue))
        {
            numAgents = parsedValue;
            numAgentsLabel.text = "Number of Agents: " + numAgents;

            // Optional: Reset the agents list to apply the new number of agents
            ResetAgents();
        }
    }

    public void ResetAgents()
    {
        agents.Clear();
        occupiedCells.Clear();

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
