using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    //Esse método que efetivamente gera uma textura, a partir de um colorMap (Seja ele preto e branco pra gerar o noiseMap preto e branco tradicional
    //Seja ele colorido pra gerar terreno), a largura e altura da textura, e cria uma textura com essas informações, e então retorna ela ou direta
    //mente pro DrawTexture no map display, ou pro TextureFromHeightMap, pra entao o metodo TextureFromHeightMap retornar isso pro DrawTexture do
    //MapDisplay.cs
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; //Isso resolve o blur(embaçado) da textura
        texture.wrapMode = TextureWrapMode.Clamp; //Isso resolve a borda zuada que tinha antes
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }
    

    //Esse metodo basicamente é o map display (foi só trocado de lugar a parte que gera um color map preto e branco baseado no heightMap do noiseMap
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        //Pega os valores de altura e largura com base no noiseMap
        int width = heightMap.GetLength(0); //Vai pegar o  lenght da primeira dimensão, ou seja, do x, largura
        int height = heightMap.GetLength(1); //Pega o valor da segunda dimensão, ou seja, do y, altura


        //Cria uma nova Textura 2D, de altura e largura setadas logo acima
        Texture2D texture = new Texture2D(width, height);


        //Cria um array de cores, e seta como tamanho width*height, pq em um é a quantidade de pixels que tem dentro de uma area (Tipo area de quadrado)
        //Tipo 1280x720, pra descobrir quantos pixels tem ai, multiplica 1280*720
        Color[] colorMap = new Color[width * height];

        //Itera y e x ate bater height e width
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //Em cada posição ele converte a posição 2D da lista 2D em "1D", ou seja, y * width + x é o index
                //Exemplo, y = 0 (Primeira linha, a mais de baixo), width = 10 (O noise map tem 10 de largura) e x = 1 (Segunda coluna da 1 linha)
                //Nesse caso o index seria 0*10+1 = 1, ou seja, da diagonal inferior esquerda em direção a diagonal superior direita, ele ta anali
                //sando o pixel de numero 1, que é o segundo pixel da imagem (a contagem de pixel começa por 0, assim 0,1,2...), ou seja, o segundo
                //pixel da primeira linha de pixels, assim você converte a coordenada 2D (x,y) em um só valor que é o número do pixel, e não sua posi
                //ção, ou seja, segundo pixel, terceiro pixel, quarto pixel, ao ivés de pixel(x,y)
                //Ele basicamente vai estar na ponta de cima da diagonal direita do pixel que ele estiver analisando
                //Seta nesse index uma cor entre preto e branco baseado no valor que ta em x e y no noise map
                //Ai o pixel que ta no index y*width+x (vamos supor index 1), vai pegar uma cor entre preto e branco baseado no valor do pixel(x,y)
                //Do noise map (Que é ele mesmo, só ta convertendo o valor dele em cor pra uma lista 1D)
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        //Vai setar os pixels na textura com as cores que ele setou no colorMap a partir do noiseMap
        texture.SetPixels(colorMap);
        //Aplica esses pixels na textura
        texture.Apply();
        
        //E retorna uma textura 2D preta e branca criada pelo metodo acima, que pega um ColorMap (Que é gerado nesse metodo) e cria uma textura com ele
        return TextureFromColorMap(colorMap, width, height);
    }
}
