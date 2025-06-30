using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderLeg : MonoBehaviour
{
    public Transform Joint1;
    public Transform Joint2;
    public Transform Joint3;
    public int LegIx;

    private Coroutine LegAnimCoroutine;

    public struct LegAnim
    {
        public LegFrame[] Frames;
        public float[] Durations;

        public LegAnim(LegFrame[] frames, float[] durations)
        {
            Frames = frames;
            Durations = durations;
        }
    }

    public class LegFrame
    {
        public float Joint1A;
        public float Joint1B;
        public float Joint2;
        public float Joint3;

        public LegFrame(float joint1A, float joint1B, float joint2, float joint3)
        {
            Joint1A = joint1A;
            Joint1B = joint1B;
            Joint2 = joint2;
            Joint3 = joint3;
        }

        public static LegFrame Lerp(LegFrame frameA, LegFrame frameb, float t)
        {
            return new LegFrame(
                Mathf.Lerp(frameA.Joint1A, frameb.Joint1A, t),
                Mathf.Lerp(frameA.Joint1B, frameb.Joint1B, t),
                Mathf.Lerp(frameA.Joint2, frameb.Joint2, t),
                Mathf.Lerp(frameA.Joint3, frameb.Joint3, t));
        }
    }

    private static readonly LegAnim[] AllAnims = new LegAnim[]
    {
        new LegAnim(
            new LegFrame[]
            {
                new LegFrame(60, -30, 80, -28),
                new LegFrame(40, -30, 120, -42),
                new LegFrame(80, -30, 0, 0),
                new LegFrame(60, -30, 55, 20)
            },
            new float[]
            {
                0.075f, 0.15f, 0.15f, 0.075f
            }),
        new LegAnim(
            new LegFrame[]
            {
                new LegFrame(80, 30, 0, 0),
                new LegFrame(60, 30, 55, 20),
                new LegFrame(60, 30, 80, -28),
                new LegFrame(40, 30, 120, -42)
            },
            new float[]
            {
                0.15f, 0.075f, 0.075f, 0.15f
            }),
        new LegAnim(
            new LegFrame[]
            {
                new LegFrame(40, -45, 70, -20),
                new LegFrame(65, -45, 70, -24),
                new LegFrame(60, -55, 80, -28),
                new LegFrame(55, -65, 90, -33)
            },
            new float[]
            {
                0.15f, 0.075f, 0.075f, 0.15f
            }),
        new LegAnim(
            new LegFrame[]
            {
                new LegFrame(60, 55, 80, -28),
                new LegFrame(55, 65, 90, -33),
                new LegFrame(40, 45, 70, -20),
                new LegFrame(65, 45, 70, -24)
            },
            new float[]
            {
                0.075f, 0.15f, 0.15f, 0.075f
            }),
        new LegAnim(
            new LegFrame[]
            {
                new LegFrame(60, -90, 80, -28),
                new LegFrame(50, -105, 75, -40),
                new LegFrame(50, -90, 60, -10),
                new LegFrame(68, -75, 75, -40)
            },
            new float[]
            {
                0.075f, 0.15f, 0.15f, 0.075f
            }),
        new LegAnim(
            new LegFrame[]
            {
                new LegFrame(50, 90, 60, -10),
                new LegFrame(68, 75, 75, -40),
                new LegFrame(60, 90, 80, -28),
                new LegFrame(50, 105, 75, -40)
            },
            new float[]
            {
                0.15f, 0.075f, 0.075f, 0.15f
            }),
        new LegAnim(
            new LegFrame[]
            {
                new LegFrame(65, -150, 50, -10),
                new LegFrame(40, -140, 120, -43),
                new LegFrame(60, -150, 80, -28),
                new LegFrame(75, -160, 50, -15)
            },
            new float[]
            {
                0.15f, 0.075f, 0.075f, 0.15f
            }),
        new LegAnim(
            new LegFrame[]
            {
                new LegFrame(60, 150, 80, -28),
                new LegFrame(75, 160, 50, -15),
                new LegFrame(65, 150, 50, -10),
                new LegFrame(40, 140, 120, -43)
            },
            new float[]
            {
                0.075f, 0.15f, 0.15f, 0.075f
            }),
    };

    private LegFrame BasePosition;

    private LegFrame CurrentToFrame()
    {
        return new LegFrame(Joint1.localEulerAngles.x, Joint1.localEulerAngles.y, Joint2.localEulerAngles.x, Joint3.localEulerAngles.x);
    }

    // Use this for initialization
    void Start()
    {
        BasePosition = CurrentToFrame();
        }

    private void SetLegPosition(float joint1a, float joint1b, float joint2, float joint3)
    {
        Joint1.localEulerAngles = new Vector3(joint1a, joint1b, 0);
        Joint2.localEulerAngles = new Vector3(joint2, 0, 0);
        Joint3.localEulerAngles = new Vector3(joint3, 0, 0);
    }

    private void SetLegPosition(LegFrame frame)
    {
        Joint1.localEulerAngles = new Vector3(frame.Joint1A, frame.Joint1B, 0);
        Joint2.localEulerAngles = new Vector3(frame.Joint2, 0, 0);
        Joint3.localEulerAngles = new Vector3(frame.Joint3, 0, 0);
    }

    public void RunAnimation(int animIx)
    {
        if (LegAnimCoroutine != null)
            StopCoroutine(LegAnimCoroutine);
        LegAnimCoroutine = StartCoroutine(PlayAnimation(animIx));
    }

    private IEnumerator PlayAnimation(int animIx)
    {
        var posInAnim = 0;
        while (true)
        {
            SetLegPosition(AllAnims[animIx].Frames[0]);
            for (int i = 0; i < AllAnims[animIx].Frames.Length; i++)
            {
                var pos = (i + 1) % AllAnims[animIx].Frames.Length;
                float timer = 0;
                float duration = AllAnims[animIx].Durations[i];
                while (timer < duration)    // Intentionally i and not pos, here.
                {
                    yield return null;
                    timer += Time.deltaTime;
                    SetLegPosition(LegFrame.Lerp(AllAnims[animIx].Frames[i], AllAnims[animIx].Frames[pos], timer / duration));
                }
                SetLegPosition(AllAnims[animIx].Frames[pos]);
            }
        }
    }

    public void StopAnimation()
    {
        if (LegAnimCoroutine != null)
            StopCoroutine(LegAnimCoroutine);
        LegAnimCoroutine = StartCoroutine(ResetToBase());
    }

    private IEnumerator ResetToBase(float duration = 0.1f)
    {
        LegFrame initial = CurrentToFrame();
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            SetLegPosition(LegFrame.Lerp(initial, BasePosition, timer / duration));
        }
        SetLegPosition(BasePosition);
    }
}
