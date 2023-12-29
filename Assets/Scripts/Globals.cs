using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class CharacterData
{
    public string CharacterKey;
    public int MaxHP;
    public int MaxBP;
    public int CurrHP;
    public int CurrBP;

    public int STR;
    public int DEF;
    public int MAG;
    public int HEA;
    public int AGL;

    public StatContainer BaseStats;

    public string Ability1Path;
    public string Ability2Path;
    public string Ability3Path;
    public string Ability4Path;

    public void DB()
    {
        Debug.Log("Character Data: " + CharacterKey + " - " + CurrHP + "/" + MaxHP);
    }
}

//this isnt for saving yet
public static class Globals
{
    public static Dictionary<string, CharacterData> StoredCharacterDataByCharacterKey;

    public static Ability AttackAbility;

    public static void Init()
    {
        StoredCharacterDataByCharacterKey = new Dictionary<string, CharacterData>();

        AttackAbility = Resources.Load("Abilities/Attack") as Ability;

        string path = "Assets/Resources/Data.txt";
        if (!File.Exists(path))
        {
            return;
        }

        using (StreamReader sr = File.OpenText(path))
        {
            while(!sr.EndOfStream)
            {
                string js = sr.ReadLine();
                CharacterData cd = new CharacterData();
                cd = DeserializeStr(js);
                StoredCharacterDataByCharacterKey[cd.CharacterKey] = cd;
            }
        }
    }

    public static string SerializeStr(CharacterData cd)
    {
        return JsonUtility.ToJson(cd);
    }

    public static CharacterData DeserializeStr(string JS)
    {
        CharacterData cd = new CharacterData();
        JsonUtility.FromJsonOverwrite(JS, cd); //this really shouldnt be so vague, unity.
        return cd;
    }

    public static void CreateAndStoreCharacterDataFor(Character c)
    {
        if(StoredCharacterDataByCharacterKey == null)
        {
            StoredCharacterDataByCharacterKey = new Dictionary<string, CharacterData>();
        }

        CharacterData cd = new CharacterData();
        cd.CharacterKey = c.CharacterKey;
        cd.MaxHP = c.MaxHP;
        cd.CurrHP = c.HP;
        cd.MaxBP = c.MaxBP;
        cd.CurrBP = c.BP;
        cd.STR = c.UnadjustedSTR;
        cd.DEF = c.UnadjustedDEF;
        cd.MAG = c.UnadjustedMAG;
        cd.HEA = c.UnadjustedHEA;
        cd.AGL = c.UnadjustedAGL;

        cd.BaseStats = c.BaseStats;

        cd.Ability1Path = "Abilities/" + c.CharacterKey + "/" + c.abilities[0].name.Replace(' ', '_');
        cd.Ability2Path = "Abilities/" + c.CharacterKey + "/" + c.abilities[1].name.Replace(' ', '_');
        cd.Ability3Path = "Abilities/" + c.CharacterKey + "/" + c.abilities[2].name.Replace(' ', '_');
        cd.Ability4Path = "Abilities/" + c.CharacterKey + "/" + c.abilities[3].name.Replace(' ', '_');

        StoredCharacterDataByCharacterKey[c.CharacterKey] = cd;
    }

    static float c = 1.05f;
    public static int CalculateFromBase(int BaseStat, int Level)
    {
        float f = BaseStat + (BaseStat / 100.0f) * Level;
        return (int)Mathf.Ceil(f);
    }
}
