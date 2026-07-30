using Game.Experimental.Rogue.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Experimental.Rogue.Editor
{
    public static class RoguePrototypeSceneBuilder
    {
        private const string ScenePath =
            "Assets/Scenes/RoguePrototype.unity";

        [MenuItem("Tools/Rogue Prototype/Rebuild Prototype Scene")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );

            GameObject root = new("RoguePrototype");
            root.AddComponent<RoguePrototypeBootstrap>();

            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created isolated rogue prototype scene: {ScenePath}");
        }
    }
}
