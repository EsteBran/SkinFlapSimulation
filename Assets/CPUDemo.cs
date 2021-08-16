using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace VoxelSystem.Demo {

    [RequireComponent (typeof(MeshFilter))]
    public class CPUDemo : MonoBehaviour {
        
        public List<Voxel_t> voxels;
		[SerializeField] protected Mesh mesh;
        [SerializeField] protected int resolution = 24;
        [SerializeField] protected bool useUV = false;

        void Start () {
           
            float unit;
            CPUVoxelizer.Voxelize(mesh, resolution, out voxels, out unit);

            // for (int i = 0; i < voxels.Count(); i++) {
            //     Debug.Log(voxels[i].position);
            // }

            // var filter = GetComponent<MeshFilter>();
            // filter.sharedMesh = VoxelMesh.Build(voxels.ToArray(), unit, useUV);
        }

    }
}



