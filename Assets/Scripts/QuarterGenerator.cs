using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuarterGenerator : MonoBehaviour
{
    [SerializeField, Range(0.001f, 0.5f)] float streetWidth = 0.1f;
    [SerializeField] int seed = 0;
    [SerializeField, Range(0, 2)] float houseLocalOffset = 0.25f;
    [SerializeField, Range(0, 1)] float randomOffset = 0.5f;
    [SerializeField, Range(10, 50)] float scale = 25f;
    [SerializeField, Range(0, 2)] float sphererizeAmount = 0.5f;
    [SerializeField, Range(0, 1)] float sphereStrength = 0.5f;
    [SerializeField, Range(0, 2)] float houseWidthVarianceStrength = 0.5f;
    [SerializeField, Range(3,20)] int size = 5;

    [SerializeField] MeshCreator housePrefab;

    List<Vector3[]> houses = new List<Vector3[]>();

    QuarterInfo[,] quarters;

    private void Start()
    {
        GenerateHousePoints();
        StartCoroutine(SpawnHousesRoutine());
    }

    IEnumerator SpawnHousesRoutine()
    {
        foreach (Vector3[] house in houses)
        {
            MeshCreator instance = Instantiate(housePrefab);
            instance.CreateRandomHouse(house);
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            GenerateHousePoints();
    }

    private void GenerateHousePoints()
    {
        houses.Clear();
        Vector3[,] corners = new Vector3[size, size];
        quarters = new QuarterInfo[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                UnityEngine.Random.InitState(seed + x + y * 33);
                float xx = Random.Range(x, x + randomOffset);
                UnityEngine.Random.InitState(seed + x * y);
                float yy = Random.Range(y, y + randomOffset);

                float spherizeAmount = Mathf.Pow(Mathf.Abs(((xx - (size / 2)) * (yy - (size / 2))) / ((float)size)), sphererizeAmount) * sphereStrength;

                //Gizmos.DrawSphere(new Vector3(x, 0, y), spherizeAmount);

                Vector3 center = new Vector3(size / 2f, 0, size / 2f);

                corners[x, y] = Vector3.Lerp(new Vector3(xx, 00, yy), center, spherizeAmount);
            }
        }


        for (int x = 1; x < size; x++)
        {
            for (int y = 1; y < size; y++)
            {
                Vector3 topRight = corners[x, y];
                Vector3 topLeft = corners[x - 1, y];
                Vector3 botRight = corners[x, y - 1];
                Vector3 botLeft = corners[x - 1, y - 1];

                quarters[x, y] = new QuarterInfo();
                quarters[x, y].CornerPoints = new Vector3[4];

                quarters[x, y].CornerPoints[0] = OffsetPoint(topRight, topLeft, botRight);
                quarters[x, y].CornerPoints[1] = OffsetPoint(botRight, botLeft, topRight);
                quarters[x, y].CornerPoints[2] = OffsetPoint(botLeft, botRight, topLeft);
                quarters[x, y].CornerPoints[3] = OffsetPoint(topLeft, topRight, botLeft);
            }
        }

        Gizmos.color = Color.red;

        for (int x = 1; x < size; x++)
        {
            for (int y = 1; y < size; y++)
            {
                Vector3[] cornerPoints = quarters[x, y].CornerPoints;
                for (int i = 0; i < cornerPoints.Length; i++)
                {
                    Vector3 p1 = cornerPoints.GetElementWrapped(i) * scale;
                    Vector3 p2 = cornerPoints.GetElementWrapped(i - 1) * scale;

                    Vector2 perp = Vector2.Perpendicular(new Vector2(p2.x, p2.z) - new Vector2(p1.x, p1.z)).normalized;
                    Vector3 forward = new Vector3(perp.x, 0, perp.y);

                    Vector3 leftForward = (cornerPoints.GetElementWrapped(i + 1) - cornerPoints.GetElementWrapped(i)).normalized;
                    Vector3 rightForward = (cornerPoints.GetElementWrapped(i - 2) - cornerPoints.GetElementWrapped(i - 1)).normalized;

                    float streetLength = Vector3.Distance(p1, p2);

                    int houseCount = Mathf.RoundToInt(streetLength / 2f);

                    for (int h = 3; h <= houseCount; h++)
                    {
                        int seed = x * y * (1 + i);

                        float lerp1 = GetHousePoint(streetLength, houseCount, h, seed, h != 3);
                        float lerp2 = GetHousePoint(streetLength, houseCount, h - 1, seed, h != 3);

                        Vector3 lForward = lerp2 < 0.5 ? Vector3.Lerp(leftForward, forward, lerp2 * 2) : Vector3.Lerp(rightForward, forward, (1f - lerp2) * 2f);
                        Vector3 rForward = lerp1 < 0.5 ? Vector3.Lerp(leftForward, forward, lerp1 * 2) : Vector3.Lerp(rightForward, forward, (1f - lerp1) * 2f);

                        Vector3.Lerp(leftForward, rightForward, Mathf.Max(lerp1, 0f));

                        Vector3 houseRight = Vector3.Lerp(p1, p2, lerp1);
                        Vector3 houseLeft = Vector3.Lerp(p1, p2, lerp2);

                        float length = h == houseCount ? 4 : UnityEngine.Random.Range(3, 5);
                        float offset = UnityEngine.Random.Range(-houseLocalOffset, houseLocalOffset);

                        List<Vector3> points = new List<Vector3>();
                        points.Add(houseLeft + lForward * (length + offset));
                        points.Add(houseRight + rForward * (length + offset));
                        points.Add(houseRight + rForward * offset);
                        points.Add(houseLeft + lForward * offset);

                        houses.Add(points.ToArray());
                    }
                }
            }
        }
    }

    [Button]
    private void Randomize()
    {
        seed = Mathf.RoundToInt(Time.time * 10f);
    }

    private void OnDrawGizmos()
    {
        foreach (Vector3[] house in houses)
        {
            for (int pi = 0; pi < house.Length; pi++)
            {
                Vector3 pp1 = house[(pi == 0 ? house.Length : pi) - 1];
                Vector3 pp2 = house[pi];

                Random.InitState(Mathf.RoundToInt(house[0].magnitude * 100f));

                Gizmos.color = new Color[] { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan }[UnityEngine.Random.Range(0, 5)];
                Gizmos.DrawLine(pp1, pp2);
            }
        }
    }

    private float GetHousePoint(float streetLength, int houseCount, int h, int seed, bool addVariance)
    {
        UnityEngine.Random.InitState(seed * h);
        float variance = UnityEngine.Random.Range(-houseWidthVarianceStrength, houseWidthVarianceStrength);
        float lerp = (h / (float)houseCount) + (addVariance ? variance / streetLength : 0f);
        return lerp;
    }

    private Vector3 OffsetPoint(Vector3 topRight, Vector3 topLeft, Vector3 botRight)
    {
        Vector3 p1 = topRight + (topLeft - topRight).normalized * streetWidth;
        Vector3 p2 = topRight + (botRight - topRight).normalized * streetWidth;

        var pp = (p1 + p2) / 2f;
        return pp;
    }
}

[System.Serializable]
public class QuarterInfo
{
    public Vector3[] CornerPoints;
}

public static class ArrayExtention
{
    public static Vector3 GetElementWrapped(this Vector3[] array, int i)
    {
        int max = array.Length;
        while (i < 0) i += max;
        return array[i % max];
    }
}
