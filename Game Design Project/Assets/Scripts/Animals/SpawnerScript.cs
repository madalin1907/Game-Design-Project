using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerScript : MonoBehaviour
{
    public GameObject cowPrefab;
    public GameObject pigPrefab;
    public GameObject chickenPrefab;
    public GameObject zombiePrefab;

    public float spawnAnimalsTime;
    public float spawnMobsTime;
    int randAnimal;
    bool isDay;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(spawn());
    }

    IEnumerator spawn()
    {
        isDay = true;
        while (isDay)
        {
            yield return new WaitForSeconds(spawnAnimalsTime);
            randAnimal = Random.Range(0, 3);
            if (randAnimal == 0)
            {
                Instantiate(cowPrefab, transform.position, Quaternion.identity);

            }
            if (randAnimal == 1)
            {
                Instantiate(pigPrefab, transform.position, Quaternion.identity);

            }
            if (randAnimal == 2)
            {
                Instantiate(chickenPrefab, transform.position, Quaternion.identity);

            }
        }
    }
}
