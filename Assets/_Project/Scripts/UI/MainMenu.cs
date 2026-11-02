// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Botones del menú principal: jugar y salir.

using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena del nivel")]
    [SerializeField] private string gameSceneName = "Level_01";

    public void Play()
    {
        SceneLoader.Load(gameSceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
