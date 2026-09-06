using System.IO;
using BallRolling.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BallRolling.EditorTools
{
    /// <summary>ステップ3のプレイ環境セットアップ（玉・傾け・カメラ）。Undo対応・シーンのダーティ登録を行う。</summary>
    public static class Step3_PlaySetup
    {
        public const string BallName = "Ball";
        private const string MaterialFolder = "Assets/Art/Materials";
        private const string PhysicsMaterialFolder = "Assets/Art/PhysicsMaterials";

        /// <summary>玉をスポーンし、MazeRootにBoardControllerを設定してカメラをボードの子にする。</summary>
        public static GameObject Apply(Step3_PlayParameters p)
        {
            var root = GameObject.Find(Step2_SceneMazeBuilder.RootName);
            if (root == null)
            {
                Debug.LogError("MazeRoot が見つかりません。先に Step2 Maze Builder で迷路を生成して下さい。");
                return null;
            }

            var ball = SpawnBall(p, root.transform);
            AttachBoardController(p, root);
            AttachCameraToBoard(root.transform);
            Physics.gravity = new Vector3(0f, -9.81f * p.GravityScale, 0f);
            Time.fixedDeltaTime = 0.01f; // 物理ステップを細かく（すり抜け・爆発抑制）

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return ball;
        }

        private static GameObject SpawnBall(Step3_PlayParameters p, Transform root)
        {
            var oldBall = root.transform.Find(BallName);
            if (oldBall != null)
                Undo.DestroyObjectImmediate(oldBall.gameObject);

            // 迷路モデルの入り口 = (0,0)。セル中心は +0.5
            var entrance = new Vector3(0.5f, p.BallSpawnHeight, 0.5f);

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = BallName;
            ball.transform.SetParent(root, false);
            ball.transform.localPosition = entrance;
            ball.transform.localScale = Vector3.one * p.BallSize;
            ball.GetComponent<Renderer>().sharedMaterial = GetOrCreateBallMaterial(p.BallColor);
            ball.AddComponent<Rigidbody>();

            var rigidbody = ball.GetComponent<Rigidbody>();
            rigidbody.mass = p.BallMass;
            rigidbody.linearDamping = p.BallDrag;
            rigidbody.angularDamping = p.BallAngularDrag;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ball.GetComponent<Collider>().sharedMaterial = GetOrCreateBallPhysicsMaterial(p);

            var limiter = ball.GetComponent<BallSpeedLimiter>();
            if (limiter == null)
                ball.AddComponent<BallSpeedLimiter>();
            ball.GetComponent<BallSpeedLimiter>().Configure(p.MaxLinearSpeed, p.MaxAngularSpeed);

            Undo.RegisterCreatedObjectUndo(ball, "Step3: Spawn Ball");
            return ball;
        }

        private static void AttachBoardController(Step3_PlayParameters p, GameObject root)
        {
            var controller = root.GetComponent<BoardController>();
            if (controller == null)
            {
                Undo.AddComponent<BoardController>(root);
                controller = root.GetComponent<BoardController>();
            }

            Undo.RecordObject(controller, "Step3: Configure BoardController");
            controller.Configure(p.MaxTiltAngle, p.TiltSpeed);
            EditorUtility.SetDirty(controller);

            // 回転するボードにはKinematic Rigidbodyが必須。Rigidbodyなしの静的コライダーを
            // Transformで回転すると接触計算が不正確になり、玉が床をすり抜ける原因になる。
            var rootBody = root.GetComponent<Rigidbody>();
            if (rootBody == null)
            {
                Undo.AddComponent<Rigidbody>(root);
                rootBody = root.GetComponent<Rigidbody>();
            }

            Undo.RecordObject(rootBody, "Step3: Configure Board Rigidbody");
            rootBody.isKinematic = true;
            rootBody.useGravity = false;
            rootBody.interpolation = RigidbodyInterpolation.Interpolate;
            EditorUtility.SetDirty(rootBody);
        }

        private static void AttachCameraToBoard(Transform root)
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            if (camera.transform.parent == root)
                return; // 既にボードの子

            var position = camera.transform.position;
            var rotation = camera.transform.rotation;

            Undo.SetTransformParent(camera.transform, root, false, "Step3: Attach Camera to Board");
            Undo.RecordObject(camera.transform, "Step3: Retain Camera Transform");
            camera.transform.localPosition = root.InverseTransformPoint(position);
            camera.transform.localRotation = Quaternion.Inverse(root.rotation) * rotation;
            EditorUtility.SetDirty(camera.transform);
        }

        private static Material GetOrCreateBallMaterial(Color color)
        {
            var path = $"{MaterialFolder}/Ball.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Directory.CreateDirectory(MaterialFolder);
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static PhysicsMaterial GetOrCreateBallPhysicsMaterial(Step3_PlayParameters p)
        {
            var path = $"{PhysicsMaterialFolder}/BallPhysics.physicMaterial";
            var physicMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (physicMaterial == null)
            {
                Directory.CreateDirectory(PhysicsMaterialFolder);
                physicMaterial = new PhysicsMaterial("BallPhysics");
                AssetDatabase.CreateAsset(physicMaterial, path);
            }

            physicMaterial.dynamicFriction = p.Friction;
            physicMaterial.staticFriction = p.Friction;
            physicMaterial.bounciness = p.Bounciness;
            // Minimum: 床（素のコライダー摩擦0.6）との組み合わせで玉側の低摩擦を採用するため。
            // Maximum にすると床側0.6が勝ち、傾き15度（tan15°=0.27）では静止摩擦に阻まれて転がり始めない。
            physicMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            physicMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
            EditorUtility.SetDirty(physicMaterial);
            AssetDatabase.SaveAssets();
            return physicMaterial;
        }
    }
}
