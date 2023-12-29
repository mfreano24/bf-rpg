using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CharacterSaver : MonoBehaviour
{
    private void Start()
    {
        string path = "Assets/Resources/Data.txt";
        if (!File.Exists(path))
        {
            File.CreateText(path);
        }

        File.WriteAllText(path, "");
        Character[] carr = GameObject.FindObjectsOfType<Character>();

        using (StreamWriter sw = File.CreateText(path))
        {
            foreach (Character c in carr)
            {
                Globals.CreateAndStoreCharacterDataFor(c);
                string js = Globals.SerializeStr(Globals.StoredCharacterDataByCharacterKey[c.CharacterKey]);
                sw.WriteLine(js);
            }
        }
    }
}
