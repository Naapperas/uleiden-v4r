using UnityEngine;
using TMPro;

public class CollectibleCTRL : MonoBehaviour
{
    public int bananasCollected = 0;

    public int maxBananas = 0;

    public TextMeshProUGUI collectibleText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var collectibleSpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoints");

        maxBananas = collectibleSpawnPoints.Length;
    }

    // Update is called once per frame
    void Update()
    {
        collectibleText.text = $"Bananas collected: {bananasCollected}/{maxBananas}";
    }

    public void collect()
    {
        bananasCollected++;
    }
}
