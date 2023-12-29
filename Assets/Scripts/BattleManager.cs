using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private static BattleManager m_instance = null;
    public static BattleManager Instance
    {
        get
        {
            return m_instance;
        }
        set
        {
            if(m_instance == null)
            {
                m_instance = value;
            }
        }
    }

    public List<Character> characters;
    [HideInInspector] public List<Character> TurnOrder;
    int Idx = 0;
    Coroutine BattleCoroutine;

    void Awake()
    {
        if(m_instance == null)
        {
            m_instance = this;
        }

        Globals.Init(); //initialize the character data!
    }

    public int RegisteredPlayers = 0;

    void Start()
    {
        StartCoroutine(WaitForBattleStart());
    }

    IEnumerator WaitForBattleStart()
    {
        yield return new WaitWhile(() => RegisteredPlayers < 4);
        StartBattle(); //is this a good idea. who knows.
    }

    public void StartBattle()
    {
        TurnOrder = new List<Character>();
        RegisterAllCharactersForBattle(); //TODO: need to guarantee this defers then calls when everythings set up
    }

    public void GetAllCharactersOnSide(bool bAI, out List<Character> outChars)
    {
        outChars = new List<Character>();
        foreach(Character c in characters)
        {
            if(c.isAI == bAI)
            {
                outChars.Add(c);
            }
        }
    }

    public void GetCharacterByName(string name, out Character character)
    {
        character = null;
        foreach(Character c in TurnOrder)
        {
            if(c.CharacterKey == name)
            {
                character = c;
            }
        }
    }

    public void GetCurrentTurnCharacter(out Character character)
    {
        character = TurnOrder[Idx % TurnOrder.Count];
    }

    void UpdateTurnOrder()
    {
        TurnOrder.Sort(delegate (Character c1, Character c2) { return c2.AGL.CompareTo(c1.AGL); });

        string LogTurnOrder = "Turn Order: ";
        foreach (Character c in TurnOrder)
        {
            c.SetTurnFlag(false);
            LogTurnOrder += c.CharacterKey + " -  AGL: " + c.AGL + " ";
        }

        Debug.Log(LogTurnOrder);
    }

    public void TagOutCharacter(Character CharacterSlot, string newCKey)
    {
        Globals.CreateAndStoreCharacterDataFor(CharacterSlot);
        string old_key = CharacterSlot.CharacterKey;
        if(Globals.StoredCharacterDataByCharacterKey.ContainsKey(newCKey))
        {
            CharacterSlot.ReplaceWithTagIn(newCKey);
        }
        else
        {
            Debug.LogWarning("Key " + newCKey + " isn't stored. Maybe somethings wrong with character JSON reading?");
            return;
        }

        UIManager.Instance.TagOutStatBars(old_key, CharacterSlot.CharacterKey, CharacterSlot.HP, CharacterSlot.MaxHP, CharacterSlot.BP, CharacterSlot.MaxBP);
        DialogueManager.Instance.CallNewDialogue(old_key + " tags out!");
        DialogueManager.Instance.CallNewDialogue(CharacterSlot.CharacterKey + " is here!");
    }

    void RegisterAllCharactersForBattle()
    {
        int NextStatBar = 0;
        foreach (Character c in characters)
        {
            TurnOrder.Add(c);

            if(!c.isAI)
            {
                UIManager.Instance.SetupStatBar_Init(NextStatBar++, c.CharacterKey, c.HP, c.MaxHP, c.BP, c.MaxBP);
            }
        }

        UpdateTurnOrder();
        BattleCoroutine = StartCoroutine(RunBattle());
    }

    bool VictoryCheck(out bool bPlayersWon)
    {
        int players = 0;
        int ai = 0;
        foreach(Character c in TurnOrder)
        {
            if(c.isAI && !c.bIsDead)
            {
                ai++;
            }
            else if(!c.bIsDead)
            {
                players++;
            }
        }

        bPlayersWon = (ai == 0);
        return ai == 0 || players == 0;
    }

    void OnTurnOrderEnd()
    {
        bool bPlayersWon;
        if(VictoryCheck(out bPlayersWon))
        {
            if(bPlayersWon)
            {
                Debug.Log("Boyfriend Barn wins!");
            }
            else
            {
                Debug.Log("Mudae wins!");
            }
            return;
        }

        UpdateTurnOrder();

        BattleCoroutine = StartCoroutine(RunBattle());
    }

    public IEnumerator RunBattle()
    {
        Idx = 0;
        while(Idx < TurnOrder.Count)
        {
            Character next = TurnOrder[Idx];
            next.StartTurn();

            yield return new WaitWhile(() => !next.GetTurnFlag());

            bool bPlayersWon;
            if (VictoryCheck(out bPlayersWon))
            {
                if (bPlayersWon)
                {
                    Debug.Log("Boyfriend Barn wins!");
                }
                else
                {
                    Debug.Log("Mudae wins!");
                }
                yield break;
            }

            next.SetTurnFlag(true);
            yield return new WaitWhile(() => DialogueManager.Instance.PendingDialogueCount > 0 && DialogueManager.Instance.bDialogueLock);
            Idx++;
        }

        OnTurnOrderEnd();
    }
}
