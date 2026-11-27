using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ModelPrefabSetup
{
    private const string MenuPath = "Bip-project/Personagens/Gerar Prefabs dos Modelos";
    private const string SourceFolder = "Assets/_Project/Art/Characters";
    private const string PrefabFolder = "Assets/_Project/Prefabs/Characters";
    private const string SceneGroupName = "Personagens";
    private const string UndoName = "Criar prefabs dos personagens";

    private static readonly CharacterModel[] Characters =
    {
        new CharacterModel("Bip.glb", "Bip", new Vector3(0f, 0f, -8f)),
        new CharacterModel("Arcano_Lumen.glb", "ArcanoLumen", new Vector3(-6f, 0f, 0f)),
        new CharacterModel("Sentinela_Rubra.glb", "SentinelaRubra", new Vector3(-12f, 0f, 55f)),
        new CharacterModel("Imperador_Rubro.glb", "ImperadorRubro", new Vector3(0f, 0f, 85f))
    };

    [MenuItem(MenuPath)]
    private static void GerarPrefabs()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Abra uma cena antes de gerar os prefabs dos personagens.");
            return;
        }

        var sourceModels = new List<GameObject>(Characters.Length);
        foreach (CharacterModel character in Characters)
        {
            string sourcePath = SourceFolder + "/" + character.FileName;
            AssetDatabase.ImportAsset(sourcePath);
            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourceModel == null)
            {
                Debug.LogError(
                    "O modelo " + sourcePath + " ainda nao foi importado. Confirme a instalacao do glTFast e aguarde a importacao no Unity.");
                return;
            }

            sourceModels.Add(sourceModel);
        }

        GameObject existingGroup = FindSceneGroup(scene);
        bool existingPrefabs = HasExistingPrefabs();
        if ((existingGroup != null || existingPrefabs) &&
            !EditorUtility.DisplayDialog(
                "Atualizar prefabs dos personagens",
                "Os prefabs ou as instancias dos personagens ja existem. Deseja substitui-los?",
                "Substituir",
                "Cancelar"))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Characters");
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);

        var prefabs = new List<GameObject>(Characters.Length);
        for (int i = 0; i < Characters.Length; i++)
        {
            CharacterModel character = Characters[i];
            GameObject sourceInstance = PrefabUtility.InstantiatePrefab(sourceModels[i], scene) as GameObject;
            if (sourceInstance == null)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("Nao foi possivel instanciar o modelo " + character.FileName + ".");
                return;
            }

            sourceInstance.name = character.PrefabName;
            string prefabPath = PrefabFolder + "/" + character.PrefabName + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sourceInstance, prefabPath, out bool success);
            Object.DestroyImmediate(sourceInstance);
            if (!success || prefab == null)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("Nao foi possivel criar o prefab " + prefabPath + ".");
                return;
            }

            prefabs.Add(prefab);
        }

        if (existingGroup != null)
        {
            Undo.DestroyObjectImmediate(existingGroup);
        }

        var group = new GameObject(SceneGroupName);
        Undo.RegisterCreatedObjectUndo(group, UndoName);

        for (int i = 0; i < Characters.Length; i++)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefabs[i], scene) as GameObject;
            if (instance == null)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogError("Nao foi possivel adicionar " + Characters[i].PrefabName + " a cena.");
                return;
            }

            instance.name = Characters[i].PrefabName;
            instance.transform.SetParent(group.transform, false);
            instance.transform.localPosition = Characters[i].Position;
            Undo.RegisterCreatedObjectUndo(instance, UndoName);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("Prefabs dos quatro personagens criados em " + PrefabFolder + ".");
    }

    private static bool HasExistingPrefabs()
    {
        foreach (CharacterModel character in Characters)
        {
            string path = PrefabFolder + "/" + character.PrefabName + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject FindSceneGroup(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == SceneGroupName)
            {
                return root;
            }
        }

        return null;
    }

    private sealed class CharacterModel
    {
        public readonly string FileName;
        public readonly string PrefabName;
        public readonly Vector3 Position;

        public CharacterModel(string fileName, string prefabName, Vector3 position)
        {
            FileName = fileName;
            PrefabName = prefabName;
            Position = position;
        }
    }
}
