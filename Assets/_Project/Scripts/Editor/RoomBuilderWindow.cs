// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Herramienta de editor que arma salas modulares (piso, paredes, puertas y columnas) y las guarda como prefabs.

using System.IO;
using UnityEditor;
using UnityEngine;

public class RoomBuilderWindow : EditorWindow
{
    private enum Side { Wall, Door, Open }

    private const string DefaultModelsFolder = "Assets/_Project/Models/Enviroment";
    private const string DefaultPrefabFolder = "Assets/_Project/Prefabs/Environment";

    // ---------- Piezas ----------
    private GameObject floorPiece;
    private GameObject wallPiece;
    private GameObject baseboardPiece;
    private GameObject archPiece;
    private GameObject columnPiece;

    // ---------- Configuración de la sala ----------
    private string roomName = "Room_New";
    private int widthTiles = 6;   // eje X
    private int lengthTiles = 6;  // eje Z
    private float tileSize = 2f;
    private Side north = Side.Wall;
    private Side south = Side.Wall;
    private Side east = Side.Wall;
    private Side west = Side.Wall;
    private int doorTiles = 2;
    private bool useArch = true;
    private bool addBaseboard = true;
    private bool addCornerColumns = true;
    private float columnHeightScale = 0.55f; // las columnas miden 4 m; las paredes 2 m
    private string groundLayerName = "Ground";
    private bool markStatic = true;

    // ---------- Guardado ----------
    private bool saveAsPrefab = true;
    private string prefabFolder = DefaultPrefabFolder;

    private Vector2 scroll;

    [MenuItem("Tools/Room Builder")]
    public static void Open()
    {
        GetWindow<RoomBuilderWindow>("Room Builder");
    }

    private void OnEnable()
    {
        if (floorPiece == null) LoadDefaultPieces();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        // --- Presets ---
        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        // Medidas pares: así los bordes caen sobre la grilla de 2 m y las puertas quedan centradas
        if (GUILayout.Button("Start")) ApplyPreset("Room_Start", 6, 6, Side.Door, Side.Wall, Side.Wall, Side.Wall, true);
        if (GUILayout.Button("Hall")) ApplyPreset("Room_Hall", 10, 6, Side.Door, Side.Door, Side.Door, Side.Door, true);
        if (GUILayout.Button("Altar")) ApplyPreset("Room_Altar", 8, 8, Side.Wall, Side.Door, Side.Wall, Side.Wall, true);
        if (GUILayout.Button("Boss")) ApplyPreset("Room_Boss", 10, 10, Side.Wall, Side.Door, Side.Wall, Side.Wall, true);
        // Los pasillos no llevan columnas: las esquinas ya las pone la sala con la que se unen
        if (GUILayout.Button("Corridor")) ApplyPreset("Corridor_Straight", 2, 4, Side.Open, Side.Open, Side.Wall, Side.Wall, false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // --- Sala ---
        EditorGUILayout.LabelField("Sala", EditorStyles.boldLabel);
        roomName = EditorGUILayout.TextField("Nombre", roomName);
        widthTiles = Mathf.Max(1, EditorGUILayout.IntField("Ancho (baldosas, X)", widthTiles));
        lengthTiles = Mathf.Max(1, EditorGUILayout.IntField("Largo (baldosas, Z)", lengthTiles));
        tileSize = Mathf.Max(0.1f, EditorGUILayout.FloatField("Tamaño de baldosa (m)", tileSize));
        EditorGUILayout.HelpBox($"Tamaño final: {widthTiles * tileSize} x {lengthTiles * tileSize} m", MessageType.None);
        ShowGridWarnings();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lados", EditorStyles.boldLabel);
        north = (Side)EditorGUILayout.EnumPopup("Norte (+Z)", north);
        south = (Side)EditorGUILayout.EnumPopup("Sur (-Z)", south);
        east = (Side)EditorGUILayout.EnumPopup("Este (+X)", east);
        west = (Side)EditorGUILayout.EnumPopup("Oeste (-X)", west);
        doorTiles = Mathf.Max(1, EditorGUILayout.IntField("Ancho de puerta (baldosas)", doorTiles));
        useArch = EditorGUILayout.Toggle("Arco en las puertas", useArch);
        ShowDoorWarnings();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Extras", EditorStyles.boldLabel);
        addBaseboard = EditorGUILayout.Toggle("Zócalo", addBaseboard);
        addCornerColumns = EditorGUILayout.Toggle("Columnas en esquinas", addCornerColumns);
        if (addCornerColumns) columnHeightScale = EditorGUILayout.Slider("  Altura columnas", columnHeightScale, 0.3f, 1f);
        groundLayerName = EditorGUILayout.TextField("Layer del piso", groundLayerName);
        markStatic = EditorGUILayout.Toggle("Marcar como Static", markStatic);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Piezas", EditorStyles.boldLabel);
        floorPiece = (GameObject)EditorGUILayout.ObjectField("Piso", floorPiece, typeof(GameObject), false);
        wallPiece = (GameObject)EditorGUILayout.ObjectField("Pared", wallPiece, typeof(GameObject), false);
        baseboardPiece = (GameObject)EditorGUILayout.ObjectField("Zócalo", baseboardPiece, typeof(GameObject), false);
        archPiece = (GameObject)EditorGUILayout.ObjectField("Arco", archPiece, typeof(GameObject), false);
        columnPiece = (GameObject)EditorGUILayout.ObjectField("Columna", columnPiece, typeof(GameObject), false);
        if (GUILayout.Button("Cargar piezas por defecto")) LoadDefaultPieces();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Guardado", EditorStyles.boldLabel);
        saveAsPrefab = EditorGUILayout.Toggle("Guardar como prefab", saveAsPrefab);
        if (saveAsPrefab) prefabFolder = EditorGUILayout.TextField("Carpeta", prefabFolder);

        EditorGUILayout.Space();
        GUI.enabled = floorPiece != null && wallPiece != null;
        if (GUILayout.Button("GENERAR SALA", GUILayout.Height(32))) Generate();
        GUI.enabled = true;

        if (floorPiece == null || wallPiece == null)
        {
            EditorGUILayout.HelpBox("Faltan piezas: asigná al menos Piso y Pared.", MessageType.Error);
        }

        EditorGUILayout.EndScrollView();
    }

    // ---------- Generación ----------

    private void Generate()
    {
        GameObject root = new GameObject(roomName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Room");

        Transform floorGroup = CreateGroup("Floor", root.transform);
        Transform wallGroup = CreateGroup("Walls", root.transform);
        CreateGroup("Props", root.transform);
        CreateGroup("Lights", root.transform);

        BuildFloor(floorGroup);

        float halfW = widthTiles * tileSize * 0.5f;
        float halfL = lengthTiles * tileSize * 0.5f;

        // Norte y Sur recorren el eje X; Este y Oeste el eje Z
        BuildSide(wallGroup, north, widthTiles, t => new Vector3(t, 0f, halfL), 0f, "N");
        BuildSide(wallGroup, south, widthTiles, t => new Vector3(t, 0f, -halfL), 180f, "S");
        BuildSide(wallGroup, east, lengthTiles, t => new Vector3(halfW, 0f, t), 90f, "E");
        BuildSide(wallGroup, west, lengthTiles, t => new Vector3(-halfW, 0f, t), 270f, "W");

        if (addCornerColumns && columnPiece != null) BuildCorners(wallGroup, halfW, halfL);

        if (markStatic) SetStaticRecursive(root);

        // Lo dejamos seleccionado para verlo enseguida
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();

        if (saveAsPrefab) SavePrefab(root);
    }

    private void BuildFloor(Transform parent)
    {
        int groundLayer = LayerMask.NameToLayer(groundLayerName);
        if (groundLayer < 0)
        {
            Debug.LogWarning($"RoomBuilder: el layer '{groundLayerName}' no existe. El piso queda en Default y el click para moverse no va a funcionar.");
            groundLayer = 0;
        }

        for (int x = 0; x < widthTiles; x++)
        {
            for (int z = 0; z < lengthTiles; z++)
            {
                // El piso mide 0.25 de alto con el pivote al centro: lo bajamos para que la cara de arriba quede en y = 0
                Vector3 pos = new Vector3(TileCenter(x, widthTiles), -0.125f, TileCenter(z, lengthTiles));
                Spawn(floorPiece, parent, pos, 0f, $"Floor_{x}_{z}");
            }
        }

        // Un solo collider para todo el piso: lo usa el raycast del click (layer Ground)
        BoxCollider col = parent.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -0.125f, 0f);
        col.size = new Vector3(widthTiles * tileSize, 0.25f, lengthTiles * tileSize);

        SetLayerRecursive(parent.gameObject, groundLayer);
    }

    private void BuildSide(Transform parent, Side side, int tiles, System.Func<float, Vector3> edgePos, float yaw, string label)
    {
        if (side == Side.Open) return;

        int doorStart = -1, doorEnd = -1;
        if (side == Side.Door)
        {
            int width = Mathf.Min(doorTiles, tiles);
            doorStart = (tiles - width) / 2;
            doorEnd = doorStart + width - 1;
        }

        for (int t = 0; t < tiles; t++)
        {
            if (t >= doorStart && t <= doorEnd) continue;

            // La pared mide 2 m de alto con el pivote al centro: y = 1 la apoya en el piso
            Vector3 pos = edgePos(TileCenter(t, tiles)) + Vector3.up * 1f;
            Spawn(wallPiece, parent, pos, yaw, $"Wall_{label}_{t}");
            AddAlignedCollider(parent, pos, yaw, new Vector3(tileSize, 2f, 0.44f), $"WallCollider_{label}_{t}");

            if (addBaseboard && baseboardPiece != null)
            {
                Spawn(baseboardPiece, parent, pos, yaw, $"Baseboard_{label}_{t}");
            }
        }

        if (side == Side.Door && useArch && archPiece != null)
        {
            // Centro de la abertura = promedio de las baldosas que ocupa la puerta
            float center = (TileCenter(doorStart, tiles) + TileCenter(doorEnd, tiles)) * 0.5f;
            Vector3 archPos = edgePos(center);
            Spawn(archPiece, parent, archPos, yaw, $"Arch_{label}");

            // Colliders en los dos pilares del arco (el hueco central queda libre, ~2.5 m)
            Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 pillarSize = new Vector3(0.8f, 4f, 1f);
            AddAlignedCollider(parent, archPos + yawRot * new Vector3(-1.65f, 2f, 0f), yaw, pillarSize, $"ArchCollider_{label}_L");
            AddAlignedCollider(parent, archPos + yawRot * new Vector3(1.65f, 2f, 0f), yaw, pillarSize, $"ArchCollider_{label}_R");
        }
    }

    private void BuildCorners(Transform parent, float halfW, float halfL)
    {
        TryCorner(parent, new Vector3(halfW, 0f, halfL), north, east, "NE");
        TryCorner(parent, new Vector3(-halfW, 0f, halfL), north, west, "NW");
        TryCorner(parent, new Vector3(halfW, 0f, -halfL), south, east, "SE");
        TryCorner(parent, new Vector3(-halfW, 0f, -halfL), south, west, "SW");
    }

    private void TryCorner(Transform parent, Vector3 pos, Side a, Side b, string label)
    {
        if (a == Side.Open && b == Side.Open) return;

        GameObject column = Spawn(columnPiece, parent, pos, 0f, $"Column_{label}");
        Vector3 s = column.transform.localScale;
        column.transform.localScale = new Vector3(s.x, s.y * columnHeightScale, s.z);
    }

    // ---------- Utilidades ----------

    // Instancia la pieza manteniendo el vínculo con el FBX y respetando su rotación/escala de importación
    private static GameObject Spawn(GameObject piece, Transform parent, Vector3 localPos, float yaw, string name)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(piece, parent);
        go.name = name;
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * piece.transform.localRotation;
        go.transform.localScale = piece.transform.localScale;
        return go;
    }

    // Collider en un objeto vacío alineado con la sala (no depende de la rotación interna del FBX)
    private static void AddAlignedCollider(Transform parent, Vector3 localPos, float yaw, Vector3 size, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        BoxCollider col = go.AddComponent<BoxCollider>();
        col.size = size;
    }

    private float TileCenter(int index, int count)
    {
        return (index - (count - 1) * 0.5f) * tileSize;
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }

    private static void SetStaticRecursive(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        foreach (Transform child in go.transform) SetStaticRecursive(child.gameObject);
    }

    // Con el pivote en el centro, un lado impar deja los bordes en metros impares (fuera de la grilla de 2 m)
    private void ShowGridWarnings()
    {
        if (widthTiles % 2 != 0 || lengthTiles % 2 != 0)
        {
            EditorGUILayout.HelpBox(
                $"Hay un lado con cantidad impar de baldosas ({widthTiles} x {lengthTiles}). Sus bordes no caen en la grilla de {tileSize} m: " +
                $"al unir esta sala con otra va a quedar un hueco de {tileSize * 0.5f} m. Usá cantidades pares.",
                MessageType.Warning);
        }
    }

    private void ShowDoorWarnings()
    {
        CheckDoorParity(north, widthTiles, "Norte");
        CheckDoorParity(south, widthTiles, "Sur");
        CheckDoorParity(east, lengthTiles, "Este");
        CheckDoorParity(west, lengthTiles, "Oeste");
    }

    private void CheckDoorParity(Side side, int tiles, string label)
    {
        if (side != Side.Door) return;
        if ((tiles - doorTiles) % 2 != 0)
        {
            EditorGUILayout.HelpBox(
                $"Lado {label}: con {tiles} baldosas y una puerta de {doorTiles}, la abertura queda corrida {tileSize * 0.5f} m del centro. " +
                "Usá una cantidad par de baldosas en ese lado para que quede centrada.",
                MessageType.Warning);
        }
    }

    private void ApplyPreset(string name, int w, int l, Side n, Side s, Side e, Side wSide, bool cornerColumns)
    {
        addCornerColumns = cornerColumns;
        roomName = name;
        widthTiles = w;
        lengthTiles = l;
        north = n;
        south = s;
        east = e;
        west = wSide;
    }

    private void LoadDefaultPieces()
    {
        floorPiece = Load("Floor_Modular");
        wallPiece = Load("Wall_Modular");
        baseboardPiece = Load("WallCover_Modular");
        archPiece = Load("Arch");
        columnPiece = Load("Column");
    }

    private static GameObject Load(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"{DefaultModelsFolder}/{fileName}.fbx");
    }

    private void SavePrefab(GameObject root)
    {
        EnsureFolder(prefabFolder);
        string path = $"{prefabFolder}/{roomName}.prefab";

        if (File.Exists(path) &&
            !EditorUtility.DisplayDialog("Room Builder", $"Ya existe {path}. ¿Reemplazarlo?", "Reemplazar", "Cancelar"))
        {
            return;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);
        Debug.Log($"RoomBuilder: prefab guardado en {path}");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] parts = folder.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
