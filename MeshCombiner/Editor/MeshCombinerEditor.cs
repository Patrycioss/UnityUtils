using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Editor
{
	[CustomEditor(typeof(MeshCombiner))]
	public class MeshCombinerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			MeshCombiner meshCombiner = (MeshCombiner) target;
			EditorGUILayout.HelpBox("Combining will delete all children and this is not undoable. I would advise copying your object first!", MessageType.Warning);
			
			if (GUILayout.Button("Combine Meshes")) 
				meshCombiner.CombineMeshes();
			
			EditorGUILayout.HelpBox("Only change this setting to 'U Int 32' if you get an error in generation about reaching the maximum size.", MessageType.Info);
			meshCombiner.IndexFormat = (IndexFormat) EditorGUILayout.EnumPopup("Mesh Index Format", meshCombiner.IndexFormat);
		}
	}
}