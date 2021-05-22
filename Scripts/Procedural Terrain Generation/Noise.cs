using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;

/*
Resumo da copa em relação a Lacunarity, Amplitude, Persistance, ocataves, samples e etc
Amplitude e Frequencia, tem haver com onda, noise é tipo uma onda
Amplitude - Valor y, altura, da onda, altura maxima que a onda pode atingir
Frequencia - valor x, largura, da onda, ou força da onda
Se a frequencia for muito alta a onda vai atingir altas amplitudes rapidamente, porem ela vai ser muito comprimida, ou seja, aquelas ondas bem 
espinhosas em zig zag, ondas de alta frequencia
Se a frequencia for muito baixa ela vai atingir altas amplitudes mais devagar, resultando em ondas gordas e suaves, ou seja, ondas de baixa
frequencia
Octaves é basicamente mesclar varias ondas de frequecias e amplitudes diferentes, tendo uma como dominante, pra criar "detalhes" na onda,
muito util quando se esta usando a onda nesse contexto de noise
Lacunarity - controla o aumento da frequencia das ondas, ou seja, aumentando esse valor a onda fica com uma frequencia mais forte
Tendo em vista tres octaves (Tres ondas) se voce fizer:
frequencia = lacunarity^n (sendo n o número do octave começando a contagem por 0)
(nesse caso, frequencia = lacunarity^0 pro primeiro, lacunarity^1 pro segundo e lacunarity^2 pro terceiro)
Ou seja, aumenta pra cada octave, aumentando o nivel de "detalhes" que cada um tem
Obs.: Se lacunarity for = 1, todas terão a mesma frequencia, pq 1^n é sempre 1, por isso o valor minimo de lacunarity é 1 (OnValidate no MapGenerator
cuida desses minimos e validação)
você pode controlar a frequencia de cada octave de forma progressiva, aumentando a frequencia, dessa forma elas vão se tornando ondas de alta fre
quencia e tem mais 'detalhes' porem elas vão ser muito espinhosas e muito altas, ai vem a persistencia, para controlar essa altura indesejada
Persistance = (Valor entre 0 e 1)controla o decrésimo da amplitude das ondas, ou seja, abaixar esse valor diminui a altura da onda ainda
mantendo a frequencia
Tendo em vista tres octaves (Tres ondas) se voce fizer:
amplitude = persistance^n (sendo n o número do octave começando a contagem por 0)
(nesse caso, amplitude = persistance^0 pro primeiro, persistance^1 pro segundo e persistance^2 pro terceiro)
você pode controlar a amplitude das ondas de cada octave, ou seja, o primeiro octave se torna o dominante e sua forma vai ser a mais preservada
na onda final, ou seja, a forma do primeiro octave vai ganhar detalhes (Pegou a ideia)
*/

//Não usa o Mono Behaviour
//É uma clase statica, portanto pode ser chamada de qualquer lugar
public static class Noise
{
    //Isso é um método publico statico da classe Noise, que retorna uma lista 2D de floats (Varias listas com dois valores x, y cada, dentro da lista
    //Pai, é tipo matriz em python, dentro do index "x" tem um valor y que vai ser igual a algo, nesse caso de noise, um valor de altura), 
    //o método recebe esses valores como parametro e tem como função gerar uma lista de noiseMap

    //mapWidth (Largura do noise Map a ser gerado), mapHeight (Altura do Noise Map a ser gerado), seed (Valor que é usado como semente do noise map)
    //scale (Escala do noise Map, na pratica é tipo um zoom), octaves ([analogia da onda]Quantos noise maps vão ser empilhados pra formar o final)
    //persistance (Valor de 0 a 1 que controla a amplitude dos octaves), lacunarity (Valor >= 1, controla a frequencia dos octaves)
    //offset (Distancia do ponto central do map, usado na pratica pra andar pelo noise map e usar outras samples [amostras] dele)
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int ocataves, float persistance, float lacunarity, Vector2 offset)
    {
        //Aqui é criada a lista 2D que a função vai retornar mais tarde, considerando a altura e a largura em pixels do mapa, podemos determinar
        //quantos pixels tem no total, usando como lenght de x o width e de y o height, basicmanete para cada ponto x no noise map, vai ter uma lista
        //com valores para todos os ponto z(representado por y) que ta cruza com esse ponto x em um angulo de 90º
        /*
         zzzz  |z|  ^ ==
         zzzz  |z|  ^ ==
         zzzz  |z|  ^ ==
         zzzz  |z|  ^ ==
         zzzz  |z| -> Pontos z que cruzam de forma perpendicular a esse x que estão guardados na lista que fica no index x, os valores y no caso
         xxxx  |x| -> Ponto x a ser analisado (1º index)
         
         */

        //Tipo lista de python, dentro do index x da lista tem o valor y
        float[,] noiseMap = new float[mapWidth, mapHeight];
                             //Parametro dessa função é uma seed, por isso vair sair o mesmo numero com a mesma seed
        System.Random prng = new System.Random(seed); //Random number generator, gera um numero baseado na seed, se repetir a seed repete o numero
        Vector2[] octaveOffsets = new Vector2[ocataves]; //pra pegar um sample diferente dos octaves, andar pelo noise basicamente, sample é amostra
        //Amostra do noise, parte dele


        //Pega aquele numero random a partir de uma seed e "pula" esse ofset mais o offset que passa por parametro (O que controla a movimentação pelo noise)
        //Pra pegar sempre a mesma coordenada do noise (Pq ele é esencialmente infinito)
        for(int i = 0; i < ocataves; i++) {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            //Pelo que eu entenid procurando na internet, esse next vai gerar um numero aleatorio entre esse valor minimo e maximo baseado no valor
            //que esse prng representa, mas como esse prng tem seed, vai sair o mesmo sempre, ele só passa esse parametro pra gerar seed bem longes
            //uma das outras
            octaveOffsets[i] = new Vector2(offsetX, offsetY); //E adciona esses valores de offsets em cada octave, pra dar uma variada braba e ter 
            //um sistema de seeds
        }


        //Handler que seta a escala pra um valor minimo caso ela seja igual ou menor a 0
        if (scale <= 0)
        {
            scale = 0.0001f;
        }
        //Isso aqui é tipo aquele esquema no python pra setar valor máximo e minino, onde o minimo começa no maior possivel e tudo que for
        //Menor que esse valor é setado como minimo até que o menor da lista esteja na var min value, e a mesma coisa pro max só que inverso,
        //Ele comeca baixo e tudo que for maior toma seu lugar
        /*
         min = 1000
         max = 0
         if(10 < min):
           min = 10
         if(10 > max):
           max = 10
        resultado
        min = 10
        max = 10
         
         */
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        //pra calcular metade da largura e da altura (pra poder centralizar a amostra no centro ao invez do canto superior direito)
        //Assim quando da zoom ele da zoom no centro
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        //Vai iterar y até que ele bata na altura maxima do mapa de noise que setamos
        for (int y = 0; y < mapHeight; y++)
        {
            //Vai iterar x até que ele bata na altura maxima do mapa de noise que setamos
            for (int x = 0; x < mapWidth; x++)
            {
                //Placeholders pra amplitude, frequencia e noise height
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                //Esse outro for vai gerar um noiseMap pra cada octave, então ele vai iterar por x e y de cada um dos octaves gerando todos os 
                //noises
                for(int i = 0; i < ocataves; i++)
                {
                    //Pra cada valor de x e y nesse ponto que ele ta, ele vai dividir pela scala de noise (setada e "handlada")
                    //La em cima, e atribuir ao float sampleX e sampleY
                    //Multiplica pela frequencia, assim quanto maior a frequencia maior a distancia dos samples, e mais rapido
                    //A mudanca de height (tem haver com a amplitude de onda e sua relação com frequencia, lacunarity e persistance)
                    //subrai esse half width e halfheight pq senão ele cria a amostra a partir do canto superior direito, assim ele centraliza a amostra
                    //Testa na pratica, so mexer no NoiseScale do editor
                    float sampleX = (x-halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y-halfHeight) / scale * frequency + octaveOffsets[i].y;

                    //Então esses samples(amostras) serão usadas de parametro para efetivamente gerar um valor de perlinNoise de determinada coord
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; //*2 - 1 vai fazer ele poder ir de -1 a 1 ao inves de 0 a 1

                    //E vai ser atribuido a sua posição no noiseMap final
                    //Ou seja, no ponto x do mapa, algum valor de y(z disfarcado) vai ter esse valor de altura gerado pelo perlin noise
                    noiseMap[x, y] = perlinValue;
                    //Isso é o que seria a amplitude da onda, no exemplo da montanha da pra ver certinho
                    //(Sebastian League - Procedural Terrain Generation - Introduction)
                    noiseHeight += perlinValue * amplitude;

                    //Esquema explicado la em cima
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                //Aqui ele começa determinar o maior e o menor valor de altura dentro do noise map, a cada loop do for
                //Se noise height for maior que o maxNoiseHeight, o novo maxNoiseHeight é ele, se ele for menor que o minNoiseHeight
                //o novo menor é ele
                if(noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }else if(noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                //nesse ponto ele salva esse noise height na lista 2D
                noiseMap[x, y] = noiseHeight;
            }
        }

        //Esse for é um normalize, pq la em cima setamos o perlin pra retornar valores negativos e ter quedas no "terreno"
        //Mas como ele não le o negativo ele vai normalizar isso entre 0 e 1 sem perder o "peso" do valor, basicamente ta traduzindo de uma forma
        //que vai poder ser lida e usada depois mais não perde o significado, ainda vai ter quedas
        //Pra isso ele itera pela lista 2D e acha o valor que interpola(interpolate) entre o minNoiseHeight e o maxNoiseHeight baseado no height
        //daquele ponto, gerando um valor entre 0 e 1
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
        
        return noiseMap;
    }
}
