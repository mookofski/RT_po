using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MeshDataCollector : MonoBehaviour
{

    public static List<Mesh> Mlist;
    public List<string> Midentity;
    public static int MIndex;

    public static bool constructed = false;

    public static List<ObjectInstnce> InstanceList;


    public struct ObjectInstnce
    {
        public Transform transform;
        public int Model;
        public Color color;
        /// <summary>
        /// X:Type Y:ModelID
        /// </summary>
        public ObjectType Type_Index;
    }

    public ObjectInstnce MakeOinstnce(Transform t, int modelindex, Color col, ObjectType ot)
    {
        ObjectInstnce buf = new ObjectInstnce();

        buf.transform = t;
        buf.Model = modelindex;
        buf.color = col;
        buf.Type_Index = ot;
        return buf;
    }


    public struct RenderData
    {
        public List<Vector3> VertexBuffer;


        /// <summary>
        ///stores vertex index: Made per Tri
        /// </summary>
        public List<int> VerterxStride;


        /// <summary>
        ///store position of each object's vertex head: Made per Mesh//
        /// </summary>
        public int[] ObjectStride;

    };


    private void Start()
    {
        InstanceList = new List<ObjectInstnce>();
        Mlist = new List<Mesh>();
        List<MeshFilter> mlist_temp = new List<MeshFilter>();


        mlist_temp.AddRange(FindObjectsOfType<MeshFilter>());



        bool dupe = false;


        Midentity = new List<string>();

        //initialize first index
        foreach (MeshFilter b in mlist_temp)
        {
            dupe = false;
            b.TryGetComponent(out ObjectType ot);
            //if no object type found
            if (ot == null)
            {
                ot = new ObjectType();
                ot.Type = 0;
                ot.RefIndex = 0;
            }

            if (ot.Type != ObjectType.ObjectTypeID.ignore)
            {
                foreach (Mesh c in Mlist)
                {
                    if (Equals(c.name, b.mesh.name))
                    {
                        dupe = true;
                        break;
                    }
                }
                if (!dupe)
                {
                    Mlist.Add(b.mesh);
                    Midentity.Add(b.mesh.name);
                }

                InstanceList.Add(
                    MakeOinstnce(
                        b.GetComponentInParent<Transform>(),
                        Midentity.IndexOf(b.mesh.name),
                        ot.GetComponentInParent<MeshRenderer>().material.color,
                        ot
                    ));


            }
        }

        MIndex = Mlist.Count;
        constructed = true;


    }

    /// <summary>
    /// Creates Constant Data to be sent to the Shader| |
    /// Vertex Coords, Indices, Index for index etc
    /// 点座標/インデックス等非変動データ生成
    /// </summary>
    /// <returns></returns>
    public static RenderData GetVlist()
    {

        RenderData rd = new RenderData();
        rd.VertexBuffer = new List<Vector3>();
        rd.VerterxStride = new List<int>();
        rd.ObjectStride = new int[Mlist.Count * 3];
        {


            int k = 0;
            //*Offsets index value 
            int IndexOffset = 0;
            for (int i = 0; i < Mlist.Count; i++)//*RUN PER UNIQUE MODEL
            {

                //vertex data
                rd.VertexBuffer.AddRange(Mlist[i].vertices);
                //triangle index
                rd.VerterxStride.AddRange(Mlist[i].triangles);
                //triangle index offset per unique model

                //start of index
                rd.ObjectStride[i * 3] = k;
                //vertex count in mdoel
                rd.ObjectStride[(i * 3) + 1] = rd.VerterxStride.Count - k;
                //Hightest value of each index per model    
                rd.ObjectStride[(i * 3) + 2] = IndexOffset * 3;

                //*update cumulative index offset  
                IndexOffset += (Mlist[i].triangles.Max() + 1);


                k = rd.VerterxStride.Count;

            }
        }

        return rd;

    }



    // Update is called once per frame
}
