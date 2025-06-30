using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class SpiderFace : MonoBehaviour
{
    public SpriteRenderer[] LeftEyes;
    public SpriteRenderer[] RightEyes;

    public Sprite LeftEyesOpen;
    public Sprite LeftEyesClosed;
    public Sprite RightEyesOpen;
    public Sprite RightEyesClosed;

    private void Start()
    {
        StartCoroutine(Blink());
    }

    private IEnumerator Blink(float closedTime = 0.1f)
    {
        while (true)
        {
            yield return new WaitForSeconds(Rnd.Range(0.25f, 3f));
            for (int i = 0; i < LeftEyes.Length; i++)
                LeftEyes[i].sprite = LeftEyesClosed;
            for (int i = 0; i < RightEyes.Length; i++)
                RightEyes[i].sprite = RightEyesClosed;
            yield return new WaitForSeconds(closedTime);
            for (int i = 0; i < LeftEyes.Length; i++)
                LeftEyes[i].sprite = LeftEyesOpen;
            for (int i = 0; i < RightEyes.Length; i++)
                RightEyes[i].sprite = RightEyesOpen;
        }
    }
}
