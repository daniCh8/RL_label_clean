using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Baseline : MonoBehaviour
{
    public GameObject labelPrefab;
    public List<GameObject> labelGroups, labels;
    public Material lower, upper, center;
    public Material[] middleMats;
    private List<GameObject[]> labelsCorners, labelsMiddles;
    private Vector3 sphereScale = new Vector3(.3f, .3f, .3f);
    private bool spheresInit = false, hideSpheres = true, toInit = false;
    private float yThreshold = 3f, step = .2f, bigStep = 2f,
        positiveStep, negativeStep, movementSpeed = .25f;
    public int algo;

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
        switch (algo)
        {
            case 1:
                LabelsAlgorithmThreeDim();
                break;
            case 2:
                break;
            default:
                LabelsAlgorithmOneDim();
                break;
        }
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
            l.GetComponent<LabelFollowPlayer>().followX = (algo == 0);
        }
        spheresInit = false;
        labelsCorners = new List<GameObject[]>();
        labelsMiddles = new List<GameObject[]>();
        UpdateSpheres();
        spheresInit = true;
    }

    public void ResetPositions()
    {
        foreach (var l in labelGroups)
        {
            l.GetComponent<LabelFollowPlayer>().ResetPosition();
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
                    !hit.collider.CompareTag("wall") && hit.collider.name != "Field" &&
                    !hit.collider.name.StartsWith("Goal") && !hit.collider.CompareTag("court"))
                {
                    if (!hit.collider.name.StartsWith("bg"))
                    {
                        // Debug.LogFormat("name: {0}, tag: {1}", hit.collider.name, hit.collider.tag);
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
            Vector3 oldPosition = labelGroups[lId].transform.position;
            if (cornerHit == 4 || labelGroups[lId].transform.position.y < yThreshold)
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
            Movement(labelGroups[lId], xUpdate, yUpdate);
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
            if (cornerHit == 4 || labelGroups[lId].transform.position.y < yThreshold)
            {
                yUpdate = bigStep;
            }
            Vector3 oldPosition = labelGroups[lId].transform.position;
            yUpdate = oldPosition.y < 1f ? bigStep : yUpdate;
            Movement(labelGroups[lId], 0f, yUpdate);
            UpdateSpheres();
            cornerHit = CheckHitFromCorners(labelsCorners[lId], labels[lId].name, draw);
            counter--;
        }
    }

    private Vector3[] Corners(Renderer r)
    {
        Vector3 min = r.bounds.min, max = r.bounds.max;
        float z = (min.z + max.z) / 2;
        Vector3[] corners = new Vector3[] {
            new Vector3(min.x, min.y, z),
            new Vector3(max.x, min.y, z),
            new Vector3(min.x, max.y, z),
            new Vector3(max.x, max.y, z),
            r.bounds.center
        };
        return corners;
    }

    private Vector3[] Middles(Renderer r)
    {
        Vector3 min = r.bounds.min, max = r.bounds.max;
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

    private void UpdateCornerSpheres()
    {
        int i = 0;
        foreach (var l in labels)
        {
            Vector3[] lCorners = Corners(l.GetComponent<Renderer>());
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
            Vector3[] lMiddles = Middles(l.GetComponent<Renderer>());
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
