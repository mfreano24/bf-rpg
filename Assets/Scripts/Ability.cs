using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum EReceiverType
{
    Self,
    OneEnemy,
    AllEnemies,
    OneAlly,
    AllAllies,
    Summons
}

[System.Serializable]
public enum EApplicationType
{
    Damage,
    Healing,
    Status
}

[System.Serializable]
public enum EStat
{
    STR,
    DEF,
    MAG,
    HEA,
    AGL
}

public enum ESpecialBehaviour
{
    Summon,
    Gamble,
    Song,
    Charge,
    DrainBP,
    None
}

[System.Serializable]
public class AbilityEffect
{
    public EApplicationType ApplicationType = EApplicationType.Damage;
    public EStat TargetStat = EStat.STR;
    public EStat CalcStat = EStat.STR;

    [Tooltip("For attacks/healing: potency of the attack. For status effect moves: +/-Potency stages")]
    public int Potency = 100;
}

[CreateAssetMenu]
public class Ability : ScriptableObject
{
    public string DisplayName = "";
    [TextArea(3, 5)]
    public string Message = "{NAME} does something...";
    public string AnimationStateName = "Attack";
    public EReceiverType ReceiverType = EReceiverType.Self;
    public List<AbilityEffect> AbilityEffects;

    [Tooltip("BP cost to use this move.")]
    public int BPCost = 10;

    [Header("Special Behaviors")]
    public ESpecialBehaviour SpecialBehavior = ESpecialBehaviour.None;
    public int TurnsToHold = 0;
    [Tooltip("From 0.0 to 1.0 - 1.0 meaning 100% chance")]
    public float ChanceToProc = 1.0f;
    public int ProcPotency = 100;

    public void ExecuteBehaviors(Character character, Character target = null)
    {
        List<Character> targets = new List<Character>();
        if (ReceiverType == EReceiverType.Self)
        {
            target = character;
            targets.Add(target);
        }
        else if (ReceiverType == EReceiverType.AllAllies)
        {
            BattleManager.Instance.GetAllCharactersOnSide(character.isAI, out targets);
        }
        else if (ReceiverType == EReceiverType.AllEnemies)
        {
            BattleManager.Instance.GetAllCharactersOnSide(!character.isAI, out targets);
        }
        else if (ReceiverType == EReceiverType.Summons)
        {
            //TODO: summons
        }
        else if (target != null)
        {
            //single target, chosen
            targets.Add(target);
        }

        string log = Message;
        log = log.Replace("{NAME}", character.CharacterKey);
        if(target != null)
        {
            log = log.Replace("{TARGET}", target.CharacterKey); //SHOULD only be on single-targets
        }
        
        DialogueManager.Instance.CallNewDialogue(log);

        character.PlayAnimationForSprite(AnimationStateName);
        foreach (AbilityEffect Effect in AbilityEffects)
        {
            ExecuteBehavior(Effect, character, targets);
        }
    }

    //target only matters if applicable otherwise whoooo cares
    void ExecuteBehavior(AbilityEffect Effect, Character character, List<Character> targets)
    {
        switch (Effect.ApplicationType)
        {
            case EApplicationType.Damage:
                foreach(Character t in targets)
                {
                    AttackBehavior(Effect,character, t);
                }
                break;
            case EApplicationType.Healing:
                foreach (Character t in targets)
                {
                    HealingBehavior(Effect, character, t);
                }
                break;
            case EApplicationType.Status:
                foreach (Character t in targets)
                {
                    StatusBehavior(Effect, character, t);
                }
                break;
        }
    }

    EStat GetDefendingStat(EStat CStat)
    {
        switch(CStat)
        {
            case EStat.STR:
                return EStat.DEF;
            case EStat.MAG:
                return EStat.HEA;
            default:
                return EStat.DEF;
        }
    }

    void AttackBehavior(AbilityEffect Effect, Character character, Character target = null)
    {
        if (target == null || target.bIsDead) return;

        int amount = (int)(GetStatValueForCharacter(character, Effect.CalcStat) * Effect.Potency / (100 + GetStatValueForCharacter(target, GetDefendingStat(Effect.CalcStat)))); //dota formula!
        DamageTypeContainer NewEntry = new DamageTypeContainer();
        NewEntry.CauserName = character.CharacterKey;
        NewEntry.TargetName = target.CharacterKey;
        NewEntry.AnimationName = AnimationStateName;
        NewEntry.bHealing = false;
        NewEntry.Amount = amount;

        DialogueManager.Instance.CallNewDialogue(target.CharacterKey + " takes " + amount + " damage!", false, NewEntry);
        //target.TakeDamage(Mathf.Max(amount, 0), false);
    }

    void HealingBehavior(AbilityEffect Effect, Character character, Character target = null)
    {
        if (target == null || target.bIsDead) return;

        int amount = -(int)(GetStatValueForCharacter(character, Effect.CalcStat) * Effect.Potency / 100); //dota formula? kinda? what's the "defending" stat on calculating heals
        DamageTypeContainer NewEntry = new DamageTypeContainer();
        NewEntry.CauserName = character.CharacterKey;
        NewEntry.TargetName = target.CharacterKey;
        NewEntry.AnimationName = AnimationStateName;
        NewEntry.bHealing = false;
        NewEntry.Amount = amount;

        DialogueManager.Instance.CallNewDialogue(target.CharacterKey + " heals " + -amount + " HP!", false, NewEntry);
        //target.TakeDamage(Mathf.Min(amount, 0), true);
    }

    void StatusBehavior(AbilityEffect Effect, Character character, Character target = null) 
    {
        if (target == null || target.bIsDead) return;

        target.StatModifier(Effect.TargetStat, Effect.Potency);
       
        DialogueManager.Instance.CallNewDialogue(target.CharacterKey + (Effect.Potency > 0 ? " raises" : " lowers") + " their " + Effect.TargetStat.ToString() + " by " + Mathf.Abs(Effect.Potency));
    }

    void MultiTurnBehaviour()
    {
        //anything else that might not behave normally, or would otherwise force a character to forgo their turn
        /* TODO FOR THIS:
         * Multi-turn "charging" - FDrank spells
         * Songs - Yoplin
         */
    }

    void SummonBehaviour()
    {
        //Summons - this is mainly functions just for cats
    }

    int GetStatValueForCharacter(Character character, EStat stat)
    {
        switch(stat)
        {
            case EStat.STR:
                return character.STR;
            case EStat.DEF:
                return character.DEF;
            case EStat.MAG:
                return character.MAG;
            case EStat.HEA:
                return character.HEA;
            case EStat.AGL:
                return character.AGL;
            default:
                return -1;
        }
    }
}
