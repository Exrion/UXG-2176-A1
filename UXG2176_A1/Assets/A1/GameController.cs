using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Assignment1
{
    public class GameController : MonoBehaviour
    {
        public Transform arena;
        public Transform contestantAPos, contestantBPos;
        public HPBar contestantAHP, contestantBHP;
        public TextMeshProUGUI matchTitle;
        public TextMeshProUGUI matchInfo;
        public TextMeshProUGUI matchTimer;

        private List<AutoBotContestant> currentContestant;
        private List<AutoBotContestant> nextBracket;
        int currentRound;

        private bool isReady;
        private bool inMatch;
        private bool isOver;
        private bool inMatchResult;
        private float matchTimeLeft;
        private const float matchDuration = 120f;

        private AutoBotContestant contestantA, contestantB;

        void Start()
        {
            isReady = false;
            // fetch all autobot in contestant group
            Addressables.LoadAssetsAsync<GameObject>("contestant").Completed += (handler) =>
            {
                if (handler.Status == AsyncOperationStatus.Succeeded)
                {
                    inMatch = false;
                    currentRound = 0;
                    currentContestant = new List<AutoBotContestant>();
                    nextBracket = new List<AutoBotContestant>();
                    foreach (var autobot in handler.Result)
                    {
                        var obj = Instantiate(autobot, arena);
                        obj.name = autobot.name;
                        obj.SetActive(false);
                        var contestant = obj.GetComponent<AutoBotContestant>();
                        contestant.Init();
                        currentContestant.Add(contestant);
                    }

                    // shuffle contestant
                    currentContestant = currentContestant.OrderBy(a => Random.value).ToList();

                    if (currentContestant.Count <= 1)
                    {
                        isOver = true;
                    }

                    // setup grouping
                    isReady = true;

                    handler.Release();
                }
            };
        }

        private void Update()
        {
            if (!isReady || isOver)
            {
                return;
            }

            if (inMatchResult)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    inMatchResult = false;
                    NextRound();
                }
                return;
            }

            if (isReady && !inMatch)
            {
                StartMatch();
            }
            else if (contestantA != null && contestantB != null)
            {
                contestantAHP.Refresh(contestantA.GetHP(), contestantA.GetMaxHP());
                contestantBHP.Refresh(contestantB.GetHP(), contestantB.GetMaxHP());
                if (contestantA.IsDead() || contestantB.IsDead())
                {
                    inMatchResult = true;
                    if (contestantA.IsDead() && contestantB.IsDead())
                    {
                        matchTitle.text = $"Draw! Both Eliminated!";
                        matchInfo.text = $"{matchInfo.text} (Draw! Both Eliminated!)";
                    }
                    else if (contestantA.IsDead())
                    {
                        matchTitle.text = $"Winner: {contestantB.name}";
                        matchInfo.text = $"{matchInfo.text} (Winner: {contestantB.name})";
                        nextBracket.Add(contestantB);
                    }
                    else
                    {
                        matchTitle.text = $"Winner: {contestantA.name}";
                        matchInfo.text = $"{matchInfo.text} (Winner: {contestantA.name})";
                        nextBracket.Add(contestantA);
                    }
                    return;
                }
            }
            matchTimeLeft -= Time.deltaTime;
            if (matchTimeLeft <= 0f)
            {
                inMatchResult = true;
                matchTitle.text = $"Draw! Both Eliminated!";
            }
            else
            {
                matchTimer.text = $"Time Left: {Mathf.CeilToInt(matchTimeLeft)}";
            }
        }

        private void NextRound()
        {
            // proceed to next bracket
            Debug.Log("Next Round");
            ++currentRound;
            inMatch = false;
            contestantA.Clear();
            contestantB.Clear();
            // start next match
            StartMatch();
        }

        private void StartMatch()
        {
            if (!inMatch)
            {
                if (currentContestant.Count == 0)
                {
                    isOver = true;
                    matchTitle.text = $"No Winner!";
                    OutputResultToFile();
                    return;
                }
                else if (currentContestant.Count == 1)
                {
                    isOver = true;
                    contestantA = currentContestant[0];
                    contestantA.transform.position = Vector3.zero;
                    contestantA.Ready(contestantB.GetAutoBot(), Color.blue);
                    foreach (var script in contestantA.GetComponents<MonoBehaviour>())
                    {
                        script.enabled = false;
                    }
                    contestantA.gameObject.SetActive(true);
                    matchTitle.text = $"Winner!<br>{contestantA.name}";
                    matchInfo.text = $"{matchInfo.text}<br>{currentRound + 1}. {contestantA.name} Wins!)";

                    OutputResultToFile();
                    return;
                }
                var contestantCount = currentContestant.Count;
                var currentIndex = currentRound * 2;
                if (contestantCount <= currentIndex)
                {
                    // proceed to next bracket
                    Debug.Log("Next Bracket");
                    // Randomly add the contestant in next bracket to current bracket
                    currentContestant = nextBracket;
                    currentRound = 0;
                    nextBracket = new List<AutoBotContestant>();
                    matchInfo.text = $"{matchInfo.text}<br>Next Bracket!";
                    StartMatch();
                }
                else if (contestantCount <= currentIndex + 1)
                {
                    // walk over
                    Debug.Log("Walk Over");
                    nextBracket.Add(currentContestant[currentIndex]);
                    matchInfo.text = $"{matchInfo.text}<br>{currentRound + 1}. {currentContestant[currentIndex].name} Walkover!)";
                    ++currentRound;
                    StartMatch();
                }
                else
                {
                    inMatch = true;
                    // start match
                    contestantA = currentContestant[currentIndex];
                    contestantB = currentContestant[currentIndex + 1];
                    contestantA.transform.position = contestantAPos.position;
                    contestantA.transform.up = Vector3.right;
                    contestantB.transform.position = contestantBPos.position;
                    contestantB.transform.up = Vector3.left;
                    contestantA.Ready(contestantB.GetAutoBot(), Color.blue);
                    contestantB.Ready(contestantA.GetAutoBot(), Color.green);
                    contestantA.gameObject.SetActive(true);
                    contestantB.gameObject.SetActive(true);
                    matchTimeLeft = matchDuration;
                    matchTitle.text = $"Round {currentRound + 1}<br>{contestantA.name} vs {contestantB.name}";
                    Debug.Log("start match");
                    matchInfo.text = $"{matchInfo.text}<br>{currentRound + 1}. {contestantA.name} vs {contestantB.name})";
                }
            }
        }

        private void OutputResultToFile()
        {
            var filePath = Application.persistentDataPath + "/Result.log";
            using (var writer = new StreamWriter(filePath))
            {
                var info = matchInfo.text.Replace("<br>", "\n");
                writer.Write(info);
                Debug.Log("Saved new game data to: " + filePath);
            }
        }
    }
}