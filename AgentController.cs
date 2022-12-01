// TC2008B. Sistemas Multiagentes y Gráficas Computacionales
// C# client to interact with Python. Based on the code provided by Sergio Ruiz.
// Noviembre 2022
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class AgentData
{
    public string id;
    public float x, y, z;

    public AgentData(string id, float x, float y, float z)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

[Serializable]
public class SemaforoData
{
    public string id;
    public float x, y, z;
    public bool state;

    public SemaforoData(string id, float x, float y, float z, bool state)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.z = z;
        this.state = state;
    }
}

public class AgentsData
{
    public List<AgentData> positions;

    public AgentsData() => this.positions = new List<AgentData>();
}

[Serializable]

public class SemaforosData
{
    public List<SemaforoData> positions;

    public SemaforosData() => this.positions = new List<SemaforoData>();
}


public class AgentController : MonoBehaviour
{
    // private string url = "https://agents.us-south.cf.appdomain.cloud/";
    string serverUrl = "http://localhost:8585";
    string getAgentsEndpoint = "/getAgents";
    string getObstaclesEndpoint = "/getSemaforos";
    string sendConfigEndpoint = "/init";
    string updateEndpoint = "/update";
    AgentsData agentsData;
    SemaforosData semaforosData;
    Dictionary<string, GameObject> agents, semaforosInst;
    Dictionary<string, Vector3> prevPositions, currPositions;

    bool updated = false, started = false;

    public GameObject carro, semaforoPrefab;
    public int width, height;
    public float timeToUpdate = 5.0f;
    private float timer, dt;

    void Start()
    {
        agentsData = new AgentsData();
        semaforosData = new SemaforosData();
        prevPositions = new Dictionary<string, Vector3>();
        currPositions = new Dictionary<string, Vector3>();

        agents = new Dictionary<string, GameObject>();
        semaforosInst = new Dictionary<string, GameObject>();

        //floor.transform.localScale = new Vector3((float)width/10, 1, (float)height/10);
        //floor.transform.localPosition = new Vector3((float)width/2-0.5f, 0, (float)height/2-0.5f);
        
        timer = timeToUpdate;

        StartCoroutine(SendConfiguration());
    }

    private void Update() 
    {
        if(timer < 0)
        {
            timer = timeToUpdate;
            updated = false;
            StartCoroutine(UpdateSimulation());
        }

        if (updated)
        {
            timer -= Time.deltaTime;
            dt = 1.0f - (timer / timeToUpdate);

            foreach(var agent in currPositions)
            {
                Vector3 currentPosition = agent.Value;
                Vector3 previousPosition = prevPositions[agent.Key];

                Vector3 interpolated = Vector3.Lerp(previousPosition, currentPosition, dt);
                Vector3 direction = currentPosition - interpolated;

                agents[agent.Key].transform.localPosition = interpolated;
                if (direction != Vector3.zero) agents[agent.Key].transform.rotation = Quaternion.LookRotation(direction);

            }

            // float t = (timer / timeToUpdate);
            // dt = t * t * ( 3f - 2f*t);
        }
    }
 
    IEnumerator UpdateSimulation()
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + updateEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            StartCoroutine(GetAgentsData());
            StartCoroutine(GetSemaforosData());
        }
    }

    IEnumerator SendConfiguration()
    {
        WWWForm form = new WWWForm();


        UnityWebRequest www = UnityWebRequest.Post(serverUrl + sendConfigEndpoint, form);
        www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Configuration upload complete!");
            Debug.Log("Getting Agents positions");
            StartCoroutine(GetAgentsData());
            StartCoroutine(GetSemaforosData());
        }
    }

    IEnumerator GetAgentsData() 
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getAgentsEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            agentsData = JsonUtility.FromJson<AgentsData>(www.downloadHandler.text);

            Debug.Log(www.downloadHandler.text);

            foreach (AgentData agent in agentsData.positions)
            {
                Vector3 newAgentPosition = new Vector3(agent.x, (float)0, agent.z);

                if(!started)
                {
                    prevPositions[agent.id] = newAgentPosition;
                    agents[agent.id] = Instantiate(carro, newAgentPosition, Quaternion.identity);
                    agents[agent.id].SetActive(true);
                }
                else
                {
                    Vector3 currentPosition = new Vector3();
                    if(currPositions.TryGetValue(agent.id, out currentPosition))
                        prevPositions[agent.id] = currentPosition;
                    currPositions[agent.id] = newAgentPosition;
                    agents[agent.id].SetActive(true);
                }
            }
        }
    }


    IEnumerator GetSemaforosData()
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getObstaclesEndpoint);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else
        {
            semaforosData = JsonUtility.FromJson<SemaforosData>(www.downloadHandler.text);

            Debug.Log(semaforosData.positions);

            /*foreach (SemaforoData semaforo in semaforosData.positions)
            {
                    if(!started)
                    {
                        //semaforosInst[semaforo.id] = Instantiate(semaforoPrefab, new Vector3(semaforo.x, semaforo.y, semaforo.z), Quaternion.identity);
                        Debug.Log(semaforosInst[semaforo.id]);
                    }
            }
            updated = true;
            if(!started) started = true;*/
        }
    }


}