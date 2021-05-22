using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading; //Pra poder usar threading obviamente
using UnityEngine;

public class MapGenerator : MonoBehaviour
{

    //Isso aqui basicamente cria rapidamente uma especie de lista, a gente pode escolher os elementos pelo nome, mas o computador entende eles
    //Como numeros
    public enum DrawMode {NoiseMap, ColorMap, Mesh};
    //Aqui é a variavel que a gente vai escolher de dentro desse drawmode, seja NoiseMap, ColorMap ou Mesh
    public DrawMode drawMode;

    //Isso aqui é usado no LOD, é um numero divisivel por todos os pares de 0 a 12, então da um range legal de LOD
    public const int mapChunkSize = 241;
    [Range(0,6)]
    public int levelOfDetail;

    //Aqui passamos as variaveis de parametro que ele vai mandar pra outras classes que vao mandar pra outras e outras até retornar o que a gente que
    //Ele é a nossa interface com o MapGenerator basicamente, nosso painel de controle
    public float noiseScale;

    public int octaves;
    [Range(0,1)] //Pq a persistance sempre é entre 0 e 1
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    
    //Sera documentado amanha 25/01 quando eu estudar melhor threading, reasistir o video e ler aquele comentario brabo
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapGenerator.MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();
        //Vai encontrar o map display (Responsavel por analisar o noiseMap e criar uma textura com ele) e instanciar ele aqui
        MapDisplay display = FindObjectOfType<MapDisplay>();
        //chama a função drawTexture, e dependendo do drawmode que tiver seleionada, vai desenhar ou o noise em escala de cinza ou o noise colorido
        //Como terreno ja, pra isso o mapDisplay usa como parametro as texturas geradas por outro código de classe estatica
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            //Aqui é onde ele passa aquele colorMap das regions ali em cima
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
    }

    //Dessa (67) linha até a linha 124 é tudo sobre threading, sera documentado amanha
    public void RequestMapData(Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();
        lock (mapDataThreadInfoQueue) { 
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMapData(MapData mapData, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if(meshDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
    //Linha 124, daqui até a linha 67 (Acima dessa obviamente) é threading e vai ser documentado amanha
    MapData GenerateMapData()
    {
        //Aqui ele passa os valores que o GenerateNoiseMap da Classe Estatica Noise precisa pra gerar o Noise map, e armazena esse NoiseMap
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        //Cria um array de cores, e seta como tamanho width*height, pq em um é a quantidade de pixels que tem dentro de uma area (Tipo area de quadrado)
        //Tipo 1280x720, pra descobrir quantos pixels tem ai, multiplica 1280*720, vai ser o mesmo esquema do map display
        //Ele cria isso pra poder depois mostrar as "Regions" no Plano/Mesh, basicamente isso vai definiti se x, y ponto do mapa é da região a ou b...
        //E guardar aqui a cor que esse "pixel" do mapa tem que ter baseado na região dele
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        //Ele vai iterar pelo noise map pelas coordenadas x e y
        for(int y = 0; y < mapChunkSize; y++)
        {
            for(int x = 0; x < mapChunkSize; x++)
            {
                //Salvar esse valor numa variavel que vai segurar ele pro if, lembrando que o valor dentro do noiseMap é basicamente uma altura
                //Ele indica uma altura entre 0 e 1, que a gente interpreta em cores e etc
                float currentHeight = noiseMap[x, y];
                //Ai ele vai iterar pela lista de regiões
                for(int i = 0; i < regions.Length; i++)
                {
                    //Se a altura daquele ponto, for menor que a altura da região ele entre no if
                    //Esse bang da altura é tipo assim, região agua, altura 0.4, quer dizer que tudo de 0 ate 0.4 é agua
                    //Região terra, altura 1, ou seja, tudo entre 0.4 e 1 é terra
                    if(currentHeight <= regions[i].height)
                    {
                        //Encontra o index 1D equivalente a coordenada x, y da lista 2D, e nesse index salva a cor da região que ele detectou no if
                        //(Agua, terra, montanha, pico), depende da altura, o que quer dizer que o pixel que ta nesse ponto possue essa cor de região
                        //Eu expliquei melhor isso de Index 1D no TextureGenerator.cs se não me engano
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        //Quebra pq depois que decidiu esse ponto não tem pq perder tempo olhando os outros, ele só decide um por vez, então bora pro proximo
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colorMap);
    }

    //Essa função é chamada pelo unity sempre que alguma variavel é modificada no inspetor
    void OnValidate()
    {
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }
        if(octaves < 0)
        {
            octaves = 0;
        }
    }

    //Essa struct está relacionada a Threading e sera documentada amanha junto com tudo que tem haver com isso
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T paremter)
        {
            this.callback = callback;
            this.parameter = paremter;
        }
    }

}

//Com esse atributo ele aparece no inspector
[System.Serializable]
//Esse tipo de função basicamente cria um objeto, ela é uma lista grandinha que vai ter esses atributos dentro dela
//Estudar isso melhor depois
public struct TerrainType
{
    public string name; //Nome do tipo de terreno
    public float height; //Em que altura esse tipo de terreno aparece
    public Color color; //Qual a cor dele
}

//Metodo struct que serve pra guarda dados relacionados ao noise e ao color map, pra ser usado em mesh, noise Texture ou color Texture depois
//Esta assim agora (Pq antes não era e talvez tenha contradições entre documentações antigas) por causa da implementação de Threading
public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
