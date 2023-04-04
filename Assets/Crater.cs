using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
 
public class Crater : MonoBehaviour
{
	[SerializeField] ComputeShader _ComputeShader;
	[SerializeField] Transform _Emitter;
	[SerializeField] Terrain _Terrain;
	[SerializeField] Shape _Shape = Shape.Random;
	[SerializeField] float _Radius = 5.0f;
	[SerializeField] [Range(0.0f, 10.0f)] float _Depth = 3.0f;
	[SerializeField] [Range(0.0f, 0.01f)] float _Border = 0.003f;
	[SerializeField] [Range(8, 40)] int _Steps = 24;
	[SerializeField] [Range(-1e5f, 1e5f)] float _Seed = 0f;
	[SerializeField] [Range(0.001f, 0.01f)] float _Amplitude = 0.005f;
	[SerializeField] [Range(0.05f, 0.4f)] float _Smoothness = 0.3f;

	enum Shape {Circular, Random}

	//p = world space point(xz); s0 = terrain position(xz); sx = terrain size x; sz = terrain size z; h = heightmap resolution;
	Vector2 WorldToHeightMapSpace(Vector2 p, Vector2 s0, float sx, float sz, int h) 
	{
		float x = (p.x - s0.x) / sx;
		float y = (p.y - s0.y) / sz;
		return new Vector2(h * x, h * y);
	}

	//p = heightmap point; s0 = terrain position(xz); sx = terrain size x; sz = terrain size z; h = heightmap resolution;
	Vector2 HeightMapToWorldSpace(Vector2 p, Vector2 s0, float sx, float sz, int h) 
	{
		Vector2 k = new Vector2(p.x / h, p.y / h);
		float x = Mathf.Lerp(s0.x, s0.x + sx, k.x);
		float y = Mathf.Lerp(s0.y, s0.y + sz, k.y);
		return new Vector2(x, y);
	}

	void GenerateCrater (ComputeShader shader, Terrain terrain, int shape, Vector2 center, float radius, float depth, float border, int steps, float seed, float amplitude, float smoothness)
	{
		RenderTexture heightmapTexture = terrain.terrainData.heightmapTexture;
		RectInt region = new RectInt();
		region.x = Mathf.RoundToInt(center.x - radius);
		region.y = Mathf.RoundToInt(center.y - radius);
		region.width = Mathf.RoundToInt(radius * 2f);
		region.height = Mathf.RoundToInt(radius * 2f);
		if (region.x <= 0 || region.x >= heightmapTexture.width || region.y <= 0 || region.y >= heightmapTexture.height) return;
		if (region.width > (heightmapTexture.width - region.x) || region.height > (heightmapTexture.height - region.y)) return;
		RenderTextureDescriptor descriptor = heightmapTexture.descriptor;
		descriptor.width = region.width;
		descriptor.height = region.height;
		descriptor.enableRandomWrite = true;
		RenderTexture renderTexture = new RenderTexture(descriptor);
		Graphics.CopyTexture(heightmapTexture, 0, 0, region.x, region.y, region.width, region.height, renderTexture, 0, 0, 0, 0);
		shader.SetInt("_Shape", shape);
		shader.SetInt("_Steps", steps);
		shader.SetFloat("_Amplitude", amplitude);
		shader.SetFloat("_Border", border);
		shader.SetFloat("_Depth", depth);
		shader.SetFloat("_Radius", radius);
		shader.SetFloat("_Seed", seed);
		shader.SetFloat("_Smoothness", smoothness);
		shader.SetVector("_Center", center);
		shader.SetTexture(0, "_RenderTexture", renderTexture);
		shader.Dispatch(0, Mathf.Max(1, renderTexture.width / 8), Mathf.Max(1, renderTexture.height / 8), 1);
		Graphics.CopyTexture(renderTexture, 0, 0, 0, 0, region.width, region.height, heightmapTexture, 0, 0, region.x, region.y);
		terrain.terrainData.DirtyHeightmapRegion(region, TerrainHeightmapSyncControl.None);
		terrain.terrainData.SyncHeightmap();
		renderTexture.Release();
	}

	public void Execute()
	{
		Vector2 position = new Vector2(_Terrain.transform.position.x, _Terrain.transform.position.z);
		Vector3 size = _Terrain.terrainData.size;
		int resolution = _Terrain.terrainData.heightmapResolution;
		Vector2 center = WorldToHeightMapSpace(new Vector2(_Emitter.position.x, _Emitter.position.z), position, size.x, size.z, resolution);
		float radius = resolution / Mathf.Max(size.x, size.z) * _Radius;
		float depth = _Depth / size.y;
		_Seed = Random.Range(-1e5f, 1e5f);
		GenerateCrater(_ComputeShader, _Terrain, (int)_Shape, center, radius, depth, _Border, _Steps, _Seed, _Amplitude, _Smoothness);
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(Crater))]
public class CraterEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		Crater crater = (Crater)target;
		if(GUILayout.Button("Generate")) crater.Execute();
	}
}
#endif