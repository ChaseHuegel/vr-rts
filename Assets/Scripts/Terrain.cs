using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;

namespace Swordfish
{

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class Terrain : MonoBehaviour
{
    public bool readyForRelight = false;

    public int Resolution = 512;

    public float Scale = 1.0f;
    public int MaxHeight = 128;
    public float HeightFactor = 1.5f;
    public float zoom = 1.0f;

    public string seed = "";

    public Texture2D mask = null;

    private Mesh mesh = null;
    private MeshFilter meshFilter = null;
    private MeshCollider meshCollider = null;
    private float textureUnits = 0.0f;
    private Vector3[] tileMap = new Vector3[0];
    private float[,] heightMap = new float[0,0];

    private FastNoise baseNoise;
    private FastNoise carverNoise;
    private FastNoise detailNoise;

    private void Start()
    {
        PrepareNoise();
        Remesh();
    }

    public int toIndex(int _x, int _y)
    {
        if (_x < 0) _x = 0;
        if (_y < 0) _y = 0;
        if (_x >= Resolution) _x = Resolution - 1;
        if (_y >= Resolution) _y = Resolution - 1;

        return (_y * Resolution) + _x;
    }

    private void PrepareNoise()
    {
        baseNoise = new FastNoise();
        baseNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
        baseNoise.SetFractalOctaves(6);

        carverNoise = new FastNoise();
        carverNoise.SetNoiseType(FastNoise.NoiseType.Cellular);
        carverNoise.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Euclidean);
        carverNoise.SetCellularReturnType(FastNoise.CellularReturnType.Distance2Mul);

        detailNoise = new FastNoise();
        detailNoise.SetNoiseType(FastNoise.NoiseType.CubicFractal);
        detailNoise.SetFractalOctaves(6);
        detailNoise.SetFrequency(3);

        if (seed != "")
        {
            baseNoise.SetSeed( seed.GetHashCode() );
            carverNoise.SetSeed( seed.GetHashCode() );
            detailNoise.SetSeed( seed.GetHashCode() );
        }
    }

    public float GetHeightOnGrid(int x, int y)
    {
        int padding = (int)((Resolution - World.GetLength()) * 0.5f);
        return heightMap[x + padding, y + padding];
    }

    public float GetHeight(int x, int y)
    {
        return heightMap[x, y];
    }

    public float SampleHeight(float x, float y)
    {
        x *= zoom;
        y *= zoom;

        int halfRes = Mathf.RoundToInt(Resolution / 2);

        double value = 0.0f;

        float noise = baseNoise.GetNoise(x, y) + 1.0f;

        value = Math.Pow( noise, HeightFactor ) * MaxHeight;

        if (carverNoise.GetNoise(x, y) > 0)
        {
            value -= carverNoise.GetNoise(x, y) * MaxHeight;
        }

        value += detailNoise.GetNoise(x, y);

        return (float) value;
    }

    private void Remesh(bool populate = true)
    {
        meshFilter = this.GetComponent<MeshFilter>();
        meshCollider = this.GetComponent<MeshCollider>();

        textureUnits = 0.0625f;
        tileMap = new Vector3[Resolution * Resolution];
        heightMap = new float[Resolution, Resolution];
        for (int i = 0; i < tileMap.Length; i++) { tileMap[i] = new Vector3(0, 0); }

        int halfRes = Mathf.RoundToInt(Resolution / 2);

        //  Generate the base plane
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;    //  Allows up to 4b verts


        Color[] maskPixels = null;
        if (mask != null)
            maskPixels = mask.GetPixels();

        int vertexIndex = 0;
        List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector3> uvs = new List<Vector3>();
		List<Vector2> uvs2 = new List<Vector2>();
		List<Color> colors = new List<Color>();

        System.Random random = new System.Random();

        float y1, y2, y3, y4 = 0;
        for (int x = 0; x < Resolution; x++)
        {
            for (int z = 0; z < Resolution; z++)
            {
                int arrayX = x;
                int arrayY = z;

                int vertexPixel1 = toIndex(arrayX, arrayY + 1);
                int vertexPixel2 = toIndex(arrayX + 1, arrayY + 1);
                int vertexPixel3 = toIndex(arrayX + 1, arrayY);
                int vertexPixel4 = toIndex(arrayX, arrayY);

                float scaleX = arrayX;
                float scaleZ = arrayY;

                y1 = SampleHeight(scaleX, scaleZ + 1);
                y2 = SampleHeight(scaleX + 1, scaleZ + 1);
                y3 = SampleHeight(scaleX + 1, scaleZ);
                y4 = SampleHeight(scaleX, scaleZ);

                if (mask != null)
                {
                    y1 *= maskPixels[vertexPixel1].grayscale;
                    y2 *= maskPixels[vertexPixel2].grayscale;
                    y3 *= maskPixels[vertexPixel3].grayscale;
                    y4 *= maskPixels[vertexPixel4].grayscale;
                }

                vertexIndex = vertices.Count;
                Vector3 offset = new Vector3(1f, 0f, 1f);
                vertices.Add( new Vector3(x * Scale, y1, (z + 1) * Scale) - (offset * halfRes * Scale) );
                vertices.Add( new Vector3((x + 1) * Scale, y2, (z + 1) * Scale) - (offset * halfRes * Scale) );
                vertices.Add( new Vector3((x + 1) * Scale, y3, z * Scale) - (offset * halfRes * Scale) );
                vertices.Add( new Vector3(x * Scale, y4, z * Scale) - (offset * halfRes * Scale) );

                //  Triangle 1
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);

                //  Triangle 2
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 3);

                normals.Add( new Vector3(0, 1, 0) );
                normals.Add( new Vector3(0, 1, 0) );
                normals.Add( new Vector3(0, 1, 0) );
                normals.Add( new Vector3(0, 1, 0) );

                Color baseShade = new Color(0.15f, 0.15f, 0.15f, 1.0f);
                Color maxShade = new Color(0.75f, 0.75f, 0.75f, 1.0f);
                Color minShade = Color.white * 1.25f;

                int tileTexIndex = 0;

                float heightAverage = (y1 + y2 + y3 + y4) / 4;

                float highestHeight = y1;
                if (highestHeight < y2) highestHeight = y2;
                if (highestHeight < y3) highestHeight = y3;
                if (highestHeight < y4) highestHeight = y4;
                if (highestHeight < heightAverage) highestHeight = heightAverage;

                float steepness = Mathf.Clamp( (highestHeight - heightAverage) * 0.5f, 0.0f, 1.0f );

                float shadingOrigin = y3;
                float shading = shadingOrigin - heightAverage;

                float occlusion = (steepness + shading) / 2;

                if (y1 - heightAverage > 0.95f ||
                    y2 - heightAverage > 0.95f ||
                    y3 - heightAverage > 0.95f ||
                    y4 - heightAverage > 0.95f)
                {
                    tileTexIndex = 2;
                }
                else if (
                    y1 - heightAverage > 0.5f ||
                    y2 - heightAverage > 0.5f ||
                    y3 - heightAverage > 0.5f ||
                    y4 - heightAverage > 0.5f)
                {
                    tileTexIndex = 1;
                }

                heightMap[x, z] = heightAverage * transform.localScale.y;

                colors.Add(
                    Color.Lerp(minShade, maxShade, steepness) * Color.Lerp(minShade, baseShade, shading)
                    );
                colors.Add(
                    Color.Lerp(minShade, maxShade, steepness) * Color.Lerp(minShade, baseShade, shading)
                    );
                colors.Add(
                    Color.Lerp(minShade, maxShade, steepness) * Color.Lerp(minShade, baseShade, shading)
                    );
                colors.Add(
                    Color.Lerp(minShade, maxShade, steepness) * Color.Lerp(minShade, baseShade, shading)
                    );

                uvs.Add( new Vector3(0, 1, tileTexIndex) );
                uvs.Add( new Vector3(1, 1, tileTexIndex) );
                uvs.Add( new Vector3(1, 0, tileTexIndex) );
                uvs.Add( new Vector3(0, 0, tileTexIndex) );
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.SetUVs(0, uvs.ToArray());
        // mesh.uv2 = uvs2.ToArray();
        mesh.colors = colors.ToArray();
        // mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private void Relight(bool firstHalf = true)
    {
        mesh = meshFilter.mesh;

        int halfRes = Mathf.RoundToInt(Resolution / 2);

		List<Color> colors = new List<Color>();

        float y1, y2, y3, y4 = 0;
        for (int x = -halfRes; x < halfRes; x++)
        {
            for (int z = -halfRes; z < halfRes; z++)
            {
                int arrayX = x + halfRes;
                int arrayY = z + halfRes;

                int vertexPixel1 = toIndex(arrayX, arrayY + 1);
                int vertexPixel2 = toIndex(arrayX + 1, arrayY + 1);
                int vertexPixel3 = toIndex(arrayX + 1, arrayY);
                int vertexPixel4 = toIndex(arrayX, arrayY);

                float scaleX = arrayX;
                float scaleZ = arrayY;

                y1 = SampleHeight(scaleX, scaleZ + 1);
                y2 = SampleHeight(scaleX + 1, scaleZ + 1);
                y3 = SampleHeight(scaleX + 1, scaleZ);
                y4 = SampleHeight(scaleX, scaleZ);

                Color baseShade = new Color(0.15f, 0.15f, 0.15f, 1.0f);
                Color maxShade = new Color(0.75f, 0.75f, 0.75f, 1.0f);
                Color minShade = Color.white * 1.25f;

                float heightAverage = (y1 + y2 + y3 + y4) / 4;

                float highestHeight = y1;
                if (highestHeight < y2) highestHeight = y2;
                if (highestHeight < y3) highestHeight = y3;
                if (highestHeight < y4) highestHeight = y4;
                if (highestHeight < heightAverage) highestHeight = heightAverage;

                float steepness = Mathf.Clamp( (highestHeight - heightAverage) * 0.5f, 0.0f, 1.0f );

                float shadingOrigin = firstHalf ? y3 : y2;
                float shading = shadingOrigin - heightAverage; //Mathf.Clamp( (shadingOrigin - heightAverage) * 0.5f, 0.0f, 1.0f );

                colors.Add(
                    Color.Lerp(minShade, maxShade, steepness) * Color.Lerp(minShade, baseShade, shading)
                    );
                colors.Add(
                    Color.Lerp(minShade, maxShade, steepness) * Color.Lerp(minShade, baseShade, shading)
                    );
                colors.Add(
                    Color.Lerp(minShade, maxShade, steepness) * Color.Lerp(minShade, baseShade, shading)
                    );
                colors.Add(
                    Color.Lerp(minShade, maxShade, steepness) * Color.Lerp(minShade, baseShade, shading)
                    );
            }
        }

        meshFilter.mesh.colors = colors.ToArray();

        // meshFilter.mesh = mesh;
    }

}   //  Class

}