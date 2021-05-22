using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


//Esse classe basicamente vai pegar as texturas/meshs geradas pelos codigos geradores e efetivamente aplicar no plano/mesh
public class MapDisplay : MonoBehaviour
{
    //Pega o Renderer do plano, o filtro de data da malha, e o render da malha
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    //Função que vai ser responsavel por criar a textura do NoiseMap do Noise.cs
    //Recebe como parametro uma textura gerada pelo TextureGenerator.cs
    public void DrawTexture(Texture2D texture)
    {
        //Aplica essa textura no shared material pq ai ele muda sem ter que dar play
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    //Faz praticamete a mesma coisa que o de cima, so que para uma Mesh
    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
