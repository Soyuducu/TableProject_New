using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audscr : MonoBehaviour
{
    /*bool f,c;
    float a, b;
    public bool d;
    recogn dd;
    Animator g;
    Animation fg;*/
    // Start is called before the first frame update
    void Start()
    {
        //dd = GameObject.FindObjectOfType<recogn>();
        
    }

    // Update is called once per frame
    void Update()
    {
        /*if (dd.d)
        {
            a += Time.deltaTime;
            if ((a >= 14.4f) & (!f))
            {
                GetComponent<AudioSource>().Play();
                f = true;
            }
            if (f)
            {
                b += Time.deltaTime;
            }
            if (b >= 20.350f)
            {
                GetComponent<AudioSource>().Play();
                b = 0;
            }
        }
        else {
            a = b = 0;
            f = false;
        }*/
        
    }
    public void audscrin()
    {
        if (GetComponents<AudioSource>()[0] != null)
            GetComponents<AudioSource>()[0].Play();
    }
    public void audscrin1()
    {
        if (GetComponents<AudioSource>()[1] != null)
            GetComponents<AudioSource>()[1].Play();
    }
}
