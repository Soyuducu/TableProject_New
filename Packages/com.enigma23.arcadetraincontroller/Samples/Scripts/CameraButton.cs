// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;
using UnityEngine.UI;

namespace e23.TrainController.Demo
{
    public class CameraButton : MonoBehaviour
    {
        [SerializeField] private int _camIndex = 0;

        private Button _button;
        private CameraController _cameraController;

        private void Awake() => GetRequiredComponents();
        private void OnEnable() => RegisterActions(true);
        private void OnDisable() => RegisterActions(false);

        private void GetRequiredComponents()
        {
            _button = GetComponent<Button>();
            _cameraController = GetComponentInParent<CameraController>();
        }

        private void RegisterActions(bool register)
        {
            _button.onClick.RemoveListener(ChangeCam);

            if (register == false) { return; }

            _button.onClick.AddListener(ChangeCam);
        }

        private void ChangeCam() => _cameraController.ChangeActiveCamera(_camIndex);
    }
}