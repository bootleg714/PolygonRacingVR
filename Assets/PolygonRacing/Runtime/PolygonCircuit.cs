using UnityEngine;

namespace PolygonRacing
{
    public enum PolygonCircuitLayout
    {
        Forest,
        CoastalExpressway,
        Downtown
    }

    // Shared geometry for scenery, race position and AI routes.
    public static class PolygonCircuit
    {
        const int Samples=1024;
        static readonly Vector3[] forestKnots={
            new Vector3(0,0,100),new Vector3(150,0,100),new Vector3(300,0,100),new Vector3(400,0,100),
            new Vector3(435,0,65),new Vector3(428,0,20),new Vector3(385,0,-10),
            new Vector3(417,0,-55),new Vector3(395,0,-98),new Vector3(200,0,-120),
            new Vector3(-35,0,-120),new Vector3(-120,0,-95),new Vector3(-165,0,-50),
            new Vector3(-165,0,15),new Vector3(-115,0,76),new Vector3(-80,0,100)};
        static readonly Vector3[] coastalKnots={
            new Vector3(0,0,0),new Vector3(168,0,0),new Vector3(336,0,0),new Vector3(504,0,0),
            new Vector3(672,0,0),new Vector3(823.2f,0,33.6f),new Vector3(940.8f,0,84),new Vector3(1008,0,252),
            new Vector3(1008,0,403.2f),new Vector3(940.8f,0,537.6f),new Vector3(823.2f,0,621.6f),
            new Vector3(672,0,655.2f),new Vector3(504,0,655.2f),new Vector3(336,0,655.2f),
            new Vector3(168,0,655.2f),new Vector3(0,0,655.2f),new Vector3(-151.2f,0,638.4f),
            new Vector3(-277.2f,0,571.2f),new Vector3(-352.8f,0,453.6f),new Vector3(-369.6f,0,319.2f),
            new Vector3(-327.6f,0,218.4f),new Vector3(-252,0,184.8f),new Vector3(-176.4f,0,126),
            new Vector3(-197.4f,0,50.4f),new Vector3(-294,0,-33.6f),new Vector3(-352.8f,0,-134.4f),
            new Vector3(-252,0,-252),new Vector3(-117.6f,0,-151.2f)};
        // Downtown street circuit: city-block grid with 90-degree corners (about 35 m radius),
        // long boulevards for drafting and two block-step jogs. Knots sit 32 m either side of
        // each grid corner, so consecutive knots on one street keep the road exactly straight.
        static readonly Vector3[] downtownKnots={
            new Vector3(32f,0,0f),new Vector3(182f,0,0f),new Vector3(332f,0,0f),new Vector3(482f,0,0f),new Vector3(632f,0,0f),
            new Vector3(668f,0,0f),new Vector3(700f,0,32f),new Vector3(700f,0,182f),new Vector3(700f,0,332f),new Vector3(700f,0,388f),
            new Vector3(668f,0,420f),new Vector3(518f,0,420f),new Vector3(412f,0,420f),new Vector3(380f,0,452f),new Vector3(380f,0,602f),
            new Vector3(380f,0,668f),new Vector3(348f,0,700f),new Vector3(198f,0,700f),new Vector3(48f,0,700f),new Vector3(-102f,0,700f),
            new Vector3(-252f,0,700f),new Vector3(-288f,0,700f),new Vector3(-320f,0,668f),new Vector3(-320f,0,518f),new Vector3(-320f,0,368f),
            new Vector3(-320f,0,292f),new Vector3(-288f,0,260f),new Vector3(-152f,0,260f),new Vector3(-120f,0,228f),new Vector3(-120f,0,78f),
            new Vector3(-120f,0,32f),new Vector3(-88f,0,0f),new Vector3(-32f,0,0f)};
        static readonly Vector3[] points=new Vector3[Samples+1];
        static readonly float[] distances=new float[Samples+1];
        static Vector3[] knots;
        static PolygonCircuitLayout activeLayout;
        static bool initialized;

        public static int KnotCount=>knots.Length;
        public static int SampleCount=>Samples;
        public static float RoadHalfWidth { get; private set; }
        public static float Length { get; private set; }
        public static PolygonCircuitLayout Layout=>activeLayout;
        public static string LayoutName=>activeLayout==PolygonCircuitLayout.CoastalExpressway?"Coastal Expressway":activeLayout==PolygonCircuitLayout.Downtown?"Downtown Street Circuit":"Forest Circuit";
        public static string LayoutId=>activeLayout==PolygonCircuitLayout.CoastalExpressway?"CoastalExpresswayV01":activeLayout==PolygonCircuitLayout.Downtown?"DowntownV01":"ForestCircuitV12";

        static PolygonCircuit()
        {
            SetLayout(PolygonCircuitLayout.Forest);
        }

        public static void SetLayout(PolygonCircuitLayout layout)
        {
            if(initialized&&activeLayout==layout)return;
            activeLayout=layout;
            knots=layout==PolygonCircuitLayout.CoastalExpressway?coastalKnots:layout==PolygonCircuitLayout.Downtown?downtownKnots:forestKnots;
            RoadHalfWidth=layout==PolygonCircuitLayout.CoastalExpressway?11f:layout==PolygonCircuitLayout.Downtown?10f:9f;
            for(int i=0;i<=Samples;i++)
            {
                points[i]=Evaluate(i*Mathf.PI*2/Samples);
                if(i>0)distances[i]=distances[i-1]+Vector3.Distance(points[i-1],points[i]);
            }
            Length=distances[Samples];
            initialized=true;
        }

        public static Vector3 Evaluate(float angle)
        {
            float u=Mathf.Repeat(angle/(Mathf.PI*2),1)*knots.Length;
            int i=Mathf.FloorToInt(u);float t=u-i,t2=t*t,t3=t2*t;
            var p0=knots[(i+knots.Length-1)%knots.Length];var p1=knots[i%knots.Length];
            var p2=knots[(i+1)%knots.Length];var p3=knots[(i+2)%knots.Length];
            return (p0*(1-3*t+3*t2-t3)+p1*(3*t3-6*t2+4)+p2*(-3*t3+3*t2+3*t+1)+p3*t3)/6;
        }

        public static Vector3 Tangent(float angle)=>(Evaluate(angle+.0005f)-Evaluate(angle-.0005f)).normalized;

        public static float AngleAtDistance(float meters)
        {
            meters=Mathf.Repeat(meters,Length);int lo=0,hi=Samples;
            while(hi-lo>1){int m=(lo+hi)/2;if(distances[m]<=meters)lo=m;else hi=m;}
            return (lo+Mathf.InverseLerp(distances[lo],distances[hi],meters))*Mathf.PI*2/Samples;
        }

        public static float DistanceAtKnot(float knot)
        {
            float sample=Mathf.Repeat(knot/knots.Length,1)*Samples;
            int index=Mathf.FloorToInt(sample);
            return Mathf.Lerp(distances[index],distances[index+1],sample-index);
        }

        public static Vector3 AtDistance(float meters)=>Evaluate(AngleAtDistance(meters));

        public static float Curvature(float meters)
        {
            var a=Tangent(AngleAtDistance(meters-2));var b=Tangent(AngleAtDistance(meters+2));
            return Vector3.SignedAngle(a,b,Vector3.up)*Mathf.Deg2Rad/4;
        }

        // Closest segment projection returns arc-length fraction, rather than spline angle.
        public static float ProjectLap(Vector3 p,out float offset)
        {
            p.y=0;float best=float.PositiveInfinity,bestDistance=0;
            for(int i=0;i<Samples;i++)
            {
                var ab=points[i+1]-points[i];float t=Mathf.Clamp01(Vector3.Dot(p-points[i],ab)/Mathf.Max(ab.sqrMagnitude,.000001f));
                float sq=(p-(points[i]+ab*t)).sqrMagnitude;
                if(sq<best){best=sq;bestDistance=Mathf.Lerp(distances[i],distances[i+1],t);}
            }
            offset=Mathf.Sqrt(best);return Mathf.Repeat(bestDistance/Length,1);
        }
    }
}
