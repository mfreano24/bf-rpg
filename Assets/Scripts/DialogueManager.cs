using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DamageTypeContainer
{
    public string CauserName = "";
    public string TargetName = "";
    public int Amount = 0;
    public bool bHealing = false;
    public string AnimationName = "Attack";
}

public class DialogueManager : MonoBehaviour
{
    private static DialogueManager m_instance = null;
    public static DialogueManager Instance
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

    public TextMeshProUGUI DialogueText;
    public bool bDialogueLock = false;

    private Queue<(string, DamageTypeContainer)> DialoguePending;
    private bool bTextScrollFlag = false;
    private bool bTextScrollSkip = false;

    private void Start()
    {
        DialoguePending = new Queue<(string, DamageTypeContainer)>();
    }

    IEnumerator LockDialogue(string newDialogue, bool bFromQueue = false)
    {
        if (bDialogueLock)
        {
            yield break;
        }
        bDialogueLock = true;

        DialogueText.maxVisibleCharacters = 0;
        DialogueText.text = newDialogue;

        bTextScrollFlag = true;
        for (int i = 0; i < newDialogue.Length; i++)
        {
            DialogueText.maxVisibleCharacters++;
            if(bTextScrollSkip)
            {
                DialogueText.maxVisibleCharacters = newDialogue.Length;
                bTextScrollSkip= false;
                break;
            }

            yield return new WaitForSeconds(0.05f);
        }
        bTextScrollFlag = false;

        yield return new WaitForSeconds(0.75f);
        bDialogueLock = false;

        if(bFromQueue)
        {
            DialoguePending.Dequeue();
        }
    }

    public int PendingDialogueCount
    {
        get
        {
            return DialoguePending.Count;
        }
    }

    public void CallNewDialogue(string newDialogue, bool bFromQueue = false, DamageTypeContainer damageData = null)
    {
        if(bDialogueLock)
        {
            DialoguePending.Enqueue((newDialogue, damageData));
            return;
        }

        StartCoroutine(LockDialogue(newDialogue, bFromQueue));

        if (damageData != null)
        {
            Character target, causer;
            BattleManager.Instance.GetCharacterByName(damageData.TargetName, out target);
            BattleManager.Instance.GetCharacterByName(damageData.CauserName, out causer);
            target.TakeDamage(damageData.Amount, damageData.bHealing);
        }
    }

    private void Update()
    {
        if(!bDialogueLock && DialoguePending.Count > 0)
        {
            CallNewDialogue(DialoguePending.Peek().Item1, true, DialoguePending.Peek().Item2);
        }

        if(bTextScrollFlag && Input.GetKeyDown(KeyCode.Space))
        {
            bTextScrollSkip = true;
        }
    }
}
