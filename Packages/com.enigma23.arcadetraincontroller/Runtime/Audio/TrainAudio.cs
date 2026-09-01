// Copyright (c) 2026 Enigma 23. All rights reserved.

using e23.Common.Physics;
using e23.Common.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AudioType = e23.Common.AudioType;

namespace e23.TrainController.Audio
{
    [RequireComponent(typeof(TrainBehaviour))]
    public class TrainAudio : MonoBehaviour
    {
        [SerializeField] protected List<AudioData> _audioData = null;
        
        protected TrainBehaviour _trainBehaviour;
        protected Dictionary<AudioType, AudioSource> _audioSources;
        protected Dictionary<string, AudioSource> _customAudioSources;
        
        protected float _defaultPitch;
        protected WaitForSeconds _waitForSeconds;
        protected CollisionManager _collisionManager;
        
        protected virtual void Awake()
        {
            GetRequiredComponents();
            AddAudioSources();
            RegisterActions(true);
        }

        protected virtual void GetRequiredComponents()
        {
            _trainBehaviour = GetComponent<TrainBehaviour>();
            _collisionManager = GetComponentInParent<CollisionManager>();
        }

        protected virtual void RegisterActions(bool register)
        {
            _trainBehaviour.OnStartEngine -= PlayEngine;
            if (_collisionManager != null)
            {
                _collisionManager.OnVehicleCollisionEnter -= PlayClipOnCollision;
            }

            if (register == false) { return; }

            _trainBehaviour.OnStartEngine += PlayEngine;
            if (_collisionManager != null)
            {
                _collisionManager.OnVehicleCollisionEnter += PlayClipOnCollision;
            }
        }

        protected virtual void PlayEngine(bool enable)
        {
            if (enable)
            {
                if (_audioSources.ContainsKey(AudioType.EngineStart))
                {
                    _waitForSeconds = new WaitForSeconds(GetClip(AudioType.EngineStart).length);
                    PlayClip(AudioType.EngineStart);
                    StartCoroutine(PlayEngineClipAfterDelay(AudioType.EngineRunning));
                    return;
                }

                PlayClip(AudioType.EngineRunning);
                _trainBehaviour.InvokeEngineStarted(true);
            }
            else
            {
                _audioSources[AudioType.EngineRunning].Stop();
                _trainBehaviour.InvokeEngineStarted(false);
                if (_audioSources.ContainsKey(AudioType.EngineStart)) { PlayClip(AudioType.EngineOff); }
            }
        }

        protected virtual void AddAudioSources()
        {
            _audioSources ??= new Dictionary<AudioType, AudioSource>();

            _audioData.ForEach(data =>
            {
                if (data.AudioType == AudioType.Custom)
                {
                    _customAudioSources ??= new Dictionary<string, AudioSource>();
                    _customAudioSources.Add(data.AudioID, gameObject.AddComponent<AudioSource>());
                    SetupAudioSource(_customAudioSources[data.AudioID], data);
                }
                else
                {
                    _audioSources.Add(data.AudioType, gameObject.AddComponent<AudioSource>()); 
                    SetupAudioSource(_audioSources[data.AudioType], data);
                }
                
                if (data.AudioType == AudioType.EngineRunning) { _defaultPitch = data.Pitch; }
            });
        }

        protected virtual void SetupAudioSource(AudioSource audioSource, AudioData data)
        {
            audioSource.clip = data.AudioClip;
            audioSource.outputAudioMixerGroup = data.AudioMixerGroup;
            audioSource.playOnAwake = data.PlayOnAwake;
            audioSource.loop = data.Loop;
            audioSource.priority = data.Priority;
            audioSource.volume = data.Volume;
            audioSource.pitch = data.Pitch;
            audioSource.panStereo = data.StereoPan;
            audioSource.spatialBlend = data.SpatialBlend;
            audioSource.reverbZoneMix = data.ReverbZoneMix;

            audioSource.dopplerLevel = data.DopplerLevel;
            audioSource.spread = data.Spread;
            audioSource.rolloffMode = data.AudioRollOff;
            audioSource.minDistance = data.MinDistance;
            audioSource.maxDistance = data.MaxDistance;
        }
        
        protected virtual void Update()
        {
            if (_trainBehaviour.EngineRunning == true)
            {
                float extra = 0f;
                // if (_trainBehaviour.OnGround == false) { extra = Mathf.Lerp(extra, 3, Time.deltaTime * 50f); }
                // else 
                if (extra > 0f) { extra = Mathf.Lerp(extra, 0f, Time.deltaTime * 12f); }
                
                float normalisedSpeed = _trainBehaviour.CurrentSpeed / _trainBehaviour.MaxSpeed;
                _audioSources[AudioType.EngineRunning].pitch = _defaultPitch + normalisedSpeed + extra;
            }
        }

        protected virtual void PlayClip(AudioType audioType)
        {
            if (_audioSources.ContainsKey(audioType) == false)
            {
                Debug.LogWarning($"Attempted to play vehicle audio of type {audioType}, no clip found.", gameObject);
                return;
            }
            _audioSources[audioType].Play();
        }

        protected virtual void PlayClip(string audioID)
        {
            if (_customAudioSources.ContainsKey(audioID) == false)
            {
                Debug.LogWarning($"Attempted to play vehicle audio of type {audioID}, no clip found.", gameObject);
                return;
            }
            _customAudioSources[audioID].Play();
        }

        protected virtual IEnumerator PlayEngineClipAfterDelay(AudioType audioType)
        {
            yield return _waitForSeconds;
            _trainBehaviour.InvokeEngineStarted(true);
            PlayClip(audioType);
        }

        protected virtual void PlayClipOnCollision(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < 15f)
            { return; }
            
            float velocityDot = Vector3.Dot(collision.GetContact(0).normal, collision.relativeVelocity);
            float upDot = Vector3.Dot(collision.GetContact(0).normal, Vector3.up);
            
            if ((collision.relativeVelocity.magnitude <= 0.1f && upDot == 1f) || (velocityDot >= -0.21f && velocityDot <= 0.21f && upDot != 0f))
            {
                return;
            }
            
            if (collision.gameObject.TryGetComponent(out AudioTag audioTag))
            {
                if (_customAudioSources == null || _customAudioSources.ContainsKey(audioTag.ID) == false)
                {
                    Debug.LogWarning($"Audio clip with ID {audioTag.ID} has not been added to VehicleAudio", gameObject);
                }
                else
                {
                    PlayClip(audioTag.ID);
                    return;
                }
            }

            PlayClip(AudioType.Collision);
        }
        
        protected virtual void StopClip(AudioType audioType) => _audioSources[audioType].Stop();

        protected virtual AudioClip GetClip(AudioType audioType)
        {
            foreach (AudioData data in _audioData)
            {
                if (data.AudioType == audioType)
                {
                    return data.AudioClip;
                }
            }

            Debug.LogWarning($"Audio clip for {audioType} not found in list of Audio Data, please make sure it's assigned.", gameObject);
            return null;
        }
        
        protected virtual AudioSource GetAudioSource(string id)
        {
            foreach (AudioData data in _audioData)
            {
                if (string.Compare("", id) == 0)
                {
                    return _audioSources[data.AudioType];
                }
            }

            Debug.LogWarning($"Audio ID {id} not found in list of Audio Data, please make sure it's assigned and named correctly", gameObject);
            return null;
        }

        public virtual void AddAudioData(AudioData newData)
        {
            if (_audioData == null) { _audioData = new List<AudioData>(); }
            _audioData.Add(newData);
        }
    }
}