using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor; //Usado pra modificar coisas no inspector

//Indica que essa classe é um custom editor do tipo MapGenerator (do tipo == o que vai modificar)
[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{

    //Vai dar override na função principal do inspector
    public override void OnInspectorGUI()
    {

        //Vai instanciar o target (O objeto que ele modifica o inspector)
        MapGenerator mapGen = (MapGenerator)target;

        //Vai mandar desenhar o inspector normalmente
        //Pq estamos apenas adcionando coisas, e não criando toda a aba do inspector do zero
        //Esse if diz que, se ocorrer alguma alteração no inspector e o autoUpdate estiver ligado no mapGenerator ele tambem tem que dar
        //GenerateMap, assim como ele faz quando aperta o botão, pq sempre que altera o inspector ele é rapidamente desenhado dnvo, então quer dizer
        //Que a função DrawDefaultInspector foi chamada, e retorna true pro nosso if, por isso funciona
        if (DrawDefaultInspector())
        {
            if (mapGen.autoUpdate)
            {
                mapGen.DrawMapInEditor();
            }
        }

        //Cria um botão chamado "Generate" que se for clicado chama a função generate map do MapGenerator, que vai gerar o noiseMap e passar
        //pro MapDisplay, que vai converter ele em textura e desenhar no plano
        if (GUILayout.Button("Generate"))
        {
            mapGen.DrawMapInEditor();
        }
    }
}
