  Shader "Instanced/InstancedParticle" {
    Properties {
        _Test2 ("Test2", COLOR) = (1,0,0,1)
        _Test1 ("Test1", COLOR) = (1,1,0.5,1)
        _Force ("Force", COLOR) = (1, 0, 1, 1)
        _Size ("Size", float) = 0.035
        _Skin ("Skin", COLOR) = (0.76,0.55,0.57,1)
        _Liver ("Liver", COLOR) = (0.422, 0.17, 0.117, 1)
        _Cut ("Cut", COLOR) = (0,0,0,0)
    }

    SubShader {
        Tags { "Queue"="Overlay+1" }
        ZTest LEqual
        Pass {
            Tags { "LightMode"="ForwardBase" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "False" }
            Cull Back
            ZWrite On
            //Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM

            #pragma glsl
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5
            
            #include "UnityCG.cginc"

            // matches the structure of our data on the CPU side
            struct Particle {
                float3 x;
                float3 v;
                float3 C[3];
                float mass;
                float padding;

                float elastic_lambda;
                float elastic_mu;

                float spacing;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            
            //float _Size;
            float4 _Test2;
            float4 _Test1;
            float4 _Force;
            float4 _Skin;
            float4 _Liver;
            float4 _Cut;


            StructuredBuffer<Particle> particle_buffer;

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID) {
                // take in data from the compute buffer, filled with data each frame in SimRenderer
                // offsetting and scaling it from the (0...grid_res, 0...grid_res) resolution of our sim into a nicer range for rendering
                float4 data = float4((particle_buffer[instanceID].x.xyz - float3(0, 0, 0)) * 0.1, 1.0);
                
                // Scaling vertices by our base size param (configurable in the material) and the mass of the particle
                float3 localPosition = v.vertex.xyz * (0.02 * data.w);
                float3 worldPosition = data.xyz + localPosition;
				

                // project into camera space
                v2f o;
                o.pos = float4(0,0,0,0);
                o.color = float4(0,0,0,0);

                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                float lightDot = clamp(dot(v.normal, lightDir), -1, 1);
                lightDot = exp(-pow(2.0f*(1 - lightDot), 1.2f));
                lightDot += ShadeSH9(half4(v.normal, 1));


                //assign color based on lambda
                if (particle_buffer[instanceID].elastic_lambda == 10.0f) {
                    
                        o.color = _Skin * lightDot;
                    
                } else if (particle_buffer[instanceID].elastic_lambda < 10.0f) {
                    
                   
                        o.color = _Test2 * lightDot;
                    
                } else {
                    o.color = _Cut;
                }
                
                return o;
            }

            fixed4 frag (v2f i, uint instanceID : SV_InstanceID) : SV_Target {

                return i.color;
            }


            ENDCG
        }
    }
}
