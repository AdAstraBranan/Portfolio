///This file works within the UnityEngine and extends the existing Terrain System to allow run-time manipulation of data masks.

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("MaxQ/Scripts/Level Editing/Landscaping")]

//***LANDSCAPING (MASTER)***///
public class Landscaping : MonoBehaviour
{
    public GameObject LandscapingBrushProjector;

    float BrushDiameter = 5, BrushRadius;
    float CurBrushWeight = .03f, RaiseElevation_BrushWeight = .03f, LowerElevation_BrushWeight = -.03f, SmoothElevation_BrushWeight = .65f;

    float DefaultElevation = 20f;

    List<ResetTerrainData> TerrainsToReset = new List<ResetTerrainData>();
    List<Terrain> TerrainsModified = new List<Terrain>();
    List<float[,,]> TerrainsModifiedAlphamaps = new List<float[,,]>();

    // Start is called before the first frame update
    void Start()
    {
        //Get all of the terrains in a scene
        var AllTerrainsInScene = FindObjectsOfType<Terrain>();

        //Load all the terrains in scene into the list so we can reset when we quit
        for (int t = 0; t < AllTerrainsInScene.Length; t++)
        {
            TerrainData TerrainInSceneData = AllTerrainsInScene[t].terrainData;

            TerrainsToReset.Add(new ResetTerrainData(TerrainInSceneData));

            Debug.Log("Added Terrain to List: " + TerrainInSceneData.name);
        }

        //Set our default brush size
        SetBrushSize(BrushDiameter);
    }

    //Create a class to store the terrains we need to reset at the end of the scene
    private class ResetTerrainData
    {
        public TerrainData TerrainDataToReset;
        public float[,] StoredHeightmaps;
        public float[,,] StoredAlphamaps;

        public ResetTerrainData(TerrainData sceneTerrainData)
        {
            TerrainDataToReset = sceneTerrainData;
            StoredHeightmaps = sceneTerrainData.GetHeights(0, 0, sceneTerrainData.heightmapWidth, sceneTerrainData.heightmapHeight);
            StoredAlphamaps = sceneTerrainData.GetAlphamaps(0, 0, sceneTerrainData.alphamapWidth, sceneTerrainData.alphamapHeight);
        }
    }

    // Update is called once per frame
    public void UpdateLandscaping()
    {
        //Get our mouse position in the world
        Ray MouseToWorld = Camera.main.ScreenPointToRay(Input.mousePosition);

        //Raycast into the world, and return a hit value on only the terrain layer
        if (Physics.Raycast(MouseToWorld, out RaycastHit MouseHit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
        {
            //Move our editor brush to be at the point at which we hit the terrain
            LandscapingBrushProjector.SetActive(true);
            LandscapingBrushProjector.transform.position = MouseHit.point;
            LandscapingBrushProjector.GetComponentInChildren<Projector>().fieldOfView = BrushDiameter;

            //Get the area we want to modify
            if (Input.GetMouseButton(0))
            {
                if(Input.GetKey(KeyCode.LeftShift))
                {
                    CurBrushWeight = LowerElevation_BrushWeight;
                }
                else
                {
                    CurBrushWeight = RaiseElevation_BrushWeight;
                }

                BrushArea(MouseHit.point, false);
            }

            if(Input.GetMouseButton(1))
            {
                CurBrushWeight = SmoothElevation_BrushWeight;
                BrushArea(MouseHit.point, true);
            }
        }
        else
        {
            LandscapingBrushProjector.SetActive(false);
        }

        if(Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            FlushTerrainData();
        }
    }

    //Sample the area to determine what we want to modify
    private void BrushArea(Vector3 CenterPoint, bool SmoothHeights)
    {
        //Define our four corners
        Vector3[] AreaCorners = new Vector3[4];

        Vector3 TopLeft = CenterPoint + new Vector3(-BrushRadius, 1000f, BrushRadius);
        Vector3 TopRight = CenterPoint + new Vector3(BrushRadius, 1000f, BrushRadius);
        Vector3 BottomLeft = CenterPoint + new Vector3(-BrushRadius, 1000f, -BrushRadius);
        Vector3 BottomRight = CenterPoint + new Vector3(BrushRadius, 1000f, -BrushRadius);

        AreaCorners[0] = TopLeft;
        AreaCorners[1] = TopRight;
        AreaCorners[2] = BottomLeft;
        AreaCorners[3] = BottomRight;

        //Debug.Log("AREA \n Width: " + Mathf.Abs(TopRight.x - TopLeft.x) + " | Length: " + Mathf.Abs(TopLeft.z - BottomLeft.z));

        //A list of all the terrains within the area, as well as a list for temporarily getting the heights
        List<Terrain> TerrainsInArea = new List<Terrain>();

        //For every corner that we have, create a raycast down to see what terrains we hit
        for (int CornerIndex = 0; CornerIndex < 4; CornerIndex++)
        {
            if (Physics.Raycast(AreaCorners[CornerIndex], -Vector3.up, out RaycastHit CornerHit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
            {
                //If the terrain is already in our list do not add it
                if(TerrainsInArea.Contains(CornerHit.collider.GetComponent<Terrain>()) == false)
                {
                    TerrainsInArea.Add(CornerHit.collider.GetComponent<Terrain>());
                }

                if(TerrainsModified.Contains(CornerHit.collider.GetComponent<Terrain>()) == false)
                {
                    TerrainsModified.Add(CornerHit.collider.GetComponent<Terrain>());
                    TerrainsModifiedAlphamaps.Add(CornerHit.collider.GetComponent<Terrain>().terrainData.GetAlphamaps(0, 0, CornerHit.collider.GetComponent<Terrain>().terrainData.alphamapWidth, CornerHit.collider.GetComponent<Terrain>().terrainData.alphamapHeight));
                }
            }
        }

        float AverageSmoothHeight = 0f, SmoothHeightSum = 0f;
        int SmoothHeightCount = 0;

        //if we want to smooth we toggle this value to get the averages before applying the true brush
        if(SmoothHeights)
        {
            //Cycle through our brush radius area Z and X (instead of X and Y, due to the values being reversed for heightmapping)
            for (int ZPos = -(int)BrushRadius; ZPos <= BrushRadius; ZPos++)
            {
                for (int XPos = -(int)BrushRadius; XPos <= BrushRadius; XPos++)
                {
                    for (int TerrainIndex = 0; TerrainIndex < TerrainsInArea.Count; TerrainIndex++)
                    {
                        //Assign the point we want to sample on the heightmap
                        Vector3 BrushPoint = GetPositionOnHeightmap(CenterPoint, TerrainsInArea[TerrainIndex]);

                        BrushPoint.x += XPos;
                        BrushPoint.z += ZPos;

                        // check if the position is within the heightmapData array size
                        if (BrushPoint.x >= 0 && BrushPoint.x < TerrainsInArea[TerrainIndex].terrainData.heightmapWidth && BrushPoint.z >= 0 && BrushPoint.z < TerrainsInArea[TerrainIndex].terrainData.heightmapHeight)
                        {
                            //only get the samples we want to average out
                            float[,] SampleToUpdate = TerrainsInArea[TerrainIndex].terrainData.GetHeights((int)BrushPoint.x, (int)BrushPoint.z, 1, 1);

                            // read current height
                            float TargetHeight = SampleToUpdate[0, 0];

                            SmoothHeightSum += TargetHeight;
                            SmoothHeightCount++;
                        }
                    }
                }
            }

            //Get an average height so we can apply this later for smoothing
            AverageSmoothHeight = SmoothHeightSum / SmoothHeightCount;
        }

        //Cycle through our brush radius area Z and X (instead of X and Y, due to the values being reversed for heightmapping)
        for (int ZPos = -(int)BrushRadius; ZPos <= BrushRadius; ZPos++)
        {
            for (int XPos = -(int)BrushRadius; XPos <= BrushRadius; XPos++)
            {
                //Cycle through all the terrains within this area and 
                for (int TerrainIndex = 0; TerrainIndex < TerrainsInArea.Count; TerrainIndex++)
                {
                    //for a circle, calcualate a relative Vector2
                    Vector2 BrushCircleCalculation = new Vector2(XPos, ZPos);

                    //check if the magnitude is within the circle radius
                    if (BrushCircleCalculation.magnitude <= BrushRadius)
                    {
                        //Assign the point we want to brush on the heightmap
                        Vector3 BrushOnHeightmap = GetPositionOnHeightmap(CenterPoint, TerrainsInArea[TerrainIndex]);

                        BrushOnHeightmap.x += XPos;
                        BrushOnHeightmap.z += ZPos;

                        // check if the position is within the heightmapData array size
                        if (BrushOnHeightmap.x >= 0 && BrushOnHeightmap.x < TerrainsInArea[TerrainIndex].terrainData.heightmapWidth && BrushOnHeightmap.z >= 0 && BrushOnHeightmap.z < TerrainsInArea[TerrainIndex].terrainData.heightmapHeight)
                        {
                            //only get the samples we want to update
                            float[,] HeightPointToUpdate = TerrainsInArea[TerrainIndex].terrainData.GetHeights((int)BrushOnHeightmap.x, (int)BrushOnHeightmap.z, 1, 1);

                            // read current height
                            float TargetHeight = HeightPointToUpdate[0, 0];

                            if(SmoothHeights)
                            {
                                // update heights array
                                HeightPointToUpdate[0, 0] = Mathf.Lerp(TargetHeight, AverageSmoothHeight, SmoothElevation_BrushWeight * Time.smoothDeltaTime);
                            }
                            else
                            {
                                // set our target height
                                TargetHeight += CurBrushWeight * Time.smoothDeltaTime;

                                // update heights array
                                HeightPointToUpdate[0, 0] = TargetHeight;
                            }

                            //update the heights on the terrain using the Delay method for processing time
                            TerrainsInArea[TerrainIndex].terrainData.SetHeightsDelayLOD((int)BrushOnHeightmap.x, (int)BrushOnHeightmap.z, HeightPointToUpdate);
                        }
                    }
                }

                //Cycle through all our terrains modified and see if we need to update our alphamaps. We do this so we can apply alpha maps afterword, since there is no delay feature for alphamaps.
                for(int TerrainModifiedIndex = 0; TerrainModifiedIndex < TerrainsModified.Count; TerrainModifiedIndex++)
                {
                    //Assign the point we want to brush on the heightmap
                    Vector3 BrushOnAlphamap = GetPositionOnAlphamap(CenterPoint, TerrainsModified[TerrainModifiedIndex]);

                    BrushOnAlphamap.x += XPos;
                    BrushOnAlphamap.z += ZPos;

                    // check if the position is within the heightmapData array size
                    if (BrushOnAlphamap.x >= 0 && BrushOnAlphamap.x < TerrainsModified[TerrainModifiedIndex].terrainData.alphamapWidth && BrushOnAlphamap.z >= 0 && BrushOnAlphamap.z < TerrainsModified[TerrainModifiedIndex].terrainData.alphamapHeight)
                    {
                        // read current height
                        float SampleHeight = TerrainsModified[TerrainModifiedIndex].terrainData.GetHeight((int)BrushOnAlphamap.x, (int)BrushOnAlphamap.z);

                        //get all of our layer weights on the terrain
                        float[] LayerWeights = new float[TerrainsModified[TerrainModifiedIndex].terrainData.terrainLayers.Length];

                        //Set the first layer within our terrain (our underground layer)
                        LayerWeights[0] = (SampleHeight <= DefaultElevation) ? (1.0f - (SampleHeight / DefaultElevation)) : 0;

                        //Set the second layer within our terrain (our ground level layer)
                        LayerWeights[1] = .1f;

                        //Set the third layer within our terrain (the hill layer)
                        LayerWeights[2] = (SampleHeight >= DefaultElevation) ? 10 * ((SampleHeight - DefaultElevation) / (TerrainsModified[TerrainModifiedIndex].terrainData.heightmapHeight - DefaultElevation)) : 0;

                        //get a sum for blending
                        float LayerWeights_Sum = LayerWeights.Sum();

                        for (int l = 0; l < TerrainsModified[TerrainModifiedIndex].terrainData.terrainLayers.Length; l++)
                        {
                            //the total sum for the alphamap has to equal 1 for blending, so we divide our total layers by the sum
                            LayerWeights[l] /= LayerWeights_Sum;

                            //update our alpha array
                            TerrainsModifiedAlphamaps[TerrainModifiedIndex][(int)BrushOnAlphamap.z, (int)BrushOnAlphamap.x, l] = LayerWeights[l];
                        }
                    }
                }
            }
        }
    }

    //Apply all changes and remove all terrain data from our global lists
    private void FlushTerrainData()
    {
        //apply height LOD and alphamap changes to every terrain we've modified so far
        for (int TerrainAlphamapIndex = 0; TerrainAlphamapIndex < TerrainsModifiedAlphamaps.Count; TerrainAlphamapIndex++)
        {
            TerrainsModified[TerrainAlphamapIndex].ApplyDelayedHeightmapModification();
            TerrainsModified[TerrainAlphamapIndex].terrainData.SetAlphamaps(0, 0, TerrainsModifiedAlphamaps[TerrainAlphamapIndex]);
        }

        //clear our global lists
        TerrainsModified.Clear();
        TerrainsModifiedAlphamaps.Clear();
    }

    //Get a world position relative to our heightmap
    Vector3 GetPositionOnHeightmap(Vector3 PositionInWorld, Terrain TerrainToSample)
    {
        Vector3 WorldToTerrainSpace = (PositionInWorld - TerrainToSample.GetPosition());
        Vector3 TerrainPosition = new Vector3((WorldToTerrainSpace.x / TerrainToSample.terrainData.size.x) * TerrainToSample.terrainData.heightmapWidth, 0, (WorldToTerrainSpace.z / TerrainToSample.terrainData.size.z) * TerrainToSample.terrainData.heightmapHeight);

        TerrainPosition.x = Mathf.Clamp(TerrainPosition.x, 0, TerrainToSample.terrainData.heightmapWidth - 1);
        TerrainPosition.z = Mathf.Clamp(TerrainPosition.z, 0, TerrainToSample.terrainData.heightmapHeight - 1);

        return TerrainPosition;
    }

    //Get a world position relative to our alphamap
    Vector3 GetPositionOnAlphamap(Vector3 PositionInWorld, Terrain TerrainToSample)
    {
        Vector3 WorldToTerrainSpace = (PositionInWorld - TerrainToSample.GetPosition());
        Vector3 TerrainPosition = new Vector3((WorldToTerrainSpace.x / TerrainToSample.terrainData.size.x) * TerrainToSample.terrainData.alphamapWidth, 0, (WorldToTerrainSpace.z / TerrainToSample.terrainData.size.z) * TerrainToSample.terrainData.alphamapHeight);

        TerrainPosition.x = Mathf.Clamp(TerrainPosition.x, 0, TerrainToSample.terrainData.alphamapWidth - 1);
        TerrainPosition.z = Mathf.Clamp(TerrainPosition.z, 0, TerrainToSample.terrainData.alphamapHeight - 1);

        return TerrainPosition;
    }

    //Change the size of the brush by diameter
    private void SetBrushSize(float NewBrushDiameter)
    {
        BrushDiameter = NewBrushDiameter;
        BrushRadius = BrushDiameter / 2;
    }

    //When we exit play mode or the application
    public void OnApplicationQuit()
    {
        //Reset all of our scenes terrain data
        for (int t = 0; t < TerrainsToReset.Count; t++)
        {
            TerrainsToReset[t].TerrainDataToReset.SetAlphamaps(0, 0, TerrainsToReset[t].StoredAlphamaps);
            TerrainsToReset[t].TerrainDataToReset.SetHeights(0, 0, TerrainsToReset[t].StoredHeightmaps);
        }
    }
}
