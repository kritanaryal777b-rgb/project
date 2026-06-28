using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class RoomGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Room Scene")]
    public static void GenerateRoom()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        GameObject courtRoot = new GameObject("BasketballCourt");
        GameManager courtScorer = courtRoot.AddComponent<GameManager>();

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        AssetDatabase.Refresh();

        Texture2D floorTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/floor.jpg");
        Texture2D brickTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/brick.jpg");
        Texture2D woodTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/wood.jpg");

        Texture2D ballTex = CreateProceduralBasketballTexture();

        Material floorMat = GetOrCreateMaterial("CourtFloorMat", woodTex, new Vector2(5f, 10f));
        Material wallMat = GetOrCreateMaterial("StadiumWallMat", brickTex, new Vector2(10f, 4f));
        Material ceilingMat = GetOrCreateMaterial("CeilingMat", null, Vector2.one);
        ceilingMat.color = new Color(0.15f, 0.15f, 0.18f);

        Material steelMat = GetOrCreateMaterial("SteelMat", null, Vector2.one);
        steelMat.color = new Color(0.3f, 0.3f, 0.35f);

        Material backboardMat = GetOrCreateMaterial("BackboardMat", null, Vector2.one);
        backboardMat.color = Color.white;

        Material redTargetMat = GetOrCreateMaterial("RedTargetMat", null, Vector2.one);
        redTargetMat.color = Color.red;

        Material rimMat = GetOrCreateMaterial("RimMat", null, Vector2.one);
        rimMat.color = new Color(1.0f, 0.35f, 0.0f);

        Material ballMat = GetOrCreateMaterial("BasketballMat", ballTex, Vector2.one);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.parent = courtRoot.transform;
        floor.transform.position = new Vector3(0, -0.5f, 0);
        floor.transform.localScale = new Vector3(30, 1, 60);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;
        GroundMissTrigger groundTrigger = floor.AddComponent<GroundMissTrigger>();
        groundTrigger.scorer = courtScorer;

        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.parent = courtRoot.transform;
        ceiling.transform.position = new Vector3(0, 15.5f, 0);
        ceiling.transform.localScale = new Vector3(30, 1, 60);
        ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMat;

        CreateWall(courtRoot.transform, new Vector3(0, 7.5f, 30), new Vector3(30, 15, 1), Quaternion.identity, "NorthWall", wallMat);
        CreateWall(courtRoot.transform, new Vector3(0, 7.5f, -30), new Vector3(30, 15, 1), Quaternion.identity, "SouthWall", wallMat);

        Material wallMatRotated = GetOrCreateMaterial("StadiumWallMatRotated", brickTex, new Vector2(20f, 4f));
        CreateWall(courtRoot.transform, new Vector3(15, 7.5f, 0), new Vector3(1, 15, 60), Quaternion.identity, "EastWall", wallMatRotated);
        CreateWall(courtRoot.transform, new Vector3(-15, 7.5f, 0), new Vector3(1, 15, 60), Quaternion.identity, "WestWall", wallMatRotated);

        GameObject linesRoot = new GameObject("CourtLines");
        linesRoot.transform.parent = courtRoot.transform;
        Material lineMat = GetOrCreateMaterial("CourtLineMat", null, Vector2.one);
        lineMat.color = Color.white;

        CreateLine(linesRoot.transform, new Vector3(0f, 0.005f, 0f), new Vector3(30f, 0.01f, 0.1f), Quaternion.identity, "CenterLine", lineMat);
        CreateLine(linesRoot.transform, new Vector3(0f, 0.005f, 15f), new Vector3(4f, 0.01f, 0.1f), Quaternion.identity, "NorthFreeThrowLine", lineMat);
        CreateLine(linesRoot.transform, new Vector3(0f, 0.005f, -15f), new Vector3(4f, 0.01f, 0.1f), Quaternion.identity, "SouthFreeThrowLine", lineMat);

        CreateLine(linesRoot.transform, new Vector3(0f, 0.005f, 30f), new Vector3(30f, 0.01f, 0.1f), Quaternion.identity, "NorthBorder", lineMat);
        CreateLine(linesRoot.transform, new Vector3(0f, 0.005f, -30f), new Vector3(30f, 0.01f, 0.1f), Quaternion.identity, "SouthBorder", lineMat);
        CreateLine(linesRoot.transform, new Vector3(15f, 0.005f, 0f), new Vector3(0.1f, 0.01f, 60f), Quaternion.identity, "EastBorder", lineMat);
        CreateLine(linesRoot.transform, new Vector3(-15f, 0.005f, 0f), new Vector3(0.1f, 0.01f, 60f), Quaternion.identity, "WestBorder", lineMat);

        for (int i = 0; i < 24; i++)
        {
            float angle = i * 15f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 3f, 0.005f, Mathf.Sin(angle) * 3f);
            CreateLine(linesRoot.transform, pos, new Vector3(0.8f, 0.01f, 0.1f), Quaternion.Euler(0f, -i * 15f + 90f, 0f), $"CenterCircleSegment_{i}", lineMat);
        }

        GameObject lightRoot = new GameObject("GymLights");
        lightRoot.transform.parent = courtRoot.transform;

        Vector3[] lightPositions = {
            new Vector3(0f, 13f, 15f),
            new Vector3(0f, 13f, -15f),
            new Vector3(-8f, 13f, 0f),
            new Vector3(8f, 13f, 0f)
        };

        for (int i = 0; i < lightPositions.Length; i++)
        {
            GameObject lObj = new GameObject($"Spotlight_{i}");
            lObj.transform.parent = lightRoot.transform;
            lObj.transform.position = lightPositions[i];
            Light lightComponent = lObj.AddComponent<Light>();
            lightComponent.type = LightType.Point;
            lightComponent.range = 35f;
            lightComponent.intensity = 2.5f;
            lightComponent.color = new Color(1f, 0.98f, 0.9f);
        }

        CreateHoop(courtRoot.transform, new Vector3(0, 0, 25f), Quaternion.Euler(0, 180, 0), steelMat, backboardMat, redTargetMat, rimMat, courtScorer);
        CreateHoop(courtRoot.transform, new Vector3(0, 0, -25f), Quaternion.identity, steelMat, backboardMat, redTargetMat, rimMat, courtScorer);

        GameObject basketballsRoot = new GameObject("Basketballs");
        basketballsRoot.transform.parent = courtRoot.transform;

        PhysicsMaterial bouncyMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/Bouncy.physicMaterial");
        if (bouncyMat == null)
        {
            bouncyMat = new PhysicsMaterial("Bouncy");
            bouncyMat.bounciness = 0.82f;
            bouncyMat.bounceCombine = PhysicsMaterialCombine.Maximum;
            AssetDatabase.CreateAsset(bouncyMat, "Assets/Materials/Bouncy.physicMaterial");
        }

        Vector3[] spawnPositions = {
            new Vector3(0, 0.4f, 0),
            new Vector3(-4, 0.4f, -5),
            new Vector3(4, 0.4f, 5)
        };

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = $"Basketball_{i}";
            ball.transform.parent = basketballsRoot.transform;
            ball.transform.position = spawnPositions[i];
            ball.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
            ball.GetComponent<Renderer>().sharedMaterial = ballMat;

            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.62f;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            SphereCollider sc = ball.GetComponent<SphereCollider>();
            if (sc != null)
            {
                sc.sharedMaterial = bouncyMat;
                sc.isTrigger = false;
            }

            ball.AddComponent<Pickupable>();
        }

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1f, -15f);
        player.layer = 2;

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1f, 0);
        cc.height = 2f;
        cc.radius = 0.5f;

        GameObject pivot = new GameObject("CameraPivot");
        pivot.transform.parent = player.transform;
        pivot.transform.localPosition = new Vector3(0, 1.8f, 0);
        pivot.transform.localRotation = Quaternion.identity;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.parent = pivot.transform;
            mainCam.transform.localPosition = Vector3.zero;
            mainCam.transform.localRotation = Quaternion.identity;
        }

        PlayerMovement pm = player.AddComponent<PlayerMovement>();
        PlayerPickup pp = player.AddComponent<PlayerPickup>();
        ThirdPersonCamera tpc = pivot.AddComponent<ThirdPersonCamera>();

        if (mainCam != null)
        {
            SerializedObject soTpc = new SerializedObject(tpc);
            SerializedProperty spCam = soTpc.FindProperty("cameraTransform");
            if (spCam != null)
            {
                spCam.objectReferenceValue = mainCam.transform;
                soTpc.ApplyModifiedProperties();
            }
        }

        string path = "Assets/Scenes/RoomScene.unity";
        bool saveSuccess = EditorSceneManager.SaveScene(scene, path);
        if (saveSuccess)
        {
            Debug.Log($"Successfully generated and saved Basketball Court Scene at {path}");
            EditorSceneManager.MarkSceneDirty(scene);
        }
        else
        {
            Debug.LogError("Failed to save the generated Basketball Court Scene.");
        }
    }

    [MenuItem("Tools/Generate Half Court Scene")]
    public static void GenerateHalfCourtRoom()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        GameObject courtRoot = new GameObject("BasketballCourt");
        GameManager courtScorer = courtRoot.AddComponent<GameManager>();

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        AssetDatabase.Refresh();

        Texture2D floorTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/floor.jpg");
        Texture2D brickTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/brick.jpg");
        Texture2D woodTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/wood.jpg");
        Texture2D ballTex = CreateProceduralBasketballTexture();

        Material floorMat = GetOrCreateMaterial("CourtFloorMat", woodTex, new Vector2(5f, 5f));
        Material wallMat = GetOrCreateMaterial("StadiumWallMat", brickTex, new Vector2(10f, 4f));
        Material ceilingMat = GetOrCreateMaterial("CeilingMat", null, Vector2.one);
        ceilingMat.color = new Color(0.15f, 0.15f, 0.18f);

        Material steelMat = GetOrCreateMaterial("SteelMat", null, Vector2.one);
        steelMat.color = new Color(0.3f, 0.3f, 0.35f);

        Material backboardMat = GetOrCreateMaterial("BackboardMat", null, Vector2.one);
        backboardMat.color = Color.white;

        Material redTargetMat = GetOrCreateMaterial("RedTargetMat", null, Vector2.one);
        redTargetMat.color = Color.red;

        Material rimMat = GetOrCreateMaterial("RimMat", null, Vector2.one);
        rimMat.color = new Color(1.0f, 0.35f, 0.0f);

        Material ballMat = GetOrCreateMaterial("BasketballMat", ballTex, Vector2.one);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.parent = courtRoot.transform;
        floor.transform.position = new Vector3(0, -0.5f, 15f);
        floor.transform.localScale = new Vector3(30, 1, 30);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;
        GroundMissTrigger groundTrigger = floor.AddComponent<GroundMissTrigger>();
        groundTrigger.scorer = courtScorer;

        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.parent = courtRoot.transform;
        ceiling.transform.position = new Vector3(0, 15.5f, 15f);
        ceiling.transform.localScale = new Vector3(30, 1, 30);
        ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMat;

        CreateWall(courtRoot.transform, new Vector3(0, 7.5f, 30), new Vector3(30, 15, 1), Quaternion.identity, "NorthWall", wallMat);
        CreateWall(courtRoot.transform, new Vector3(0, 7.5f, 0), new Vector3(30, 15, 1), Quaternion.identity, "SouthWall", wallMat);

        Material wallMatRotated = GetOrCreateMaterial("StadiumWallMatRotated", brickTex, new Vector2(10f, 4f));
        CreateWall(courtRoot.transform, new Vector3(15, 7.5f, 15), new Vector3(1, 15, 30), Quaternion.identity, "EastWall", wallMatRotated);
        CreateWall(courtRoot.transform, new Vector3(-15, 7.5f, 15), new Vector3(1, 15, 30), Quaternion.identity, "WestWall", wallMatRotated);

        GameObject linesRoot = new GameObject("CourtLines");
        linesRoot.transform.parent = courtRoot.transform;
        Material lineMat = GetOrCreateMaterial("CourtLineMat", null, Vector2.one);
        lineMat.color = Color.white;

        CreateLine(linesRoot.transform, new Vector3(0f, 0.005f, 0f), new Vector3(30f, 0.01f, 0.1f), Quaternion.identity, "CenterLine", lineMat);
        CreateLine(linesRoot.transform, new Vector3(0f, 0.005f, 15f), new Vector3(4f, 0.01f, 0.1f), Quaternion.identity, "NorthFreeThrowLine", lineMat);
        
        CreateLine(linesRoot.transform, new Vector3(0f, 0.005f, 30f), new Vector3(30f, 0.01f, 0.1f), Quaternion.identity, "NorthBorder", lineMat);
        CreateLine(linesRoot.transform, new Vector3(15f, 0.005f, 15f), new Vector3(0.1f, 0.01f, 30f), Quaternion.identity, "EastBorder", lineMat);
        CreateLine(linesRoot.transform, new Vector3(-15f, 0.005f, 15f), new Vector3(0.1f, 0.01f, 30f), Quaternion.identity, "WestBorder", lineMat);

        for (int i = 0; i < 24; i++)
        {
            float angle = i * 15f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 3f, 0.005f, Mathf.Sin(angle) * 3f);
            CreateLine(linesRoot.transform, pos, new Vector3(0.8f, 0.01f, 0.1f), Quaternion.Euler(0f, -i * 15f + 90f, 0f), $"CenterCircleSegment_{i}", lineMat);
        }

        GameObject lightRoot = new GameObject("GymLights");
        lightRoot.transform.parent = courtRoot.transform;

        Vector3[] lightPositions = {
            new Vector3(0f, 13f, 15f),
            new Vector3(-8f, 13f, 7.5f),
            new Vector3(8f, 13f, 7.5f)
        };

        for (int i = 0; i < lightPositions.Length; i++)
        {
            GameObject lObj = new GameObject($"Spotlight_{i}");
            lObj.transform.parent = lightRoot.transform;
            lObj.transform.position = lightPositions[i];
            Light lightComponent = lObj.AddComponent<Light>();
            lightComponent.type = LightType.Point;
            lightComponent.range = 35f;
            lightComponent.intensity = 2.5f;
            lightComponent.color = new Color(1f, 0.98f, 0.9f);
        }

        CreateHoop(courtRoot.transform, new Vector3(0, 0, 25f), Quaternion.Euler(0, 180, 0), steelMat, backboardMat, redTargetMat, rimMat, courtScorer);

        GameObject basketballsRoot = new GameObject("Basketballs");
        basketballsRoot.transform.parent = courtRoot.transform;

        PhysicsMaterial bouncyMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/Bouncy.physicMaterial");
        if (bouncyMat == null)
        {
            bouncyMat = new PhysicsMaterial("Bouncy");
            bouncyMat.bounciness = 0.82f;
            bouncyMat.bounceCombine = PhysicsMaterialCombine.Maximum;
            AssetDatabase.CreateAsset(bouncyMat, "Assets/Materials/Bouncy.physicMaterial");
        }

        Vector3[] spawnPositions = {
            new Vector3(0, 0.4f, 5),
            new Vector3(-2, 0.4f, 5),
            new Vector3(2, 0.4f, 5),
            new Vector3(-4, 0.4f, 5),
            new Vector3(4, 0.4f, 5)
        };

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = $"Basketball_{i}";
            ball.transform.parent = basketballsRoot.transform;
            ball.transform.position = spawnPositions[i];
            ball.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
            ball.GetComponent<Renderer>().sharedMaterial = ballMat;

            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.62f;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            SphereCollider sc = ball.GetComponent<SphereCollider>();
            if (sc != null)
            {
                sc.sharedMaterial = bouncyMat;
                sc.isTrigger = false;
            }

            ball.AddComponent<Pickupable>();
        }

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1f, 2f);
        player.layer = 2;

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1f, 0);
        cc.height = 2f;
        cc.radius = 0.5f;

        GameObject pivot = new GameObject("CameraPivot");
        pivot.transform.parent = player.transform;
        pivot.transform.localPosition = new Vector3(0, 1.8f, 0);
        pivot.transform.localRotation = Quaternion.identity;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.parent = pivot.transform;
            mainCam.transform.localPosition = Vector3.zero;
            mainCam.transform.localRotation = Quaternion.identity;
        }

        PlayerMovement pm = player.AddComponent<PlayerMovement>();
        PlayerPickup pp = player.AddComponent<PlayerPickup>();
        ThirdPersonCamera tpc = pivot.AddComponent<ThirdPersonCamera>();

        if (mainCam != null)
        {
            SerializedObject soTpc = new SerializedObject(tpc);
            SerializedProperty spCam = soTpc.FindProperty("cameraTransform");
            if (spCam != null)
            {
                spCam.objectReferenceValue = mainCam.transform;
                soTpc.ApplyModifiedProperties();
            }
        }

        string path = "Assets/Scenes/HalfCourtScene.unity";
        bool saveSuccess = EditorSceneManager.SaveScene(scene, path);
        if (saveSuccess)
        {
            Debug.Log($"Successfully generated and saved Half Court Scene at {path}");
            EditorSceneManager.MarkSceneDirty(scene);
        }
        else
        {
            Debug.LogError("Failed to save the generated Half Court Scene.");
        }
    }

    private static void CreateWall(Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, string name, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.parent = parent;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.transform.rotation = rotation;
        if (mat != null)
        {
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    private static void CreateLine(Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, string name, Material mat)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.parent = parent;
        line.transform.position = position;
        line.transform.localScale = scale;
        line.transform.rotation = rotation;
        if (line.GetComponent<Collider>() != null)
        {
            DestroyImmediate(line.GetComponent<Collider>());
        }
        if (mat != null)
        {
            line.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    private static void CreateHoop(Transform parent, Vector3 position, Quaternion rotation, Material poleMat, Material boardMat, Material targetMat, Material rimMat, GameManager scorer)
    {
        GameObject hoopRoot = new GameObject("BasketballHoop");
        hoopRoot.transform.parent = parent;
        hoopRoot.transform.position = position;
        hoopRoot.transform.rotation = rotation;

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.parent = hoopRoot.transform;
        pole.transform.localPosition = new Vector3(0, 1.975f, -0.5f);
        pole.transform.localScale = new Vector3(0.3f, 1.975f, 0.3f);
        pole.GetComponent<Renderer>().sharedMaterial = poleMat;

        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "ExtensionArm";
        arm.transform.parent = hoopRoot.transform;
        arm.transform.localPosition = new Vector3(0, 3.45f, 0.1f);
        arm.transform.localScale = new Vector3(0.3f, 0.3f, 1.2f);
        arm.GetComponent<Renderer>().sharedMaterial = poleMat;

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Backboard";
        board.transform.parent = hoopRoot.transform;
        board.transform.localPosition = new Vector3(0, 3.65f, 0.7f);
        board.transform.localScale = new Vector3(1.8f, 1.05f, 0.1f);
        board.GetComponent<Renderer>().sharedMaterial = boardMat;

        GameObject tBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tBottom.name = "TargetBottom";
        tBottom.transform.parent = hoopRoot.transform;
        tBottom.transform.localPosition = new Vector3(0f, 3.25f, 0.76f);
        tBottom.transform.localScale = new Vector3(0.59f, 0.05f, 0.01f);
        tBottom.GetComponent<Renderer>().sharedMaterial = targetMat;

        GameObject tTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tTop.name = "TargetTop";
        tTop.transform.parent = hoopRoot.transform;
        tTop.transform.localPosition = new Vector3(0f, 3.65f, 0.76f);
        tTop.transform.localScale = new Vector3(0.59f, 0.05f, 0.01f);
        tTop.GetComponent<Renderer>().sharedMaterial = targetMat;

        GameObject tLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tLeft.name = "TargetLeft";
        tLeft.transform.parent = hoopRoot.transform;
        tLeft.transform.localPosition = new Vector3(-0.27f, 3.45f, 0.76f);
        tLeft.transform.localScale = new Vector3(0.05f, 0.4f, 0.01f);
        tLeft.GetComponent<Renderer>().sharedMaterial = targetMat;

        GameObject tRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tRight.name = "TargetRight";
        tRight.transform.parent = hoopRoot.transform;
        tRight.transform.localPosition = new Vector3(0.27f, 3.45f, 0.76f);
        tRight.transform.localScale = new Vector3(0.05f, 0.4f, 0.01f);
        tRight.GetComponent<Renderer>().sharedMaterial = targetMat;

        GameObject rimVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rimVisual.name = "RimVisual";
        rimVisual.transform.parent = hoopRoot.transform;
        rimVisual.transform.localPosition = new Vector3(0, 3.05f, 1.1f);
        rimVisual.transform.localScale = new Vector3(0.45f, 0.03f, 0.45f);
        rimVisual.GetComponent<Renderer>().sharedMaterial = rimMat;
        DestroyImmediate(rimVisual.GetComponent<Collider>());

        TargetBounce tbBottom = tBottom.AddComponent<TargetBounce>();
        tbBottom.rimCenter = rimVisual.transform;
        TargetBounce tbTop = tTop.AddComponent<TargetBounce>();
        tbTop.rimCenter = rimVisual.transform;
        TargetBounce tbLeft = tLeft.AddComponent<TargetBounce>();
        tbLeft.rimCenter = rimVisual.transform;
        TargetBounce tbRight = tRight.AddComponent<TargetBounce>();
        tbRight.rimCenter = rimVisual.transform;

        GameObject rimColliders = new GameObject("RimColliders");
        rimColliders.transform.parent = hoopRoot.transform;
        rimColliders.transform.localPosition = new Vector3(0, 3.05f, 1.1f);

        CreateRimCollider(rimColliders.transform, new Vector3(0, 0, 0.225f), new Vector3(0.45f, 0.06f, 0.06f), rimMat);
        CreateRimCollider(rimColliders.transform, new Vector3(0, 0, -0.225f), new Vector3(0.45f, 0.06f, 0.06f), rimMat);
        CreateRimCollider(rimColliders.transform, new Vector3(0.225f, 0, 0), new Vector3(0.06f, 0.06f, 0.45f), rimMat);
        CreateRimCollider(rimColliders.transform, new Vector3(-0.225f, 0, 0), new Vector3(0.06f, 0.06f, 0.45f), rimMat);

        GameObject netRoot = new GameObject("HoopNet");
        netRoot.transform.parent = hoopRoot.transform;
        netRoot.transform.localPosition = new Vector3(0, 3.05f, 1.1f);

        float rimRadius = 0.225f;
        float netHeight = 0.8f;
        int segmentCount = 12;

        Material netMat = GetOrCreateMaterial("NetStrandMat", null, Vector2.one);
        netMat.color = new Color(0.9f, 0.9f, 0.9f, 0.6f);

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i * (360f / segmentCount) * Mathf.Deg2Rad;
            Vector3 rimPos = new Vector3(Mathf.Cos(angle) * rimRadius, 0, Mathf.Sin(angle) * rimRadius);
            Vector3 bottomPos = new Vector3(Mathf.Cos(angle) * (rimRadius * 0.7f), -netHeight, Mathf.Sin(angle) * (rimRadius * 0.7f));

            GameObject strand = new GameObject($"NetStrand_{i}");
            strand.transform.parent = netRoot.transform;
            strand.transform.localPosition = Vector3.zero;

            LineRenderer lr = strand.AddComponent<LineRenderer>();
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.SetPosition(0, rimPos);
            lr.SetPosition(1, bottomPos);
            lr.sharedMaterial = netMat;
        }

        // Arcade-style scoring: single generous trigger zone around the rim/net area.
        // Wider and taller than the old top/bottom pair so fast passes reliably register.
        GameObject entryTriggerObj = new GameObject("HoopEntryTriggerZone");
        entryTriggerObj.transform.parent = hoopRoot.transform;
        entryTriggerObj.transform.localPosition = new Vector3(0, 2.95f, 1.1f);
        BoxCollider boxColEntry = entryTriggerObj.AddComponent<BoxCollider>();
        boxColEntry.isTrigger = true;
        boxColEntry.size = new Vector3(0.55f, 0.5f, 0.55f);
        HoopEntryTrigger entryTrigger = entryTriggerObj.AddComponent<HoopEntryTrigger>();
        entryTrigger.scorer = scorer;
    }

    private static void CreateRimCollider(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject colObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        colObj.name = "RimSegment";
        colObj.transform.parent = parent;
        colObj.transform.localPosition = localPos;
        colObj.transform.localScale = scale;
        colObj.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static Material GetOrCreateMaterial(string name, Texture2D texture, Vector2 scale)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        Shader targetShader = Shader.Find("Universal Render Pipeline/Lit");
        if (targetShader == null)
        {
            targetShader = Shader.Find("Standard");
        }

        if (mat == null)
        {
            mat = new Material(targetShader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = targetShader;
        }

        if (texture != null)
        {
            mat.mainTexture = texture;
            mat.mainTextureScale = scale;
            mat.color = Color.white;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Texture2D CreateProceduralBasketballTexture()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, true);
        Color orange = new Color(0.85f, 0.35f, 0.1f);
        Color black = Color.black;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = orange;

                if (Mathf.Abs(y - size / 2) <= 2 || Mathf.Abs(x - size / 2) <= 2)
                {
                    c = black;
                }

                float nx = (float)x / size * 2f - 1f;
                float ny = (float)y / size * 2f - 1f;

                float distFromCenter = Mathf.Sqrt(nx * nx + ny * ny);
                if (Mathf.Abs(distFromCenter - 0.7f) <= 0.05f)
                {
                    c = black;
                }

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        if (!System.IO.Directory.Exists("Assets/Textures"))
        {
            AssetDatabase.CreateFolder("Assets", "Textures");
        }

        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes("Assets/Textures/basketball.png", bytes);
        AssetDatabase.ImportAsset("Assets/Textures/basketball.png");

        return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/basketball.png");
    }
}