using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using VoxelSystem;

using UnityEngine.Jobs;
using UnityEngine.UI;
using System.Collections.Generic;

using UnityEngine.Profiling;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;

public class MLS_MPM_NeoHookean_Multithreaded : MonoBehaviour {
    struct Particle {
        public float3 x; // position
        public float3 v; // velocity
        public float3x3 C; // affine momentum matrix
        public float mass;
        public float volume_0; // initial volume

        public float elastic_lambda;
        public float elastic_mu;

        public float spacing; 
    }

    struct Cell {
        public float3 v; // velocity
        public float mass;
        public float padding; // unused
    }
    

    const int grid_res = 16;

    //number of grid cells
    const int num_cells = grid_res * grid_res * grid_res;

    // batch size for the job system. just determined experimentally
    const int division = 16;
    
    // simulation parameters
    const float dt = 0.15f; // timestep
    const float iterations = (int)(1.0f / dt);
    const float gravity = -0.5f;

    // Lamé parameters for stress-strain relationship
    const float lambda = 10.0f;
    const float mu = 20.0f;
    
    NativeArray<Particle> ps; // particles
    NativeArray<Cell> grid;
    
    // deformation gradient. stored as a separate array to use same rendering code for all demos, but feel free to store this field in the particle struct instead
    NativeArray<float3x3> Fs;

    float3[] weights = new float3[3];

    int num_particles;
    List<float3> temp_positions;
    
    SimRenderer sim_renderer;
    
    // interaction
    const float mouse_radius = 10.0f;
    bool mouse_down = false;
    float3 mouse_pos;
    float2 [] mouse_rect = new float2[4];

    //for voxels
    public List<VoxelSystem.Voxel_t> voxels;
	[SerializeField] protected Mesh mesh;
    [SerializeField] protected int resolution = 24;
    [SerializeField] protected bool useUV = false;

    [SerializeField] public static Transform laserPtr;
    float3 laser;
    float3 laserDir;

    LineRenderer lineRenderer;


    //UI



    void spawn_box(int x, int y, int z, int box_x = 8, int box_y = 8, int box_z = 8) {
        const float spacing = 0.5f;
        for (float i = -box_x / 2; i < box_x / 2; i += spacing) {
            for (float j = -box_y / 2; j < box_y / 2; j += spacing) {
                for (float k = -box_z / 2; k < box_z / 2; k += spacing) {
                    var pos = math.float3(x + i, y + j, z + k);

                    temp_positions.Add(pos);
                }
            }
        }
    }

    void meshNormalized(in List<VoxelSystem.Voxel_t> voxels) {
        float3 min = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        float3 max = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i < voxels.Count; i++) {
            var pos = voxels[i].position;

            min.x = math.min(min.x, pos.x);
            min.y = math.min(min.y, pos.y);
            min.z = math.min(min.z, pos.z);

            max.x = math.max(max.x, pos.x);
            max.y = math.max(max.y, pos.y);
            max.z = math.max(max.z, pos.z);
        }

        float scale_x, scale_y, scale_z;

        scale_x = math.abs(max.x - min.x);
        scale_y = math.abs(max.y - min.y);
        scale_z = math.abs(max.z - min.z);

        Debug.Log(scale_x + " " + scale_y + " " + scale_z);

        float scale = math.max(scale_x, math.max(scale_y, scale_z));

        for(int j = 0; j < voxels.Count; j++) {
            var position = voxels[j].position;

            position = (position - new Vector3(scale, scale, scale)) / (2*scale) + new Vector3(1.0f, 1.0f, 1.0f) ;
            position *= (grid_res - 6);

            //Debug.Log(position);

            temp_positions.Add(position);

        }


    }
    

    void Start () {
        
        laserPtr = GameObject.Find("LaserPtr").transform;
        
        lineRenderer = GameObject.Find("LaserPtr").AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(255, 0, 0);
        lineRenderer.endColor = lineRenderer.startColor; 
        lineRenderer.widthMultiplier = 0.1f;
        

        laser = new float3(laserPtr.position.x, laserPtr.position.y, laserPtr.position.z);
        laserDir = new float3(laserPtr.up.x,laserPtr.up.y,laserPtr.up.z);
        // Debug.Log(laser);
        // Debug.Log(laserDir);
        
        // populate our array of particles


        temp_positions = new List<float3>();
        //spawn_box(grid_res/2, grid_res/2 , grid_res/2, 15, 15, 15);
        //var c  = CPUVoxelizer.Voxelize();
        
        spawn_box(grid_res/2, grid_res/2 , grid_res/2, grid_res/2, grid_res/2, grid_res/2);

        //Comment out next 3 lines to get rid of custom mesh
        // float unit;
        // VoxelSystem.CPUVoxelizer.Voxelize(mesh, resolution, out voxels, out unit);
        // meshNormalized(voxels);
        // for (int i = 0; i < voxels.Count; i++) {
        //     var voxel = voxels[i];
        //     voxel.position = voxel.position*0.0045f + new Vector3(5f, 5f, 5f) ;
        //     //Debug.Log(voxel.position);

        //     temp_positions.Add(voxel.position);
        // }
        


        
        num_particles = temp_positions.Count;

        //Debug.Log(num_particles + " " + num_cells);

        ps = new NativeArray<Particle>(num_particles, Allocator.Persistent);
        Fs = new NativeArray<float3x3>(num_particles, Allocator.Persistent);

        // initialise particles
          for (int i = 0; i < num_particles; ++i) {
            Particle p = new Particle();
            p.x = temp_positions[i];
            p.v = 0;
            p.C = 0;
            p.mass = 1.0f;

            // if (i >= ( num_particles / 2)) {p.elastic_lambda = 100.0f;}
            // else {p.elastic_lambda = 100.0f;}
            p.elastic_lambda = lambda;
            
            p.elastic_mu = mu;

            ps[i] = p;

            // deformation gradient initialised to the identity
            Fs[i] = math.float3x3(
                1, 0, 0,
                0, 1, 0,
                0, 0, 1
            );
        }

        //Debug.Log(num_particles);

        grid = new NativeArray<Cell>(num_cells, Allocator.Persistent);

        for (int i = 0; i < num_cells; ++i) {
            var cell = new Cell();
            cell.v = 0;
            grid[i] = cell;
        }

        // ---- begin precomputation of particle volumes
        // MPM course, equation 152 

        // launch a P2G job to scatter particle mass to the grid
        new Job_P2G() {
            ps = ps,
            Fs = Fs,
            grid = grid,
            num_particles = num_particles
        }.Schedule().Complete();
        
        for (int i = 0; i < num_particles; ++i) {
            var p = ps[i];

            // quadratic interpolation weights
            float3 cell_idx = math.floor (p.x) ;
            float3 cell_diff = (p.x - cell_idx) - 0.5f;
            weights[0] = 0.5f * math.pow(0.5f - cell_diff, 2);
            weights[1] = 0.75f - math.pow(cell_diff, 2);
            weights[2] = 0.5f * math.pow(0.5f + cell_diff, 2);
           
            float density = 0.0f;
            // iterate over neighbouring 3x3x3 cells
            for (int gx = 0; gx < 3; ++gx) {
                for (int gy = 0; gy < 3; ++gy) {
                    for (int gz = 0; gz < 3; ++gz) {
                        float weight = weights[gx].x * weights[gy].y * weights[gz].z;

                        // map 3D to 1D index in grid x*WIDTH*DEPTH + y*WIDTH + z
                        int cell_index = ((int)cell_idx.x + (gx - 1))*grid_res*grid_res + ((int)cell_idx.y + (gy - 1)) * grid_res + ((int)cell_idx.z + gz - 1);
                        density += grid[cell_index].mass * weight;
                    }
                    
                }
            }

            // per-particle volume estimate has now been computed
            float volume = p.mass / density;
            p.volume_0 = volume;

            ps[i] = p;
        }

        // ---- end precomputation of particle volumes

        // boilerplate rendering code handled elsewhere
        sim_renderer = GameObject.FindObjectOfType<SimRenderer>();
        sim_renderer.Initialise(num_particles, Marshal.SizeOf(new Particle()));

       //Debug.Log(Marshal.SizeOf(new Particle()));
    }

    void initialize_particles(int particle_type) {
      
    }

    private void Update() {
        laser.x = laserPtr.position.x;
        laser.y = laserPtr.position.y;
        laser.z = laserPtr.position.z;

        laserDir.x = laserPtr.up.x;
        laserDir.y = laserPtr.up.y;
        laserDir.z = laserPtr.up.z;

        lineRenderer.SetPosition(0, laserPtr.position);
        lineRenderer.SetPosition(1, laserPtr.position+laserPtr.up*5);

        HandleMouseInteraction();

        // for (int i = 0; i < iterations; ++i) {
        //     Simulate();

        // }
        Simulate();
        
        

        sim_renderer.RenderFrame(ps);
    }



    void HandleMouseInteraction() {
        mouse_down = false;
        if (Input.GetMouseButton(0)) {
            mouse_down = true;
            var mp = Camera.main.ScreenToViewportPoint(Input.mousePosition);

            Cutter cutter =  GameObject.Find("3D_Cursor").GetComponent<Cutter>();
            Vector3 cursor_pos = cutter.mousePos;

            mouse_pos = math.float3(cursor_pos.x * grid_res, cursor_pos.y * grid_res, cursor_pos.z * grid_res);

            
            
        }
    }
    
    void Simulate() {
        Profiler.BeginSample("ClearGrid");
        new Job_ClearGrid() {
            grid = grid
        }.Schedule(num_cells, division).Complete();
        Profiler.EndSample();

        // P2G, first round
        Profiler.BeginSample("P2G");
        new Job_P2G() {
            ps = ps,
            Fs = Fs,
            grid = grid,
            num_particles = num_particles
        }.Schedule().Complete();
        Profiler.EndSample();
        
        Profiler.BeginSample("Update grid");
        new Job_UpdateGrid() {
            grid = grid
        }.Schedule(num_cells, division).Complete();
        Profiler.EndSample();
        
        Profiler.BeginSample("G2P");
        new Job_G2P() {
            ps = ps,
            Fs = Fs,
            mouse_down = mouse_down,
            mouse_pos = mouse_pos,
            grid = grid,
            laser = laser,
            laserDir = laserDir
        }.Schedule(num_particles, division).Complete();
        Profiler.EndSample();
    }

    #region Jobs
    [BurstCompile]
    struct Job_ClearGrid : IJobParallelFor {
        public NativeArray<Cell> grid;

        public void Execute(int i) {
            var cell = grid[i];

            // reset grid scratch-pad entirely
            cell.mass = 0;
            cell.v = 0;

            grid[i] = cell;
        }
    }
    
    [BurstCompile]
    unsafe struct Job_P2G : IJob {
        public NativeArray<Cell> grid;
        [ReadOnly] public NativeArray<Particle> ps;
        [ReadOnly] public NativeArray<float3x3> Fs;
        [ReadOnly] public int num_particles;
        
        public void Execute() {
            var weights = stackalloc float3[3];

            for (int i = 0; i < num_particles; ++i) {
                var p = ps[i];
                
                float3x3 stress = 0;

                // deformation gradient
                var F = Fs[i];

                var J = math.determinant(F);

                // MPM course, page 46
                var volume = p.volume_0 * J;

                // useful matrices for Neo-Hookean model
                var F_T = math.transpose(F);
                var F_inv_T = math.inverse(F_T);
                var F_minus_F_inv_T = F - F_inv_T;

                // MPM course equation 48
                var P_term_0 = p.elastic_mu * (F_minus_F_inv_T);
                var P_term_1 = p.elastic_lambda * math.log(J) * F_inv_T;
                var P = P_term_0 + P_term_1;

                // cauchy_stress = (1 / det(F)) * P * F_T
                // equation 38, MPM course
                stress = (1.0f / J) * math.mul(P, F_T);

                // (M_p)^-1 = 4, see APIC paper and MPM course page 42
                // this term is used in MLS-MPM paper eq. 16. with quadratic weights, Mp = (1/4) * (delta_x)^2.
                // in this simulation, delta_x = 1, because i scale the rendering of the domain rather than the domain itself.
                // we multiply by dt as part of the process of fusing the momentum and force update for MLS-MPM
                var eq_16_term_0 = -volume * 4 * stress * dt;

                // quadratic interpolation weights
                uint3 cell_idx = (uint3) p.x ;
                float3 cell_diff = (p.x - cell_idx) - 0.5f;
                weights[0] = 0.5f * math.pow(0.5f - cell_diff, 2);
                weights[1] = 0.75f - math.pow(cell_diff, 2);
                weights[2] = 0.5f * math.pow(0.5f + cell_diff, 2);

                // for all surrounding 9 cells
                for (uint gx = 0; gx < 3; ++gx) {
                    for (uint gy = 0; gy < 3; ++gy) {
                        for (uint gz = 0; gz < 3; ++gz) {
                        float weight = weights[gx].x * weights[gy].y * weights[gz].z;
                        
                        uint3 cell_x = new uint3(cell_idx.x + gx - 1, cell_idx.y + gy - 1, cell_idx.z + gz - 1);
                        float3 cell_dist = (cell_x - p.x) + 0.5f;
                        float3 Q = math.mul(p.C, cell_dist);

                        // scatter mass and momentum to the grid
                         // map 3D to 1D index in grid x*WIDTH*DEPTH + y*WIDTH + z
                        int cell_index = ((int)cell_x.x)*grid_res*grid_res + ((int)cell_x.y)*grid_res + ((int)cell_x.z);
                        //if (cell_index > 32768) Debug.Log(cell_idx);
                        //int tPrint = 13824 - cell_index; 
                        //string pRINT = string.Format("index is {0}", tPrint); 
                        //Debug.Log(pRINT);
                        Cell cell = grid[cell_index];

                        // MPM course, equation 172
                        float weighted_mass = weight * p.mass;
                        cell.mass += weighted_mass;

                        // APIC P2G momentum contribution
                        cell.v += weighted_mass * (p.v + Q);

                        // fused force/momentum update from MLS-MPM
                        // see MLS-MPM paper, equation listed after eqn. 28
                        float3 momentum = math.mul(eq_16_term_0 * weight, cell_dist);
                        cell.v += momentum;

                        // total update on cell.v is now:
                        // weight * (dt * M^-1 * p.volume * p.stress + p.mass * p.C)
                        // this is the fused momentum + force from MLS-MPM. however, instead of our stress being derived from the energy density,
                        // i use the weak form with cauchy stress. converted:
                        // p.volume_0 * (dΨ/dF)(Fp)*(Fp_transposed)
                        // is equal to p.volume * σ

                        // note: currently "cell.v" refers to MOMENTUM, not velocity!
                        // this gets converted in the UpdateGrid step below.

                        grid[cell_index] = cell;
                        }
                    }
                }
            }
        }
    }

    [BurstCompile]
    struct Job_UpdateGrid : IJobParallelFor {
        public NativeArray<Cell> grid;

        public void Execute(int i) {
            var cell = grid[i];

            if (cell.mass > 0) {
                // convert momentum to velocity, apply gravity
                cell.v /= cell.mass;
                cell.v += dt * math.float3(0, gravity, 0);

                // 'slip' boundary conditions
                int x = i / (grid_res * grid_res);
                int y = (i / grid_res) % grid_res;
                int z = i % grid_res;

                int res = 3;
                if (x < res || x > grid_res - res) { cell.v.x = 0; }
                if (y < res || y > grid_res - res) { cell.v.y = 0; }
                if (z < res || z > grid_res - res) { cell.v.z = 0; }
                

                grid[i] = cell;
            }
        }
    }

    [BurstCompile]
    unsafe struct Job_G2P : IJobParallelFor {
        public NativeArray<Particle> ps;
        public NativeArray<float3x3> Fs;
        [ReadOnly] public NativeArray<Cell> grid;

        [ReadOnly] public bool mouse_down;
        [ReadOnly] public float3 mouse_pos;

        [ReadOnly] public float3 laser;
        [ReadOnly] public float3 laserDir;
        
        public void Execute(int i) {
            Particle p = ps[i];

            // reset particle velocity. we calculate it from scratch each step using the grid
            p.v = 0;

            // quadratic interpolation weights
            uint3 cell_idx = (uint3)p.x ;
            float3 cell_diff = (p.x - cell_idx) - 0.5f;
            var weights = stackalloc float3[] {
                0.5f * math.pow(0.5f - cell_diff, 2),
                0.75f - math.pow(cell_diff, 2), 
                0.5f * math.pow(0.5f + cell_diff, 2)
            };
            
            // constructing affine per-particle momentum matrix from APIC / MLS-MPM.
            // see APIC paper (https://web.archive.org/web/20190427165435/https://www.math.ucla.edu/~jteran/papers/JSSTS15.pdf), page 6
            // below equation 11 for clarification. this is calculating C = B * (D^-1) for APIC equation 8,
            // where B is calculated in the inner loop at (D^-1) = 4 is a constant when using quadratic interpolation functions
            float3x3 B = 0;
            for (uint gx = 0; gx < 3; ++gx) {
                for (uint gy = 0; gy < 3; ++gy) {
                    for (uint gz = 0; gz < 3; ++gz) {
                    float weight = weights[gx].x * weights[gy].y * weights[gz].z;

                    uint3 cell_x = math.uint3(cell_idx.x + gx - 1, cell_idx.y + gy - 1, cell_idx.z + gz - 1);
                    
                    // map 3D to 1D index in grid x*WIDTH*DEPTH + y*WIDTH + z
                    int cell_index = ((int)cell_x.x)*grid_res*grid_res + ((int)cell_x.y)*grid_res + ((int)cell_x.z);
                    
                    float3 dist = (cell_x - p.x) + 0.5f;
                    float3 weighted_velocity = grid[cell_index].v * weight;


                    // APIC paper equation 10, constructing inner term for B
                    var term = math.float3x3(weighted_velocity * dist.x, weighted_velocity * dist.y, weighted_velocity * dist.z);

                    B += term;

                    p.v += weighted_velocity;
                    }
                }
            }
            p.C = B * 4;

            // advect particles
            p.x += p.v * dt;

            // safety clamp to ensure particles don't exit simulation domain
            p.x = math.clamp(p.x, 1, grid_res - 2);
            
            // mouse interaction
            if ( true || mouse_down) {
                // var dist = p.x - mouse_pos;

                // var dist_x = dist.x;
                // var dist_y = dist.y;
                // var dist_z = dist.z;

               
                // if (math.dot(dist, dist) < 100.0f) {
                //     var force = math.normalize(dist) * 2;
                //     p.v = force * 2;
                //     //p.aForce = math.sqrt(math.pow(force.x, 2) + math.pow(force.y, 2) + math.pow(force.z, 2));
                // }

                // if (math.dot(dist, dist) < 0.2f) {

                //     //p.x = math.float2(100.0f, 50.0f + UnityEngine.Random.Range(0.0f, 100.0f));
                //     p.mass = 0.0f;
                //     p.v = 0.0f;
                //     p.elastic_mu = 0.0f;
                //     p.elastic_lambda = 0.0f;
                //     p.C = math.float3x3 (0,0,0,
                //                          0,0,0,
                //                          0,0,0);
                // }
                // float t = (-p.x.x*laser.x+math.pow(laser.x,2)-p.x.y*laser.y+math.pow(laser.y, 2)-p.x.z*laser.z+math.pow(laser.z, 2));
                // float h = (p.x.x*laserDir.x - 2*p.x.x*laserDir.x+p.x.y*laserDir.y - 2*p.x.y*laserDir.y+p.x.z*laserDir.z - 2*p.x.z*laserDir.z);
                // t /= -h;
                // float distance = math.sqrt(math.pow(p.x.x-laser.x-laserDir.x*t, 2) + math.pow(p.x.y-laser.y-laserDir.y*t, 2) + math.pow(p.x.z-laser.z-laserDir.z*t, 2));
                
                
                
                //-------------Attempt 2-------------------
                //From https://math.stackexchange.com/questions/1905533/find-perpendicular-distance-from-point-to-line-in-3d
                //laserDir*5 + laser = C
                // laser = B
                //p.x*scale = A
                //v = p.x*scale - laser
                // t = v dot laserDir
                
                // float t;
                // float distance;
                float3 scale = new float3(0.1f, 0.1f, 0.1f);
                // float3 boing = laserDir*5; 
                // float3 d = (laserDir*5) / math.sqrt(math.pow(boing.x, 2) + math.pow(boing.y, 2) + math.pow(boing.z, 2));
                // float3 v = p.x*scale - laser;
                // t = math.dot(d, v);
                // float3 P = laser + math.dot(t, d);
                // distance = math.sqrt(math.pow(P.x-p.x.x*0.1f, 2) + math.pow(P.x-p.x.y*0.1f, 2) + math.pow(P.z-p.x.z*0.1f, 2));
                //Debug.Log($"Distance is {distance}");
                
                //--------------Attempt 3-------------
                float3 BA = p.x*scale - laser;
                float3 BC = laserDir*5;
                
                float3 d = math.cross(BA, BC);
                float distance = math.sqrt(math.pow(d.x, 2) + math.pow(d.y, 2) + math.pow(d.z, 2)); 
                float div = math.sqrt(math.pow(BC.x, 2) + math.pow(BC.y, 2) + math.pow(BC.z, 2));
                distance /= div;


                if (distance < 0.1f) {
                    p.mass = 0.0f;
                    p.v = 0.0f;
                    p.elastic_mu = 0.0f;
                    p.elastic_lambda = 0.0f;
                    p.C = math.float3x3 (0,0,0,
                                         0,0,0,
                                         0,0,0);
                }

            }

            // deformation gradient update - MPM course, equation 181
            // Fp' = (I + dt * p.C) * Fp
            var Fp_new = math.float3x3(
                1, 0, 0, 
                0, 1, 0,
                0, 0, 1
            );
            Fp_new += dt * p.C;
            Fs[i] = math.mul(Fp_new, Fs[i]);

            ps[i] = p;
        }
    }

    #endregion


    private void OnDestroy() {
        ps.Dispose();
        grid.Dispose();
        Fs.Dispose();
    }
}

