using UnityEngine;
using UnityEngine.Assertions;

public class SpawnPointCTRL : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject collectiblePrefab;

    void Start()
    {
        var collectibleSpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoints");

        var effectiveSpawnPointAmount = collectibleSpawnPoints.Length / 3;

        var effectiveSpawnPoints = Sample(collectibleSpawnPoints, effectiveSpawnPointAmount);

        foreach (var spawnPoint in effectiveSpawnPoints)
        {
            Instantiate(collectiblePrefab, spawnPoint.transform.position, Quaternion.identity);
        }
    }

    static T[] Sample<T>(T[] elements, int amount)
    {
        // implements "selection sampling" according to https://stackoverflow.com/a/48089 

        int left = elements.Length;
        int needed = amount;

        Assert.IsFalse(left < needed);

        T[] sample = new T[amount];

        while (needed > 0 && left > 0)
        {
            var pickProbability = (double)needed / left--;

            var probability = Random.Range(0f, 1f);

            if (probability < pickProbability)
            {
                sample[--needed] = elements[left];
            }
        }

        return sample;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
