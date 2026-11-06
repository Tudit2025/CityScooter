using System;
using UnityEngine;

namespace CityScooter.Rendering
{
    // Stable texture-array slices; append new types rather than reordering existing ones.
    public enum ComicSurface { Plain, Plaster, Brick, Concrete, Asphalt, Paving, PaintedMetal, Metal, Wood, Rubber, Fabric, Glass, Foliage, Plastic }

    [Serializable]
    public sealed class ComicSurfaceProfile
    {
        public ComicSurface surface;
        public string label;
        [Min(.1f)] public float tileMetres=2;
        [Range(0,1)] public float roughness=.8f;
        [Range(0,.4f)] public float shadeContrast=.12f;
        [Range(0,.03f)] public float reliefMetres=.003f;
        public int seed=7301;
        public Color previewColor=Color.gray;
    }

    [Serializable]
    public sealed class ComicSurfaceAssignment
    {
        public string assetId;
        public ComicSurface primarySurface;
    }

    [CreateAssetMenu(menuName="CityScooter/Comic Surface Library")]
    public sealed class ComicSurfaceLibrary : ScriptableObject
    {
        public int schemaVersion=1;
        [Range(0,1.5f)] public float shadeStrength=1;
        [Range(0,2)] public float normalStrength=1;
        [Range(0,1)] public float highlightStrength=.32f;
        public Vector2 distanceFade=new Vector2(24,100);
        public ComicSurfaceProfile[] profiles=Array.Empty<ComicSurfaceProfile>();
        public ComicSurfaceAssignment[] assignments=Array.Empty<ComicSurfaceAssignment>();
    }
}
