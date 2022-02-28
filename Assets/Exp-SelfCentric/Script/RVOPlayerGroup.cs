using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class RVOPlayerGroup : MonoBehaviour
{
    RVOSettings m_RVOSettings;
    // player + label
    public GameObject playerLabel_prefab;
    public Sprite redLabel;
    public Sprite blueLabel;
    public Baseline b;

    public Transform court;
    public Camera cam;
    float minZInCam;
    float maxZInCam;
    public int totalStep = -1;
    int currentStep = 0;

    private List<RVOplayer> m_playerMap = new List<RVOplayer>();

    struct Metrics
    {
        public float occlusionRate;
        public int intersections;
        public override string ToString()
        {
            return occlusionRate.ToString() + "," + intersections.ToString();
        }
    }


    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
    }

    // Start is called before the first frame update
    void Start()
    {
        court = transform.parent.Find("fancy_court");
        cam = transform.parent.Find("Camera").GetComponent<Camera>();

        minZInCam = cam.WorldToViewportPoint(new Vector3(0, 0, -m_RVOSettings.courtZ)).z;
        maxZInCam = cam.WorldToViewportPoint(new Vector3(0, 0, m_RVOSettings.courtZ)).z;
        Debug.Log("Min and Max Z in Cam: (" + minZInCam.ToString() + "," + maxZInCam.ToString() + ")");

        var positions = GetPos();
        int maxPlayers = Mathf.Min(positions.Count(), 20);

        List<GameObject> labelGroups = new List<GameObject>(),
            labels = new List<GameObject>();

        for(int i = 0; i < maxPlayers; ++i)
        {
            var playerLab = CreatePlayerLabelFromPos(i, positions[i].ToArray());
            labelGroups.Add(playerLab.Item1);
            labels.Add(playerLab.Item2);
        }
        // max velocity
        Vector3 maxVel = Vector3.zero;
        Vector3 minVel = new Vector3(Mathf.Infinity, 0, Mathf.Infinity);
        foreach(var player in m_playerMap)
        {
            float maxX = player.velocities.Max(v => Mathf.Abs(v.x));
            float minX = player.velocities.Min(v => Mathf.Abs(v.x));
            float maxZ = player.velocities.Max(v => Mathf.Abs(v.z));
            float minZ = player.velocities.Min(v => Mathf.Abs(v.z));

            maxVel = new Vector3(Mathf.Max(maxX, maxVel.x), 0, Mathf.Max(maxZ, maxVel.z));
            minVel = new Vector3(Mathf.Min(minX, minVel.x), 0, Mathf.Min(minZ, minVel.z));
        }
        Debug.Log("Max Vel:" + maxVel.ToString());
        Debug.Log("Min Vel:" + minVel.ToString());
        m_RVOSettings.playerSpeedX = maxVel.x - minVel.x;
        m_RVOSettings.playerSppedZ = maxVel.z - minVel.z;

        b.InitFrom(labelGroups, labels);
    }

    public Vector3 GetRandomSpawnPos(int idx)
    {
        var randomSpawnPos = Vector3.zero;
        if (m_RVOSettings.CrossingMode)
        {
            float radius = Mathf.Min(m_RVOSettings.courtX * 0.95f, m_RVOSettings.courtZ * 0.95f);
            float variance = 1.0f;
       
            var angle = (idx + 0.8f * Random.value) * Mathf.PI * 2 / m_RVOSettings.numOfPlayer;
            var randomPosX = Mathf.Cos(angle) * radius;
            var randomPosZ = Mathf.Sin(angle) * radius;
            randomPosX += Random.value * variance;
            randomPosZ += Random.value * variance;

            randomSpawnPos = new Vector3(randomPosX, 0.5f, randomPosZ);
        }
        else
        {
            float oneLine = m_RVOSettings.courtZ / 8f;
            float posZ = idx * oneLine - m_RVOSettings.numOfPlayer * 0.5f * oneLine;
            float randomPosX = -m_RVOSettings.courtX + Random.value * 0.3f * m_RVOSettings.courtX;
            randomSpawnPos = new Vector3(randomPosX, 0.5f, posZ);
        }
  
        return randomSpawnPos;
    }

    public (GameObject, GameObject) CreatePlayerLabelFromPos(int sid, Vector3[] positions)
    {

        GameObject playerObj = Instantiate(playerLabel_prefab, Vector3.zero, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.name = sid + "_PlayerLabel";

        var text = playerObj.transform.Find("player_parent/player/BackCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = sid.ToString();
        text = playerObj.transform.Find("player_parent/player/TopCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = sid.ToString();

        RVOplayer player = playerObj.GetComponentInChildren<RVOplayer>();
        player.positions = positions;

        player.sid = sid;
        m_playerMap.Add(player);

        Transform label = playerObj.gameObject.transform.Find("label");
        label.name = sid + "_label";

        Debug.Log("Finish initialize " + label.name);
        var name = label.Find("panel/Player_info/Name").GetComponent<TMPro.TextMeshProUGUI>();
        name.text = Random.Range(10, 99).ToString();
        
        var iamge = label.Find("panel/Player_info").GetComponent<Image>();
        iamge.sprite = (sid % 2 == 0) ? blueLabel : redLabel;

        if (sid % 2 != 0)
        {
            Color color = new Color(239f / 255f, 83f / 255f, 80f / 255f);
            var cubeRenderer = player.player.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_Color", color);
        }

        return (playerObj, label.gameObject);
    }

    // Update is called once per frame
    public int step = 0;

    private float time = 0.0f;
    private float timeStep = 0.04f;

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;

        if (time >= timeStep)
        {
            time -= timeStep;
            currentStep += 1;
        }

        if(currentStep < totalStep)
        {
            m_playerMap.ForEach(p => p.step(currentStep));
        }
        else
        {
            currentStep = 0;
        }
    }

    private List<List<Vector3>> GetPos()
    {

        //string fileName = Path.Combine(Application.streamingAssetsPath, "student_20_100.csv");
        string fileName = Path.Combine(Application.streamingAssetsPath, "nba_7501.csv");
        StreamReader r = new StreamReader(fileName);
        string pos_data = r.ReadToEnd();

        string[] records = pos_data.Split('\n');

        List<List<Vector3>> playerPos = new List<List<Vector3>>();
        for(int i = 0; i < records.Length; ++i)
        {
            string[] array = records[i].Split(',');
            int playerIdx = int.Parse(array[0]);

            if ((playerIdx + 1) > playerPos.Count)
            {
                playerPos.Add(new List<Vector3>());
            }
            
            playerPos[playerIdx].Add(new Vector3(
                float.Parse(array[1]),
                0.5f,
                float.Parse(array[2])
            ));
        }

        totalStep = playerPos.Max(p => p.Count);

        return playerPos;
    }
}
