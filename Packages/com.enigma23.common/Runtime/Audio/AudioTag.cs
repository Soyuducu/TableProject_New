// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;

namespace e23.Common.Audio
{
    public class AudioTag : MonoBehaviour
    {
        [SerializeField] private string audioID = "";

        public string ID => audioID;
    }
}