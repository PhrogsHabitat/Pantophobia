#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public class ModelTextureAutoAssigner : AssetPostprocessor
{
	private void OnPreprocessModel()
	{
		if (!assetPath.ToLower().EndsWith(".fbx"))
		{
			return;
		}

		ModelImporter importer = (ModelImporter)assetImporter;

		// Always extract materials
		importer.materialLocation = ModelImporterMaterialLocation.External;
	}

	private void OnPostprocessModel(GameObject g)
	{
		if (!assetPath.ToLower().EndsWith(".fbx"))
		{
			return;
		}

		string modelDir = Path.GetDirectoryName(assetPath);
		string textureDir = Path.Combine(modelDir, "textures");

		// Process all renderers in the model
		Renderer[] renderers = g.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in renderers)
		{
			foreach (Material mat in renderer.sharedMaterials)
			{
				if (mat == null)
				{
					continue;
				}

				// Find matching textures
				string matName = mat.name.Replace(" (Instance)", "").ToLower();
				TryAssignTexture(mat, textureDir, "_MainTex", new[] {
					"albedo", "diffuse", "basecolor", "color",
					"col", "d", "base", "main", matName
				});

				TryAssignTexture(mat, textureDir, "_BumpMap", new[] {
					"normal", "nrm", "n", "norm", "bump"
				}, true);

				TryAssignTexture(mat, textureDir, "_MetallicGlossMap", new[] {
					"metallic", "metalness", "metal", "mtl", "m"
				});

				TryAssignTexture(mat, textureDir, "_OcclusionMap", new[] {
					"occlusion", "ao", "ambientocclusion"
				});

				TryAssignTexture(mat, textureDir, "_EmissionMap", new[] {
					"emissive", "emission", "emit", "glow"
				});

				TryAssignTexture(mat, textureDir, "_ParallaxMap", new[] {
					"height", "displacement", "bumpheight", "h"
				});
			}
		}
	}

	private void TryAssignTexture(Material mat, string textureDir, string property, string[] keywords, bool isNormal = false)
	{
		if (!mat.HasProperty(property))
		{
			return;
		}

		foreach (string file in Directory.GetFiles(textureDir))
		{
			string fileName = Path.GetFileNameWithoutExtension(file).ToLower();

			// Skip meta files
			if (file.EndsWith(".meta"))
			{
				continue;
			}

			foreach (string keyword in keywords)
			{
				if (fileName.Contains(keyword))
				{
					Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(file);
					if (texture != null)
					{
						mat.SetTexture(property, texture);
						if (isNormal)
						{
							mat.EnableKeyword("_NORMALMAP");
						}

						return;
					}
				}
			}
		}
	}
}
#endif
