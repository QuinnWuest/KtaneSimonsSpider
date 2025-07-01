using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Text;
using UnityEngine.SocialPlatforms;

public class SimonsSpiderScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;
    public SpiderLegManager Spider;

    public KMSelectable[] Sels;

    public Light SpiderLight;
    public GameObject SpiderObj;
    public GameObject[] SpiderParts;
    public GameObject[] SilkObjs;

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
        new Vector3(0.0725f, 0, 0.0725f)
    };
    private static readonly int[][] _silkIxs = new int[][]
    {
        new int[] {-1, 0, -1, 2, 3, -1, -1, -1, -1 },
        new int[] {0, -1, 1, 4, 5, 6, -1, -1, -1 },
        new int[] {-1, 1, -1, -1, 7, 8, -1, -1, -1 },
        new int[] {2, 4, -1, -1, 9, -1, 11, 12, -1 },
        new int[] {3, 5, 7, 9, -1, 10, 13, 14, 15 },
        new int[] {-1, 6, 8, -1, 10, -1, -1, 16, 17 },
        new int[] {-1, -1, -1, 11, 12, -1, -1, 18, -1 },
        new int[] {-1, -1, -1, 13, 14, 15, 18, -1, 19 },
        new int[] {-1, -1, -1, -1, 16, 17, -1, 19, -1 }
    };
    private static readonly string[] _colorNames = new string[] { "Red", "Orange", "Yellow", "Green", "Cyan", "Blue", "Purple", "Magenta", "White" };
    private static readonly string[] _3by3PosNames = new string[] { "TL", "TM", "TR", "ML", "MM", "MR", "BL", "BM", "BR" };

    private readonly int[][] _colorGrid = new int[81][];
    private readonly Loop[] _loops = new Loop[3];
    private bool _playSounds;
    private int _currentStage;
    private int _spiderPos = 4;
    private int _ixWithinLoop;
    private float _currentAngle;
    private Coroutine _flashSequence;
    private bool _spiderInAnimation;
    private List<int> _userInputQueue = new List<int>();
    private bool _checkingLoop;
    private List<int> _inputtedLoop = new List<int>();

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
            Debug.LogFormat("[Simon's Spider #{0}] Stage #{1}: Positions in the grid: {2}", _moduleId, st + 1, _loops[st].GridPos.Select(i => GetCoord(i)).Join(", "));
            Debug.LogFormat("[Simon's Spider #{0}] Stage #{1}: Shape of the loop: {2}", _moduleId, st + 1, _loops[st].WebPos.Select(i => _3by3PosNames[i]).Join(", "));
        }

        Debug.LogFormat("Simon's Spider #{0}] View the filtered log to see every flash.", _moduleId);
        _flashSequence = StartCoroutine(FlashSequence());

        StartCoroutine(HandlePressQueue());
    }

    private string GetCoord(int num)
    {
        return "ABCDEFGHIJ"[num % 9].ToString() + "1234567890"[num / 9].ToString();
    }

    private KMSelectable.OnInteractHandler SelPress(int i)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return false;
            _playSounds = true;
            if (!_checkingLoop)
                _userInputQueue.Add(i);
            return false;
        };
    }

    private IEnumerator HandlePressQueue()
    {
        while (true)
        {
            yield return null;
            if (_checkingLoop)
                continue;
            while (_spiderInAnimation)
                yield return null;

            if (_userInputQueue.Count == 0)
                continue;

            if (_flashSequence != null)
                StopCoroutine(_flashSequence);

            if (_inputtedLoop.Count != _userInputQueue.Count)
                _inputtedLoop.Add(_userInputQueue[_inputtedLoop.Count]);
            else
                continue;

            int curIx = _inputtedLoop.Last();

            Debug.LogFormat("[Simon's Spider #{0}] Spider moved to {1} position.", _moduleId, _3by3PosNames[curIx]);

            yield return RotateSpider(_spiderPos, curIx);
            yield return WalkSpider(_spiderPos, curIx);

            var adjs = GetAdjacents(_spiderPos);
            var oldPos = _spiderPos;
            _spiderPos = curIx;
            if (!adjs.Contains(curIx) && _inputtedLoop.Count != 1)
            {
                Debug.LogFormat("[Simon's Spider #{0}] Input reset via travelling to a non-adjacent position.", _moduleId);
                ResetSilk();
                _checkingLoop = false;
                _flashSequence = StartCoroutine(FlashSequence());
                _userInputQueue.Clear();
                _inputtedLoop.Clear();
                continue;
            }

            if (_inputtedLoop.Count > 1)
                SetSilk(oldPos, _spiderPos);

            if (_inputtedLoop.Distinct().Count() != _inputtedLoop.Count)
            {
                if (_inputtedLoop.First() == _inputtedLoop.Last())
                {
                    _checkingLoop = true;
                    var submission = _inputtedLoop.Take(_inputtedLoop.Count - 1).ToArray();
                    Debug.LogFormat("[Simon's Spider #{0}] Submitted loop: {1}", _moduleId, submission.Select(i => _3by3PosNames[i]).Join(", "));
                    var loop = new Loop(submission, null);
                    StartCoroutine(CheckLoop(loop));
                }
                else
                {
                    Debug.LogFormat("[Simon's Spider #{0}] Input reset via travelling to a visited position other than the starting position.", _moduleId);
                    ResetSilk();
                    _checkingLoop = false;
                    _flashSequence = StartCoroutine(FlashSequence());
                    _userInputQueue.Clear();
                    _inputtedLoop.Clear();
                }
            }
        }
    }

    private void SetSilk(int oldPos, int newPos)
    {
        var ixs = new int[] { oldPos, newPos }.OrderBy(x => x).ToArray();
        SilkObjs[_silkIxs[ixs[0]][ixs[1]]].SetActive(true);
    }

    private void ResetSilk()
    {
        foreach (var silk in SilkObjs)
            silk.SetActive(false);
    }

    private IEnumerator CheckLoop(Loop submission)
    {
        bool correct = submission.Equals(_loops[_currentStage]);
        if (!correct)
        {
            yield return RotateSpider(_spiderPos, 9);
            yield return WalkSpider(_spiderPos, 9);
            _spiderPos = 9;
            Debug.LogFormat("[Simon's Spider #{0}] Incorrect loop drawn. Strike.", _moduleId);
            yield return new WaitForSeconds(0.3f);
            Module.HandleStrike();
            StartCoroutine(FlashSpiderColor(0));
            yield return new WaitForSeconds(1);
            ResetSilk();
            yield return new WaitForSeconds(1);
            _checkingLoop = false;
            _flashSequence = StartCoroutine(FlashSequence());
            _userInputQueue.Clear();
            _inputtedLoop.Clear();
            yield break;
        }
        Debug.LogFormat("[Simon's Spider #{0}] Correct loop drawn.", _moduleId);
        _currentStage++;
        if (_currentStage == 3)
        {
            yield return RotateSpider(_spiderPos, 9);
            yield return WalkSpider(_spiderPos, 9);
            _spiderPos = 9;
            Debug.LogFormat("[Simon's Spider #{0}] Module solved!", _moduleId);
            yield return new WaitForSeconds(0.3f);
            _moduleSolved = true;
            Module.HandlePass();
            Audio.PlaySoundAtTransform("solve", transform);
            StartCoroutine(FlashSpiderColor(3, true));
        }
        else
        {
            yield return RotateSpider(_spiderPos, 9);
            yield return WalkSpider(_spiderPos, 9);
            _spiderPos = 9;
            Debug.LogFormat("[Simon's Spider #{0}] Advancing to stage {1}.", _moduleId, _currentStage + 1);
            yield return new WaitForSeconds(0.3f);
            PlaySpiderHissSound();
            StartCoroutine(FlashSpiderColor(8));
            yield return new WaitForSeconds(1);
            ResetSilk();
            yield return new WaitForSeconds(1);
            _checkingLoop = false;
            _flashSequence = StartCoroutine(FlashSequence());
            _userInputQueue.Clear();
            _inputtedLoop.Clear();
        }
        yield break;
    }

    private IEnumerator RotateSpider(int oldPos, int newPos)
    {
        float dy = _posOnMod[newPos].z - _posOnMod[oldPos].z;
        float dx = _posOnMod[newPos].x - _posOnMod[oldPos].x;
        var angle = Mathf.Atan2(-dy, dx) * (180f / Mathf.PI) + 90;
        var elapsed = 0f;
        var duration = 0.3f;
        if (_currentAngle == angle)
            goto skipRotate;
        while (elapsed < duration)
        {
            SpiderObj.transform.localRotation = Quaternion.Lerp(Quaternion.Euler(0, _currentAngle, 0), Quaternion.Euler(0, angle, 0), Easing.InOutQuad(elapsed, 0, 1, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        SpiderObj.transform.localEulerAngles = new Vector3(0, angle, 0);
        yield return new WaitForSeconds(0.3f);
        skipRotate:
        _currentAngle = angle;
    }

    private IEnumerator WalkSpider(int oldPos, int newPos)
    {
        var oldV = _posOnMod[oldPos];
        var newV = _posOnMod[newPos];
        float elapsed = 0f;
        float duration = Mathf.Sqrt(Mathf.Pow(_posOnMod[oldPos].x - _posOnMod[newPos].x, 2) + Mathf.Pow(_posOnMod[oldPos].z - _posOnMod[newPos].z, 2)) * 8f;
        PlaySpiderWalkSound();
        Spider.RunAnimation(0);
        while (elapsed < duration)
        {
            SpiderObj.transform.localPosition = new Vector3(Mathf.Lerp(oldV.x, newV.x, elapsed / duration), SpiderObj.transform.localPosition.y, Mathf.Lerp(oldV.z, newV.z, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        SpiderObj.transform.localPosition = new Vector3(newV.x, SpiderObj.transform.localPosition.y, newV.z);
        Spider.StopAnimation();
    }

    private IEnumerator FlashSequence()
    {
        var loop = _loops[_currentStage];
        while (true)
        {
            var oldPos = _spiderPos;
            _ixWithinLoop = (_ixWithinLoop + 1) % loop.GridPos.Length;
            int coord = loop.GridPos[_ixWithinLoop];
            int newPos = Rnd.Range(0, 9);
            int gCol = _colorGrid[coord][newPos];
            
            Debug.LogFormat("<Simon's Spider #{0}> {1} in the grid. {2} pos, {3} color.", _moduleId, GetCoord(coord), _3by3PosNames[newPos], _colorNames[gCol]);

            if (oldPos != newPos)
            {
                _spiderInAnimation = true;
                yield return RotateSpider(oldPos, newPos);
                yield return WalkSpider(oldPos, newPos);
                yield return new WaitForSeconds(0.5f);
                _spiderPos = newPos;
            }

            PlaySpiderHissSound();
            StartCoroutine(FlashSpiderColor(gCol));
            yield return new WaitForSeconds(0.6f);
            _spiderInAnimation = false;
            yield return new WaitForSeconds(0.4f);
        }
    }

    private IEnumerator FlashSpiderColor(int c, bool stay = false)
    {
        foreach (var p in SpiderParts)
            p.GetComponent<MeshRenderer>().material.color = _colors[c];
        SpiderLight.enabled = true;
        SpiderLight.color = _colors[c];
        if (stay)
            yield break;
        yield return new WaitForSeconds(0.6f);
        foreach (var p in SpiderParts)
            p.GetComponent<MeshRenderer>().material.color = _colors[9];
        SpiderLight.enabled = false;
        SpiderLight.color = _colors[9];
        yield return new WaitForSeconds(0.4f);
    }
    
    private void PlaySpiderHissSound()
    {
        if (_playSounds)
            Audio.PlaySoundAtTransform("sp" + Rnd.Range(0, 4), SpiderObj.transform);
    }

    private void PlaySpiderWalkSound()
    {
        if (_playSounds)
            Audio.PlaySoundAtTransform("w" + Rnd.Range(0, 4), SpiderObj.transform);
    }

    private Loop GenerateLoop(int size)
    {
        tryAgain:
        var oldPos = Enumerable.Range(0, 9).ToArray().Shuffle().Take(size).ToArray();

        // Check if the next position in the sequence is adjacent to the current.
        for (int i = 0; i < size; i++)
            if (!GetAdjacents(oldPos[i]).ToArray().Contains(oldPos[(i + 1) % size]))
                goto tryAgain;

        // Get the positions within a 10x10 grid.
        int randX = Rnd.Range(0, 6);
        int randY = Rnd.Range(0, 6);
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
        if (newArr.All(i => i % 3 > 0))
            newArr = newArr.Select(i => i - 1).ToArray();
        if (newArr.All(i => i / 3 > 0))
            newArr = newArr.Select(i => i - 3).ToArray();
        return newArr;
    }

    private int ConvertGrids(int ix, int prevWidth, int newWidth)
    {
        return (ix % prevWidth) + newWidth * (ix / prevWidth);
    }
}
