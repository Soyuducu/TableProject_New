// Copyright (c) 2026 Enigma 23. All rights reserved.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace e23.TrainController.Editor
{
    public class TrainPrefabBuilderEditorWindow : EditorWindow
    {
        [MenuItem("Tools/e23/ATC/Train Prefab Builder")]
        public static void DisplayWindow()
        {
            TrainPrefabBuilderEditorWindow wnd = GetWindow<TrainPrefabBuilderEditorWindow>();
            wnd.titleContent = new GUIContent("TrainPrefabBuilderEditorWindow");
        }

        private TrainBuilderSettings _trainBuilderSettings;
        private SerializedObject _serializedSettings;

        private void OnEnable() => FindTrainBuilderSettings();
        private void OnDisable() => ApplyAndSave();

        private void ApplyAndSave()
        {
            if (_serializedSettings == null) { return; }

            _serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(_trainBuilderSettings);
            AssetDatabase.SaveAssets();
        }

        private void FindTrainBuilderSettings()
        {
            string settingsType = "t:" + nameof(TrainBuilderSettings);
            string[] guids = AssetDatabase.FindAssets(settingsType);

            if (guids.Length == 0)
            {
                TrainBuilderSettings newSettings = CreateInstance<TrainBuilderSettings>();
                AssetDatabase.CreateAsset(newSettings, "Assets/e23/ArcadeTrainController/Scripts/Editor/TrainBuilderSettings.asset");
                _trainBuilderSettings = newSettings;
            }
            else
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    _trainBuilderSettings = (TrainBuilderSettings) AssetDatabase.LoadAssetAtPath(path, typeof(TrainBuilderSettings));
                }
            }

            _serializedSettings = new SerializedObject(_trainBuilderSettings);
        }

        public void CreateGUI()
        {
            _serializedSettings.Update();

            VisualElement root = rootVisualElement;
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.marginBottom = 5;
            container.style.marginTop = 5;
            container.style.marginLeft = 5;
            container.style.marginRight = 5;

            PropertyField prefabName = new PropertyField(_serializedSettings.FindProperty("_trainName"), "Train Name:");
            PropertyField trainModel = new PropertyField(_serializedSettings.FindProperty("_trainModel"), "Model:");
            PropertyField bodyName = new PropertyField(_serializedSettings.FindProperty("_bodyName"), "Body:");
            PropertyField trainSettings = new PropertyField(_serializedSettings.FindProperty("_trainBehaviourSettings"), "Train Settings:");

            PropertyField wheelNamesField = new PropertyField(_serializedSettings.FindProperty("_wheelNames"), "Wheel Names:");
            wheelNamesField.style.marginBottom = 4;

            SerializedProperty addCarriagesProp = _serializedSettings.FindProperty("_addCarriages");
            PropertyField carriagesToggle = new PropertyField(addCarriagesProp, "Add Carriages:");

            VisualElement carriageHidden = new VisualElement();
            carriageHidden.style.display = addCarriagesProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;

            PropertyField carriagesField = new PropertyField(_serializedSettings.FindProperty("_carriages"), "Carriage Model(s):");
            carriagesField.style.marginBottom = 4;

            PropertyField carriageWheelNamesField = new PropertyField(_serializedSettings.FindProperty("_carriageWheelNames"), "Carriage Wheel Names:");
            carriageWheelNamesField.style.marginBottom = 4;

            carriageHidden.Add(carriagesField);
            carriageHidden.Add(carriageWheelNamesField);

            carriagesToggle.RegisterValueChangeCallback(evt =>
            {
                _serializedSettings.ApplyModifiedProperties();
                carriageHidden.style.display = _serializedSettings.FindProperty("_addCarriages").boolValue == true ? DisplayStyle.Flex : DisplayStyle.None;
            });

            VisualElement additionalLabel = new Label("Additional");
            additionalLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            additionalLabel.style.marginTop = 6;

            PropertyField colliderToggle = new PropertyField(_serializedSettings.FindProperty("_addColliders"), "Add Colliders:");

            SerializedProperty addExampleInputProp = _serializedSettings.FindProperty("_addExampleInput");
            PropertyField addInputToggle = new PropertyField(addExampleInputProp, "Add Example Input:");

            VisualElement inputRadioContainer = new VisualElement();
            inputRadioContainer.style.display = addExampleInputProp.boolValue == true ? DisplayStyle.Flex : DisplayStyle.None;

            SerializedProperty addNewInputProp = _serializedSettings.FindProperty("_addNewInput");
            RadioButtonGroup inputRadioGroup = new RadioButtonGroup();
            inputRadioGroup.Add(new RadioButton("New Input System"));
            inputRadioGroup.Add(new RadioButton("Old Input System"));
            inputRadioGroup.SetValueWithoutNotify(addNewInputProp.boolValue == true ? 0 : 1);

            inputRadioGroup.RegisterValueChangedCallback(evt =>
            {
                addNewInputProp.boolValue = evt.newValue == 0;
                _serializedSettings.ApplyModifiedProperties();
            });

            addInputToggle.RegisterValueChangeCallback(evt =>
            {
                _serializedSettings.ApplyModifiedProperties();
                inputRadioContainer.style.display = _serializedSettings.FindProperty("_addExampleInput").boolValue == true ? DisplayStyle.Flex : DisplayStyle.None;
            });

            inputRadioContainer.Add(inputRadioGroup);

            Button buildButton = new Button(BuildTrain)
            {
                text = "Build Train",
                style =
                {
                    width = Length.Percent(100),
                    height = 50
                }
            };

            container.Add(buildButton);
            container.Add(prefabName);
            container.Add(trainModel);
            container.Add(bodyName);
            container.Add(trainSettings);
            container.Add(wheelNamesField);
            container.Add(new Label("Carriages") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6 } });
            container.Add(carriagesToggle);
            container.Add(carriageHidden);
            container.Add(additionalLabel);
            container.Add(colliderToggle);
            container.Add(addInputToggle);
            container.Add(inputRadioContainer);

            root.Add(container);
            root.Bind(_serializedSettings);
        }

        private void BuildTrain()
        {
            GameObject trainParent = new GameObject(_trainBuilderSettings.TrainName);
            GameObject train = (GameObject) PrefabUtility.InstantiatePrefab(_trainBuilderSettings.TrainModel);
            train.transform.SetParent(trainParent.transform);
            train.transform.position = Vector3.zero;
            train.transform.rotation = Quaternion.identity;
            train.transform.localScale = Vector3.one;

            if (_trainBuilderSettings.AddCarriages) { BuildMultiTrain(trainParent); }
            else { BuildSingleTrain(trainParent); }
        }

        private void BuildSingleTrain(GameObject trainParent)
        {
            TrainBehaviour tb = trainParent.AddComponent<TrainBehaviour>();
            tb.TrainSettings = _trainBuilderSettings.TrainBehaviourSettings;

            if (_trainBuilderSettings.AddColliders)
            {
                Rigidbody trb = trainParent.AddComponent<Rigidbody>();
                trb.isKinematic = true;
                Transform colliderParent = SearchForPart(trainParent.transform, _trainBuilderSettings.BodyName);
                if (colliderParent != null) { AddCollider(colliderParent.gameObject); }
            }

            if (_trainBuilderSettings.WheelNames.Count > 0)
            {
                Wheels wheels = trainParent.AddComponent<Wheels>();
                wheels.TrainBehavour = tb;

                List<Transform> wheelTransforms = new List<Transform>();
                foreach (string wheelName in _trainBuilderSettings.WheelNames)
                {
                    Transform wheel = SearchForPart(trainParent.transform, wheelName);

                    if (wheel != null) { wheelTransforms.Add(wheel); }
                }

                wheels.TrainWheels = wheelTransforms;
            }

            if (_trainBuilderSettings.AddExampleInput == true)
            {
                if (_trainBuilderSettings.AddNewInput == true)
                {
                    ExampleInputNew newInput = trainParent.AddComponent<ExampleInputNew>();
                    newInput.TrainBehaviour = tb;
                }
                else
                {
                    ExampleInputOld oldInput = trainParent.AddComponent<ExampleInputOld>();
                    oldInput.TrainBehaviour = tb;
                }
            }
        }

        private void BuildMultiTrain(GameObject trainParent)
        {
            MultiTrainBehaviour mtb = trainParent.AddComponent<MultiTrainBehaviour>();
            mtb.TrainSettings = _trainBuilderSettings.TrainBehaviourSettings;

            GameObject trainModel = trainParent.transform.GetChild(0).gameObject;
            GameObject train = new GameObject("Train");
            train.transform.SetParent(trainParent.transform);
            train.transform.position = Vector3.zero;
            train.transform.rotation = Quaternion.identity;
            train.transform.localScale = Vector3.one;

            trainModel.transform.SetParent(train.transform);
            trainModel.transform.position = Vector3.zero;
            trainModel.transform.rotation = Quaternion.identity;
            trainModel.transform.localScale = Vector3.one;

            train.AddComponent<Carriage>();

            if (_trainBuilderSettings.AddColliders == true)
            {
                Rigidbody trb = train.AddComponent<Rigidbody>();
                trb.isKinematic = true;
                Transform colliderTransform = SearchForPart(trainParent.transform, _trainBuilderSettings.BodyName);
                if (colliderTransform != null) { AddCollider(colliderTransform.gameObject); }
            }

            if (_trainBuilderSettings.WheelNames.Count > 0)
            {
                Wheels wheels = trainParent.AddComponent<Wheels>();
                wheels.TrainBehavour = mtb;

                List<Transform> wheelTransforms = new List<Transform>();
                foreach (string wheelName in _trainBuilderSettings.WheelNames)
                {
                    Transform wheel = SearchForPart(train.transform, wheelName);

                    if (wheel != null) { wheelTransforms.Add(wheel); }
                }

                wheels.TrainWheels = wheelTransforms;
            }

            if (_trainBuilderSettings.AddCarriages == true)
            {
                List<Carriage> carriages = new List<Carriage>
                {
                    train.GetComponent<Carriage>()
                };

                for (int i = 0; i < _trainBuilderSettings.Carriages.Count; i++)
                {
                    string carriageNo = i < 10 ? $"0{i + 1}" : $"{i + 1}";
                    GameObject carriageParent = new GameObject($"Carriage_{carriageNo}");
                    Carriage cp = carriageParent.AddComponent<Carriage>();
                    carriageParent.transform.SetParent(trainParent.transform);

                    GameObject carriage = (GameObject) PrefabUtility.InstantiatePrefab(_trainBuilderSettings.Carriages[i]);
                    carriage.transform.SetParent(carriageParent.transform);
                    carriage.transform.position = Vector3.zero;
                    carriage.transform.rotation = Quaternion.identity;
                    carriage.transform.localScale = Vector3.one;

                    if (_trainBuilderSettings.AddColliders == true)
                    {
                        Rigidbody trb = carriageParent.AddComponent<Rigidbody>();
                        trb.isKinematic = true;
                        Transform colliderTransform = carriage.transform;
                        if (colliderTransform != null) { AddCollider(colliderTransform.gameObject); }
                    }

                    Wheels carriageWheels = carriageParent.AddComponent<Wheels>();
                    carriageWheels.TrainBehavour = mtb;

                    List<Transform> wheelTransforms = new List<Transform>();
                    foreach (string wheelName in _trainBuilderSettings.CarriageWheelNames)
                    {
                        Transform wheel = SearchForPart(carriageParent.transform, wheelName);

                        if (wheel != null) { wheelTransforms.Add(wheel); }
                    }

                    carriageWheels.TrainWheels = wheelTransforms;

                    carriages.Add(cp);
                }

                mtb.Carriages = carriages;
            }


            if (_trainBuilderSettings.AddExampleInput == true)
            {
                if (_trainBuilderSettings.AddNewInput == true)
                {
                    ExampleInputNew newInput = trainParent.AddComponent<ExampleInputNew>();
                    newInput.TrainBehaviour = mtb;
                }
                else
                {
                    ExampleInputOld oldInput = trainParent.AddComponent<ExampleInputOld>();
                    oldInput.TrainBehaviour = mtb;
                }
            }
        }

        private void AddCollider(GameObject colliderParent)
        {
            GameObject colliderObject = new GameObject("Collider");
            colliderObject.transform.SetParent(colliderParent.transform, false);
            BoxCollider boxCollider = colliderObject.AddComponent<BoxCollider>();

            Bounds bodyBounds = colliderParent.GetComponentInChildren<Renderer>().bounds;
            var renderers = colliderParent.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                bodyBounds.Encapsulate(renderer.bounds);
            }

            boxCollider.size = bodyBounds.size;

            float yRawPos = bodyBounds.center.y + bodyBounds.max.y;
            float yPos = bodyBounds.size.y < 1f ? yRawPos : yRawPos / Mathf.Floor(yRawPos);
            Vector3 boxPos = new Vector3(boxCollider.transform.localPosition.x, yPos, boxCollider.transform.localPosition.z);

            boxCollider.center = boxPos;
        }

        private Transform SearchForPart(Transform parent, string part)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>())
            {
                string name = t.name.ToLower();
                if (name.Contains(part.ToLower()) && t.parent != null)
                {
                    return t;
                }
            }

            Debug.LogError($"Part: {part} was not found, train build incomplete.");
            return null;
        }
    }
}