using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner : MonoBehaviour
{
	[SerializeField] public IndexFormat IndexFormat = IndexFormat.UInt16;

	public void CombineMeshes()
	{
		Transform tTransform = transform;
		Vector3 tPos = tTransform.position;
		Quaternion tRot = tTransform.rotation;
		Vector3 tScale = tTransform.localScale;

		// Default all transform information to prevent weird offsets
		tTransform.position = Vector3.zero;
		tTransform.rotation = Quaternion.identity;
		tTransform.localScale = Vector3.one;
		
		Dictionary<Material, List<MeshFilter>> materialFilters = new Dictionary<Material, List<MeshFilter>>();

		// Go through all children and gather all the MeshFilters in a dictionary
		// with the associated Material functioning as the key
		for (int i = 0; i < tTransform.childCount; i++)
		{
			RetrieveMaterialFilters(tTransform.GetChild(i), materialFilters);
		}

		// Combine the meshFilters foreach material into meshes and store them in another dictionary
		Dictionary<Material, Mesh> materialMeshPairs = CombineMeshFilters(materialFilters);

		// Make a new mesh that will contain the combined mesh.
		Mesh mesh = new Mesh();
		mesh.indexFormat = IndexFormat;
		
		// Set the subMesh count and add the materials
		mesh.subMeshCount = materialMeshPairs.Count;

		MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
		meshRenderer.SetMaterials(materialMeshPairs.Keys.ToList());

		// Copy all of the mesh information from the meshes into the big mesh.
		AddMeshesToMesh(materialMeshPairs, mesh);
		
		mesh.Optimize();

		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		// Assign the mesh.
		GetComponent<MeshFilter>().sharedMesh = mesh;

		// Destroy all the children.
		for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
		{
			DestroyImmediate(transform.GetChild(childIndex).gameObject);
		}

		// Set all transform information back
		tTransform.position = tPos;
		tTransform.rotation = tRot;
		tTransform.localScale = tScale;
	}

	private void RetrieveMaterialFilters(Transform target, Dictionary<Material, List<MeshFilter>> materialFilters)
	{
		if (target.TryGetComponent(out MeshRenderer meshRenderer))
		{
			if (target.TryGetComponent(out MeshFilter meshFilter))
			{
				Material material = meshRenderer.sharedMaterial;

				if (!materialFilters.TryAdd(material, new List<MeshFilter> {meshFilter}))
				{
					materialFilters[material].Add(meshFilter);
				}
			}
		}

		for (int i = 0; i < target.childCount; i++)
		{
			RetrieveMaterialFilters(target.GetChild(i), materialFilters);
		}
	}

	private void AddMeshesToMesh(Dictionary<Material, Mesh> materialMeshPairs, Mesh mesh)
	{
		int subMeshIndex = 0;
		foreach (Mesh matMesh in materialMeshPairs.Values)
		{
			List<Vector3> newVertices = mesh.vertices.ToList();
			newVertices.AddRange(matMesh.vertices);

			List<Vector2> newUVs = mesh.uv.ToList();
			newUVs.AddRange(matMesh.uv);

			int vertexOffset = mesh.vertices.Length;
			List<int> newTriangles = matMesh.triangles.Select(tri => tri + vertexOffset).ToList();

			mesh.SetVertices(newVertices);
			mesh.uv = newUVs.ToArray();

			mesh.SetTriangles(newTriangles, subMeshIndex);
			subMeshIndex++;
		}
	}

	private Dictionary<Material, Mesh> CombineMeshFilters(Dictionary<Material, List<MeshFilter>> materialFilters)
	{
		Dictionary<Material, Mesh> materialMeshPairs = new Dictionary<Material, Mesh>();
		
		foreach (KeyValuePair<Material, List<MeshFilter>> materialFilter in materialFilters)
		{
			CombineInstance[] newCombine = new CombineInstance[materialFilter.Value.Count];

			for (int index = 0; index < materialFilter.Value.Count; index++)
			{
				MeshFilter meshFilter = materialFilter.Value[index];
				newCombine[index].mesh = meshFilter.sharedMesh;
				newCombine[index].transform = meshFilter.transform.localToWorldMatrix;
			}

			Mesh newMesh = new Mesh();
			newMesh.indexFormat = IndexFormat;
			newMesh.CombineMeshes(newCombine);
			materialMeshPairs.Add(materialFilter.Key, newMesh);
		}

		return materialMeshPairs;
	}
}