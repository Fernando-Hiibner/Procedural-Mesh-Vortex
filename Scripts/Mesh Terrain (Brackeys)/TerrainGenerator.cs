using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MeshFilter))]
public class TerrainGenerator : MonoBehaviour
{
    Mesh terrainMesh; //Instancia a Mesh(Malha) do terreno

    Vector3[] vertices; //Cria um vetor com a posição dos vertices da malha
    int[] triangles; //Vetor para colocar os inteiros indices pra fazer o triangulo que forma a malha

    public int xSize = 16;
    public int zSize = 16;

    // Start is called before the first frame update
    void Start()
    {
        terrainMesh = new Mesh(); //Cria efetivamente a Malha do terrainMesh
        GetComponent<MeshFilter>().mesh = terrainMesh; //Atribui essa malha ao atributo Mesh do MeshFilter

        CreateShape();
        UpdateMesh();
    }
    void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)]; //Lista que vai conter todas as posições dos vertices

        for (int z = 0, i = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise(x * .3f, z * .3f) * 2f;
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }

        triangles = new int[xSize*zSize*6];

        int vertex = 0;
        int tri = 0;
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {

                triangles[tri + 0] = vertex + 0;
                triangles[tri + 1] = vertex + xSize + 1;
                triangles[tri + 2] = vertex + 1;
                triangles[tri + 3] = vertex + 1;
                triangles[tri + 4] = vertex + xSize + 1;
                triangles[tri + 5] = vertex + xSize + 2;

                vertex++;
                tri += 6;
            }

            vertex++;
        }



    }

    void UpdateMesh()
    {
        terrainMesh.Clear();

        terrainMesh.vertices = vertices;
        terrainMesh.triangles = triangles;

        terrainMesh.RecalculateNormals();
    }

}
