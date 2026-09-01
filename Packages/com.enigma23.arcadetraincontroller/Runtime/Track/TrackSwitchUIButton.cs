// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace e23.TrainController
{
    public class TrackSwitchUIButton : MonoBehaviour
    {
        [SerializeField] protected List<TrackPathManager> _trackPathManager;
        [SerializeField] protected Button _button;

        public virtual void AddTrackPathManager(TrackPathManager trackPathManager)
        {
            if (_trackPathManager == null) { _trackPathManager = new List<TrackPathManager>(); }
            _trackPathManager.Add(trackPathManager);
        }

        protected virtual void Awake() => GetRequiredComponents();
        protected virtual void OnEnable() => RegisterActions(true);
        protected virtual void OnDisable() => RegisterActions(false);

        protected virtual void GetRequiredComponents()
        {
            if (_button == null)
            {
                try
                {
                    _button = GetComponent<Button>();
                }
                catch (System.Exception)
                {
                    Debug.LogWarning($"Button component not found. Please assign a button to {gameObject.name}.", gameObject);
                    throw;
                }
            }
        }

        protected virtual void RegisterActions(bool register)
        {
            _button.onClick.RemoveListener(SwitchTrack);

            if (register == false) { return; }

            _button.onClick.AddListener(SwitchTrack);
        }

        protected virtual void SwitchTrack() { }
    }
}