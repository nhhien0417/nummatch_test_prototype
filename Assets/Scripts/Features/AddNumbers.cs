using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AddNumbers : MonoBehaviour
{
    public void CloneCell()
    {
        AudioManager.Instance.PlaySFX("pop_button");
        if (GameManager.Instance.AddCount <= 0 || Board.Instance.IsAnimating) return;

        GameManager.Instance.UpdateAddCount();

        var originalCells = Board.Instance.GetCells();
        var cellsCopy = originalCells.Where(c => c.IsActive).ToList();
        var startIndex = originalCells.Count;

        // Update values and find all possible match pairs before cloning
        BoardController.Instance.UpdateBoardValues(cellsCopy);
        BoardController.Instance.FindAllMatchPairs();

        var spawnedGems = 0;
        var maxGems = GemManager.Instance.GemProgresses.Count(); // Maximum gems allowed this turn (Z)

        var spawnGemCounter = 0;
        var gemInterval = Mathf.CeilToInt((cellsCopy.Count + 1) / 2); // Force spawn at least 1 gem every Y cells (Y)

        var forceSpawnGem = false;
        var spawnedGemIndexes = new List<int>();

        var delay = 0f;

        for (int i = 0; i < cellsCopy.Count; i++)
        {
            var originalCell = cellsCopy[i];
            if (!originalCell.IsActive) continue;
            var currentIndex = startIndex + i;

            var value = originalCell.Value;
            var shouldSpawnGem = false;
            var gemTypeToSpawn = GemType.None;

            // Check if gem should be spawned
            if (spawnedGems < maxGems)
            {
                var chance = Random.Range(5f, 8f); // X% chance to spawn a gem (X)

                if ((forceSpawnGem || Random.Range(0f, 100f) < chance) && BoardController.Instance.IsSafeToSpawnGem(currentIndex, spawnedGemIndexes))
                {
                    var validGemTypes = GemManager.Instance.AvailableGemTypes;
                    gemTypeToSpawn = validGemTypes[Random.Range(0, validGemTypes.Count)];
                    shouldSpawnGem = true;
                }
            }

            delay += 0.05f;
            var clone = BoardController.Instance.SpawnCell(value, gemTypeToSpawn, delay);
            originalCells.Add(clone);

            if (shouldSpawnGem)
            {
                spawnedGems++;
                spawnGemCounter = 0;
                spawnedGemIndexes.Add(currentIndex);
            }
            else
            {
                spawnGemCounter++;
            }

            // If no gem has been spawned for `gemInterval`, or no more safe spots exist, force the next one to spawn a gem
            forceSpawnGem = spawnGemCounter >= gemInterval || !BoardController.Instance.AnySafeIndexRemaining(currentIndex + 1, spawnedGemIndexes);
        }

        Board.Instance.UpdateContainerHeight();
        GameManager.Instance.CheckLoseGame();
    }
}
