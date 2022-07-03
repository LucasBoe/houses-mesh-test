using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshCreator : MonoBehaviour
{
    [SerializeField] Vector3[] cornerPoints;
    [SerializeField] HouseData data;
    [SerializeField, ReadOnly] int seed;

    [SerializeField] HouseData[] randomHouses;

    [Button] 
    private void FullRandom()
    {

        UnityEngine.Random.InitState(Mathf.RoundToInt(Time.time * 1000f));
        RandomizeSeed();
        data = randomHouses[UnityEngine.Random.Range(0,randomHouses.Length)];
        UpdateMaterials();
    }

    public void CreateRandomHouse(Vector3[] points)
    {
        FullRandom();
        cornerPoints = points;
        GenerateMesh();
    }

    [Button]
    private void RandomizeSeed()
    {
        string glyphs = "abcdefghijklmnopqrstuvwxyz0123456789"; //add the characters you want
        int charAmount = 99;
        string myString = "";


        for (int i = 0; i < charAmount; i++)
        {
            myString += glyphs[UnityEngine.Random.Range(0, glyphs.Length)];
        }

        seed = myString.GetHashCode();
    }

    [Button]
    private void UpdateMaterials()
    {
        GetComponent<MeshRenderer>().sharedMaterials = new Material[]
        {
            data.WallMaterial, data.RoofFrontMaterial, data.RoofMaterials[UnityEngine.Random.Range(0, data.RoofMaterials.Length)]
        };
    }

    private void GenerateMesh()
    {
        HouseValues values = new HouseValues();

        UnityEngine.Random.InitState(seed);
        values.groundFloorHeight = UnityEngine.Random.Range(data.GroundFloor.MinMaxHeight.x, data.GroundFloor.MinMaxHeight.y);
        UnityEngine.Random.InitState(seed + 1);
        values.upperFloorHeight = UnityEngine.Random.Range(data.UpperFloor.MinMaxHeight.x, data.UpperFloor.MinMaxHeight.y);
        values.roofFrontHeight = ((Vector3.Distance(cornerPoints[0], cornerPoints[1]) + Vector3.Distance(cornerPoints[2], cornerPoints[3])) / 2f) * data.RoofHeightRatio;
        values.FloorCount = UnityEngine.Random.Range(data.minMaxFloorCount.x, data.minMaxFloorCount.y);

        List<Mesh> meshes = new List<Mesh>()
        {
            CreateWalls(values),
            CreateRoofFront(values),
            CreateRoofTop(values)
        };

        CombineInstance[] combine = new CombineInstance[meshes.Count];

        int i = 0;
        while (i < meshes.Count)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = transform.localToWorldMatrix;
            i++;
        }
        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine, false);
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private Mesh CreateRoofTop(HouseValues values)
    {
        Mesh mesh = new Mesh();

        int floorCount = values.FloorCount;

        List<Vector3> points = new List<Vector3>();
        int[] tris = new int[4 * 3];
        Vector2[] uvs = new Vector2[4 * 2];

        Vector3[] centers = new Vector3[4];
        centers[0] = (cornerPoints[2] + cornerPoints[3]) / 2f;
        centers[1] = (cornerPoints[0] + cornerPoints[1]) / 2f;
        centers[2] = centers[1];
        centers[3] = centers[0];

        for (int i = 0; i <= 2; i += 2)
        {
            Vector3 corner1 = cornerPoints[(i == 0 ? cornerPoints.Length : i) - 1];
            Vector3 corner2 = cornerPoints[i];

            float height = values.roofTopHeight;

            float roofBaseHeight = GetHeightAt(floorCount, values);
            points.Add(centers[i + 1] + (roofBaseHeight + height) * Vector3.up);
            points.Add(centers[i] + (roofBaseHeight + height) * Vector3.up);
            points.Add(corner2 + (roofBaseHeight) * Vector3.up);
            points.Add(corner1 + (roofBaseHeight) * Vector3.up);

            int index4 = i * 2;

            CreateTriangles(tris, i * 3, index4);
            SetUVs(uvs, index4, 0, Vector3.Distance(corner2, corner1), 0, values.roofTopHeight);
        }

        mesh.SetVertices(points);
        mesh.triangles = tris;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        return mesh;
    }

    private static void SetUVs(Vector2[] uvs, int baseIndex, float minX, float maxX, float minY, float maxY)
    {
        uvs[baseIndex + 0] = new Vector2(minX, maxY);
        uvs[baseIndex + 1] = new Vector2(maxX, maxY);
        uvs[baseIndex + 2] = new Vector2(minX, minY);
        uvs[baseIndex + 3] = new Vector2(maxX, minY);
    }

    private Mesh CreateRoofFront(HouseValues values)
    {
        Mesh mesh = new Mesh();

        List<Vector3> points = new List<Vector3>();
        int[] tris = new int[8 * 3];
        Vector2[] uvs = new Vector2[4 * 4];

        CreateSingleFront(values, points, tris, uvs, 0, cornerPoints[0], cornerPoints[1]);
        CreateSingleFront(values, points, tris, uvs, 2, cornerPoints[2], cornerPoints[3]);
        CreateSingleFront(values, points, tris, uvs, 4, cornerPoints[1], cornerPoints[0]);
        CreateSingleFront(values, points, tris, uvs, 6, cornerPoints[3], cornerPoints[2]);

        mesh.SetVertices(points);
        mesh.triangles = tris;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        return mesh;
    }

    private void CreateSingleFront(HouseValues values, List<Vector3> points, int[] tris, Vector2[] uvs, int i, Vector3 corner1raw, Vector3 corner2raw)
    {
        Vector3 corner1 = corner2raw + (corner1raw - corner2raw) * (1f + data.RoofFrontOverhang);
        Vector3 corner2 = corner1raw + (corner2raw - corner1raw) * (1f + data.RoofFrontOverhang);

        float height = values.roofFrontHeight;
        int floorCount = values.FloorCount;

        float roofBaseHeight = GetHeightAt(floorCount, values);
        points.Add(corner2 + (roofBaseHeight + height) * Vector3.up);
        points.Add(corner1 + (roofBaseHeight + height) * Vector3.up);
        points.Add(corner2 + (roofBaseHeight) * Vector3.up);
        points.Add(corner1 + (roofBaseHeight) * Vector3.up);

        int index4 = i * 2;
        CreateTriangles(tris, i * 3, index4);
        SetUVs(uvs, index4, 0, 1, 0, 1);
    }

    private static void CreateTriangles(int[] tris, int index6, int index4)
    {
        tris[index6 + 0] = index4 + 2;
        tris[index6 + 1] = index4 + 0;
        tris[index6 + 2] = index4 + 1;
        tris[index6 + 3] = index4 + 2;
        tris[index6 + 4] = index4 + 1;
        tris[index6 + 5] = index4 + 3;
    }

    private Mesh CreateWalls(HouseValues values)
    {
        Mesh mesh = new Mesh();
        int floorCount = values.FloorCount;

        List<Vector3> points = new List<Vector3>();
        int[] tris = new int[cornerPoints.Length * floorCount * 6];
        Vector2[] uvs = new Vector2[cornerPoints.Length * floorCount * 4];

        for (int f = 0; f < floorCount; f++)
        {
            CreateSingleFloor(points, tris, uvs, f, values);
        }

        mesh.SetVertices(points);
        mesh.triangles = tris;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        return mesh;
    }
    private void CreateSingleFloor(List<Vector3> points, int[] tris, Vector2[] uvs, int floor, HouseValues values)
    {

        for (int c = 0; c < cornerPoints.Length; c++)
        {
            Vector3 corner1 = cornerPoints[(c == 0 ? cornerPoints.Length : c) - 1];
            Vector3 corner2 = cornerPoints[c];

            points.Add(corner2 + GetHeightAt(floor + 1, values) * Vector3.up);
            points.Add(corner1 + GetHeightAt(floor + 1, values) * Vector3.up);
            points.Add(corner2 + GetHeightAt(floor, values) * Vector3.up);
            points.Add(corner1 + GetHeightAt(floor, values) * Vector3.up);

            int index6 = floor * cornerPoints.Length * 6 + c * 6;
            int index4 = floor * cornerPoints.Length * 4 + c * 4;
            CreateTriangles(tris, index6, index4);

            float uvHeight = floor == 0 ? 0 : 0.5f;
            float edgeSize = data.EdgeSize;

            SetUVs(uvs, index4, -edgeSize, DistanceToUVLength(Vector3.Distance(corner2, corner1)) + edgeSize, uvHeight, uvHeight + 0.5f);
        }
    }

    private float GetHeightAt(int floor, HouseValues values)
    {
        if (floor == 0) return 0f;
        return values.groundFloorHeight + (floor - 1) * values.upperFloorHeight;
    }

    private float DistanceToUVLength(float distance)
    {
        return Mathf.Round((distance - 0.3f) * 2);
    }

    private class HouseValues
    {
        public float upperFloorHeight;
        public float groundFloorHeight;
        public float roofFrontHeight;
        public float roofTopHeight => roofFrontHeight * 0.8f;

        public int FloorCount;
    }
}
