using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EndlessTerrain : MonoBehaviour
{

    /*
     * Parametros:
     * maxViewDst = distancia maxima que o "player" enxerga
     * viewer = o "player"
     * mapMaterial = o Material do mapa (Textura, shader e etc)
     * viewerPosition = a posição do "player" em um vetor 2D que só guarda os valores do x e do z (plano 2d)
     * mapGenerator = variavel que vai ser usada pra guardar a referencia ao MapGenerator.cs
     * chunkSize = o tamanho de cada chunk
     * chunksVisibleInViewDst = os chunks que estão visiveis no campo de visão
     * 
     * terrainChunkDictionary = um dicionario, onde a Chave é uma posição x, z de Vector2, e o valor é um objeto da classe TerrainChunk (classe
     * que ta la embaixo no código), vai guardar o terrainChunk que ta na coordenada x, z, pra não ficar gerando copias ou chuncks diferentes na
     * mesma coordenada
     * 
     * terrainChunkVisibleLastUpdate = uma lista que vai guardar objetos do tipo TerrainChunk, mais especificamente os que foram visto na ultima vez
     * que o método update foi chamado(ultimo frame), essa lista é usada la embaixo pra saber quais terrenos desses ainda estão sendo vistos e quais não
     * assim despawnando os que não estão mais sendo vistos
     */
    public const float maxViewDst = 450;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    //Método start padrão
    void Start()
    {
        //Vai instanciar o MapGenerator aqui
        mapGenerator = FindObjectOfType<MapGenerator>();
        //pega o valor de chunkSize (Que é na pratica 240, mas la na variavel dele no MapGenerator ta 241 pra fazer sentido nos loop for) e subtrai
        //1 pra ficar 240 aqui, pq aqui tem que ser 240, vai ser usada pra setar certinho as coordenadas dos chunks e criar meio que um "Grid"
        chunkSize = MapGenerator.mapChunkSize - 1;
        //Divide a distancia de visão pelo tamanho do chunk, pra descobrir quantos chunks são visiveis nessa distancia de visão, e então arredonda
        //pro inteiro mais proximo, atualmente (com 450 de maxViewDst e 240 de chunkSize) a divisão da aproximadamente 1.8 que é arredondado pra dois
        //O que significa que 2 chunks são visiveis pra todos os lados das 4 direções, se gerar a mesh com essa configuração da pra ver, e por esse 
        //motivo as diagonais faltam um chunk, pq nelas a conta não da dois, da só 1, qualquer duvida é so gerar o terreno com essas config
        //Alias, se mover o player pelo chunk, da pra ver que os chunks de tras despawnam (1 fileira deles) pq eles sairam do range
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
    }

    void Update()
    {
        //O update vai ser responsavel por ficar atualizando a posição atual do "player" e fazendo o Update nos chunks por meio desta
        //Chunks fora do campo de visão somem, chunks que estão e continuam no campo, apenas ficam, e chunks novos que entraram pro campo de visão
        //são spawnados
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        //Esse é o metodo que da update nos chunks
        //Esse for vai iterar por todos os chunks que estavam visiveis no frame anterior e desativar eles pra eles passarem pela checkagem dnvo
        //é melhor fazer dessa forma, desativando todos que estavam ativados e depois checando quais chunks precisam ser ativados, do que despawnar
        //aqui só os que não são mais vistos e depois na hora de ativar ter que ficar checado se ja ta ativado ou não
        for(int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        //Após desativar todos, ele limpa a lista
        terrainChunksVisibleLastUpdate.Clear();

        //Isso aqui vai definir qual a coordenada do chunk que o player" está nesse momento
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        //Isso daqui vai percorrer as coordenadas dos chunks que são visiveis atualmente, por exemplo, nas configs atual (450 de viewDst e 240 de ChunkSize)
        //são visiveis 2 chunks por vez em cada uma das direções, se tem 2 pra frente (2 positivo) vão ter 2 pra tras (2 negativo) se olhar do
        //ponto de vista de coordenada, e vale lembrar que ali em cima quando pegamos o valor de currentChunkCoordX e Y, esses valores não são
        //em coordenadas tipo 240;0 ou 0;240 ou -240;240, eles são nos valores de 0,1,2,3,4, é meio que o numero deles uma forma simplificada de
        //coordenada, e esse for vai percorrer por eles dessa forma, começando pelo chunk -n até o chunk n, linha por linha, coluna por coluna, um por um
        for(int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for(int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                //Esse vetor guarda a posição do chunk que foi visto, somando a posição do chunk atual do player e somando a posição do Offset
                //Então basicamente se o Offset for 2 no X, ele vai olhar o chunk que esta a 2 positivo de distancia do atual no eixo X
                //E guardar isso num vetor 2, basicamente ta guardando a coordenada dele, pra depois usar como chave no dicionario de terreno e checar
                //se esse chunk ja foi visto ou não
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                //Aqui é o if que checa se o dicionario de terreno ja contem esse chunk
                //Caso contenha ele vai executar o metodo UpdateTerrainChunk pra esse chunk e ver se ele ta no alcance de visão ou não
                //Se tiver, ele ativa o chunk, senão ele desativa
                //Depois ele faz uma checagem pra ver se esse chunk foi ativado usando o metodo IsVisible, que basicamente retorna se ele
                //ta ativado ou não (A setinha com o sinal de certinho/marcado no inspector), se ele estiver ativado, adciona ele na lista de chunks
                //vistos no ultimo Update(ultimo frame, até pq ele ta sendo visto agora no que jaja vai ser esse ultimo frame)
                //Se ele não ta ativado ele não foi visto e blz, só deixa ele queto e fora da lista
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                }
                //Caso contrario ele adciona essa coordenada em um dicionario como chave (O dicionario que guarda todos os chunks que existem)
                //E cria o chunk efetivamente usando a classe que esta ali embaixo, basicamente cria um novo objeto do tipo TerrainChunk
                //pra ser o valor da chave coordenada no dicionario, basicamente é o chunk que ta ali naquela coordenadad o mundo
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        /* 
         * Parametros:
         * meshObject = a variavel do tipo GameObject que jaja vai guardar a nossa nova mesh
         * position = uma variavel vetor 2 que vai guardar a posição do chunk
         * 
         * bounds = basicamente é um objeto que vai criar uma caixa envolta do objeto pra definir suas fronteiras (bounds), ele é usado la embaixo
         * pq ele tem um metodo chamado SqrDistance, que basicamente retorna a menor distancia em formas quadradas entre um ponto que passamos
         * por parametro e a bound em si, usaremos isso la em baixo pra descobrir a distancia que o "player" esta da fronteira mais proxima do chunk
         * 
         * meshRenderer/meshFilter = basico do basico, referencia a um meshRenderer e um Filter, que vai ser criado la embaixo
        */
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        //Esse metodo é o que constroi o chunk
        /*
         * Parametros:
         * coord = a coordenada do chunk que ele ta gerando, a gente passa ela la emcima no update visibleChunks
         * size = o tamanho dos chunks (atualmente 240)
         * parente = o transform do que a gente quer que seja o objeto pai desse chunk, isso é opcional mas extremamente convenienete, dessa forma
         * os chunks vão ser gerados como filho de algo (nesse caso do MapGenerator) e não vão ficar fazendo várzea na hierarquia do unity
         * material = o material do mapa né plru
         
        */
        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material)
        {
            //esse position basicamente vai converter a coordenada que atualmente ta (2;0),(1;0),(-2;0), ai vai multiplicar pelo size(240),
            //e vai ficar nas coordenadas full, que ficam no centro do chunk (em coordenadas reais) (480;0), (240;0), (-480,0), pensa assim
            //o centro do mundo, o spawn é 0,0, que é o centro do primeiro chunk, 120 pra algum lado, da na fronteira do chunk, e mais 120 (somando 240)
            //da no centro do chunk vizinho, sacou?
            position = coord * size;
            //Isso aqui vai criar o bound efetivamente, nessa coordenada real do mundo, e ele vai ter o tamanho do chunk, multiplicando um vetor (1,1)
            //pelo tamanho
            bounds = new Bounds(position, Vector2.one * size);
            //Esse aqui easy, basicamente vai converter a position em um vetor 3, onde x é o valor de x, y = 0, e z = o valor de y no vetor 2
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            //Aqui atribuimos ao mesh object um novo objeto, basicamente estamos criando um GameObject via código, o parametro é o nome
            //Depois fazendo o mesmo pro meshRenderer e Filter e adcionando eles a esse novo objeto
            //ai setamos no renderer o material do nosso objeto, aquele que passamos por parametro
            //colocamos esse objeto nessa coordenada que a gente criou ali em cima no positionV3
            //E atribuimos como pai dele o pai que passamos por parametrp (O MapGenerator) pra ficar limpa a hierarquia
            //E desativamos ele, pq é função do update chunk saber se ele tem que ta ativo ou não
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            //Essa linha de baixo e os outros dois métodos é relacionado a Threading, estudar threading, rever o video, ler aquele comentario no video
            //E depois documentar, o mesmo vale pros outros threading que estão se eu não me engano no MeshGenerator ou no MapGenerator
            mapGenerator.RequestMapData(OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            mapGenerator.RequestMapData(mapData, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        //Essa é a função que da update nos chunks e ve se estão no campo de visão ou não
        public void UpdateTerrainChunk()
        {
            //Aqui ele vai pegar a distancia do "player" até a fronteira mais proxima usando aquela função do bound e depois pegando a raiz quadrada disso
            //Pq? não tenho ctz do pq pega a raiz quadrada, testar depois, provavelmente retorna um valor insano ai precisa do sqrt pra deixar ele "traduzido"
            //pras medidas que estamos usando
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            //Esse boleano indica se esta visivel ou não, se esse float de cima for menor ou igual a distancia maxima de visão, então sim, esta visivel
            //se não for, ele não esta visivel
            bool visible = viewerDstFromNearestEdge <= maxViewDst;
            //Aqui ele chama o método SetVisible (o metodo que ta logo abaixo) e passa esse boleano pra ele 
            //Se for true ele ativa, se não for ele desativa
            SetVisible(visible);
        }

        //Le o comentaria acima desse que explica kkk
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        //Esse metodo só retorna se o objeto esta ou não ativo, usamos ele la emcima
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

    }
}
