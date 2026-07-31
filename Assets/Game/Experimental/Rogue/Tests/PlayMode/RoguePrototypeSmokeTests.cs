using System.Collections;
using Game.Experimental.Rogue.Domain;
using Game.Experimental.Rogue.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Experimental.Rogue.PlayModeTests
{
    public sealed class RoguePrototypeSmokeTests
    {
        private static readonly ActorId PlayerId = new("player");

        private GameObject root;
        private RogueGameController controller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("Rogue Prototype Test Root");

            if (Camera.main == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(root.transform);
                cameraObject.AddComponent<Camera>();
            }

            root.AddComponent<RogueBoardView>();
            controller = root.AddComponent<RogueGameController>();

            // Allow MonoBehaviour.Start to create the domain state and view.
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null)
            {
                Object.Destroy(root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Startup_CreatesDomainAndRenderedBoard()
        {
            Assert.That(controller.GameState, Is.Not.Null);
            Assert.That(controller.GameState.Map.Width, Is.EqualTo(12));
            Assert.That(controller.GameState.Map.Height, Is.EqualTo(9));
            Assert.That(
                controller.GameState.Map.TryGetActor(
                    PlayerId,
                    out ActorState player
                ),
                Is.True
            );
            Assert.That(player.Position, Is.EqualTo(new GridPosition(2, 2)));
            Assert.That(
                root.transform.Find("Rendered Board"),
                Is.Not.Null
            );
            Assert.That(
                FindRenderedActor(PlayerId),
                Is.Not.Null
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator Move_UpdatesDomainAndPlayerVisual()
        {
            controller.SubmitMove(1, 0);
            yield return null;

            Assert.That(
                controller.GameState.Map.TryGetActor(
                    PlayerId,
                    out ActorState player
                ),
                Is.True
            );
            Assert.That(player.Position, Is.EqualTo(new GridPosition(3, 2)));

            Transform playerVisual = FindRenderedActor(PlayerId);
            Assert.That(playerVisual, Is.Not.Null);
            Assert.That(playerVisual.localPosition.x, Is.EqualTo(3f));
            Assert.That(playerVisual.localPosition.y, Is.EqualTo(2f));
        }

        [UnityTest]
        public IEnumerator Pickup_UpdatesInventoryAndRemovesGroundVisual()
        {
            controller.SubmitMove(1, 0);
            controller.SubmitPickup();
            yield return null;

            Assert.That(
                controller.GameState.Map.TryGetActor(
                    PlayerId,
                    out ActorState player
                ),
                Is.True
            );
            Assert.That(player.Inventory.Items.Count, Is.EqualTo(1));
            Assert.That(
                player.Inventory.Items[0].Id,
                Is.EqualTo(new ItemId("small-potion"))
            );
            Assert.That(
                controller.GameState.Map.GetItemsAt(player.Position),
                Is.Empty
            );
            Assert.That(
                root.transform.Find(
                    "Rendered Board/Item small-potion"
                ),
                Is.Null
            );
        }

        [UnityTest]
        public IEnumerator CompleteFloor_LocksStateAndKeepsViewSynchronized()
        {
            controller.SubmitMove(1, 0);
            controller.SubmitPickup();

            SubmitMoves(
                (0, 1),
                (1, 0),
                (0, 1),
                (1, 0),
                (1, 0),
                (1, 0),
                (1, 0)
            );

            Assert.That(controller.GameState.Map.HasEnemies, Is.False);

            SubmitMoves(
                (1, 0),
                (1, 0),
                (1, 0),
                (0, 1),
                (0, 1),
                (0, 1),
                (1, 0),
                (1, 0),
                (1, 0)
            );
            controller.SubmitDescend();
            yield return null;

            Assert.That(
                controller.GameState.Progress,
                Is.EqualTo(RogueGameProgress.FloorCompleted)
            );
            Assert.That(controller.GameState.IsTerminal, Is.True);
            Assert.That(
                controller.GameState.Map.TryGetActor(
                    PlayerId,
                    out ActorState player
                ),
                Is.True
            );
            Assert.That(
                player.Position,
                Is.EqualTo(new GridPosition(10, 7))
            );

            controller.SubmitMove(-1, 0);
            yield return null;

            Assert.That(
                player.Position,
                Is.EqualTo(new GridPosition(10, 7))
            );
            Transform playerVisual = FindRenderedActor(PlayerId);
            Assert.That(playerVisual, Is.Not.Null);
            Assert.That(playerVisual.localPosition.x, Is.EqualTo(10f));
            Assert.That(playerVisual.localPosition.y, Is.EqualTo(7f));
        }

        private void SubmitMoves(
            params (int deltaX, int deltaY)[] moves
        )
        {
            for (int index = 0; index < moves.Length; index++)
            {
                (int deltaX, int deltaY) = moves[index];
                controller.SubmitMove(deltaX, deltaY);
            }
        }

        private Transform FindRenderedActor(ActorId actorId) =>
            root.transform.Find(
                $"Rendered Board/{actorId}"
            );
    }
}
