using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;


public class RSMAIN : MonoBehaviour
{
/*
Shader-Engine Interface
シェーダー-エンジン間インターフェース
*/


	//*シェーダーアウトプット関係*//
    public ComputeShader CPShader;
    public static RenderTexture RTraceImage;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (initialized)
        {
            Render();
            Graphics.Blit(RTraceImage, destination);
            // GetComponent<Camera>().transform.position=new Vector3(0,0,0);

        }

    }
    private void Update()
    {

        if (MeshDataCollector.constructed && !initialized)
        {
            initialize();

        }
        if (initialized)
        {
            UpdateShaderParam();
        }







    }

    private void Render()
    {

        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        CPShader.SetTexture(0, "Result", RTraceImage);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 32.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 32.0f);
        CPShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        // Blit the result texture to the screen


    }

    private void InitRenderTexture()
    {
   
        if (RTraceImage != null)
            RTraceImage.Release();

        RTraceImage = new RenderTexture(
            Screen.width,
            Screen.height, 
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);

        RTraceImage.enableRandomWrite = true;
        RTraceImage.Create();
        
    }
    
    
    
    //*シェーダー-エンジン間情報インターフェース*//
    

    List<Vector4> v3to4(List<Vector3> v)
    {
        List<Vector4> buf = new List<Vector4>();
        foreach (Vector3 b in v)
        {
            buf.Add(new Vector4(b.x, b.y, b.z, 0));
        }
        return buf;
    }

//==VARIABLE==//
    private Camera _camera;
    
    //変動のない情報
    //点座標　　
    private List<Vector4> VertexBuf;
    //オブジェクト毎インデックス
    private List<int> ObjStride;
    
    /// <summary>
    /// Vertex Index, Per Triangle　三角インデックス
    /// </summary>
    List<Vector4> TriIndex_ObjStride;
    //* CONSTANT BUFFER END*//
    //変動のない情報終了

    //* INSTANCE DATA BUFFER*//
    //個体毎座標
    private List<Transform> Inst_T;
    /// <summary>
    /// *XYZ COLOR, W MODEL INDEX　個体色、モデルID
    /// </summary>
    private List<Vector4> Inst_CI;
    /// <summary>
    ///* X=TYPE Y=INDEX　X=衝突時処理 Y=屈折度数
    /// </summary>
    public List<ObjectType> Inst_TI;
    //* INSTANCE DATA END*//

    private bool initialized = false;
    
    //*FUNCTIONS*//
    private void initialize()
    {
        initialized = true;
        InitRenderTexture();

        if (CPShader == null)
        {
            Application.Quit();
        }

        _camera = GetComponent<Camera>();

	TriIndex_ObjStride = new List<Vector4>();
        Inst_T = new List<Transform>();
        Inst_CI = new List<Vector4>();
        Inst_TI = new List<ObjectType>();

	//シーンから集めたモデル情報を取得
        MeshDataCollector.RenderData rd = new MeshDataCollector.RenderData();
        rd = MeshDataCollector.GetVlist();

        VertexBuf = v3to4(rd.VertexBuffer);

        ObjStride = new List<int>();
                ObjStride.AddRange(rd.ObjectStride);
   


        //三角インデックス生成
        //SetfloatArrayが機能しないためインデックス4次元目にオフセットを記録
        {
            List<int> VertexStride = rd.VerterxStride;

            int k = 0;

            for (int i = 0; i < VertexStride.Count / 3; i++)
            {
                if (ObjStride.Count > i)
                {
                    k = ObjStride[i];
                }

                TriIndex_ObjStride.Add(
                    new Vector4(
                        VertexStride[i * 3],
                        VertexStride[i * 3 + 1],
                        VertexStride[i * 3 + 2],
                        k / 3)
                        );


                //            Debug.Log(TriIndex_ObjStride[i].ToString());
                k = 0;
            }
        }


        //オブジェクト毎データを変換/記録

        foreach (var b in MeshDataCollector.InstanceList)
        {
            Inst_T.Add(b.transform);
            Inst_CI.Add(new Vector4(b.color.r, b.color.g, b.color.b, b.Model));

            Inst_TI.Add(b.Type_Index);
        }

        SetShaderParameters();

    }


    /// <summary>
    /// モデル等変更の無いデータの生成
    /// </summary>
    private void SetShaderParameters()
    {//Name Must Match, case sensitive. but ordering does not



        //Normal Precomputation
        //法線生成
        List<Vector4> NormalL = new List<Vector4>();

        {
            Vector3 buf = new Vector3(0, 0, 0);
            for (int i = 0; i < Inst_CI.Count; i++)
            {
                buf = new Vector3(0, 0, 0);
                int offset = (int)(TriIndex_ObjStride[(int)Inst_CI[i].w * 3 + 2].w);

                for (
                       int tr = (int)TriIndex_ObjStride[(int)Inst_CI[i].w * 3].w;
                        tr <
                        (int)TriIndex_ObjStride[(int)Inst_CI[i].w * 3].w +
                        (int)TriIndex_ObjStride[(int)(Inst_CI[i].w * 3) + 1].w;

                        tr++
                )
                {
                    buf = Vector3.Cross(
                              VertexBuf[(int)TriIndex_ObjStride[tr].y + offset] - VertexBuf[(int)TriIndex_ObjStride[tr].x + offset],
                              VertexBuf[(int)TriIndex_ObjStride[tr].z + offset] - VertexBuf[(int)TriIndex_ObjStride[tr].x + offset]);

                    buf.Normalize();
                    NormalL.Add(new Vector4(buf.x, buf.y, buf.z, 0));
                }



            }
        }

        CPShader.SetInt("_TriCount", TriIndex_ObjStride.Count);
        CPShader.SetVectorArray("_VertexBuffer", VertexBuf.ToArray());
        CPShader.SetVectorArray("_VertexIndice", TriIndex_ObjStride.ToArray());
        CPShader.SetVectorArray("_NormalArray", NormalL.ToArray());

        //*Instance count
        CPShader.SetInt("_InstanceCount", MeshDataCollector.InstanceList.Count);

        //*Color, Model Index
        CPShader.SetVectorArray("_Inst_ColandIndex", Inst_CI.ToArray());


        CPShader.SetMatrix("_IPJMX", _camera.projectionMatrix.inverse);

        {
            List<Vector4> Col_MIndex = new List<Vector4>();

            foreach (var b in MeshDataCollector.InstanceList)
            {
                Col_MIndex.Add(new Vector4(b.color.r, b.color.g, b.color.b, b.Model));
            }
            CPShader.SetVectorArray("_Inst_ColandIndex", Col_MIndex.ToArray());
        }

        Inst_CI.Clear();
        TriIndex_ObjStride.Clear();
        VertexBuf.Clear();
        MeshDataCollector.Mlist.Clear();
    }




    /// <summary>
    /// 座標等変更のありえる数値の更新
    /// </summary>
    private void UpdateShaderParam()
    {


        List<Vector4> CoordsInst = new List<Vector4>();
        List<Vector4> Type_RIndex = new List<Vector4>();
        List<Matrix4x4> RotInst = new List<Matrix4x4>();
        List<Matrix4x4> Rot = new List<Matrix4x4>();

        foreach (var b in MeshDataCollector.InstanceList)
        {
            Rot.Add(Matrix4x4.Rotate(b.transform.rotation));
            CoordsInst.Add(new Vector4(b.transform.position.x, b.transform.position.y, b.transform.position.z, 0));
            RotInst.Add(b.transform.localToWorldMatrix);
            Type_RIndex.Add(new Vector4((int)b.Type_Index.Type, b.Type_Index.RefIndex, 0, 0));

        }



        //*Coords
        CPShader.SetVectorArray("_Inst_Translate", CoordsInst.ToArray());
        //*Scale/Rotation
        CPShader.SetMatrixArray("_Inst_ScaleRot", RotInst.ToArray());
        CPShader.SetMatrixArray("_Inst_RotOnly", Rot.ToArray());

        //*TYPE,REFRAC INDEX

        CPShader.SetVectorArray("_Type_Rindex", Type_RIndex.ToArray());
        CPShader.SetMatrix("_C2WMX", _camera.cameraToWorldMatrix);



    }



}
