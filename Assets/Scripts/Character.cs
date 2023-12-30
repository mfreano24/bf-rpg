using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatContainer
{
    [SerializeField] public int MaxHP = 500;
    [SerializeField] public int MaxBP = 120;
    [SerializeField] public int STR = 100;
    [SerializeField] public int DEF = 100;
    [SerializeField] public int MAG = 100;
    [SerializeField] public int HEA = 100;
    [SerializeField] public int AGL = 100;
}

public class Character : MonoBehaviour
{
    public string KeyToLoad = "Frean";

    [Header("Config")]
    public string CharacterKey;
    public bool isAI = false;
    public StatContainer BaseStats;
    public int Level = 1;

    public List<Ability> abilities;
    public int TurnHoldCount { get; set; }
    public Ability HoldAbility = null;
    
    private StatContainer CalculatedStats;
    private int CurrentHP;
    private int[] StatModifiers = new int[]{ 0, 0, 0, 0, 0 }; //str def mag hea agl

    private AIBehaviour Brain;

    public bool bIsDead = false;

    #region Stat-Getters
    public int HP
    {
        get
        {
            return CurrentHP;
        }
    }

    public int MaxHP
    {
        get
        {
            return CalculatedStats.MaxHP;
        }
    }

    private int CurrentBP;
    public int BP
    {
        get
        {
            return CurrentBP;
        }
    }

    public int MaxBP
    {
        get
        {
            return CalculatedStats.MaxBP;
        }
    }

    public int STR
    {
        get
        {
            return CalculatedStats.STR + (int)(((float)StatModifiers[0] / 2) * CalculatedStats.STR);
        }
    }

    public int UnadjustedSTR
    {
        get
        {
            return CalculatedStats.STR;
        }
    }

    public int DEF
    {
        get
        {
            return CalculatedStats.DEF + (int)(((float)StatModifiers[1] / 2) * CalculatedStats.DEF);
        }
    }
    public int UnadjustedDEF
    {
        get
        {
            return CalculatedStats.DEF;
        }
    }

    public int MAG
    {
        get
        {
            return CalculatedStats.MAG + (int)(((float)StatModifiers[2] / 2) * CalculatedStats.MAG);
        }
    }

    public int UnadjustedMAG
    {
        get
        {
            return CalculatedStats.MAG;
        }
    }

    public int HEA
    {
        get
        {
            return CalculatedStats.HEA + (int)(((float)StatModifiers[3] / 2) * CalculatedStats.HEA);
        }
    }

    public int UnadjustedHEA
    {
        get
        {
            return CalculatedStats.HEA;
        }
    }

    public int AGL
    {
        get
        {
            return CalculatedStats.AGL + (int)(((float)StatModifiers[4] / 2) * CalculatedStats.AGL); ;
        }
    }

    public int UnadjustedAGL
    {
        get
        {
            return CalculatedStats.AGL;
        }
    }
    #endregion

    bool bActiveTurn = false;
    public bool ActiveTurn
    {
        get
        {
            return bActiveTurn;
        }
        set 
        { 
            bActiveTurn = value;
        }
    }

    bool bTurnTaken = false;

    //1/5 of a base stat per level
    public void Initialize(string Key)
    {
        if(!isAI)
        {
            //initialize with character data
            CharacterData cd = Globals.StoredCharacterDataByCharacterKey[Key];
            BaseStats = cd.BaseStats;
            CurrentHP = cd.CurrHP;
            CurrentBP = cd.CurrBP;
            CharacterKey = cd.CharacterKey;

            //load abilities
            abilities.Clear();
            abilities.Add(Resources.Load(cd.Ability1Path) as Ability);
            abilities.Add(Resources.Load(cd.Ability2Path) as Ability);
            abilities.Add(Resources.Load(cd.Ability3Path) as Ability);
            abilities.Add(Resources.Load(cd.Ability4Path) as Ability);
            BattleManager.Instance.RegisteredPlayers++;
        }

        CalculatedStats = new StatContainer();
        CalculatedStats.MaxHP = Globals.CalculateFromBase(BaseStats.MaxHP, Level);
        CalculatedStats.MaxBP = Globals.CalculateFromBase(BaseStats.MaxBP, Level);
        CalculatedStats.STR = Globals.CalculateFromBase(BaseStats.STR, Level);
        CalculatedStats.DEF = Globals.CalculateFromBase(BaseStats.DEF, Level);
        CalculatedStats.MAG = Globals.CalculateFromBase(BaseStats.MAG, Level);
        CalculatedStats.HEA = Globals.CalculateFromBase(BaseStats.HEA, Level);
        CalculatedStats.AGL = Globals.CalculateFromBase(BaseStats.AGL, Level);

        CurrentHP = CalculatedStats.MaxHP;
        CurrentBP = CalculatedStats.MaxBP;

        Brain = GetComponent<AIBehaviour>();
        UpdatePresentation(true);
    }

    public void ReplaceWithTagIn(string NewKey)
    {
        if (isAI) return;

        string old_key = CharacterKey;
        //initialize with character data
        CharacterData cd = Globals.StoredCharacterDataByCharacterKey[NewKey];
        BaseStats = cd.BaseStats;
        CurrentHP = cd.CurrHP;
        CurrentBP = cd.CurrBP;
        CharacterKey = cd.CharacterKey;

        //load abilities
        abilities.Clear();
        abilities.Add(Resources.Load(cd.Ability1Path) as Ability);
        abilities.Add(Resources.Load(cd.Ability2Path) as Ability);
        abilities.Add(Resources.Load(cd.Ability3Path) as Ability);
        abilities.Add(Resources.Load(cd.Ability4Path) as Ability);

        CalculatedStats = new StatContainer();
        CalculatedStats.MaxHP = Globals.CalculateFromBase(BaseStats.MaxHP, Level);
        CalculatedStats.MaxBP = Globals.CalculateFromBase(BaseStats.MaxBP, Level);
        CalculatedStats.STR = Globals.CalculateFromBase(BaseStats.STR, Level);
        CalculatedStats.DEF = Globals.CalculateFromBase(BaseStats.DEF, Level);
        CalculatedStats.MAG = Globals.CalculateFromBase(BaseStats.MAG, Level);
        CalculatedStats.HEA = Globals.CalculateFromBase(BaseStats.HEA, Level);
        CalculatedStats.AGL = Globals.CalculateFromBase(BaseStats.AGL, Level);

        UpdatePresentation(false);
    }

    private List<Character> Allies;
    private List<Character> Enemies;

    private void Start()
    {
        Initialize(KeyToLoad);
        Allies = new List<Character>();
        Enemies = new List<Character>();
    }

    public void TakeDamage(int amount, bool bIsHealing)
    {
        if (bIsDead) return;

        CurrentHP -= amount;

        if (CurrentHP <= 0)
        {
            bIsDead = true;
            CurrentHP = 0;
            DialogueManager.Instance.CallNewDialogue(CharacterKey + " died!");
        }

        if (CurrentHP > CalculatedStats.MaxHP)
        {
            CurrentHP = CalculatedStats.MaxHP;
        }

        if (!isAI)
        {
            UIManager.Instance.ModifyStatBarHP(CharacterKey, CurrentHP, CalculatedStats.MaxHP);
        }

        if(bIsHealing)
        {
            //healing effects
        }
        else
        {
            //damage effects
        }
    }

    public void LoseBP(int amount)
    {
        if (bIsDead) return;

        CurrentBP -= amount;

        if (CurrentBP > CalculatedStats.MaxBP)
        {
            CurrentBP = CalculatedStats.MaxBP;
        }

        UIManager.Instance.ModifyStatBarBP(CharacterKey, CurrentBP, CalculatedStats.MaxBP);
    }

    public void StatModifier(EStat stat, int amount)
    {
        switch(stat)
        {
            case EStat.STR:
                StatModifiers[0] += amount;
                if (StatModifiers[0] < -6) StatModifiers[0] = -6;
                if (StatModifiers[0] > 6) StatModifiers[0] = 6;
                break;
            case EStat.DEF:
                StatModifiers[1] += amount;
                if (StatModifiers[1] < -6) StatModifiers[1] = -6;
                if (StatModifiers[1] > 6) StatModifiers[1] = 6;
                break;
            case EStat.MAG:
                StatModifiers[2] += amount;
                if (StatModifiers[2] < -6) StatModifiers[2] = -6;
                if (StatModifiers[2] > 6) StatModifiers[2] = 6;
                break;
            case EStat.HEA:
                StatModifiers[3] += amount;
                if (StatModifiers[3] < -6) StatModifiers[3] = -6;
                if (StatModifiers[3] > 6) StatModifiers[3] = 6;
                break;
            case EStat.AGL:
                StatModifiers[4] += amount;
                if (StatModifiers[4] < -6) StatModifiers[4] = -6;
                if (StatModifiers[4] > 6) StatModifiers[4] = 6;
                break;
            default:
                break;
        }
    }

    bool CheckBPAffordable(Ability ability)
    {
        return ability.BPCost <= CurrentBP;
    }

    void DebugCharacter()
    {
        string log = CharacterKey + ": STR = " + STR + " DEF = " + DEF + " MAG = " + MAG + " HEA = " + HEA + " AGL = " + AGL;
        Debug.Log(log);
    }

    public void StartTurn()
    {
        DebugCharacter();

        if(Allies.Count == 0)
        {
            BattleManager.Instance.GetAllCharactersOnSide(isAI, out Allies);
        }

        if(Enemies.Count == 0)
        {
            BattleManager.Instance.GetAllCharactersOnSide(!isAI, out Enemies);
        }

        if (bIsDead)
        {
            DialogueManager.Instance.CallNewDialogue(CharacterKey + " is dead.");
            bActiveTurn = false;
            bTurnTaken = true;
            return;
        }

        if (TurnHoldCount > 0)
        {
            TurnHoldCount--;
            if (HoldAbility != null)
            {
                //song
            }

            if (TurnHoldCount == 0 && HoldAbility.SpecialBehavior == ESpecialBehaviour.Charge)
            {
                //execute charge ability now!
            }

            return;
        }

        bActiveTurn = true;

        DialogueManager.Instance.CallNewDialogue(CharacterKey + "'s Turn!");
        if(!isAI)
        {
            UIManager.Instance.UpdateAbilityNames(abilities);
            UIManager.Instance.PlayAbilityNameCardAnimation(true);
        }
    }

    Coroutine TargetSelectionCoroutine;
    bool bLockInput = false;

    IEnumerator WaitForTargetSelection(Ability ability)
    {
        bLockInput = true;
        yield return new WaitWhile(() => UIManager.Instance.bAwaitingTargetSelectionInput);
        bLockInput = false;

        UIManager.Instance.ToggleSelectors(false);
        Character target;
        UIManager.Instance.GetCharacterForSelectionInput(UIManager.Instance.LastTargetSelection, out target);
        ability.ExecuteBehaviors(this, target);
        bActiveTurn = false;
        bTurnTaken = true;
        UIManager.Instance.PlayAbilityNameCardAnimation(false);
    }

    void TurnLogic()
    {
        if(bLockInput)
        {
            return;
        }

        int AbilityIdx = 0;
        if(isAI)
        {
            if(Brain == null)
            {
                Debug.LogError("Didn't assign a brain to AI " + gameObject.name + "!!! Breaking everything...");
                return;
            }

            AbilityIdx = Brain.TakeTurn(this);
            if (CheckBPAffordable(abilities[AbilityIdx]))
            {
                CurrentBP -= abilities[AbilityIdx].BPCost;
                abilities[AbilityIdx].ExecuteBehaviors(this);
                bActiveTurn = false;
                bTurnTaken = true;
            }
        }

        int bMoved = 0;
        Ability ToUse = abilities[AbilityIdx];
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bMoved = 2;
            ToUse = Globals.AttackAbility;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            if (CheckBPAffordable(abilities[0]))
            {
                bMoved = 2;
                ToUse = abilities[0];
                LoseBP(abilities[0].BPCost);
            }
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            if (CheckBPAffordable(abilities[1]))
            {
                bMoved = 2;
                ToUse = abilities[1];
                LoseBP(abilities[1].BPCost);
            }
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (CheckBPAffordable(abilities[2]))
            {
                bMoved = 2;
                ToUse = abilities[2];
                LoseBP(abilities[2].BPCost);
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            if (CheckBPAffordable(abilities[3]))
            {
                bMoved = 2;
                ToUse = abilities[3];
                LoseBP(abilities[3].BPCost);
            }
        }

        //todo: Put this behind animations and playing things
        if (bMoved > 0)
        {
            if(bMoved == 1)
            {
                bActiveTurn = false;
                bTurnTaken = true;
            }
            else if (bMoved == 2)
            {
                if (ToUse.ReceiverType == EReceiverType.OneAlly || ToUse.ReceiverType == EReceiverType.OneEnemy)
                {
                    UIManager.Instance.SetupSelectors(ToUse.ReceiverType == EReceiverType.OneAlly ? Allies : Enemies);
                    TargetSelectionCoroutine = StartCoroutine(WaitForTargetSelection(ToUse));
                }
                else
                {
                    ToUse.ExecuteBehaviors(this);
                    bActiveTurn = false;
                    bTurnTaken = true;
                    UIManager.Instance.PlayAbilityNameCardAnimation(false);
                }
            }
        }
    }

    //presentation bits
    Animator SpriteAnimator;
    MeshRenderer MeshRenderer;

    void UpdatePresentation(bool bInitializing)
    {
        if (isAI) return;

        if(MeshRenderer == null)
        {
            MeshRenderer = GetComponent<MeshRenderer>();
        }

        MeshRenderer.material = Globals.CharacterToPresentationData[CharacterKey].PedestalMaterial;

        if(!bInitializing)
        {
            PlayAnimationForSprite("TagOut");
        }
    }

    public void SwapSpriteOut()
    {
        Debug.Log("Sprite swapped to " + CharacterKey + "'s!");
    }

    public void PlayAnimationForSprite(string StateName)
    {
        if (isAI) return;

        //get on request so we don't load this thing
        if(SpriteAnimator == null)
        {
            SpriteAnimator = transform.GetChild(0).GetComponent<Animator>();
        }

        SpriteAnimator.Play(StateName);
    }

    public void TagOutCall(string KeyToSwapWith)
    {
        if (!bActiveTurn || bTurnTaken) return;

        bActiveTurn = false;
        bTurnTaken = true;
        BattleManager.Instance.TagOutCharacter(this, KeyToSwapWith);
    }

    private void Update()
    {
        if(bActiveTurn && !bTurnTaken)
        {
            TurnLogic();
        }
    }

    public bool GetTurnFlag()
    {
        return bTurnTaken;
    }

    public void SetTurnFlag(bool bFlag)
    {
        bTurnTaken = bFlag;
    }

    public void UseBP(int Amt)
    {
        if(Amt <= CurrentBP) 
        {
            CurrentBP -= Amt;
        }
    }
}
