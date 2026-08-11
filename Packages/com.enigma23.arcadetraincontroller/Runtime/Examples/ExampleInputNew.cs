// Copyright (c) 2026 Enigma 23. All rights reserved.

using UnityEngine;
using UnityEngine.InputSystem;

namespace e23.TrainController
{
    public class ExampleInputNew : MonoBehaviour
    {
        [SerializeField] protected TrainBehaviour _trainBehaviour;
#if ENABLE_INPUT_SYSTEM
        protected TrainInputActions _trainActions;
#endif
        protected bool _isThrottleUp = false;
        protected bool _isThrottleDown = false;

        public TrainBehaviour TrainBehaviour { get => _trainBehaviour; set => _trainBehaviour = value; }

        protected virtual void Awake() => _trainActions = new TrainInputActions();
        protected virtual void OnEnable() => RegisterActions(true);
        protected virtual void OnDisable() => RegisterActions(false);

        protected virtual void RegisterActions(bool register)
        {
#if ENABLE_INPUT_SYSTEM
            _trainActions.Train.IncreaseThrottle.performed -= ThrottleUp;
            _trainActions.Train.IncreaseThrottle.canceled -= ThrottleUp;
            _trainActions.Train.DecreaseThrottle.performed -= ThrottleDown;
            _trainActions.Train.DecreaseThrottle.canceled -= ThrottleDown;
            _trainActions.Train.Stop.performed -= StopTrain;
            _trainActions.Train.Disable();

            if (register == false) { return; }

            _trainActions.Train.IncreaseThrottle.performed += ThrottleUp;
            _trainActions.Train.IncreaseThrottle.canceled += ThrottleUp;
            _trainActions.Train.DecreaseThrottle.performed += ThrottleDown;
            _trainActions.Train.DecreaseThrottle.canceled += ThrottleDown;
            _trainActions.Train.Stop.performed += StopTrain;
            _trainActions.Train.Enable();
#endif
        }
#if ENABLE_INPUT_SYSTEM                
        protected virtual void ThrottleUp(InputAction.CallbackContext context) 
        {
            if (context.performed == true) { _isThrottleUp = true; }
            else if (context.canceled == true) { _isThrottleUp = false; }
        }
        protected virtual void ThrottleDown(InputAction.CallbackContext context)
        {
            if (context.performed == true) { _isThrottleDown = true; }
            else if (context.canceled == true) { _isThrottleDown = false; }
        }
        private void StopTrain(InputAction.CallbackContext context) => _trainBehaviour.StopMoving(1.5f);
#endif
        protected virtual void Update()
        {
            if (_isThrottleUp) { _trainBehaviour.IncreaseThrottle(); }
            else if (_isThrottleDown) { _trainBehaviour.DecreaseThrottle(); }
        }
    }
}