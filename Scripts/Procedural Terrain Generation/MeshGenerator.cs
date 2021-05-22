using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using UnityEngine;

//Clase estatica que não herda do MonoBehaviour
public static class MeshGenerator
{
    //Cria um metodo que retorna a MeshData e recebe como valor o heightMap (noiseMap)
    //O heightMultiplier vai controlar a altura da  Mesh e a AnimationCurve o quanto certas alturas são afetadas pelo heightMultiplier
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        //Cria uma animaton curve com o mesmo valor da outra pra não dar problema com os Threads, pq quando colocou o sistema de threads
        //Cada thread deu um evaluate nela (La em baixo onde ela é usada) e acabou gerando um espinhos bizarros, isso conserta, ja que agora
        //Esse thread vai usar essa animation Curve que ta só nele
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        //Pega o valor de largura e altura
        //E pega os valor do maior ponto no vetor x a esquerda (negativo)
        //E pega os valor do maior ponto no vetor z a esquerda (negativo)
        //Se imagine olhando de frente pra linha de cada um desses vetores, a esquerda da linha do vetor x esta os negativos, e no z tambem
        //Calculamos isso pra la embaixo criar a mesh a partir do centro 0,0 e ter coordenadas negativas no mundo (Boom minecraft)
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        //Basicamente isso ai é um if que se for true ele seta o valor antes dos ":", ou seja, 1, caso contrario, seta o valor depois do ":"
        //ou seja levelOfDetail * 2, é tipo, levelOfDetail é igual a 0? Se sim meshSimplificationIncrement = 1, se não
        // meshSimplificationIncrement = levelOfDetail * 2
        int meshSimplificationIncrement = (levelOfDetail == 0)? 1: levelOfDetail * 2;
        int veticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        //Aqui ele cria um MeshData (Método que ta ali embaixo) e passa os parametros largura e altura pra ele
        //Basicamente vai criar uma lista de vetores 3 com coordenadas (x,y,z) com os vertices da Mesh(malha)
        //E uma lista de inteiros com os vertices que montam cada triangulo
        MeshData meshData = new MeshData(veticesPerLine, veticesPerLine);
        //Valor que vai segurar o Index atual do vertex que estamos mexendo ali no for loop logo abaixo
        int vertexIndex = 0;

        //Vai iterar pelas coordenadas claro
        for(int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for(int x = 0; x < width; x += meshSimplificationIncrement)
            {
                //vai adcionar esse vertice na lista de vertices do mesh data, na posição do index que estamos (o index do vertice em uma lista 1D)
                //vai setar como valor x o x + o topleftX pra criar as coordenadas negativas tambem, o y é o valor de altura na coordenada x, y
                //do Height map, e o Z é o valor de y nesse ponto (Que como temos como perspectiva inicial um plano, usamos o y como medida de pro
                //fundidade, mas agora pra gerar a mesh ele vira o z, tipo o blender, que o y é profundidade, por isso usa o valor de y aqui)
                //e subtrai dele o topLeftZ pra criar as coordenadas negativas
                //Add --- heightCurve e height multiplier, o height multiplier controla o quanto o valor de y vai incrementar, criando variações
                //de altura significantes no terreno, o heightcurve, segue uma curva de controle no inspector que determina o quando o valor de y
                //vai ser afetado, mexe la no inspector pra ver, isso vai permitir que a água não seja afetada pelo heightMultiplier e pareca uma
                //Planicie com sarna
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                //Vai adcionar na lista de coordenadas uvs a porcentagem da posição desse vertice em relação ao mapa todo
                //Basicamente o quanto em porcentagem do mapa que essa posição ocupa, pq é assim? Não sei, mas é assim que o calculate Uvs funciona
                //Pra poder criar UvMap e aplicar texturas na malha
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                //Pra entender melhor essa parte:
                //Sebastian League - Procedural Terrain Generation - EO5: Mesh - 8:17
                //Mas basicamente ele ta vendo se o vertice não é um vertice da ponta (Ultimo vértice da linha)
                //Pq esses vertices na ponta da malha não formam mais triangulos, pq eles tem que ser em sentido horario e se tentasse formar na ponta
                //Nao teria vertices pra la da ponta pra fazer isso
                //Nesse caso, o if valida se não está na ponta, se não está ele adiciona um triangulo usando o meshData.AddTriangle
                //Que adiciona os pontos a,b,c que constituem um triangulo (os vertices numerados) a uma lista pra poder ser renderizado depois
                //vertexIndex é o vertice que está agora, vertexIndex+width, é o vertice que está de frente pro atual, que esta em outra linha
                //vertexIndex + width + 1 é o vertice diagonal a esse, que esta na outra linha e no canto
                //Nesse caso o triangulo da esquerda do quadrado é formado indo do que ta agora, pro na diagonal dele, pro que ta do lado dele
                //lado = (cima, baixo, esquerda ou direita)
                //E o triangulo do lado direito é formado pelo que ta na diagonal do vertexIndex atual, o vertexIndex atual, e o que ta do lado do atual
                //Vertex index + 1
                //Dessa forma os dois triangulos são no sentido horário
                /*
                 i----i+1
                 |    |
                 |    |
                 i+w--i+w+1
                
                Triangulo 1: i, i+w+1, i+w
                Triangulo 2: i+w+1, i, i+1
                ve ai
                 */
                if(x < width-1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + veticesPerLine + 1, vertexIndex + veticesPerLine);
                    meshData.AddTriangle(vertexIndex + veticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                //Soma 1 no index pra continuar a lista
                vertexIndex++;
            }
        }

        return meshData;
    }
}

//Essa classe é responsavel pela data da mesh (malha)
public class MeshData
{
    //Ela vai criar uma lista de vetor3 que vai guardar as coordenadas do vertices
    //E uma lista de inteiros que vai guardar qual o numero dos 3 vertices que formar um triangulo
    public Vector3[] vertices;
    public int[] triangles;
    //Cria uma lista de Uvs que vai armazenar a porcentagem da posição do vertice em relação as dimensões da malha
    //Pra poder montar o UvMap e aplicar texturas na malha
    public Vector2[] uvs;

    //Vai criar uma variavel pra segurar o index da lista de triangulos no método AddTriangle
    int triangleIndex;

    //MeshData, vai guardar a coordenada dos vertices e os pontos que compoem os triangulos
    //Vai guardar tambem as coordenadas dos Uvs, que tem o tamanho que é basicamente todo pixel da malha
    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    //Adciona um triangulo na mesh adcionando na lista de triangulo as 3 coordenadas que o compoem
    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c; //Soma no index pra ir indo pra frente na lista e adcionando no "final"
        triangleIndex += 3; //E soma mais tres no valor do index pq no final disso foi adcionada 3 valores na frente do index da variavel
        //Ai atualiza ele pra ele poder sempre estar um na frente da ultima posição que foi adcionada
    }

    //Cria um objeto do tipo mesh e retorna ele
    public Mesh CreateMesh()
    {
        //Cria e mesh
        Mesh mesh = new Mesh();
        //Pega os vertices dela e os triangulos da lista de vertices e triangulos que criamos la emciam
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        //Mesma coisa pros uvs
        mesh.uv = uvs;
        //Recalcula os Normals, pra não ficar com um shader bizarro
        mesh.RecalculateNormals();
        //E então retorna essa mesh
        return mesh;
    }
}
