using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAnimationCallback : MonoBehaviour
{
    Character Owner;
    public void Callback()
    {
        if(Owner == null)
        {
            Owner = transform.parent.GetComponent<Character>();
        }

        Owner.SwapSpriteOut();
    }
}
