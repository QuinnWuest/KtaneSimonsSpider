using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Rnd = UnityEngine.Random;

public class FlowerManager : MonoBehaviour
{
    public GameObject[] Flowers;

    private float Area = 8f;
    private float MinimumDistance = 2f;
    private int MaximumAttempts = 1000;

    private List<Vector3> FlowerPositions = new List<Vector3>();

    void Start()
    {
        for (int i = 0; i < Flowers.Length; i++)
            Flowers[i].SetActive(false);

        var chosenFlowers = Flowers.Shuffle().Take(Rnd.Range(3, 5)).ToArray();

        for (int i = 0; i < chosenFlowers.Length; i++)
            chosenFlowers[i].SetActive(true);

        foreach (GameObject flower in chosenFlowers)
        {
            bool placed = false;

            for (int i = 0; i < MaximumAttempts; i++)
            {
                Vector3 pos = new Vector3(Rnd.Range(-Area, Area), flower.transform.localPosition.y, Rnd.Range(-Area, Area));

                if (IsFarEnough(pos))
                {
                    flower.transform.localPosition = pos;
                    flower.transform.localEulerAngles += Vector3.up * Rnd.Range(0, 360f);
                    FlowerPositions.Add(pos);
                    placed = true;
                    break;
                }
            }

            if (!placed)
                Debug.LogWarning($"Failed to place {flower.name} after {MaximumAttempts} attempts.");
        }
    }

    private bool IsFarEnough(Vector3 position)
    {
        foreach (Vector3 flower in FlowerPositions)
            if (Vector3.Distance(position, flower) < MinimumDistance)
                return false;
        return true;
    }
}
