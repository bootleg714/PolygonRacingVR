using UnityEngine;

namespace PolygonRacing
{
    // Shared look settings for the sky, light and water pass. Shaders live in
    // Assets/PolygonRacing/Resources so they are always included in Quest builds.
    public static class PolygonLook
    {
        public const string FlatShaderName="PolygonFlat",WaterShaderName="PolygonWater",
            SkyShaderName="PolygonSky",CloudShaderName="PolygonClouds";

        // Opaque scenery/car shader: the flat-lit polygon shader when present, else URP Simple Lit.
        public static Shader LitShader()
        {
            var shader=Resources.Load<Shader>(FlatShaderName);
            if(!shader)shader=Shader.Find("Universal Render Pipeline/Simple Lit");
            if(!shader)shader=Shader.Find("Standard");
            return shader;
        }

        public struct Preset
        {
            public Vector3 SunEuler;public Color SunColor;public float SunIntensity;
            public Color Zenith,Horizon,BelowHorizon,WaterZenith;
            public float FogStart,FogEnd,CloudRadius;public int CloudSeed;
        }

        public static Preset For(PolygonCircuitLayout layout)
        {
            if(layout==PolygonCircuitLayout.CoastalExpressway)
                return new Preset{
                    SunEuler=new Vector3(38,-35,0),SunColor=new Color(1,.94f,.82f),SunIntensity=1.3f,
                    Zenith=new Color(.16f,.46f,.82f),Horizon=new Color(.66f,.86f,.93f),
                    BelowHorizon=new Color(.40f,.70f,.80f),WaterZenith=new Color(.20f,.52f,.80f),
                    FogStart=380,FogEnd=1300,CloudRadius=1100,CloudSeed=11};
            return new Preset{
                SunEuler=new Vector3(42,-35,0),SunColor=new Color(1,.94f,.84f),SunIntensity=1.25f,
                Zenith=new Color(.20f,.44f,.76f),Horizon=new Color(.70f,.84f,.88f),
                BelowHorizon=new Color(.50f,.66f,.62f),WaterZenith=new Color(.20f,.50f,.80f),
                FogStart=220,FogEnd=800,CloudRadius=650,CloudSeed=23};
        }
    }
}
