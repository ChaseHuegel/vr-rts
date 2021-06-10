using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public class MapGenerator : MonoBehaviour
{
    public bool generate = true;
    public string seed = "";

    [Header("Forests")]
    [Range(0f, 10f)] public float forestAbundance = 0f;
    [Range(-1f, 1f)] public float forestCoverage = 0f;
    [Range(-1f, 1f)] public float forestDensity = 0f;

    [Header("Rocks")]
    [Range(0f, 10f)] public float rockAbundance = 0f;
    [Range(-1f, 1f)] public float rockCoverage = 0f;
    [Range(-1f, 1f)] public float rockDensity = 0f;

    public Swordfish.Terrain terrain;

    private FastNoise forestNoise;
    private FastNoise rockNoise;

    public void Update()
    {
        if (generate)
            Generate(); generate = false;
    }

    public void PrepareNoise()
    {
        forestNoise = new FastNoise();
        forestNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
        forestNoise.SetFractalOctaves(6);

        rockNoise = new FastNoise();
        rockNoise.SetNoiseType(FastNoise.NoiseType.CubicFractal);

        if (seed != "")
        {
            forestNoise.SetSeed( seed.GetHashCode() );
            rockNoise.SetSeed( seed.GetHashCode() );
        }
    }

    public void Generate()
    {
        PrepareNoise();

        foreach (Transform child in this.transform)
        {
            if (Time.time <= 0)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        for (int x = 0; x < World.GetLength(); x++)
        for (int y = 0; y < World.GetLength(); y++)
        {
            float elevation = terrain.GetHeightOnGrid(x, y);

            if (rockNoise.GetNoise(x * rockAbundance, y * rockAbundance) < rockCoverage && rockNoise.GetWhiteNoise(x, y) < rockDensity)
            {
                Instantiate(GameMaster.GetNode("gold").GetVariant(), World.ToTransformSpace(x, elevation, y), Quaternion.identity, this.transform);

                continue;
            }

            if (forestNoise.GetNoise(x * forestAbundance, y * forestAbundance) < forestCoverage && forestNoise.GetWhiteNoise(x, y) < forestDensity)
            {
                Instantiate(GameMaster.GetNode("tree").GetVariant(), World.ToTransformSpace(x, elevation, y), Quaternion.identity, this.transform);

                continue;
            }
        }
    }
}
