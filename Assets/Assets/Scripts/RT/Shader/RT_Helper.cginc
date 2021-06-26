
#define f4(x) float4(x,x,x,x)
#define f3(x) float3(x,x,x)
#define f3to4(a) float4(a.x,a.y,a.z,1.0f)

#define f(x) float3(x.x,x.y,x.z)

#define v2r(v) float3(asin(-v.y),atan2(v.x,v.z),0)

const static float MaxDist=100000000;



struct collision// generated per ray
{
	//衝突場所
    float3 Pos;
    //衝突距離
    float dist;
    //衝突表面法線
    float3 SurNorm;
    //衝突時色
    float4 color;
    //衝突処理
    int type;
    float2 uv;
    

};

struct Ray
{
    float3 orig;
    float3 dir;
    float4 color;
    //現屈折度数
    float rindex;
    //屈折しているか
    bool refracted;
};

struct tri
{
    float3 v[3];
};



inline float trisize(float3 t0,float3 t1,float3 t2);

inline void HitTri(Ray r,inout collision col,tri T,float4 color,int type,float3 N);
inline void HitSphere(Ray r,float3 co,float sc,float pull,inout collision col);
inline void colcheck(inout collision col,float3 hitv,float npos,float3 norm,float4 color,float2 uv);
inline float3 refr(Ray r,float3 sur,float N);








inline float trisize(float3 t0,float3 t1,float3 t2)
{

    return  abs((t0.r*(t1.g-t2.g))+
    (t1.r*(t2.g-t0.g))+
    (t2.r*(t0.g-t1.g)));
}


inline void colcheck(inout collision col,Ray ray,float newvec,float3 norm,int coltype,float4 color,float2 uv)
{//衝突情報更新
    if(col.dist>newvec)
    {
        col.color=color;
        col.dist=newvec;
        col.Pos=ray.orig+ray.dir*newvec;
        col.SurNorm=norm;
        col.type=coltype;
        col.uv=uv;
    }
}

inline void HitTri(Ray r,inout collision col,tri T,float4 color,int type,float3 N)
{

    //https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
    
    float3 edge[2]={(T.v[2]-T.v[0]),(T.v[1]-T.v[0])};

    if(r.refracted)
    N*=-1;

    const float acc=  0.0000001;

    if(dot(r.dir,(N))<0)
    {
        
        float v,f,u,t;
        float3 h,s,q;   


        h=cross(r.dir,edge[1]);
        v=dot(edge[0],h);
		
        if(abs(v)>0)
        {

            s=r.orig-T.v[0];

            f=1.0/v;
            u=f*dot(s,h);

            
            if(u>0&&u<1.0f)
            {
                q=cross(s,edge[0]);

                v=f*dot((r.dir),q);

                if(v>0&&u+v<1)
                {
                    float cv=f*dot(edge[1],q);
                    if(cv>acc){
                        colcheck(col,r,cv,(N),type, color,float2(u,v));
                    }
                }
            }
        }
    }

}





inline void HitSphere(Ray r,float3 co,float sc,float pull,inout collision col)
{
    
    if(dot(r.dir,co-r.orig)>0){


        float3 d=r.orig-co;
        float p1=-dot(r.dir,d);
        float p2s=(pow(p1,2))-dot(-d,-d)+pow(sc,2);
        
        if(p2s>0)//there's collision
        {
            float   p2=sqrt(p2s);
            float a1;/*
            if(!r.bent)
            {
                a1= p1 - p2 >.0 ? p1 - p2 : p1 + p2;
                r.bent=true;
            }
            else
            {
                {a1=p1 - p2 >.0 ? p1 + p2 : p1 -p2;
                r.bent=false;}            
            }*/
            a1= p1 - p2 >.0 ? p1 - p2 : p1 + p2;


            //  colcheck(col,r.orig,(r.dir)*a1,normalize(((r.dir)*a1)-co),1,f4(1));
            
            
            
            
        }
    }

}



inline float3 refr(Ray ray,float3 sur,float RIndex)
{
    //Vector ro Euler Algorithm source
    /*
    http://www.euclideanspace.com/maths/geometry/rotations/conversions/angleToEuler/index.htm
    */

    float2 r;


    //heading = y
    //bank=za
    float theta=acos(dot(sur,-ray.dir));
    float3 plane=cross(sur,-ray.dir);
    float s=sin(theta);
    float c=cos(theta);
    float t=1-c;



    r.y=atan2(
    plane.y * s- plane.x * plane.z * t ,
    1 - (plane.y*plane.y+ plane.z*plane.z ) * t);

    r.x= atan2(plane.x * s - plane.y * plane.z * t ,
    1 - (plane.x*plane.x + plane.z*plane.z) * t);

    r=r*RIndex;
    
    //屈折後の回転を内向きの法線に適応
    
    float3 buf=sur;
    float3 res=sur;

    buf.x = res.x;
    buf.y = (res.y * cos(r.x)) + (res.z * -sin(r.x));
    buf.z = (res.y * sin(r.x)) + (res.z * cos(r.x));


    res=buf;
    buf.x = (res.x * cos(r.y)) + (res.z * sin(r.y));
    buf.y = res.y;
    buf.z = (res.x * -sin(r.y)) + (res.z * cos(r.y));

    buf*=-1;
    
    ray.rindex=RIndex;

    ray.refracted=!ray.refracted;

    return  buf;
}
