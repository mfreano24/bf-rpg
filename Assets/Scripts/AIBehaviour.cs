using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIBehaviour : MonoBehaviour
{
    //Takes in the character's data and spits out which ability index to use.
    public int TakeTurn(Character c)
    {
        //im not implementing real AI yet so heres something that picks a random ability to use
        int rand_pick = Random.Range(0, c.abilities.Count);

        return rand_pick;
    }
}
