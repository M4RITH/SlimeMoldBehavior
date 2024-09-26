using UnityEngine;
using System.Collections.Generic;

public class SlimeSimulationDrawTextureMethod : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int width = 512;
    public int height = 512;
    public int numAgents = 1000;
    public float sensorDistance = 5.0f;
    public float stepSize = 1.0f;
    public float rotationAngle = 0.1f * Mathf.PI;
    public float trailIntensity = 1.0f;
    public float decayFactor = 0.99f;
    public int stepsPerFrame = 1;

    private Agent[] agents;
    private Color[] trailMap;

    void Start()
    {
        Init(); 
    }

    void Init()
    {
        trailMap = new Color[width * height];
        agents = new Agent[numAgents];

        for (int i = 0; i < numAgents; i++)
        {
            agents[i] = new Agent
            {
                position = new Vector2(Random.Range(0, width), Random.Range(0, height)),
                angle = Random.Range(0, Mathf.PI * 2)
            };
        }
    }

    void MoveAgents()
    {
        foreach (var agent in agents)
        {
            agent.position += new Vector2(Mathf.Cos(agent.angle), Mathf.Sin(agent.angle)) * stepSize;
            agent.position.x = Mathf.Repeat(agent.position.x, width);
            agent.position.y = Mathf.Repeat(agent.position.y, height);
        }
    }

    void DepositTrail()
    {
        foreach (var agent in agents)
        {
            int x = Mathf.FloorToInt(agent.position.x);
            int y = Mathf.FloorToInt(agent.position.y);
            trailMap[x + y * width].r += trailIntensity;
        }
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

    void OnRenderObject()
    {
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(trailMap);
        texture.Apply();

        Graphics.DrawTexture(new Rect(0, 0, width, height), texture);
    }

    void Update()
    {
        for (int i = 0; i < stepsPerFrame; i++)
        {
            MoveAgents();
            DepositTrail();
            ApplyDiffusionAndDecay();
        }

        OnRenderObject();
    }

    public class Agent
    {
        public Vector2 position;
        public float angle;
    }
}
