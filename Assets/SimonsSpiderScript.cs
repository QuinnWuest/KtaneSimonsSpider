using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Text;

public class SimonsSpiderScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable[] Sels;

    public Light SpiderLight;
    public GameObject SpiderObj;
    public GameObject[] SpiderParts;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private static readonly Color32[] _colors = new Color32[]
    {
        /* r */ new Color32(255, 050, 065, 255),
        /* o */ new Color32(255, 135, 066, 255),
        /* y */ new Color32(255, 221, 051, 255),
        /* g */ new Color32(050, 255, 090, 255),
        /* c */ new Color32(087, 255, 255, 255),
        /* b */ new Color32(040, 120, 255, 255),
        /* p */ new Color32(130, 070, 255, 255),
        /* m */ new Color32(255, 094, 255, 255),
        /* w */ new Color32(230, 230, 230, 255),
        /* . */ new Color32(096, 083, 077, 255)
    };
    private static readonly string[] _colorNames = new string[] { "Red", "Orange", "Yellow", "Green", "Cyan", "Blue", "Purple", "Magenta", "White" };

    private readonly int[][] _colorGrid = new int[81][];
    private readonly Loop[] _loops = new Loop[3];
    private bool _playSounds;
    private int _currentStage;
    private int _spiderPos = 4;
    private int _ixWithinLoop;
    private bool _inSequence;
    private float _currentAngle;

    private static readonly string[] _3by3PosNames = new string[] { "TL", "TM", "TR", "ML", "MM", "MR", "BL", "BM", "BR" };
    private static readonly Vector3[] _posOnMod = new Vector3[]
    {
        new Vector3(-0.05f, 0, 0.05f),
        new Vector3(0, 0, 0.05f),
        new Vector3(0.05f, 0, 0.05f),
        new Vector3(-0.05f, 0, 0),
        new Vector3(0, 0, 0),
        new Vector3(0.05f, 0, 0),
        new Vector3(-0.05f, 0, -0.05f),
        new Vector3(0, 0, -0.05f),
        new Vector3(0.05f, 0, -0.05f),
    };

    private static readonly float?[][] _angleTable = new float?[9][]
    {
        new float?[] { null, 0090, 0090, 0180, 0135, 0116, 0180, 0154, 0135 },
        new float?[] { 0270, null, 0090, 0225, 0180, 0135, 0154, 0180, 0154 },
        new float?[] { 0270, 0270, null, 0244, 0225, 0180, 0225, 0206, 0180 },
        new float?[] { 0000, 0045, 0064, null, 0090, 0090, 0180, 0135, 0116 },
        new float?[] { 0315, 0000, 0045, 0270, null, 0090, 0225, 0180, 0135 },
        new float?[] { 0296, 0315, 0000, 0270, 0270, null, 0244, 0225, 0180 },
        new float?[] { 0000, 0026, 0045, 0000, 0045, 0064, null, 0090, 0090 },
        new float?[] { 0334, 0000, 0026, 0315, 0000, 0045, 0270, null, 0090 },
        new float?[] { 0315, 0334, 0000, 0296, 0315, 0000, 0270, 0270, null }
    };

    public class Loop
    {
        public int[] WebPos;
        public int[] GridPos;

        public Loop(int[] positions, int[] pwg)
        {
            WebPos = positions;
            GridPos = pwg;
        }

        public bool Equals(Loop other)
        {
            var a = ShiftToLowest(WebPos);
            var o = ShiftToLowest(other.WebPos);
            return other != null && o.SequenceEqual(a);
        }

        public override bool Equals(object obj)
        {
            return obj is Loop && Equals((Loop)obj);
        }

        public override int GetHashCode()
        {
            return WebPos.Aggregate(47, (p, n) => p * 31 + n);
        }
    }

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (int i = 0; i < Sels.Length; i++)
            Sels[i].OnInteract += SelPress(i);

        var rnd = RuleSeedable.GetRNG();
        for (int row = 0; row < 9; row++)
            for (int cell = 0; cell < 9; cell++)
                _colorGrid[cell + 9 * row] = rnd.ShuffleFisherYates(Enumerable.Range(0, 9).ToArray());

        for (int st = 0; st < 3; st++)
        {
            _loops[st] = GenerateLoop(st + 4);
            Debug.Log(_loops[st].WebPos.Join(" "));
            Debug.LogFormat("[Simon's Spider #{0}] Positions in the grid: {1}", _moduleId, _loops[st].GridPos.Select(i => GetCoord(i)).Join(", "));
            Debug.LogFormat("[Simon's Spider #{0}] Shape of the loop: {1}", _moduleId, _loops[st].WebPos.Select(i => _3by3PosNames[i]).Join(", "));
        }

        StartCoroutine(DoSequence());
    }

    private string GetCoord(int num)
    {
        return "ABCDEFGHIJ"[num % 9].ToString() + "1234567890"[num / 9].ToString();
    }

    private IEnumerator FlashSpiderColor(int c)
    {
        foreach (var p in SpiderParts)
            p.GetComponent<MeshRenderer>().material.color = _colors[c];
        if (_playSounds)
            Audio.PlaySoundAtTransform("sp" + Rnd.Range(0, 4), SpiderObj.transform);
        SpiderLight.enabled = true;
        SpiderLight.color = _colors[c];
        yield return new WaitForSeconds(1f);
        SpiderLight.enabled = false;
        SpiderLight.color = _colors[9];
    }

    private KMSelectable.OnInteractHandler SelPress(int i)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return false;
            return false;
        };
    }

    private IEnumerator DoSequence()
    {
        var loop = _loops[_currentStage];

        while (true)
        {
            var old = _spiderPos;
            _ixWithinLoop = (_ixWithinLoop + 1) % loop.GridPos.Length;
            int coord = loop.GridPos[_ixWithinLoop];
            int gPos = Rnd.Range(0, 9);
            int gCol = _colorGrid[coord][gPos];
            Debug.LogFormat("<Simon's Spider #{0}> {1} in the grid. {2} pos, {3} color.", _moduleId, GetCoord(coord), _3by3PosNames[gPos], _colorNames[gCol]);
            float? angle = _angleTable[old][gPos];
            if (angle != null)
            {
                var elapsed = 0f;
                var duration = 0.5f;
                float rs = _currentAngle;
                float re = angle.Value;
                if (rs - re > 180)
                    re += 360;
                if (re - rs > 180)
                    rs += 360;
                while (elapsed < duration)
                {
                    SpiderObj.transform.localEulerAngles = new Vector3(0, Easing.InOutQuad(elapsed, rs, re, duration), 0);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
                SpiderObj.transform.localEulerAngles = new Vector3(0, (re + 360) % 360, 0);
                _currentAngle = re;
                yield return new WaitForSeconds(0.3f);
                elapsed = 0f;
                duration = 1f;
                var oldP = _posOnMod[old];
                var newP = _posOnMod[gPos];
                while (elapsed < duration)
                {
                    SpiderObj.transform.localPosition = new Vector3(Mathf.Lerp(oldP.x, newP.x, elapsed / duration), 0, Mathf.Lerp(oldP.z, newP.z, elapsed / duration));
                    yield return null;
                    elapsed += Time.deltaTime;
                }
                SpiderObj.transform.localPosition = new Vector3(newP.x, 0, newP.z);
                yield return new WaitForSeconds(0.5f);
            }

            _spiderPos = gPos;
            foreach (var p in SpiderParts)
                p.GetComponent<MeshRenderer>().material.color = _colors[gCol];
            if (true)
                Audio.PlaySoundAtTransform("sp" + Rnd.Range(0, 4), SpiderObj.transform);
            SpiderLight.enabled = true;
            SpiderLight.color = _colors[gCol];
            yield return new WaitForSeconds(0.6f);
            foreach (var p in SpiderParts)
                p.GetComponent<MeshRenderer>().material.color = _colors[9];
            SpiderLight.enabled = false;
            SpiderLight.color = _colors[9];
            yield return new WaitForSeconds(0.2f);
        }
    }

    private Loop GenerateLoop(int size)
    {
        tryAgain:
        var oldPos = Enumerable.Range(0, 9).ToArray().Shuffle().Take(size).ToArray();

        // Check if the next position in the sequence is adjacent to the current.
        for (int i = 0; i < size; i++)
            if (!GetAdjacents(oldPos[i]).ToArray().Contains(oldPos[(i + 1) % size]))
                goto tryAgain;

        // Force it into the top-left position.
        if (oldPos.All(x => x % 3 != 0))
            oldPos = oldPos.Select(i => i - 1).ToArray();
        if (oldPos.All(x => x / 3 != 0))
            oldPos = oldPos.Select(i => i - 3).ToArray();

        // Get the positions within a 10x10 grid.
        int randX = Rnd.Range(0, 9 - (oldPos.All(x => x % 3 != 2) ? 2 : 3));
        int randY = Rnd.Range(0, 9 - (oldPos.All(x => x / 3 != 2) ? 2 : 3));
        var newPos = oldPos.Select(i => ConvertGrids(i, 3, 9) + randX + (9 * randY)).ToArray();

        return new Loop(oldPos, newPos);
    }

    private IEnumerable<int> GetAdjacents(int pos)
    {
        if (pos % 3 != 0)
            yield return pos - 1;
        if (pos % 3 != 2)
            yield return pos + 1;
        if (pos / 3 != 0)
            yield return pos - 3;
        if (pos / 3 != 2)
            yield return pos + 3;
        if (pos % 3 != 0 && pos / 3 != 0)
            yield return pos - 4;
        if (pos % 3 != 2 && pos / 3 != 0)
            yield return pos - 2;
        if (pos % 3 != 0 && pos / 3 != 2)
            yield return pos + 2;
        if (pos % 3 != 2 && pos / 3 != 2)
            yield return pos + 4;
    }

    public static int[] ShiftToLowest(int[] array)
    {
        var newArr = array.ToArray();
        while (newArr.First() != array.Min())
        {
            var a = newArr.Skip(1).Concat(newArr.Take(1));
            newArr = a.ToArray();
        }
        return newArr;
    }

    private int ConvertGrids(int ix, int prevWidth, int newWidth)
    {
        return (ix % prevWidth) + newWidth * (ix / prevWidth);
    }
}
