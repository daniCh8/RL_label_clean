using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Baseline : MonoBehaviour
{
    public GameObject labelPrefab;
    public List<GameObject> labelGroups, labels;
    public Material lower, upper, center, planeDirs;
    public Material[] middleMats;
    private List<GameObject[]> labelsCorners, labelsMiddles;
    private Vector3 sphereScale = new Vector3(.3f, .3f, .3f),
        planeScale = new Vector3(.4f, .4f, .4f);
    private bool spheresInit = false, planesInit = false, toInit = false;
    public bool hideSpheres, hidePlanes;
    private float yThreshold = 3f, step = .2f, bigStep = 2f,
        positiveStep, negativeStep, movementSpeed = .25f;

    private struct PlanePlayer
    {
        public GameObject player, plane,
            sphereUp, sphereForward, sphereRight, sphereCt;
    }
    private List<PlanePlayer> planes;

    public enum UpdateAlgo
    {
        OneDim,
        ThreeDim,
        PlaneBased
    }
    public UpdateAlgo algo;

    private void Start()
    {
        if(toInit) {
            StartHelper();
        }
    }

    void Update()
    {
        if(!spheresInit)
        {
            return;
        }
        UpdateSpheres();

        foreach (var l in labels)
        {
            l.transform.LookAt(Camera.main.transform.position);
        }

        switch (algo)
        {
            case UpdateAlgo.OneDim:
                LabelsAlgorithmOneDim();
                break;
            case UpdateAlgo.ThreeDim:
                LabelsAlgorithmThreeDim();
                break;
            case UpdateAlgo.PlaneBased:
                UpdatePlanes();
                LabelsAlgorithmOneDim();
                break;
        }
    }

    public void InitFrom(List<GameObject> lG, List<GameObject> l)
    {
        labelGroups = lG;
        labels = l;

        StartHelper();
        ResetPositions();
    }

    public void Init(List<GameObject> players)
    {
        int counter = 1;
        foreach (var player in players)
        {
            GameObject labelObj = Instantiate(labelPrefab);
            labelObj.transform.name = string.Format("label{0}", counter);
            labelObj.transform.parent = transform;
            labelObj.transform.localPosition = Vector3.zero;
            labelObj.transform.localRotation = Quaternion.identity;
            labelObj.transform.localScale = Vector3.one;
            labelObj.GetComponent<LabelFollowPlayer>().player = player;
            labelObj.GetComponentInChildren<UpdatePole>().player = player;
            labelObj.GetComponentInChildren<TextMeshPro>().text =
                string.Format("{0}_label", counter);

            foreach (Transform child in labelObj.transform)
            {
                string old_name = child.name;
                child.name = string.Format("{0}{1}", child.name, counter);
                if (child.CompareTag("bg"))
                {
                    labels.Add(child.gameObject);
                }
            }
            labelGroups.Add(labelObj);
            counter++;
        }

        StartHelper();
        ResetPositions();
    }

    private void StartHelper()
    {
        positiveStep = step;
        negativeStep = -1 * step;
        foreach (var l in labelGroups)
        {
            l.GetComponentInChildren<LabelFollowPlayer>().followX = (algo == UpdateAlgo.OneDim);
        }
        spheresInit = false;
        labelsCorners = new List<GameObject[]>();
        labelsMiddles = new List<GameObject[]>();
        UpdateSpheres();
        spheresInit = true;

        if (algo == UpdateAlgo.PlaneBased)
        {
            CreatePlanes();
            planesInit = true;
        }
    }

    public void ResetPositions()
    {
        foreach (var l in labelGroups)
        {
            l.GetComponentInChildren<LabelFollowPlayer>().ResetPosition();
        }
        UpdateSpheres();
    }

    private void LabelsAlgorithmThreeDim()
    {
        for (int i = 0; i < labels.Count; i++)
        {
            AdjustLabelThreeDim(i);
        }
    }

    private void LabelsAlgorithmOneDim()
    {
        for (int i = 0; i < labels.Count; i++)
        {
            AdjustLabelOneDim(i);
        }
    }

    private int CheckHitFromCorners(GameObject[] corners, string myName, bool draw)
    {
        int counter = 0;
        foreach (var c in corners)
        {
            if (CheckHit(c.GetComponent<Renderer>(), c.transform, myName, draw))
            {
                return counter;
            }
            counter++;
        }
        return -1;
    }

    private bool[] CheckHitFromMiddles(GameObject[] middles, string myName, bool draw)
    {
        bool[] hits = new bool[4];
        int counter = 0;
        foreach (var m in middles)
        {
            hits[counter++] = CheckHit(m.GetComponent<Renderer>(),
                m.transform, myName, draw);
        }
        return hits;
    }

    private bool CheckHit(Renderer r, Transform t, string myName, bool draw)
    {
        if (r.isVisible)
        {
            RaycastHit hit;
            Vector3 direction = t.position - Camera.main.transform.position;
            if (Physics.Raycast(Camera.main.transform.position, direction, out hit))
            {
                if (draw)
                {
                    // Debug.DrawRay(Camera.main.transform.position, direction, Color.yellow);
                    // Debug.LogFormat("{0} collided with --> name: {1}, tag: {2}",
                        // t.name, hit.collider.name, hit.collider.tag);
                }
                if (hit.transform.gameObject.name != myName &&
                    !hit.transform.CompareTag("ground"))
                {
                    if (!hit.collider.CompareTag("label") &&
                        !hit.collider.CompareTag("player"))
                    {
                        Debug.LogFormat("name: {0}, tag: {1}", hit.collider.name, hit.collider.tag);
                    }
                    return true;
                }
            }
        }
        return false;
    }

    private void AdjustLabelThreeDim(int lId)
    {
        float yUpdate = 0, xUpdate = 0;
        bool draw = lId == 0 ? true : false;
        int cornerHit = CheckHitFromCorners(labelsCorners[lId], labels[lId].name, draw),
            counter = 10;
        while(cornerHit != -1 && counter > 0)
        {
            Vector3 oldPosition = labels[lId].transform.position;
            if (cornerHit == 4 || labels[lId].transform.position.y < yThreshold)
            {
                yUpdate = bigStep;
                xUpdate = 0;
            }
            else if ((yUpdate == bigStep && cornerHit != 4) || (yUpdate == 0 && xUpdate == 0))
            {
                bool[] middleHits = CheckHitFromMiddles(labelsMiddles[lId], labels[lId].name, draw);
                switch (cornerHit)
                {
                    case 0:
                        if (middleHits[0] == middleHits[3])
                        {
                            yUpdate = negativeStep;
                            xUpdate = positiveStep;
                        }
                        else if (middleHits[0])
                        {
                            yUpdate = positiveStep;
                            xUpdate = 0f;
                        }
                        else if (middleHits[3])
                        {
                            yUpdate = 0f;
                            xUpdate = negativeStep;
                        }
                        break;
                    case 1:
                        if (middleHits[0] == middleHits[1])
                        {
                            yUpdate = positiveStep;
                            xUpdate = positiveStep;
                        }
                        else if (middleHits[0]) {
                            yUpdate = positiveStep;
                            xUpdate = 0f;
                        }
                        else if (middleHits[1])
                        {
                            yUpdate = 0f;
                            xUpdate = positiveStep;
                        }
                        break;
                    case 2:
                        if(middleHits[1] == middleHits[2])
                        {
                            yUpdate = negativeStep;
                            xUpdate = positiveStep;
                        }
                        else if(middleHits[1])
                        {
                            yUpdate = 0f;
                            xUpdate = positiveStep;
                        }
                        else if(middleHits[2])
                        {
                            yUpdate = negativeStep;
                            xUpdate = 0f;
                        }
                        break;
                    case 3:
                        if(middleHits[2] == middleHits[3])
                        {
                            yUpdate = negativeStep;
                            xUpdate = negativeStep;
                        }
                        else if (middleHits[2])
                        {
                            yUpdate = negativeStep;
                            xUpdate = 0f;
                        }
                        else if(middleHits[3])
                        {
                            yUpdate = 0f;
                            xUpdate = negativeStep;
                        }
                        break;
                    default:
                        Debug.Log("Error. Hit found but no switch case entered.");
                        break;
                }
            }
            yUpdate = oldPosition.y < 1f ? bigStep : yUpdate;
            Movement(labels[lId], xUpdate, yUpdate);
            UpdateSpheres();
            cornerHit = CheckHitFromCorners(labelsCorners[lId], labels[lId].name, draw);
            counter--;
        }
    }

    private void Movement(GameObject obj, float xUpdate, float yUpdate)
    {
        Vector3 oldPosition = obj.transform.position;
        Vector3 targetPos = new Vector3(
                oldPosition.x + xUpdate,
                oldPosition.y + yUpdate,
                oldPosition.z);
        float step = movementSpeed * Time.deltaTime;
        obj.transform.position = Vector3.MoveTowards(oldPosition, targetPos, step);
    }

    private void AdjustLabelOneDim(int lId)
    {
        float yUpdate = 8;
        bool draw = lId == 0 ? true : false;
        int cornerHit = CheckHitFromCorners(labelsCorners[lId], labels[lId].name, draw),
            counter = 10;
        while (cornerHit != -1 && counter > 0)
        {
            if (yUpdate == 8 || (yUpdate == bigStep && cornerHit != 4))
            {
                yUpdate = cornerHit < 2 ? .5f : -.5f;
            }
            if (cornerHit == 4 || labels[lId].transform.position.y < yThreshold)
            {
                yUpdate = bigStep;
            }
            Vector3 oldPosition = labels[lId].transform.position;
            yUpdate = oldPosition.y < 1f ? bigStep : yUpdate;
            Movement(labels[lId], 0f, yUpdate);
            UpdateSpheres();
            cornerHit = CheckHitFromCorners(labelsCorners[lId], labels[lId].name, draw);
            counter--;
        }
    }

    private Bounds GetBounds(GameObject o)
    {
        return o.GetComponentInChildren<BoxCollider>().bounds;
    }

    private Vector3[] Corners(Bounds bounds)
    {
        Vector3 min = bounds.min, max = bounds.max;
        float z = (min.z + max.z) / 2;
        Vector3[] corners = new Vector3[] {
            new Vector3(min.x, min.y, z),
            new Vector3(max.x, min.y, z),
            new Vector3(min.x, max.y, z),
            new Vector3(max.x, max.y, z),
            bounds.center
        };
        return corners;
    }

    private Vector3[] Middles(Bounds bounds)
    {
        Vector3 min = bounds.min, max = bounds.max;
        float z = (min.z + max.z) / 2,
            mid_x = (min.x + max.x) / 2,
            mid_y = (min.y + max.y) / 2;
        Vector3[] middles = new Vector3[]
        {
            new Vector3(mid_x, min.y, z),
            new Vector3(min.x, mid_y, z),
            new Vector3(mid_x, max.y, z),
            new Vector3(max.x, mid_y, z)
        };
        return middles;
    }

    private GameObject CreateSphereDir(string name, Transform parent)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<SphereCollider>());
        sphere.transform.localScale = hidePlanes ?
            Vector3.zero : sphereScale;
        sphere.GetComponent<Renderer>().material = planeDirs;
        sphere.transform.parent = parent;
        string sphereName = name;
        sphere.name = sphereName;

        return sphere;
    }

    private void CreatePlanes()
    {
        planes = new List<PlanePlayer>();
        for (int i = 0; i < labelGroups.Count; i++)
        {
            GameObject l = labels[i], lG = labelGroups[i];

            GameObject player = lG.transform.Find("player_parent/player").gameObject;

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Destroy(plane.GetComponent<MeshCollider>());
            plane.transform.parent = l.transform.parent;
            plane.transform.localScale = hidePlanes ? Vector3.zero :
                planeScale;
            

            plane.name = string.Format("plane_{0}", i);
            PlanePlayer pp = new PlanePlayer();
            pp.player = player;
            pp.plane = plane;
            pp.sphereCt = CreateSphereDir(
                string.Format("plane_center_sphere_{0}", i),
                pp.plane.transform);
            pp.sphereForward = CreateSphereDir(
                string.Format("plane_forward_sphere_{0}", i),
                pp.plane.transform);
            pp.sphereRight = CreateSphereDir(
                string.Format("plane_right_sphere_{0}", i),
                pp.plane.transform);
            pp.sphereUp = CreateSphereDir(
                string.Format("plane_up_sphere_{0}", i),
                pp.plane.transform);
            planes.Add(pp);
        }
    }

    private void UpdatePlanes()
    {
        if(!planesInit)
        {
            CreatePlanes();
        }

        foreach (var pp in planes)
        {
            pp.plane.transform.position =
                pp.player.GetComponent<Renderer>().bounds.center;
            Vector3 dir = pp.plane.GetComponent<Renderer>().bounds.center -
            Camera.main.transform.position;
            pp.plane.transform.up = -dir;

            pp.sphereCt.transform.position =
                pp.plane.GetComponent<Renderer>().bounds.center;
            pp.sphereForward.transform.position =
                pp.sphereCt.transform.position +
                pp.plane.transform.forward;
            pp.sphereRight.transform.position =
                pp.sphereCt.transform.position +
                pp.plane.transform.right;
            pp.sphereUp.transform.position =
                pp.sphereCt.transform.position +
                pp.plane.transform.up;
        }
    }

    private void UpdateCornerSpheres()
    {
        int i = 0;
        foreach (var l in labels)
        {
            Vector3[] lCorners = Corners(GetBounds(l));
            int j = 0;
            GameObject[] cornerSpheres;
            if (!spheresInit)
            {
                cornerSpheres = new GameObject[5];
            }
            else
            {
                cornerSpheres = labelsCorners[i++];
            }
            foreach (var c in lCorners)
            {
                GameObject sphere;
                if (!spheresInit)
                {
                    sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(sphere.GetComponent<SphereCollider>());
                    sphere.transform.localScale = hideSpheres ?
                        Vector3.zero : sphereScale;
                    sphere.GetComponent<Renderer>().material =
                        j == 4 ? center : (j < 2 ? lower : upper);
                    cornerSpheres[j++] = sphere;
                    sphere.transform.parent = l.transform.parent;
                    string sphereName = j == 4 ?
                        "center-sphere" : string.Format("corner-sphere-{0}", j);
                    sphere.name = sphereName;
                }
                else
                {
                    sphere = cornerSpheres[j++];
                }
                sphere.transform.position = c;
            }
            if (!spheresInit)
            {
                labelsCorners.Add(cornerSpheres);
            }
        }
    }

    private void UpdateMiddleSpheres()
    {
        int i = 0;
        foreach (var l in labels)
        {
            Vector3[] lMiddles = Middles(GetBounds(l));
            int j = 0;
            GameObject[] middleSpheres;
            if (!spheresInit)
            {
                middleSpheres = new GameObject[4];
            }
            else
            {
                middleSpheres = labelsMiddles[i++];
            }
            foreach (var c in lMiddles)
            {
                GameObject sphere;
                if (!spheresInit)
                {
                    sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(sphere.GetComponent<SphereCollider>());
                    sphere.transform.localScale = hideSpheres ?
                        Vector3.zero : sphereScale;
                    sphere.GetComponent<Renderer>().material =
                        middleMats[j];
                    middleSpheres[j++] = sphere;
                    sphere.transform.parent = l.transform.parent;
                    sphere.name = string.Format("middle-sphere-{0}", j);
                }
                else
                {
                    sphere = middleSpheres[j++];
                }
                sphere.transform.position = c;
            }
            if (!spheresInit)
            {
                labelsMiddles.Add(middleSpheres);
            }
        }
    }

    private void UpdateSpheres()
    {
        UpdateCornerSpheres();
        UpdateMiddleSpheres();
    }
}
