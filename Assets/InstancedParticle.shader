Shader "Instanced/InstancedParticle" {
    Properties {
        _Colour ("Colour", COLOR) = (1, 1, 1, 1)
        _Colour1 ("Colour2", COLOR) = (0.5, 0.5, 0.5, 0.5)
        _Colour2 ("Colour2", COLOR) = (0,0,0,0)
        _Force ("Force", COLOR) = (1, 0, 1, 1)
        _Size ("Size", float) = 0.035
    }

    SubShader {
        Tags { "Queue"="Overlay+1" }
        ZTest Always
        Pass {
            Tags { "LightMode"="ForwardBase" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

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

                float aForce;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            float _Size;
            fixed4 _Colour;
            fixed4 _Colour1;
            fixed4 _Colour2;
            fixed4 _Force;

            StructuredBuffer<Particle> particle_buffer;

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID) {
                // take in data from the compute buffer, filled with data each frame in SimRenderer
                // offsetting and scaling it from the (0...grid_res, 0...grid_res) resolution of our sim into a nicer range for rendering
                float4 data = float4((particle_buffer[instanceID].x.xyz - float3(32, 32, 32)) * 0.1, 1.0);
                
                // Scaling vertices by our base size param (configurable in the material) and the mass of the particle
                float3 localPosition = v.vertex.xyz * (_Size * data.w);
                float3 worldPosition = data.xyz + localPosition;
				
                // project into camera space
                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));


                //assign color based on lambda
                if (particle_buffer[instanceID].elastic_lambda > 10.0f) {
                    if (particle_buffer[instanceID].aForce > 0) {
                        o.color = _Force;
                    }
                    else {
                        o.color = _Colour1;
                    }
                } else if (particle_buffer[instanceID].elastic_lambda > 0.0f) {
                    if (particle_buffer[instanceID].aForce > 0) {
                        o.color = _Force;
                    }
                    else {
                        o.color = _Colour;
                    }
                } else if (particle_buffer[instanceID].elastic_lambda == 0.0f) {
                   // o.color = _Colour2;
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