using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class StatBarModifiers
{
    public TextMeshProUGUI CharacterName;
    public Slider HPSlider;
    public Slider BPSlider;

    [Header("Numbers")]
    public TextMeshProUGUI HP_Value;
    public TextMeshProUGUI BP_Value;
}

public class UIManager : MonoBehaviour
{
    private static UIManager m_instance = null;
    public static UIManager Instance
    {
        get
        {
            return m_instance;
        }
        set
        {
            if (m_instance == null)
            {
                m_instance = value;
            }
        }
    }

    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }
    }

    void Update()
    {
        if(bAwaitingTargetSelectionInput)
        {
            if(Input.GetKeyDown(KeyCode.Alpha1) && SelectorNumbers[0].activeSelf)
            {
                LastTargetSelection = 0;
                bAwaitingTargetSelectionInput = false;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) && SelectorNumbers[1].activeSelf)
            {
                LastTargetSelection = 1;
                bAwaitingTargetSelectionInput = false;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) && SelectorNumbers[2].activeSelf)
            {
                LastTargetSelection = 2;
                bAwaitingTargetSelectionInput = false;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) && SelectorNumbers[3].activeSelf)
            {
                LastTargetSelection = 3;
                bAwaitingTargetSelectionInput = false;
            }
        }

        //tagout menu
        if(BattleManager.Instance.TurnOrder.Count > 0)
        {
            Character CurrentCharacter;
            BattleManager.Instance.GetCurrentTurnCharacter(out CurrentCharacter);
            if (CurrentCharacter != null)
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    ToggleTagOutMenu(!TagOutMenu.activeSelf);
                }
            }
        }
    }

    public List<TextMeshProUGUI> AbilityNames;
    public GameObject SelectorDisplay;

    public GameObject[] SelectorNumbers = new GameObject[4];
    public Character[] CurrentSelectionOptions = new Character[4];
    
    public bool bAwaitingTargetSelectionInput = false;
    public int LastTargetSelection = -1;

    public void ToggleSelectors(bool bToggle)
    {
        SelectorDisplay.SetActive(bToggle);
        if(bToggle)
        {
            bAwaitingTargetSelectionInput = true;
        }
    }

    public void MoveSelectorsTo(List<Character> potential_targets)
    {
        //TODO: given a list of potential targets, line up the numbers we have along them
        if(potential_targets.Count > 4)
        {
            Debug.LogError("Don't put more than 4 of each side please!");
            return;
        }

        for(int i = 0; i < potential_targets.Count; i++)
        {
            SelectorNumbers[i].transform.position = Camera.main.WorldToScreenPoint(potential_targets[i].transform.position);
        }

        for(int i = potential_targets.Count; i < 4; i++)
        {
            SelectorNumbers[i].SetActive(false);
        }
    }

    public void SetupSelectors(List<Character> potential_targets)
    {
        ToggleSelectors(true);
        for(int i = 0; i < 4; i++)
        {
            SelectorNumbers[i].SetActive(true); //reset them all
        }

        MoveSelectorsTo(potential_targets);
        CurrentSelectionOptions = potential_targets.ToArray();
    }

    public void GetCharacterForSelectionInput(int selection, out Character c)
    {
        c = null;

        if(selection >= CurrentSelectionOptions.Length || selection < 0)
        {
            return;
        }

        c = CurrentSelectionOptions[selection];
    }

    public void ClearSelectionData()
    {
        //this wont null refs i hope...
        for(int i = 0; i < 4; i++)
        {
            CurrentSelectionOptions[i] = null;
        }
    }

    #region StatBars
    public StatBarModifiers[] StatBars = new StatBarModifiers[4];
    public Dictionary<string, int> KeyToStatBar;
    Dictionary<int, Coroutine> StatBarCoroutines; //first = HP, second = BP

    public void TagOutStatBars(string ToReplace, string NewCharacter, int NewHP, int MaxHP, int NewBP, int MaxBP)
    {
        int Idx = KeyToStatBar[ToReplace];

        SetupStatBar_Init(Idx, NewCharacter, NewHP, MaxHP, NewBP, MaxBP);
    }

    public void SetupStatBar_Init(int Idx, string NewCharacter, int NewHP, int MaxHP, int NewBP, int MaxBP)
    {
        if(KeyToStatBar == null)
        {
            KeyToStatBar = new Dictionary<string, int>();
        }

        if(StatBarCoroutines == null)
        {
            StatBarCoroutines= new Dictionary<int, Coroutine>();
        }

        StatBars[Idx].CharacterName.text = NewCharacter;
        KeyToStatBar[NewCharacter] = Idx;

        ModifyStatBarHP(NewCharacter, NewHP, MaxHP);
        ModifyStatBarBP(NewCharacter, NewBP, MaxBP);

        StatBars[Idx].HP_Value.text = NewHP.ToString();
        StatBars[Idx].BP_Value.text = NewBP.ToString();
    }

    IEnumerator BarSlider(Slider slider, float NewValue = 0.0f)
    {
        float Init = slider.value;
        float Goal = NewValue;
        float Step = (NewValue - Init) / 20.0f;

        for (int i = 0; i < 20; i++)
        {
            slider.value += Step;
            yield return new WaitForSeconds(0.05f);
        }

        slider.value = Goal;
    }

    public void ModifyStatBarHP(string CharacterKey, int NewHP, int MaxHP)
    {
        int Idx = KeyToStatBar[CharacterKey];
        if(StatBarCoroutines.ContainsKey(Idx))
        {
            StopCoroutine(StatBarCoroutines[Idx]);
            StatBarCoroutines.Remove(Idx);
        }
        
        StatBarCoroutines.Add(Idx, StartCoroutine(BarSlider(StatBars[Idx].HPSlider, (float)NewHP / (float)MaxHP)));
        StatBars[Idx].HP_Value.text = NewHP.ToString();
    }

    public void ModifyStatBarBP(string CharacterKey, int NewBP, int MaxBP)
    {
        int Idx = KeyToStatBar[CharacterKey];
        StatBars[Idx].BPSlider.value = (float)NewBP / (float)MaxBP;
        StatBars[Idx].BP_Value.text = NewBP.ToString();
    }
    #endregion

    #region AbilityNames
    public Animator AbilityCardAnimator;

    public void UpdateAbilityNames(List<Ability> abilities)
    {
        for (int i = 0; i < 4; i++)
        {
            AbilityNames[i].text = abilities[i].DisplayName;
        }
    }

    public void PlayAbilityNameCardAnimation(bool bEntering)
    {
        return;
        //AbilityCardAnimator.Play(bEntering ? "Enter" : "Exit");
    }
    #endregion

    #region TagOutMenu
    public GameObject TagOutMenu;
    public GameObject ButtonParent;
    Dictionary<string, Button> NameToTagOutButton;
    void Init_NameToTagOutButtons()
    {
        NameToTagOutButton = new Dictionary<string, Button>();

        for(int i = 0; i < ButtonParent.transform.childCount; i++)
        {
            Transform ButtonObject = ButtonParent.transform.GetChild(i);

            Button thisButton = ButtonObject.GetComponent<Button>();
            if(thisButton != null)
            {
                NameToTagOutButton.Add(thisButton.name, thisButton);
            }
        }
    }

    void CleanTagOutButtons()
    {
        foreach(KeyValuePair<string, Button> p in NameToTagOutButton)
        {
            p.Value.interactable = true;
        }
    }

    void ToggleTagOutMenu(bool bToggle)
    {
        TagOutMenu.SetActive(bToggle);

        if(NameToTagOutButton == null)
        {
            Init_NameToTagOutButtons();
        }

        CleanTagOutButtons();

        List<Character> activeCharacters;
        BattleManager.Instance.GetAllCharactersOnSide(false, out activeCharacters);

        foreach(Character c in activeCharacters)
        {
            NameToTagOutButton[c.CharacterKey].interactable = false;
        }
    }

    public void MakeTagOutSelection(string CharacterKey)
    {
        Debug.Log("Selected " + CharacterKey);

        Character curr;
        BattleManager.Instance.GetCurrentTurnCharacter(out curr);
        if (curr.isAI)
        {
            return;
        }

        curr.TagOutCall(CharacterKey);
        ToggleTagOutMenu(false);
    }
    #endregion
}
