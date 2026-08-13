using System.IO;
using UnityEditor;
using UnityEngine;

namespace FirstCrowCode.FluidShader
{
    public class FluidShaderGUI : ShaderGUI
    {
        private static class Styles
        {
            //Flow Variables
            public static GUIContent flowText = new GUIContent("Flow Direction", "The primary direction of the fluid movement ranging from 0-360 degrees.");
            public static GUIContent surfacePatternSpeedText = new GUIContent("Surface Pattern Speed", "The speed of the pattern.");
            public static GUIContent subsurfacePatternSpeedText = new GUIContent("Subsurface Pattern Speed", "The speed of the pattern.");
            public static GUIContent intersectionPatternSpeedText = new GUIContent("Intersection Pattern Speed", "The speed of the pattern.");
            public static GUIContent scaleText = new GUIContent("Scale", "The overall scale of the fluid");
            // Wave Variables
            public static GUIContent wavesText = new GUIContent("Waves", "Enable Gerstner Waves (Vertex Displacment)");
            public static GUIContent wavelengthText = new GUIContent("Wavelength", "Wavelength of each wave");
            public static GUIContent waveSteepnessText = new GUIContent("Wave Steepness", "Determines wave shape. Larger values result in a steeper wave.");
            public static GUIContent waveSpeedText = new GUIContent("Wave Speed", "Determines speed of the waves.");
            public static GUIContent recalculateNormalText = new GUIContent("Recalculate Normals", "Allows waves to change the object's normals. Improves fresnel, but negatively impacts subsurface parallax");
            //Color Variables
            public static GUIContent intersectionOpacityText = new GUIContent("Intersection Opacity", "Opacity of the intersections.");
            public static GUIContent deepColorText = new GUIContent("Deep Color", "The color of the fluid at its deepest point.");
            public static GUIContent shallowColorText = new GUIContent("Shallow Color", "The color of the fluid at its shallowest point.");
            public static GUIContent subsurfaceColorText = new GUIContent("Subsurface Color", "The color of the subsurface patterns.");
            public static GUIContent surfaceColorText = new GUIContent("Surface Color", "The color of the surface patterns.");
            public static GUIContent intersectionColorText = new GUIContent("Intersection Color", "The color of the intersction outlines.");
            public static GUIContent fresnelPowerColorText = new GUIContent("Fresnel Power", "Determines how the fluid looks at steeper angles.");
            //Pattern Variables
            public static string[] surfacePatternNames = { "Flowing Ripples", "Static Ripples", "Cracked Rock", "Diamonds", "Hatched", "Uniform Dots", "Random Dots", "Custom Texture", "None" };
            public static string[] subsurfacePatternNames = { "Static Ripples", "Cracked Rock", "Diamonds", "Hatched", "Uniform Dots", "Random Dots", "Custom Texture", "None" };
            public static string[] intersectionPatternNames = { "Static Ripples", "Cracked Rock", "Diamonds", "Hatched", "Uniform Dots", "Random Dots", "Simple", "None" };
            public static GUIContent surfacePatternText = new GUIContent("Surface Pattern", "Choose which pattern is used for the surface.");
            public static GUIContent surfaceTextureText = new GUIContent("Surface Texture", "Custom texture to use for the surface. The field on the right controls the scale.");
            public static GUIContent surfacePatternScaleText = new GUIContent("Surface Pattern Scale", "Scale for the surface pattern");
            public static GUIContent surfaceCutoffText = new GUIContent("Surface Pattern Cutoff", "Range in which the pattern is cutoff using a noise. Set the min=max for a simple cutoff.");
            public static GUIContent surfaceDistortionText = new GUIContent("Surface Distortion", "UV distortion to keep the pattern from looking too static.");
            public static GUIContent subsurfacePatternText = new GUIContent("Subsurface Pattern", "Choose which pattern is used for the subsurface.");
            public static GUIContent subsurfaceTextureText = new GUIContent("Subsurface Texture", "Custom texture to use for the subsurface. The field on the right controls the scale.");
            public static GUIContent subsurfacePatternScaleText = new GUIContent("Subsurface Pattern Scale", "Scale for the subsurface pattern");
            public static GUIContent subsurfaceCutoffText = new GUIContent("Subsurface Pattern Cutoff", "Range in which the pattern is cutoff using a noise. Set the min=max for a simple cutoff.");
            public static GUIContent parallaxAmountText = new GUIContent("Parallax Amount", "Amount of parallax for the subsurface pattern. Adds depth.");
            public static GUIContent subsurfaceDistortionText = new GUIContent("Subsurface Distortion", "UV distortion to keep the pattern from looking too static.");
            public static GUIContent intersectionPatternText = new GUIContent("Intersection Pattern", "Choose which pattern is used for the intersections.");
            public static GUIContent intersectionPatternScaleText = new GUIContent("Intersection Pattern Scale", "Scale for the intersection pattern");
            public static GUIContent intersectionCutoffText = new GUIContent("Intersection Pattern Cutoff", "Range in which the pattern is cutoff using a noise. Set the min=max for a simple cutoff.");
            public static GUIContent lineDensityText = new GUIContent("Line Density", "Controls the number of lines that surround submerged objects.");
            public static GUIContent depthText = new GUIContent("Depth", "The visible depth of the fluid.");
            //Normal Variables
            public static GUIContent useNormalMapText = new GUIContent("Use Normal Map", "Toggles whether you will use a normal texture for the surface normals.");
            public static GUIContent normalMapText = new GUIContent("Normal Texture", "The texture used for surface normals. The field on the right controls the scale.");
            public static GUIContent normalMapScale = new GUIContent("Normal Map Scale", "Controls the scale of the normal map.");
            public static GUIContent normalStrengthText = new GUIContent("Normal Strength", "The texture used for the fluid surface detail.");
            //Rendering Variables
            public static GUIContent worldSpaceUVsText = new GUIContent("World Space UVs", "Determines whether you use UV or XZ-World Space to project the textures.");
            public static GUIContent lodFadeRangeText = new GUIContent("LOD Fade Range", "Distance at which patterns fade.");
        }

        static bool sectionFlowAndWaves = true;
        static bool sectionAppearance = true;
        static bool sectionPatterns = true;
        static bool sectionRendering = true;

        private MaterialProperty
            flowDir, waves, wavelength, waveSteepness, waveSpeed, recalculateNormals,
            deepColor, shallowColor, subsurfaceColor, surfaceColor, intersectionColor,
            deepColorHSV, shallowColorHSV, subsurfaceColorHSV, surfaceColorHSV, intersectionColorHSV,
            useNormalMap, normalMap, normalScale, normalStrength, depth,
            surfacePattern, surfacePatternType, surfaceTexture, surfaceCutoff, surfacePatternScale, surfacePatternSpeed,
            subsurfacePattern, subsurfacePatternType, subsurfaceTexture, subsurfaceCutoff, subsurfacePatternScale, subsurfacePatternSpeed, parallaxAmount,
            intersectionPattern, intersectionPatternType, intersectionCutoff, intersectionPatternScale, intersectionPatternSpeed, lineDensity,
            worldSpaceUVs, lodFadeRange;
        public void FindProperties(MaterialProperty[] props)
        {
            //Flow and Waves Variables
            flowDir = FindProperty("_FlowDirection", props);
            waves = FindProperty("_WAVES", props);
            wavelength = FindProperty("_Wavelength", props);
            waveSteepness = FindProperty("_WaveSteepness", props);
            waveSpeed = FindProperty("_WaveSpeed", props);
            recalculateNormals = FindProperty("_RecalculateNormals", props);
            //Appearance Variables
            deepColor = FindProperty("_DeepColor", props);
            shallowColor = FindProperty("_ShallowColor", props);
            subsurfaceColor = FindProperty("_SubsurfaceColor", props);
            surfaceColor = FindProperty("_SurfaceColor", props);
            intersectionColor = FindProperty("_IntersectionColor", props);
            deepColorHSV = FindProperty("_DeepColorHSV", props);
            shallowColorHSV = FindProperty("_ShallowColorHSV", props);
            subsurfaceColorHSV = FindProperty("_SubsurfaceColorHSV", props);
            surfaceColorHSV = FindProperty("_SurfaceColorHSV", props);
            intersectionColorHSV = FindProperty("_IntersectionColorHSV", props);
            depth = FindProperty("_Depth", props);
            useNormalMap = FindProperty("_USE_NORMAL_MAP", props);
            normalMap = FindProperty("_NormalTex", props);
            normalStrength = FindProperty("_NormalStrength", props);
            normalScale = FindProperty("_NormalScale", props);
            //Pattern Variables
            surfacePattern = FindProperty("_SurfacePattern", props);
            surfacePatternType = FindProperty("_SURFACE_PATTERN_TYPE", props);
            surfaceTexture = FindProperty("_SurfaceTex", props);
            surfacePatternScale = FindProperty("_SurfaceScale", props);
            surfacePatternSpeed = FindProperty("_SurfaceSpeed", props);
            surfaceCutoff = FindProperty("_SurfaceCutoff", props);
            subsurfacePattern = FindProperty("_SubsurfacePattern", props);
            subsurfacePatternType = FindProperty("_SUBSURFACE_PATTERN_TYPE", props);
            subsurfaceTexture = FindProperty("_SubsurfaceTex", props);
            subsurfacePatternScale = FindProperty("_SubsurfaceScale", props);
            subsurfacePatternSpeed = FindProperty("_SubsurfaceSpeed", props);
            subsurfaceCutoff = FindProperty("_SubsurfaceCutoff", props);
            parallaxAmount = FindProperty("_Parallax", props);
            intersectionPattern = FindProperty("_IntersectionPattern", props);
            intersectionPatternType = FindProperty("_INTERSECTION_PATTERN_TYPE", props);
            intersectionPatternScale = FindProperty("_IntersectionScale", props);
            intersectionPatternSpeed = FindProperty("_IntersectionSpeed", props);
            lineDensity = FindProperty("_LineDensity", props);
            intersectionCutoff = FindProperty("_IntersectionCutoff", props);
            //Rendering Variables
            worldSpaceUVs = FindProperty("_WorldSpaceUVs", props);
            lodFadeRange = FindProperty("_LODFadeRange", props);
        }

        public void OnEnable()
        {
            Undo.undoRedoPerformed -= OnUndoOrResetPerformed;
            Undo.undoRedoPerformed += OnUndoOrResetPerformed;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoOrResetPerformed;
        }

        private void OnUndoOrResetPerformed()
        {
            if (Selection.activeObject is Material mat)
            {
                ApplyNoiseTexture(mat, mat.shader);
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            AssignDefaultTextures(materialEditor);

            FindProperties(props);

            EditorGUI.BeginChangeCheck();
            {
                RenderCustomInspector(materialEditor);
            }
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.PropertiesChanged();
            }
        }

        void RenderCustomInspector(MaterialEditor materialEditor)
        {
            sectionFlowAndWaves = EditorGUILayout.BeginFoldoutHeaderGroup(sectionFlowAndWaves, "Flow and Waves");
            if (sectionFlowAndWaves)
            {
                GUIStyle flowSettingsLabel = new GUIStyle(EditorStyles.boldLabel);
                flowSettingsLabel.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("----------------------- Flow Settings -----------------------", flowSettingsLabel);
                materialEditor.ShaderProperty(flowDir, Styles.flowText);
                GUIStyle waveSettingsLabel = new GUIStyle(EditorStyles.boldLabel);
                waveSettingsLabel.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("----------------------- Wave Settings -----------------------", waveSettingsLabel);
                materialEditor.ShaderProperty(waves, Styles.wavesText);
                if (waves.floatValue != 0)
                {
                    materialEditor.ShaderProperty(wavelength, Styles.wavelengthText);
                    materialEditor.ShaderProperty(waveSteepness, Styles.waveSteepnessText);
                    materialEditor.ShaderProperty(waveSpeed, Styles.waveSpeedText);
                    materialEditor.ShaderProperty(recalculateNormals, Styles.recalculateNormalText);
                    EditorGUILayout.HelpBox("Recalculating normals will improve fresnel, but worsen subsurface parallax.", MessageType.Info);
                }
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            sectionAppearance = EditorGUILayout.BeginFoldoutHeaderGroup(sectionAppearance, "Appearance");
            if (sectionAppearance)
            {
                GUIStyle colorSettingsLabel = new GUIStyle(EditorStyles.boldLabel);
                colorSettingsLabel.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("----------------------- Color Settings -----------------------", colorSettingsLabel);
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(deepColor, Styles.deepColorText);
                if (EditorGUI.EndChangeCheck()) SyncHSV(deepColor, deepColorHSV);
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(shallowColor, Styles.shallowColorText);
                if (EditorGUI.EndChangeCheck()) SyncHSV(shallowColor, shallowColorHSV);
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(subsurfaceColor, Styles.subsurfaceColorText);
                if (EditorGUI.EndChangeCheck()) SyncHSV(subsurfaceColor, subsurfaceColorHSV);
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(surfaceColor, Styles.surfaceColorText);
                if (EditorGUI.EndChangeCheck()) SyncHSV(surfaceColor, surfaceColorHSV);
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(intersectionColor, Styles.intersectionColorText);
                if (EditorGUI.EndChangeCheck()) SyncHSV(intersectionColor, intersectionColorHSV);
                materialEditor.ShaderProperty(depth, Styles.depthText);
                EditorGUILayout.Space(5);
                GUIStyle normalSettingsLabel = new GUIStyle(EditorStyles.boldLabel);
                normalSettingsLabel.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("----------------------- Normal Settings -----------------------", normalSettingsLabel);
                materialEditor.ShaderProperty(useNormalMap, Styles.useNormalMapText);
                if (useNormalMap.floatValue != 0)
                {
                    materialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);
                    materialEditor.ShaderProperty(normalScale, Styles.normalMapScale);
                    materialEditor.ShaderProperty(normalStrength, Styles.normalStrengthText);
                }
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            sectionPatterns = EditorGUILayout.BeginFoldoutHeaderGroup(sectionPatterns, "Patterns");
            if (sectionPatterns)
            {
                //Surface Pattern Controls
                EditorGUILayout.Space(5);
                GUIStyle surfacePatternLabel = new GUIStyle(EditorStyles.boldLabel);
                surfacePatternLabel.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("----------------------- Surface Pattern Settings -----------------------", surfacePatternLabel);
                int surfaceIndex = (int)surfacePattern.floatValue;
                EditorGUI.BeginChangeCheck();
                surfaceIndex = EditorGUILayout.Popup(Styles.surfacePatternText, surfaceIndex, Styles.surfacePatternNames);
                if (EditorGUI.EndChangeCheck())
                {
                    surfacePattern.floatValue = (float)surfaceIndex;
                    foreach (Material mat in materialEditor.targets)
                    {
                        if (surfaceIndex == 0)
                        {
                            surfacePatternType.floatValue = 0.0f;
                            mat.EnableKeyword("_SURFACE_PATTERN_TYPE_FLOWMAP");
                            mat.DisableKeyword("_SURFACE_PATTERN_TYPE_TEXTURE");
                            mat.DisableKeyword("_SURFACE_PATTERN_TYPE_PATTERN");
                        }
                        else if (surfaceIndex == 1 || surfaceIndex == 2 || surfaceIndex == 6 || surfaceIndex == 7)
                        {
                            surfacePatternType.floatValue = 1.0f;
                            mat.EnableKeyword("_SURFACE_PATTERN_TYPE_TEXTURE");
                            mat.DisableKeyword("_SURFACE_PATTERN_TYPE_FLOWMAP");
                            mat.DisableKeyword("_SURFACE_PATTERN_TYPE_PATTERN");
                        }
                        else
                        {
                            surfacePatternType.floatValue = 2.0f;
                            mat.EnableKeyword("_SURFACE_PATTERN_TYPE_PATTERN");
                            mat.DisableKeyword("_SURFACE_PATTERN_TYPE_FLOWMAP");
                            mat.DisableKeyword("_SURFACE_PATTERN_TYPE_TEXTURE");
                        }
                    }
                }
                if (surfacePattern.floatValue == 7)
                {
                    materialEditor.TexturePropertySingleLine(Styles.surfaceTextureText, surfaceTexture);
                    materialEditor.ShaderProperty(surfacePatternScale, Styles.surfacePatternScaleText);
                    materialEditor.ShaderProperty(surfacePatternSpeed, Styles.surfacePatternSpeedText);
                }
                else if (surfacePattern.floatValue == 8) { }
                else
                {
                    // Surface Cutoff Slider
                    Vector2 surfaceRange = surfaceCutoff.vectorValue;
                    float surfaceMin = surfaceRange.x;
                    float surfaceMax = surfaceRange.y;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Styles.surfaceCutoffText);
                    EditorGUILayout.MinMaxSlider(ref surfaceMin, ref surfaceMax, -2f, 2f);
                    surfaceMin = EditorGUILayout.FloatField(surfaceMin, GUILayout.Width(50));
                    surfaceMax = EditorGUILayout.FloatField(surfaceMax, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        surfaceCutoff.vectorValue = new Vector2(surfaceMin, surfaceMax);
                    }
                    materialEditor.ShaderProperty(surfacePatternScale, Styles.surfacePatternScaleText);
                    materialEditor.ShaderProperty(surfacePatternSpeed, Styles.surfacePatternSpeedText);
                }
                EditorGUILayout.Space(5);

                //Subsurface Pattern Controls
                EditorGUILayout.Space(5);
                GUIStyle subsurfacePatternLabel = new GUIStyle(EditorStyles.boldLabel);
                subsurfacePatternLabel.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("----------------------- Subsurface Pattern Settings -----------------------", subsurfacePatternLabel);
                int subsurfaceIndex = (int)subsurfacePattern.floatValue;
                EditorGUI.BeginChangeCheck();
                subsurfaceIndex = EditorGUILayout.Popup(Styles.subsurfacePatternText, subsurfaceIndex, Styles.subsurfacePatternNames);
                if (EditorGUI.EndChangeCheck())
                {
                    subsurfacePattern.floatValue = (float)subsurfaceIndex;
                    foreach (Material mat in materialEditor.targets)
                    {
                        if (subsurfaceIndex == 0 || subsurfaceIndex == 1 || subsurfaceIndex == 5 || subsurfaceIndex == 6)
                        {
                            subsurfacePatternType.floatValue = 1.0f;
                            mat.EnableKeyword("_SUBSURFACE_PATTERN_TYPE");
                        }
                        else
                        {
                            subsurfacePatternType.floatValue = 0.0f;
                            mat.DisableKeyword("_SUBSURFACE_PATTERN_TYPE");
                        }
                    }
                }
                if (subsurfacePattern.floatValue == 6)
                {
                    materialEditor.TexturePropertySingleLine(Styles.subsurfaceTextureText, subsurfaceTexture);
                    materialEditor.ShaderProperty(subsurfacePatternScale, Styles.subsurfacePatternScaleText);
                    materialEditor.ShaderProperty(subsurfacePatternSpeed, Styles.subsurfacePatternSpeedText);
                    materialEditor.ShaderProperty(parallaxAmount, Styles.parallaxAmountText);
                }
                else if (subsurfacePattern.floatValue == 7) { }
                else
                {
                    // Subsurface Cutoff Slider
                    Vector2 subsurfaceRange = subsurfaceCutoff.vectorValue;
                    float subsurfaceMin = subsurfaceRange.x;
                    float subsurfaceMax = subsurfaceRange.y;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Styles.subsurfaceCutoffText);
                    EditorGUILayout.MinMaxSlider(ref subsurfaceMin, ref subsurfaceMax, -2f, 2f);
                    subsurfaceMin = EditorGUILayout.FloatField(subsurfaceMin, GUILayout.Width(50));
                    subsurfaceMax = EditorGUILayout.FloatField(subsurfaceMax, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        subsurfaceCutoff.vectorValue = new Vector2(subsurfaceMin, subsurfaceMax);
                    }
                    materialEditor.ShaderProperty(subsurfacePatternScale, Styles.subsurfacePatternScaleText);
                    materialEditor.ShaderProperty(subsurfacePatternSpeed, Styles.subsurfacePatternSpeedText);
                    materialEditor.ShaderProperty(parallaxAmount, Styles.parallaxAmountText);
                }
                EditorGUILayout.Space(5);

                //Intersection Pattern Controls
                EditorGUILayout.Space(5);
                GUIStyle intersectionPatternLabel = new GUIStyle(EditorStyles.boldLabel);
                intersectionPatternLabel.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("----------------------- Intersection Pattern Settings -----------------------", intersectionPatternLabel);
                int intersectionIndex = (int)intersectionPattern.floatValue;
                EditorGUI.BeginChangeCheck();
                intersectionIndex = EditorGUILayout.Popup(Styles.intersectionPatternText, intersectionIndex, Styles.intersectionPatternNames);
                if (EditorGUI.EndChangeCheck())
                {
                    intersectionPattern.floatValue = (float)intersectionIndex;
                    foreach (Material mat in materialEditor.targets)
                    {
                        if (intersectionIndex == 0 || intersectionIndex == 1 || intersectionIndex == 5)
                        {
                            intersectionPatternType.floatValue = 1.0f;
                            mat.EnableKeyword("_INTERSECTION_PATTERN_TYPE");
                        }
                        else
                        {
                            intersectionPatternType.floatValue = 0.0f;
                            mat.DisableKeyword("_INTERSECTION_PATTERN_TYPE");
                        }
                    }
                }
                if (intersectionPattern.floatValue == 6)
                {
                    // Intersection Cutoff Slider
                    Vector2 intersectionRange = intersectionCutoff.vectorValue;
                    float intersectionMin = intersectionRange.x;
                    float intersectionMax = intersectionRange.y;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Styles.intersectionCutoffText);
                    EditorGUILayout.MinMaxSlider(ref intersectionMin, ref intersectionMax, 0f, 2f);
                    intersectionMin = EditorGUILayout.FloatField(intersectionMin, GUILayout.Width(50));
                    intersectionMax = EditorGUILayout.FloatField(intersectionMax, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        intersectionCutoff.vectorValue = new Vector2(intersectionMin, intersectionMax);
                    }
                    materialEditor.ShaderProperty(lineDensity, Styles.lineDensityText);
                }
                else if (intersectionPattern.floatValue == 7) { }
                else
                {
                    // Intersection Cutoff Slider
                    Vector2 intersectionRange = intersectionCutoff.vectorValue;
                    float intersectionMin = intersectionRange.x;
                    float intersectionMax = intersectionRange.y;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Styles.intersectionCutoffText);
                    EditorGUILayout.MinMaxSlider(ref intersectionMin, ref intersectionMax, 0f, 2f);
                    intersectionMin = EditorGUILayout.FloatField(intersectionMin, GUILayout.Width(50));
                    intersectionMax = EditorGUILayout.FloatField(intersectionMax, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        intersectionCutoff.vectorValue = new Vector2(intersectionMin, intersectionMax);
                    }
                    materialEditor.ShaderProperty(intersectionPatternScale, Styles.intersectionPatternScaleText);
                    materialEditor.ShaderProperty(intersectionPatternSpeed, Styles.intersectionPatternSpeedText);
                    materialEditor.ShaderProperty(lineDensity, Styles.lineDensityText);
                }
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            sectionRendering = EditorGUILayout.BeginFoldoutHeaderGroup(sectionRendering, "Rendering");
            if (sectionRendering)
            {
                GUIStyle renderSettingsLabel = new GUIStyle(EditorStyles.boldLabel);
                renderSettingsLabel.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.LabelField("----------------------- Render Settings -----------------------", renderSettingsLabel);
                materialEditor.ShaderProperty(worldSpaceUVs, Styles.worldSpaceUVsText);
                //LOD Fade Range
                Vector2 range = lodFadeRange.vectorValue;
                GUIContent[] subLabels = { new GUIContent("Start"), new GUIContent("End") };
                float[] values = { range.x, range.y };
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Styles.lodFadeRangeText, GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUI.MultiFloatField(EditorGUILayout.GetControlRect(), subLabels, values);
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    lodFadeRange.vectorValue = new Vector2(values[0], values[1]);
                }
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void SyncHSV(MaterialProperty rgbProp, MaterialProperty hsvProp)
        {
            float h, s, v;
            Color standardRGB = PlayerSettings.colorSpace == ColorSpace.Linear ? rgbProp.colorValue.gamma : rgbProp.colorValue;
            Color.RGBToHSV(standardRGB, out h, out s, out v);
            hsvProp.vectorValue = new Vector4(h, s, v, rgbProp.colorValue.a);
        }

        private void AssignDefaultTextures(MaterialEditor materialEditor)
        {
            foreach (Object target in materialEditor.targets)
            {
                if (target is Material mat)
                {
                    ApplyNoiseTexture(mat, mat.shader);
                }
            }
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            ApplyNoiseTexture(material, newShader);
        }

        private void ApplyNoiseTexture(Material mat, Shader shader)
        {
            if (shader != null)
            {
                Texture noiseTex = mat.GetTexture("_NoiseTex");

                if (noiseTex == null || noiseTex.name == "white" || noiseTex.name == "Default-Material")
                {
                    string shaderPath = AssetDatabase.GetAssetPath(shader);
                    if (!string.IsNullOrEmpty(shaderPath))
                    {
                        string directory = Path.GetDirectoryName(shaderPath);
                        string texturePath = Path.Combine(directory, "Noise.png");

                        Texture2D noiseAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

                        if (noiseAsset != null)
                        {
                            mat.SetTexture("_NoiseTex", noiseAsset);
                            EditorUtility.SetDirty(mat);
                        }
                        else
                        {
                            Debug.LogWarning($"[FluidShaderGUI] Automated assignment failed. Could not find 'Noise.png' in: {directory}");
                        }
                    }
                }
            }
        }
    }
}