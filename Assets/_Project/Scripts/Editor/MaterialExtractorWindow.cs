// Autor: Carlos Matias Marques
// Email: Charlie.Marques92@gmail.com
// Descripción: Herramienta de editor que extrae los materiales embebidos de los FBX a archivos .mat ordenados y sin duplicados.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MaterialExtractorWindow : EditorWindow
{
    private string sourceFolder = "Assets/_Project/Models";
    private string destinationFolder = "Assets/_Project/Materials";
    [Tooltip("Carpetas (separadas por coma) donde cada modelo tiene su propia subcarpeta de materiales")]
    private string perModelFolders = "Monsters";

    // Resultado del análisis: material de destino -> modelos que lo usan
    private readonly SortedDictionary<string, List<string>> plan = new SortedDictionary<string, List<string>>();
    private int modelCount;
    private Vector2 scroll;

    [MenuItem("Tools/Material Extractor")]
    public static void Open()
    {
        GetWindow<MaterialExtractorWindow>("Material Extractor");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Carpetas", EditorStyles.boldLabel);
        sourceFolder = FolderField("Origen (FBX)", sourceFolder);
        destinationFolder = FolderField("Destino (.mat)", destinationFolder);
        perModelFolders = EditorGUILayout.TextField(
            new GUIContent("Subcarpeta por modelo en", "Ej: 'Monsters' → Materials/Monsters/Bat/, Materials/Monsters/Slime/..."),
            perModelFolders);

        EditorGUILayout.HelpBox(
            "Ejemplo de resultado:\n" +
            "  Materials/Enviroment/Fire.mat  (compartido por Torch, Woodfire...)\n" +
            "  Materials/Monsters/Bat/...      (cada monstruo en su carpeta)",
            MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1. Analizar", GUILayout.Height(28))) Analyze();
        GUI.enabled = plan.Count > 0;
        if (GUILayout.Button("2. Extraer", GUILayout.Height(28))) Extract();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (plan.Count == 0) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"{modelCount} modelos → {plan.Count} materiales", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (KeyValuePair<string, List<string>> entry in plan)
        {
            string models = string.Join(", ", entry.Value);
            EditorGUILayout.LabelField(Relative(entry.Key), models, EditorStyles.wordWrappedMiniLabel);
        }
        EditorGUILayout.EndScrollView();
    }

    // ---------- Análisis ----------

    private void Analyze()
    {
        plan.Clear();
        modelCount = 0;

        foreach (string modelPath in FindModels())
        {
            List<Material> materials = EmbeddedMaterials(modelPath);
            if (materials.Count == 0) continue;

            modelCount++;
            string modelName = Path.GetFileNameWithoutExtension(modelPath);

            foreach (Material mat in materials)
            {
                string target = TargetPath(modelPath, mat);
                if (!plan.TryGetValue(target, out List<string> users))
                {
                    users = new List<string>();
                    plan.Add(target, users);
                }
                if (!users.Contains(modelName)) users.Add(modelName);
            }
        }

        if (plan.Count == 0)
        {
            EditorUtility.DisplayDialog("Material Extractor",
                "No encontré materiales embebidos. Puede que ya estén extraídos.", "OK");
        }
    }

    // ---------- Extracción ----------

    private void Extract()
    {
        if (!EditorUtility.DisplayDialog("Material Extractor",
                $"Se van a crear/reutilizar {plan.Count} materiales en '{destinationFolder}' " +
                $"y reasignar {modelCount} modelos.\n\nEsta acción no se puede deshacer con Ctrl+Z. ¿Continuar?",
                "Extraer", "Cancelar"))
        {
            return;
        }

        List<string> models = FindModels().ToList();
        int created = 0, reused = 0;

        try
        {
            for (int i = 0; i < models.Count; i++)
            {
                string modelPath = models[i];
                EditorUtility.DisplayProgressBar("Material Extractor", Path.GetFileName(modelPath), (float)i / models.Count);

                ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null) continue;

                List<Material> materials = EmbeddedMaterials(modelPath);
                if (materials.Count == 0) continue;

                foreach (Material mat in materials)
                {
                    string target = TargetPath(modelPath, mat);
                    Material external = AssetDatabase.LoadAssetAtPath<Material>(target);

                    if (external == null)
                    {
                        EnsureFolder(Path.GetDirectoryName(target).Replace('\\', '/'));
                        external = new Material(mat) { name = mat.name };
                        AssetDatabase.CreateAsset(external, target);
                        created++;
                    }
                    else
                    {
                        reused++;
                    }

                    // El FBX deja de usar su material interno y pasa a usar el archivo .mat
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), external);
                }

                importer.SaveAndReimport();
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Material Extractor: {created} materiales creados, {reused} reutilizados, {models.Count} modelos procesados.");
        plan.Clear();
        EditorUtility.DisplayDialog("Material Extractor",
            $"Listo.\n\nMateriales creados: {created}\nReutilizados (sin duplicar): {reused}", "OK");
    }

    // ---------- Utilidades ----------

    private IEnumerable<string> FindModels()
    {
        if (!AssetDatabase.IsValidFolder(sourceFolder)) yield break;

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { sourceFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is ModelImporter) yield return path;
        }
    }

    // Materiales que todavía están "adentro" del FBX (no remapeados a un .mat externo)
    private static List<Material> EmbeddedMaterials(string modelPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(modelPath)
            .OfType<Material>()
            .Where(m => AssetDatabase.IsSubAsset(m))
            .ToList();
    }

    // Destino = carpeta destino + carpeta relativa del FBX (+ nombre del modelo si corresponde) + material.mat
    private string TargetPath(string modelPath, Material mat)
    {
        string modelDir = Path.GetDirectoryName(modelPath).Replace('\\', '/');
        string relative = modelDir.Length > sourceFolder.Length ? modelDir.Substring(sourceFolder.Length).TrimStart('/') : "";

        string folder = string.IsNullOrEmpty(relative) ? destinationFolder : $"{destinationFolder}/{relative}";

        if (UsesPerModelFolder(relative))
        {
            folder = $"{folder}/{Path.GetFileNameWithoutExtension(modelPath)}";
        }

        return $"{folder}/{SafeName(mat.name)}.mat";
    }

    private bool UsesPerModelFolder(string relativeFolder)
    {
        if (string.IsNullOrEmpty(relativeFolder)) return false;

        string firstLevel = relativeFolder.Split('/')[0];
        return perModelFolders
            .Split(',')
            .Select(s => s.Trim())
            .Any(s => s.Length > 0 && s == firstLevel);
    }

    private static string SafeName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Replace('|', '_').Replace(':', '_');
    }

    private string Relative(string path)
    {
        return path.StartsWith(destinationFolder) ? path.Substring(destinationFolder.Length).TrimStart('/') : path;
    }

    private static string FolderField(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.TextField(label, value);
        if (GUILayout.Button("...", GUILayout.Width(28)))
        {
            string picked = EditorUtility.OpenFolderPanel(label, value, "");
            if (!string.IsNullOrEmpty(picked))
            {
                string dataPath = Application.dataPath.Replace('\\', '/');
                picked = picked.Replace('\\', '/');
                if (picked.StartsWith(dataPath)) value = "Assets" + picked.Substring(dataPath.Length);
                else Debug.LogWarning("Material Extractor: la carpeta tiene que estar dentro de Assets.");
            }
        }
        EditorGUILayout.EndHorizontal();
        return value;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
