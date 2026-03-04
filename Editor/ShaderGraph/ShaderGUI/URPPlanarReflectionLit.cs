using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Rendering.Universal.ShaderGUI {
	public class URPPlanarReflectionLit : BaseShaderGUI {

		internal static class Styles
		{
			public static readonly GUIContent normalReflectionDistortionText = EditorGUIUtility.TrTextContent("Normal Reflection Distortion",
				"These settings define the surface refelction distortion.");

			public static readonly GUIContent reflectionMultiplierText = EditorGUIUtility.TrTextContent("Reflection Multiplier",
				"How much Reflection.");

			public static readonly GUIContent reflectionPowerText = EditorGUIUtility.TrTextContent("Reflection Power",
				"Reflection Power.");
		}
		
		public struct PlanarReflectionLitProperties {
			public MaterialProperty normalReflectionDistortion;
			public MaterialProperty reflectionMultiplier;
			public MaterialProperty reflectionPower;
			
			public PlanarReflectionLitProperties(MaterialProperty[] properties)
			{
				normalReflectionDistortion = BaseShaderGUI.FindProperty("_NormalReflectionDistortion", properties, false);
				reflectionMultiplier = BaseShaderGUI.FindProperty("_ReflectionMultiplier", properties, false);
				reflectionPower = BaseShaderGUI.FindProperty("_ReflectionPower", properties, false);
			}
		}

		static readonly string[] workflowModeNames = Enum.GetNames(typeof(LitGUI.WorkflowMode));

        private LitGUI.LitProperties litProperties;
        private LitDetailGUI.LitProperties litDetailProperties;
        private PlanarReflectionLitProperties planarReflectionLitProperties;

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(LitDetailGUI.Styles.detailInputs, Expandable.Details, _ => LitDetailGUI.DoDetailArea(litDetailProperties, materialEditor));
        }

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            litDetailProperties = new LitDetailGUI.LitProperties(properties);
            planarReflectionLitProperties = new PlanarReflectionLitProperties(properties);
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, LitDetailGUI.SetMaterialKeywords);
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            if (litProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, workflowModeNames);

            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
            }

            if (planarReflectionLitProperties.normalReflectionDistortion != null) {
	            materialEditor.ShaderProperty(planarReflectionLitProperties.normalReflectionDistortion, Styles.normalReflectionDistortionText);
            }

            if (planarReflectionLitProperties.reflectionMultiplier != null) {
	            materialEditor.ShaderProperty(planarReflectionLitProperties.reflectionMultiplier, Styles.reflectionMultiplierText);
            }
            
            if(planarReflectionLitProperties.reflectionPower != null) {
	            materialEditor.ShaderProperty(planarReflectionLitProperties.reflectionPower, Styles.reflectionPowerText);
            }

            
            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Blend", (float)blendMode);

            material.SetFloat("_Surface", (float)surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
        }
        
	}
}