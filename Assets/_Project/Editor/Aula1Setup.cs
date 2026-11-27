using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Aula1Setup
{
    private const string MenuPath = "Bip-project/Aula 1/Montar Greybox";
    private const string FixCircularCollidersMenuPath = "Bip-project/Aula 1/Corrigir Colisores Circulares";
    private const string ConfigurePlayerMenuPath = "Bip-project/Aula 1/Configurar Bip";
    private const string CreateAndConfigurePlayerMenuPath = "Bip-project/Aula 1/Criar e Configurar Bip";
    private const string CreatePlayerAnimatorMenuPath = "Bip-project/Aula 1/Criar Animator do Bip";
    private const string MaterialPath = "Assets/_Project/Materials/Aula1_Graybox.mat";
    private const string CharacterAssetsFolder = "Assets/_Project/Art/Characters";
    private const string PlayerAnimatorControllerPath = CharacterAssetsFolder + "/Bip.controller";
    private const string UndoName = "Montar Greybox da Aula 1";
    private const string FixCircularCollidersUndoName = "Corrigir colisores circulares";
    private const string ConfigurePlayerUndoName = "Configurar Bip";
    private const string CreatePlayerAnimatorUndoName = "Criar Animator do Bip";
    private const string TagManagerPath = "ProjectSettings/TagManager.asset";
    private const string PlayerMovementScriptPath = "Assets/_Project/Scripts/Player/PlayerMovement.cs";
    private const string PlayerCameraFollowScriptPath = "Assets/_Project/Scripts/Player/PlayerCameraFollow.cs";
    private const string InputActionsAssetPath = "Assets/InputSystem_Actions.inputactions";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Bip.prefab";
    private const string PlayerModelPath = "Assets/_Project/Art/Characters/Bip.glb";
    private const string PlayerName = "Player";
    private const string PlayerGroupName = "Personagens";
    private const float CircularPlatformThickness = 0.02f;

    [MenuItem(MenuPath)]
    private static void MontarGreybox()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Abra uma cena antes de montar o greybox da Aula 1.");
            return;
        }

        List<GameObject> existingGroups = FindExistingGroups(scene);
        if (existingGroups.Count > 0 &&
            !EditorUtility.DisplayDialog(
                "Substituir greybox",
                "Ja existem grupos da Aula 1 nesta cena. Deseja substitui-los?",
                "Substituir",
                "Cancelar"))
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);

        Material greyMaterial = GetGreyMaterial();
        if (greyMaterial == null)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            return;
        }

        foreach (GameObject group in existingGroups)
        {
            Undo.DestroyObjectImmediate(group);
        }

        CreateZone1(greyMaterial);
        CreateCorridor(greyMaterial);
        GameObject zone2 = CreateZone2(greyMaterial);
        CreateGate(greyMaterial);
        GameObject forge = CreateForge(greyMaterial);
        SetDirectionalLight(scene);
        CreateForgeLight(forge);

        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
    }

    [MenuItem(FixCircularCollidersMenuPath)]
    private static void CorrigirColisoresCirculares()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Abra uma cena antes de corrigir os colisores circulares.");
            return;
        }

        List<GameObject> plazas = FindSceneObjects(scene, "Praca_Circular");
        List<GameObject> arenas = FindSceneObjects(scene, "Arena_Circular");
        if (plazas.Count == 0 && arenas.Count == 0)
        {
            Debug.LogError("Praca_Circular e Arena_Circular nao foram encontradas na cena aberta.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(FixCircularCollidersUndoName);

        bool changed = false;
        foreach (GameObject plaza in plazas)
        {
            changed |= SetCylinderFloorCollider(plaza, FixCircularCollidersUndoName);
        }

        if (plazas.Count == 0)
        {
            Debug.LogWarning("Praca_Circular nao foi encontrada na cena aberta.");
        }

        foreach (GameObject arena in arenas)
        {
            changed |= SetCylinderFloorCollider(arena, FixCircularCollidersUndoName);
        }

        if (arenas.Count == 0)
        {
            Debug.LogWarning("Arena_Circular nao foi encontrada na cena aberta.");
        }

        int playerLayer = LayerMask.NameToLayer(PlayerName);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (CharacterController controller in root.GetComponentsInChildren<CharacterController>(true))
            {
                if (playerLayer < 0 || controller.gameObject.layer != playerLayer)
                {
                    continue;
                }

                Undo.RecordObject(controller, FixCircularCollidersUndoName);
                controller.stepOffset = Mathf.Min(0.3f, controller.height * 0.25f);
                EditorUtility.SetDirty(controller);
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("Colisores circulares e altura de degrau dos personagens atualizados.");
        }
        else
        {
            Undo.RevertAllDownToGroup(undoGroup);
        }
    }

    private static List<GameObject> FindExistingGroups(Scene scene)
    {
        var existingGroups = new List<GameObject>();
        string[] groupNames =
        {
            "Zona1_Vila",
            "Corredor_Zona1_Zona2",
            "Zona2_Patio",
            "Portao_Zona2_Zona3",
            "Zona3_Forja"
        };

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (string groupName in groupNames)
            {
                if (root.name == groupName)
                {
                    existingGroups.Add(root);
                    break;
                }
            }
        }

        return existingGroups;
    }

    private static Material GetGreyMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
            {
                Debug.LogError("A pasta Assets/_Project/Materials nao foi encontrada.");
                return null;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("O shader Universal Render Pipeline/Lit nao foi encontrado.");
                return null;
            }

            material = new Material(shader)
            {
                name = "Aula1_Graybox"
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        Undo.RecordObject(material, UndoName);
        material.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.55f));
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void CreateZone1(Material material)
    {
        GameObject zone = CreateGroup("Zona1_Vila", Vector3.zero);
        CreatePrimitive(zone, PrimitiveType.Cube, "Chao_Vila",
            new Vector3(0f, -0.1f, 0f), new Vector3(40f, 0.2f, 40f), material);
        GameObject plaza = CreatePrimitive(zone, PrimitiveType.Cylinder, "Praca_Circular",
            new Vector3(0f, CircularPlatformThickness * 0.5f, 0f),
            new Vector3(14f, CircularPlatformThickness * 0.5f, 14f), material);
        SetCylinderFloorCollider(plaza, UndoName);
    }

    private static void CreateCorridor(Material material)
    {
        GameObject corridor = CreateGroup("Corredor_Zona1_Zona2", new Vector3(0f, 0f, 30f));
        CreatePrimitive(corridor, PrimitiveType.Cube, "Chao_Corredor",
            new Vector3(0f, -0.1f, 0f), new Vector3(6f, 0.2f, 20f), material);
    }

    private static GameObject CreateZone2(Material material)
    {
        GameObject zone = CreateGroup("Zona2_Patio", new Vector3(0f, 0f, 55f));
        CreatePrimitive(zone, PrimitiveType.Cube, "Chao_Patio",
            new Vector3(0f, -0.1f, 0f), new Vector3(50f, 0.2f, 30f), material);

        CreatePrimitive(zone, PrimitiveType.Cube, "Muro_Oeste",
            new Vector3(-24.8f, 1.5f, 0f), new Vector3(0.4f, 3f, 30f), material);
        CreatePrimitive(zone, PrimitiveType.Cube, "Muro_Leste",
            new Vector3(24.8f, 1.5f, 0f), new Vector3(0.4f, 3f, 30f), material);

        CreatePrimitive(zone, PrimitiveType.Cube, "Muro_Entrada_Esquerda",
            new Vector3(-14f, 1.5f, -14.8f), new Vector3(22f, 3f, 0.4f), material);
        CreatePrimitive(zone, PrimitiveType.Cube, "Muro_Entrada_Direita",
            new Vector3(14f, 1.5f, -14.8f), new Vector3(22f, 3f, 0.4f), material);
        CreatePrimitive(zone, PrimitiveType.Cube, "Muro_Fundo_Esquerda",
            new Vector3(-14.5f, 1.5f, 14.8f), new Vector3(21f, 3f, 0.4f), material);
        CreatePrimitive(zone, PrimitiveType.Cube, "Muro_Fundo_Direita",
            new Vector3(14.5f, 1.5f, 14.8f), new Vector3(21f, 3f, 0.4f), material);

        float[] cratePositions = { -18f, -9f, 0f, 9f, 18f };
        int crateNumber = 1;
        foreach (float z in new[] { -7f, 7f })
        {
            foreach (float x in cratePositions)
            {
                CreatePrimitive(zone, PrimitiveType.Cube,
                    "Caixa_" + crateNumber.ToString("00"),
                    new Vector3(x, 0.75f, z), new Vector3(1.5f, 1.5f, 1.5f), material);
                crateNumber++;
            }
        }

        return zone;
    }

    private static void CreateGate(Material material)
    {
        GameObject gate = CreateGroup("Portao_Zona2_Zona3", new Vector3(0f, 0f, 70f));
        CreatePrimitive(gate, PrimitiveType.Cube, "Pilar_Esquerdo",
            new Vector3(-3.5f, 1.5f, 0f), new Vector3(1f, 3f, 0.6f), material);
        CreatePrimitive(gate, PrimitiveType.Cube, "Pilar_Direito",
            new Vector3(3.5f, 1.5f, 0f), new Vector3(1f, 3f, 0.6f), material);
        CreatePrimitive(gate, PrimitiveType.Cube, "Viga_Superior",
            new Vector3(0f, 3.25f, 0f), new Vector3(8f, 0.5f, 0.6f), material);
    }

    private static GameObject CreateForge(Material material)
    {
        GameObject forge = CreateGroup("Zona3_Forja", new Vector3(0f, 0f, 85f));
        GameObject arena = CreatePrimitive(forge, PrimitiveType.Cylinder, "Arena_Circular",
            new Vector3(0f, CircularPlatformThickness * 0.5f, 0f),
            new Vector3(30f, CircularPlatformThickness * 0.5f, 30f), material);
        SetCylinderFloorCollider(arena, UndoName);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                CreatePrimitive(forge, PrimitiveType.Cube,
                    "Pilar_" + (x < 0 ? "Oeste" : "Leste") + "_" + (z < 0 ? "Sul" : "Norte"),
                    new Vector3(x * 10f, 2.5f, z * 10f), new Vector3(2f, 5f, 2f), material);
            }
        }

        return forge;
    }

    private static void SetDirectionalLight(Scene scene)
    {
        Light directionalLight = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }

            if (directionalLight != null)
            {
                break;
            }
        }

        if (directionalLight == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.color = new Color(0.65f, 0.78f, 1f);
            Undo.RegisterCreatedObjectUndo(lightObject, UndoName);
        }
        else
        {
            Undo.RecordObject(directionalLight, UndoName);
            directionalLight.color = new Color(0.65f, 0.78f, 1f);
        }
    }

    private static void CreateForgeLight(GameObject forge)
    {
        GameObject lightObject = new GameObject("Luz_Vermelha_Forja");
        lightObject.transform.SetParent(forge.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 5f, 0f);

        Light pointLight = lightObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(1f, 0.15f, 0.15f);
        pointLight.intensity = 4f;
        pointLight.range = 25f;

        Undo.RegisterCreatedObjectUndo(lightObject, UndoName);
    }

    private static GameObject CreateGroup(string name, Vector3 position)
    {
        var group = new GameObject(name);
        group.transform.position = position;
        Undo.RegisterCreatedObjectUndo(group, UndoName);
        return group;
    }

    private static GameObject CreatePrimitive(
        GameObject parent,
        PrimitiveType type,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.SetParent(parent.transform, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localScale = localScale;
        primitive.GetComponent<Renderer>().sharedMaterial = material;
        Undo.RegisterCreatedObjectUndo(primitive, UndoName);
        return primitive;
    }

    private static bool SetCylinderFloorCollider(GameObject platform, string undoName)
    {
        MeshFilter meshFilter = platform.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("Nao foi possivel encontrar a malha da plataforma " + platform.name + ".", platform);
            return false;
        }

        MeshCollider meshCollider = platform.GetComponent<MeshCollider>();
        foreach (Collider existingCollider in platform.GetComponents<Collider>())
        {
            if (existingCollider != meshCollider)
            {
                Undo.DestroyObjectImmediate(existingCollider);
            }
        }

        if (meshCollider == null)
        {
            meshCollider = Undo.AddComponent<MeshCollider>(platform);
        }

        Undo.RecordObject(platform.transform, undoName);
        Vector3 localPosition = platform.transform.localPosition;
        localPosition.y = CircularPlatformThickness * 0.5f;
        platform.transform.localPosition = localPosition;
        Vector3 localScale = platform.transform.localScale;
        localScale.y = CircularPlatformThickness * 0.5f;
        platform.transform.localScale = localScale;

        Undo.RecordObject(meshCollider, undoName);
        meshCollider.enabled = false;
        meshCollider.sharedMesh = null;
        meshCollider.convex = false;
        meshCollider.isTrigger = false;
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.enabled = true;
        Physics.SyncTransforms();

        EditorUtility.SetDirty(platform);
        EditorUtility.SetDirty(meshCollider);
        return true;
    }

    [MenuItem(CreatePlayerAnimatorMenuPath)]
    private static void CriarAnimatorDoBip()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || !selected.scene.IsValid() || !selected.scene.isLoaded)
        {
            Debug.LogError("Selecione na Hierarchy o objeto do Bip.");
            return;
        }

        System.Type movementType = GetPlayerMovementType();
        if (movementType == null)
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(CharacterAssetsFolder))
        {
            Debug.LogError("A pasta " + CharacterAssetsFolder + " nao foi encontrada.");
            return;
        }

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerAnimatorControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerAnimatorControllerPath);
        }

        if (controller == null)
        {
            Debug.LogError("Nao foi possivel criar " + PlayerAnimatorControllerPath + ".");
            return;
        }

        List<AnimationClip> clips = FindBipAnimationClips();
        AnimationClip idleClip = FindAnimationClip(clips,
            "idle", "idling", "parado", "parada", "ocioso", "ociosa",
            "descanso", "descansar", "descansando", "repouso");
        AnimationClip walkClip = FindAnimationClip(clips,
            "walk", "walking", "andar", "andando", "caminhar",
            "caminhando", "caminhada");
        AnimationClip runClip = FindAnimationClip(clips,
            "run", "running", "correr", "correndo", "corrida");

        WarnIfAnimationClipMissing("Idle", idleClip, controller);
        WarnIfAnimationClipMissing("Andar", walkClip, controller);
        WarnIfAnimationClipMissing("Correr", runClip, controller);

        Undo.RecordObject(controller, CreatePlayerAnimatorUndoName);
        if (!ConfigurePlayerBlendTree(controller, idleClip, walkClip, runClip))
        {
            return;
        }

        Animator animator = selected.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = Undo.AddComponent<Animator>(selected);
        }

        Undo.RecordObject(animator, CreatePlayerAnimatorUndoName);
        animator.runtimeAnimatorController = controller;
        EditorUtility.SetDirty(animator);

        Component movement = selected.GetComponentInChildren(movementType, true);
        if (movement != null)
        {
            Undo.RecordObject(movement, CreatePlayerAnimatorUndoName);
            var movementProperties = new SerializedObject(movement);
            movementProperties.Update();
            SerializedProperty animatorProperty = movementProperties.FindProperty("animator");
            if (animatorProperty != null)
            {
                animatorProperty.objectReferenceValue = animator;
                movementProperties.ApplyModifiedProperties();
                EditorUtility.SetDirty(movement);
            }
            else
            {
                Debug.LogWarning("O campo animator nao foi encontrado no PlayerMovement.", movement);
            }
        }
        else
        {
            Debug.LogWarning("PlayerMovement nao foi encontrado no objeto selecionado nem nos filhos.", selected);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(selected.scene);
        Debug.Log("Animator do Bip configurado e atribuido a " + selected.name + ".", selected);
    }

    private static List<AnimationClip> FindBipAnimationClips()
    {
        var clips = new List<AnimationClip>();
        string[] assetGuids = AssetDatabase.FindAssets(string.Empty, new[] { CharacterAssetsFolder });
        string[] extensions = { ".fbx", ".glb" };

        foreach (string extension in extensions)
        {
            foreach (string assetGuid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (!assetPath.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int fileNameStart = assetPath.LastIndexOf('/') + 1;
                int extensionStart = assetPath.LastIndexOf('.');
                string fileName = assetPath.Substring(fileNameStart, extensionStart - fileNameStart);
                if (!fileName.StartsWith("Bip", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    AnimationClip clip = asset as AnimationClip;
                    if (clip != null && !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
                    {
                        clips.Add(clip);
                    }
                }
            }
        }

        return clips;
    }

    private static AnimationClip FindAnimationClip(List<AnimationClip> clips, params string[] nameVariants)
    {
        foreach (AnimationClip clip in clips)
        {
            string normalizedClipName = NormalizeAnimationName(clip.name);
            foreach (string nameVariant in nameVariants)
            {
                if (normalizedClipName == NormalizeAnimationName(nameVariant))
                {
                    return clip;
                }
            }
        }

        foreach (AnimationClip clip in clips)
        {
            string normalizedClipName = NormalizeAnimationName(clip.name);
            foreach (string nameVariant in nameVariants)
            {
                if (normalizedClipName.Contains(NormalizeAnimationName(nameVariant)))
                {
                    return clip;
                }
            }
        }

        return null;
    }

    private static string NormalizeAnimationName(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(".", string.Empty);
    }

    private static void WarnIfAnimationClipMissing(
        string animationName,
        AnimationClip clip,
        AnimatorController controller)
    {
        if (clip == null)
        {
            Debug.LogWarning(
                "Clipe " + animationName + " nao encontrado no FBX/GLB do Bip; a entrada correspondente da Blend Tree ficara vazia.",
                controller);
        }
    }

    private static bool ConfigurePlayerBlendTree(
        AnimatorController controller,
        AnimationClip idleClip,
        AnimationClip walkClip,
        AnimationClip runClip)
    {
        AnimatorControllerLayer[] layers = controller.layers;
        if (layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
            layers = controller.layers;
        }

        AnimatorStateMachine stateMachine = layers[0].stateMachine;
        if (stateMachine == null)
        {
            Debug.LogError("O Animator Controller do Bip nao possui uma maquina de estados.");
            return false;
        }

        EnsureSpeedParameter(controller);

        AnimatorState state = null;
        ChildAnimatorState[] states = stateMachine.states;
        foreach (ChildAnimatorState childState in states)
        {
            if (childState.state.name == "Locomocao")
            {
                state = childState.state;
                break;
            }
        }

        if (state == null && states.Length == 1 && states[0].state.motion == null)
        {
            state = states[0].state;
            Undo.RecordObject(state, CreatePlayerAnimatorUndoName);
            state.name = "Locomocao";
        }

        if (state == null)
        {
            state = stateMachine.AddState("Locomocao");
        }

        BlendTree blendTree = state.motion as BlendTree;
        if (blendTree == null)
        {
            blendTree = new BlendTree
            {
                name = "Bip Locomocao",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);
        }

        Undo.RecordObject(stateMachine, CreatePlayerAnimatorUndoName);
        Undo.RecordObject(state, CreatePlayerAnimatorUndoName);
        Undo.RecordObject(blendTree, CreatePlayerAnimatorUndoName);
        blendTree.name = "Bip Locomocao";
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = "Speed";
        blendTree.useAutomaticThresholds = false;
        blendTree.children = new[]
        {
            new ChildMotion { motion = idleClip, threshold = 0f },
            new ChildMotion { motion = walkClip, threshold = 0.5f },
            new ChildMotion { motion = runClip, threshold = 1f }
        };
        state.motion = blendTree;
        stateMachine.defaultState = state;

        EditorUtility.SetDirty(blendTree);
        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(controller);
        return true;
    }

    private static void EnsureSpeedParameter(AnimatorController controller)
    {
        var parameters = new List<AnimatorControllerParameter>(controller.parameters);
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].name == "Speed")
            {
                if (parameters[i].type != AnimatorControllerParameterType.Float)
                {
                    parameters[i] = new AnimatorControllerParameter
                    {
                        name = "Speed",
                        type = AnimatorControllerParameterType.Float,
                        defaultFloat = 0f
                    };
                    controller.parameters = parameters.ToArray();
                }

                return;
            }
        }

        parameters.Add(new AnimatorControllerParameter
        {
            name = "Speed",
            type = AnimatorControllerParameterType.Float,
            defaultFloat = 0f
        });
        controller.parameters = parameters.ToArray();
    }

    [MenuItem(ConfigurePlayerMenuPath)]
    private static void ConfigurarBip()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || !selected.scene.IsValid() || !selected.scene.isLoaded)
        {
            Debug.LogError("Selecione na Hierarchy o objeto raiz do Bip.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(ConfigurePlayerUndoName);

        if (!ConfigurePlayerObject(selected))
        {
            Undo.RevertAllDownToGroup(undoGroup);
            return;
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    [MenuItem(CreateAndConfigurePlayerMenuPath)]
    private static void CriarEConfigurarBip()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Abra uma cena antes de criar o Bip.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(CreateAndConfigurePlayerMenuPath);

        GameObject player = FindPlayerInScene(scene);
        if (player == null)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (model == null)
            {
                AssetDatabase.ImportAsset(PlayerModelPath, ImportAssetOptions.ForceUpdate);
                model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
            }

            if (model == null)
            {
                Debug.LogError("O modelo Bip nao foi importado. Aguarde o glTFast terminar a importacao.");
                Undo.RevertAllDownToGroup(undoGroup);
                return;
            }

            GameObject group = FindRootInScene(scene, PlayerGroupName);
            if (group == null)
            {
                group = new GameObject(PlayerGroupName);
                Undo.RegisterCreatedObjectUndo(group, ConfigurePlayerUndoName);
            }

            player = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
            if (player == null)
            {
                Debug.LogError("Nao foi possivel instanciar o modelo Bip.");
                Undo.RevertAllDownToGroup(undoGroup);
                return;
            }

            player.name = "Bip";
            player.transform.SetParent(group.transform, false);
            player.transform.localPosition = new Vector3(0f, 0f, -8f);
            Undo.RegisterCreatedObjectUndo(player, ConfigurePlayerUndoName);
        }

        if (!ConfigurePlayerObject(player))
        {
            Undo.RevertAllDownToGroup(undoGroup);
            return;
        }

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static bool ConfigurePlayerObject(GameObject selected)
    {
        if (selected == null || !selected.scene.IsValid() || !selected.scene.isLoaded)
        {
            Debug.LogError("Selecione na Hierarchy o objeto raiz do Bip.");
            return false;
        }

        System.Type movementType = GetPlayerMovementType();
        System.Type cameraFollowType = GetPlayerCameraFollowType();
        UnityEngine.Object inputActions = AssetDatabase.LoadMainAssetAtPath(InputActionsAssetPath);
        if (movementType == null || cameraFollowType == null)
        {
            return false;
        }

        if (inputActions == null)
        {
            Debug.LogError("InputSystem_Actions.inputactions nao foi encontrado.");
            return false;
        }

        int playerLayer = EnsurePlayerTagAndLayer();
        if (playerLayer < 0)
        {
            return false;
        }

        Undo.RecordObject(selected, ConfigurePlayerUndoName);
        selected.tag = PlayerName;
        SetLayerRecursively(selected, playerLayer);

        CharacterController controller = selected.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<CharacterController>(selected);
        }

        Undo.RecordObject(controller, ConfigurePlayerUndoName);
        ConfigureCharacterController(controller, selected.transform);
        Component movement = selected.GetComponent(movementType);
        if (movement == null)
        {
            movement = Undo.AddComponent(selected, movementType);
        }

        Camera camera = GetOrCreateMainCamera(selected.scene);
        if (camera == null)
        {
            return false;
        }

        Component cameraFollow = camera.GetComponent(cameraFollowType);
        if (cameraFollow == null)
        {
            cameraFollow = Undo.AddComponent(camera.gameObject, cameraFollowType);
        }

        ConfigureMovementReferences(movement, camera, inputActions, selected);
        ConfigureCameraFollow(cameraFollow, camera, selected.transform);

        EditorUtility.SetDirty(selected);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(movement);
        EditorUtility.SetDirty(cameraFollow);
        EditorSceneManager.MarkSceneDirty(selected.scene);
        return true;
    }

    private static GameObject FindPlayerInScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Bip" || child.name == PlayerName)
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject FindRootInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    private static List<GameObject> FindSceneObjects(Scene scene, string name)
    {
        var matches = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    matches.Add(child.gameObject);
                }
            }
        }

        return matches;
    }

    private static Camera GetOrCreateMainCamera(Scene scene)
    {
        Camera fallback = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true))
            {
                if (candidate.CompareTag("MainCamera"))
                {
                    if (!candidate.gameObject.activeSelf)
                    {
                        Undo.RecordObject(candidate.gameObject, ConfigurePlayerUndoName);
                        candidate.gameObject.SetActive(true);
                    }

                    if (!candidate.enabled)
                    {
                        Undo.RecordObject(candidate, ConfigurePlayerUndoName);
                        candidate.enabled = true;
                    }

                    return candidate;
                }

                if (fallback == null)
                {
                    fallback = candidate;
                }
            }
        }

        if (fallback != null)
        {
            Undo.RecordObject(fallback.gameObject, ConfigurePlayerUndoName);
            fallback.gameObject.tag = "MainCamera";
            fallback.gameObject.SetActive(true);
            Undo.RecordObject(fallback, ConfigurePlayerUndoName);
            fallback.enabled = true;
            return fallback;
        }

        var cameraObject = new GameObject("Main Camera");
        Undo.RegisterCreatedObjectUndo(cameraObject, ConfigurePlayerUndoName);
        cameraObject.tag = "MainCamera";
        return Undo.AddComponent<Camera>(cameraObject);
    }

    private static void ConfigureMovementReferences(
        Component movement,
        Camera camera,
        UnityEngine.Object inputActions,
        GameObject player)
    {
        Undo.RecordObject(movement, ConfigurePlayerUndoName);
        var movementProperties = new SerializedObject(movement);
        movementProperties.Update();
        SetObjectReference(movementProperties, "cam", camera);
        SetObjectReference(movementProperties, "inputActions", inputActions);

        Animator animator = player.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            SetObjectReference(movementProperties, "animator", animator);
        }

        movementProperties.ApplyModifiedProperties();
    }

    private static void ConfigureCameraFollow(Component cameraFollow, Camera camera, Transform target)
    {
        Undo.RecordObject(cameraFollow, ConfigurePlayerUndoName);
        var followProperties = new SerializedObject(cameraFollow);
        followProperties.Update();
        SetObjectReference(followProperties, "target", target);

        Vector3 offset = new Vector3(0f, 3f, -6f);
        Vector3 lookAtOffset = new Vector3(0f, 1.2f, 0f);
        SerializedProperty offsetProperty = followProperties.FindProperty("offset");
        SerializedProperty lookAtProperty = followProperties.FindProperty("lookAtOffset");
        if (offsetProperty != null)
        {
            offsetProperty.vector3Value = offset;
        }

        if (lookAtProperty != null)
        {
            lookAtProperty.vector3Value = lookAtOffset;
        }

        followProperties.ApplyModifiedProperties();

        Undo.RecordObject(camera.transform, ConfigurePlayerUndoName);
        camera.transform.position = target.position + target.rotation * offset;
        Vector3 lookAtPosition = target.position + target.rotation * lookAtOffset;
        camera.transform.rotation = Quaternion.LookRotation(lookAtPosition - camera.transform.position);
    }

    private static void SetObjectReference(SerializedObject target, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static System.Type GetPlayerMovementType()
    {
        MonoScript movementScript = AssetDatabase.LoadAssetAtPath<MonoScript>(PlayerMovementScriptPath);
        System.Type movementType = movementScript != null ? movementScript.GetClass() : null;
        if (movementType == null || !typeof(MonoBehaviour).IsAssignableFrom(movementType))
        {
            Debug.LogError("PlayerMovement.cs nao esta compilado como um MonoBehaviour.");
            return null;
        }

        return movementType;
    }

    private static System.Type GetPlayerCameraFollowType()
    {
        MonoScript cameraScript = AssetDatabase.LoadAssetAtPath<MonoScript>(PlayerCameraFollowScriptPath);
        System.Type cameraType = cameraScript != null ? cameraScript.GetClass() : null;
        if (cameraType == null || !typeof(MonoBehaviour).IsAssignableFrom(cameraType))
        {
            Debug.LogError("PlayerCameraFollow.cs nao esta compilado como um MonoBehaviour.");
            return null;
        }

        return cameraType;
    }

    private static int EnsurePlayerTagAndLayer()
    {
        Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
        if (tagManagerAssets.Length == 0)
        {
            Debug.LogError("Nao foi possivel carregar ProjectSettings/TagManager.asset.");
            return -1;
        }

        var tagManager = new SerializedObject(tagManagerAssets[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (tags == null || layers == null)
        {
            Debug.LogError("As propriedades tags/layers nao foram encontradas no TagManager.");
            return -1;
        }

        bool hasPlayerTag = false;
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == PlayerName)
            {
                hasPlayerTag = true;
                break;
            }
        }

        int playerLayer = -1;
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == PlayerName)
            {
                playerLayer = i;
                break;
            }
        }

        if (playerLayer < 0)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    playerLayer = i;
                    break;
                }
            }

            if (playerLayer < 0)
            {
                Debug.LogError("Nao ha uma layer de usuario livre para criar Player.");
                return -1;
            }
        }

        if (!hasPlayerTag || layers.GetArrayElementAtIndex(playerLayer).stringValue != PlayerName)
        {
            Undo.RecordObject(tagManager.targetObject, ConfigurePlayerUndoName);
            if (!hasPlayerTag)
            {
                int newTagIndex = tags.arraySize;
                tags.InsertArrayElementAtIndex(newTagIndex);
                tags.GetArrayElementAtIndex(newTagIndex).stringValue = PlayerName;
            }

            if (layers.GetArrayElementAtIndex(playerLayer).stringValue != PlayerName)
            {
                layers.GetArrayElementAtIndex(playerLayer).stringValue = PlayerName;
            }

            tagManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(tagManager.targetObject);
            AssetDatabase.SaveAssets();
        }

        return playerLayer;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        Undo.RecordObject(target, ConfigurePlayerUndoName);
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void ConfigureCharacterController(CharacterController controller, Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("O modelo nao tem Renderer; usando o tamanho humanoide padrao.", root);
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0f, 1f, 0f);
            controller.stepOffset = 0.3f;
            return;
        }

        bool hasBounds = false;
        Bounds localBounds = new Bounds();
        foreach (Renderer renderer in renderers)
        {
            Bounds worldBounds = renderer.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 worldPoint = worldBounds.center + new Vector3(
                    (corner & 1) == 0 ? -worldBounds.extents.x : worldBounds.extents.x,
                    (corner & 2) == 0 ? -worldBounds.extents.y : worldBounds.extents.y,
                    (corner & 4) == 0 ? -worldBounds.extents.z : worldBounds.extents.z);
                Vector3 localPoint = root.InverseTransformPoint(worldPoint);

                if (hasBounds)
                {
                    localBounds.Encapsulate(localPoint);
                }
                else
                {
                    localBounds = new Bounds(localPoint, Vector3.zero);
                    hasBounds = true;
                }
            }
        }

        float radius = Mathf.Max(0.01f, Mathf.Min(localBounds.extents.x, localBounds.extents.z));
        controller.radius = radius;
        controller.height = Mathf.Max(localBounds.size.y, radius * 2f);
        controller.center = localBounds.center;
        controller.stepOffset = Mathf.Min(0.3f, controller.height * 0.25f);
    }
}
