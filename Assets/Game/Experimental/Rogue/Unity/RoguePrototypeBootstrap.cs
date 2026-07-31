using UnityEngine;

namespace Game.Experimental.Rogue.Unity
{
    /// <summary>
    /// Composition root kept in the prototype scene. It makes the scene
    /// resilient while the experimental presentation layer is evolving.
    /// </summary>
    [RequireComponent(typeof(RogueGameController))]
    [RequireComponent(typeof(RogueBoardView))]
    public sealed class RoguePrototypeBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            EnsureComponent<RogueBoardView>();
            EnsureComponent<RogueGameController>();
        }

        private void EnsureComponent<T>() where T : Component
        {
            if (GetComponent<T>() == null)
            {
                gameObject.AddComponent<T>();
            }
        }
    }
}
