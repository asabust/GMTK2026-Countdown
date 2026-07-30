using Game.Experimental.Rogue.Domain;
using UnityEngine;

namespace Game.Experimental.Rogue.Unity
{
    /// <summary>
    /// Scene entry point for the isolated prototype.
    /// Presentation and Unity integration belong here, not in Domain.
    /// </summary>
    public sealed class RoguePrototypeBootstrap : MonoBehaviour
    {
        private static readonly GridPosition Origin = new(0, 0);

        private void Start()
        {
            Debug.Log(
                $"Rogue prototype ready at {Origin}. " +
                "The completed game remains unchanged.",
                this
            );
        }

        private void OnGUI()
        {
            const int width = 480;
            const int height = 80;
            Rect area = new(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height
            );

            GUI.Box(area, "Rogue Prototype");
            GUI.Label(
                new Rect(area.x + 20, area.y + 32, width - 40, 30),
                "Isolated scaffold ready — no gameplay has been migrated."
            );
        }
    }
}
