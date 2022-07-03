using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu]
public class HouseData : ScriptableObject
{
    public FloorData GroundFloor;
    public FloorData UpperFloor;
    [MinMaxSlider(1, 5)]
    public Vector2Int minMaxFloorCount = new Vector2Int(2,3);
    public Material WallMaterial, RoofFrontMaterial;
    public Material[] RoofMaterials;

    public float RoofHeightRatio = 2f;

    public float RoofFrontOverhang = 0.1f;
}

[System.Serializable]
public class FloorData
{
    [MinMaxSlider(0.5f, 3f)]
    public Vector2 MinMaxHeight = Vector2.one;
}
