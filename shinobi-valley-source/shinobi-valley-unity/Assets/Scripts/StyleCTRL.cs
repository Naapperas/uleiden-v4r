using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StyleCTRL : MonoBehaviour
{

    public Terrain[] mainTerrain;
    public Terrain surroundingTerrain;

    public GameObject patternRoot;

    public SkinnedMeshRenderer playerChar;
    public SkinnedMeshRenderer masterChar;

    [Header("Ninja Settings")]
    public Material ninjaPlayerMat;
    public Material ninjaMasterMat;

    public Material ninjaSkyMat;
    public Color ninjaFogColor;
    public Material ninjaDiggerMat;

    public Color ninjaGrassFresh;
    public Color ninjaGrassDry;
    public Texture2D[] ninjaGrass;

    public Texture2D[] ninjaTerrainTextures;
    public GameObject[] ninjaAssets;

    [Header("Space Settings")]
    public Material spacePlayerMat;
    public Material spaceMasterMat;

    public Material spaceSkyMat;
    public Color spaceFogColor;
    public Material spaceDiggerMat;

    public Color spaceGrassFresh;
    public Color spaceGrassDry;
    public Texture2D[] spaceGrass;

    public Texture2D[] spaceTerrainTextures;
    public GameObject[] spaceAssets;

    TreePrototype[] treeArray;
    DetailPrototype[] detailArray;

    TerrainLayer[] textureArray;
    Digger.DiggerSystem digger;
    Material[] charMats;

    void Awake()
    {
        GM.Instance.style = this;
    }

    public void Init()
    {

        // Get style and terrain selection
        string styleString = GM.Instance.game.gameStyle == HLP.Style.NINJA ? "NINJA" : "SPACE";

        bool patternsActive = GM.Instance.game.patternsActive;
        int ts = patternsActive ? 0 : 1;

        // Pattern-based init
        mainTerrain[0].gameObject.SetActive(patternsActive);
        mainTerrain[1].gameObject.SetActive(!patternsActive);
        patternRoot.SetActive(patternsActive);


        detailArray = mainTerrain[ts].terrainData.detailPrototypes;
        treeArray = mainTerrain[ts].terrainData.treePrototypes;

        // Tree Replacements
        for (int i = 0; i < 8; i++)
        {
            treeArray[i].prefab = (GameObject)Resources.Load(styleString + "/Tree_" + (i + 1).ToString());

        }

        mainTerrain[ts].terrainData.treePrototypes = treeArray;


        textureArray = mainTerrain[ts].terrainData.terrainLayers;
        digger = mainTerrain[ts].transform.Find("Digger").GetComponent<Digger.DiggerSystem>();





        foreach (GameObject obj in ninjaAssets)
        {
            obj.SetActive(styleString == "NINJA");
        }

        foreach (GameObject obj in spaceAssets)
        {
            obj.SetActive(styleString == "SPACE");
        }

        if (styleString == "NINJA") NinjaSettings(ts);
        else SpaceSettings(ts);
    }

    void NinjaSettings(int ts)
    {
        charMats = playerChar.materials;
        charMats[0] = ninjaPlayerMat;
        playerChar.materials = charMats;

        charMats = masterChar.materials;
        charMats[0] = ninjaMasterMat;
        masterChar.materials = charMats;

        RenderSettings.skybox = ninjaSkyMat;
        RenderSettings.fog = true;
        RenderSettings.fogColor = ninjaFogColor;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.008F;

        for (int i = 0; i < ninjaTerrainTextures.Length; i++)
        {
            textureArray[i].diffuseTexture = ninjaTerrainTextures[i];
            digger.Material.SetTexture("_Splat" + i.ToString(), (Texture)ninjaTerrainTextures[i]);

        }

        mainTerrain[ts].terrainData.terrainLayers = textureArray;

        for (int i = 0; i < detailArray.Length; i++)
        {
            detailArray[i].prototypeTexture = ninjaGrass[i];
            detailArray[i].healthyColor = ninjaGrassFresh;
            detailArray[i].dryColor = ninjaGrassDry;
        }

        mainTerrain[ts].terrainData.detailPrototypes = detailArray;

    }

    void SpaceSettings(int ts)
    {
        charMats = playerChar.materials;
        charMats[0] = spacePlayerMat;
        playerChar.materials = charMats;

        charMats = masterChar.materials;
        charMats[0] = spaceMasterMat;
        masterChar.materials = charMats;

        RenderSettings.skybox = spaceSkyMat;
        RenderSettings.fog = true;
        RenderSettings.fogColor = spaceFogColor;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.008F;

        for (int i = 0; i < spaceTerrainTextures.Length; i++)
        {
            textureArray[i].diffuseTexture = spaceTerrainTextures[i];
            digger.Material.SetTexture("_Splat" + i.ToString(), (Texture)spaceTerrainTextures[i]);
        }
        mainTerrain[ts].terrainData.terrainLayers = textureArray;

        for (int i = 0; i < detailArray.Length; i++)
        {
            detailArray[i].prototypeTexture = spaceGrass[i];
            detailArray[i].healthyColor = spaceGrassFresh;
            detailArray[i].dryColor = spaceGrassDry;
        }

        mainTerrain[ts].terrainData.detailPrototypes = detailArray;


    }

}
