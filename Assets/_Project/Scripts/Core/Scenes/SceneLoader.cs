// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Carga las escenas de forma asíncrona con fundido a negro y sobrevive entre escenas.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    // Nombre de la escena del menú (tiene que coincidir con el archivo de la escena)
    public static string MainMenuScene = "MainMenu";

    [SerializeField] private float fadeDuration = 0.4f;

    private static SceneLoader instance;

    private CanvasGroup fadeGroup;
    private bool loading;

    public static bool IsLoading => instance != null && instance.loading;

    private static SceneLoader Instance
    {
        get
        {
            if (instance == null) new GameObject("SceneLoader").AddComponent<SceneLoader>(); // Awake asigna instance
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildFadeCanvas();
    }

    // ---------- API pública ----------

    public static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneLoader: el nombre de la escena está vacío.");
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneLoader: la escena '{sceneName}' no está en File > Build Profiles > Scene List.");
            return;
        }
        if (IsLoading) return;

        Instance.StartCoroutine(Instance.LoadRoutine(sceneName));
    }

    public static void ReloadCurrent()
    {
        Load(SceneManager.GetActiveScene().name);
    }

    public static void LoadMainMenu()
    {
        Load(MainMenuScene);
    }

    // ---------- Carga ----------

    private IEnumerator LoadRoutine(string sceneName)
    {
        loading = true;
        fadeGroup.blocksRaycasts = true; // bloquea clicks durante la transición

        yield return Fade(0f, 1f);

        Time.timeScale = 1f; // por si venimos de una pantalla de victoria/pausa

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        yield return null; // un frame para que la escena nueva haga sus Start()

        yield return Fade(1f, 0f);

        fadeGroup.blocksRaycasts = false;
        loading = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // funciona aunque timeScale sea 0
            fadeGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = to;
    }

    // Canvas negro a pantalla completa, por encima de todo
    private void BuildFadeCanvas()
    {
        GameObject canvasGO = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        fadeGroup = canvasGO.GetComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        GameObject imageGO = new GameObject("Black", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);

        Image image = imageGO.GetComponent<Image>();
        image.color = Color.black;

        RectTransform rect = imageGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
