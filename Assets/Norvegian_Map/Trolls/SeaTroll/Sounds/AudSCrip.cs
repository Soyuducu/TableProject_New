using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudSCrip : MonoBehaviour
{

    public void waterfalling() {
        GetComponents<AudioSource>()[1].Play();
    }
    public void roar() {

        GetComponents<AudioSource>()[0].Play();
    }
    public void BackgroundSound()
    {

        GetComponents<AudioSource>()[2].Play();
    }
}
