// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace e23.TrainController.Demo
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _cameras;

        public void ChangeActiveCamera(int index)
        {
            foreach (GameObject cam in _cameras)
            {
                cam.SetActive(false);
            }

            _cameras[index].SetActive(true);
        }
    }
}