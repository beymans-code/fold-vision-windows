// FoldShader.hlsl
// Traducción exacta de FoldShaders.metal de lqSky7/iphone-duo-macos-animation a HLSL.
//
// Efectos implementados:
//  1. Vogel Disc Gaussian blur (32 samples adaptativo, sin pixelación)
//  2. 3D perspective fold desde el borde inferior (eje de bisagra)
//  3. Glass tint (brillo semitransparente)
//  4. Dark void horizon falloff (degradado a negro en el borde del pliegue)
//  5. motionBoost: blur extra proporcional a la velocidad de cierre

// ─────────────────────────────────────────────
// Constantes
// ─────────────────────────────────────────────
static const float MAX_TILT = 0.84106867; // acos(1.0 / 1.5)
static const float3 DARK    = float3(0.0, 0.0, 0.0);
static const float  GOLDEN_ANGLE = 2.39996323; // pi * (3.0 - sqrt(5.0))

// ─────────────────────────────────────────────
// Uniforms (cbuffer)
// ─────────────────────────────────────────────
cbuffer Uniforms : register(b0)
{
    float2 imageSize;          // tamaño en píxeles de la textura de captura
    float2 cover;              // factor de cobertura (aspect ratio correction)
    float  aspect;             // ancho / alto de la pantalla
    float  turn;               // factor de pliegue [0.0 abierto … 1.0 cerrado]
    float  blurStrength;       // multiplicador de radio de blur [0-1]
    float  reflectionIntensity;// intensidad del glass tint [0-1]
    float  sampleCount;        // 12 / 20 / 32 según el ángulo de pliegue
    float  motionBoost;        // extra blur por velocidad de cierre
    float  cameraDepth;        // distancia de cámara para perspectiva 3D [0.5-5.0]
    float  stretchMult;        // multiplicador del estiramiento vertical [0-4]
    float  cornerRadius;       // radio de las esquinas superiores en píxeles [0-80]
    float  clipHeight;         // altura máxima de la máscara [0.0-1.0 fracción de pantalla]
    float  cornerAnimRange;    // rango de apertura en el que el radio alcanza su máximo
    float  cornerStartRadius;  // radio inicial de las esquinas en píxeles
    float  opacityAnimStart;   // progreso [0-1] donde empieza el desvanecimiento a negro
    float3 _pad4;
};

// ─────────────────────────────────────────────
// Recursos
// ─────────────────────────────────────────────
Texture2D    screenTex : register(t0);
SamplerState linearSampler : register(s0);

// ─────────────────────────────────────────────
// Vertex shader — fullscreen quad de 6 vértices (sin VB)
// ─────────────────────────────────────────────
struct VSOut
{
    float4 pos : SV_POSITION;
    float2 uv  : TEXCOORD0;
};

VSOut VS(uint vid : SV_VertexID)
{
    // Dos triángulos que cubren NDC [-1, 1]
    float2 positions[6] = {
        float2(-1, -1), float2( 1, -1), float2(-1,  1),
        float2(-1,  1), float2( 1, -1), float2( 1,  1)
    };
    VSOut o;
    float2 p = positions[vid];
    o.pos = float4(p, 0, 1);
    // UV: (0,0) arriba-izquierda, (1,1) abajo-derecha
    o.uv  = float2(p.x * 0.5 + 0.5, 0.5 - p.y * 0.5);
    return o;
}

// ─────────────────────────────────────────────
// Vogel Disc Gaussian blur (idéntico a sampleSmoothMatteBlur en Metal)
// ─────────────────────────────────────────────
float4 SampleVogelBlur(float2 uv, float radius, float2 screenCoord, int maxSamples)
{
    float2 tuv = (uv - 0.5) * cover + 0.5;

    // Sin blur: muestra directa
    if (radius <= 0.15)
        return screenTex.SampleLevel(linearSampler, tuv, 0);

    // Micro-rotación per-pixel (elimina el banding en anillo)
    float noiseVal = frac(sin(dot(screenCoord, float2(12.9898, 78.233))) * 43758.5453);
    float rot    = (noiseVal - 0.5) * 0.35;
    float cosRot = cos(rot);
    float sinRot = sin(rot);

    float4 accum       = 0;
    float  totalWeight = 0;
    // Optimización extrema: Limitar a 48 muestras para evitar cuellos de botella en la GPU en monitores 4K.
    // El micro-rotado de Vogel compensa el ruido en radios muy grandes de blur.
    int    active      = clamp((int)(radius * 0.8), 24, 48);

    float pixelSize = radius / imageSize.x; // radio en UV-space

    // Loop optimizado hasta 48 muestras
    for (int i = 0; i < 48; i++)
    {
        if (i >= active) break;

        float fi    = (float)i;
        float angle = fi * GOLDEN_ANGLE + rot;
        float r     = sqrt((fi + 0.5) / (float)active);

        float2 dir = float2(
             r * (cos(angle) * cosRot - sin(angle) * sinRot),
             r * (cos(angle) * sinRot + sin(angle) * cosRot)
        );

        float2 sampleUV = tuv + dir * float2(pixelSize, pixelSize * aspect);

        // Peso gaussiano (distancia^2 en espacio normalizado)
        float weight = exp(-dot(dir, dir) * 4.0);
        accum       += screenTex.SampleLevel(linearSampler, sampleUV, 0) * weight;
        totalWeight += weight;
    }

    return accum / max(totalWeight, 1e-5);
}

// ─────────────────────────────────────────────
// Pixel shader principal
// ─────────────────────────────────────────────
float4 PS(VSOut input) : SV_TARGET
{
    float2 uv = input.uv;

    // ── 1. Proyección 3D perspectiva (fold hacia atrás) ──────────────────
    // Para que la imagen parezca quedarse "quieta" en el mundo real,
    // debemos rotarla hacia el fondo (alejándose del usuario) en la misma 
    // cantidad que la tapa física se cierra hacia el usuario.
    float foldAngle = turn * MAX_TILT;     
    float cosA = cos(foldAngle);           
    float sinA = sin(foldAngle);           

    // Distancia lineal desde la bisagra (0 = abajo, 1 = arriba)
    float dist = 1.0 - uv.y;
    
    float stretchY = 1.0 + pow(turn, 2.0) * stretchMult; // estiramiento vertical parametrizable
    float dist_img = dist / stretchY;
    float foldedY  = 1.0 - dist_img;

    // 2. Eje X: Encogimiento en perspectiva (Trapecio)
    float x_img = (uv.x - 0.5) * (1.0 + dist * (sinA / cameraDepth)) + 0.5;
    
    float2 foldUV = float2(x_img, foldedY);

    // ── 2. Blur Vogel Disc (Depth of Field) ──────────────────────────────
    // Mayor desenfoque en la parte superior (lejos de la bisagra)
    // Curva progresiva para que la transición sea más natural
    float depthFactor = pow(clamp(dist_img, 0.0, 1.0), 1.5);
    float baseBlur    = turn * 600.0 * blurStrength + motionBoost;
    float blurRadius  = baseBlur * depthFactor; 
    int   activeTaps  = (int)sampleCount; 
    float4 blurred    = SampleVogelBlur(foldUV, blurRadius, input.pos.xy, activeTaps);
    float3 color      = blurred.rgb;
    float  alpha      = blurred.a; // Canal alpha suave de los bordes

    // ── 3. Glass tint (borde superior reflectante) ───────────────────────
    // El borde superior atrapa luz al inclinarse
    float rimFactor = smoothstep(0.85, 1.0, dist_img) * turn * reflectionIntensity;
    color           = lerp(color, float3(0.95, 0.96, 1.0), rimFactor * 0.45);

    // ── 4. Vignette / oscurecimiento global ─────────────────────────────
    float darken = turn * 0.75;
    color       *= (1.0 - darken * 0.5);

    // ── 5. Máscara clip dinámica con esquinas superiores redondeadas ──────────
    // La máscara baja proporcionalmente al ángulo de cierre (turn).
    // clipHeight define el recorrido máximo (0.0 a 1.0 de la pantalla).
    float screenW = imageSize.x;
    float screenH = imageSize.y;
    float2 p      = input.pos.xy;   // coordenadas de píxel del render target
    
    float  maxDropY = clipHeight * screenH;
    float  topY     = turn * maxDropY; // Borde superior dinámico
    
    // Anima el radio de las esquinas basado en 'turn' hasta 'cornerAnimRange'
    float  animProgress = clamp(turn / max(cornerAnimRange, 0.0001), 0.0, 1.0);
    float  cr       = lerp(cornerStartRadius, cornerRadius, animProgress);

    // El clip también debe deformarse con la perspectiva (cameraDepth)
    // Para ello mapeamos la coordenada X de vuelta al espacio de la imagen
    float warpedX = x_img * screenW;
    
    // Calculamos el Signed Distance Field (SDF) de la máscara usando warpedX
    // Distancia positiva = dentro de la máscara, negativa = fuera (arriba/esquinas)
    float signed_dist = p.y - topY; 
    
    if (cr > 0.0 && p.y < topY + cr)
    {
        if (warpedX < cr)
            signed_dist = cr - length(float2(warpedX - cr, p.y - (topY + cr)));
        else if (warpedX > screenW - cr)
            signed_dist = cr - length(float2(warpedX - (screenW - cr), p.y - (topY + cr)));
    }

    // El difuminado de la máscara coincide con el nivel de blur actual
    // Esto hace que los bordes del clip se vean borrosos al igual que la pantalla
    float softness = max(blurRadius * 1.5, 1.5); 
    float clipMask = smoothstep(-softness, softness, signed_dist);

    color = lerp(DARK, color, clipMask);

    // ── 6. Suavizado de bordes (Difuminado en Alpha) ───────────────────────
    // Usamos el Alpha difuminado para fundir los bordes de la pantalla virtual
    // con el negro absoluto del espacio exterior.
    color = lerp(float3(0.0, 0.0, 0.0), color, alpha);

    // ── 7. Desvanecimiento a negro (Fade to Black) ─────────────────────────
    float fadeOpacity = 1.0;
    if (turn > opacityAnimStart && opacityAnimStart < 1.0)
    {
        fadeOpacity = 1.0 - saturate((turn - opacityAnimStart) / (1.0 - opacityAnimStart));
    }
    color = lerp(float3(0.0, 0.0, 0.0), color, fadeOpacity);

    return float4(color, 1.0);
}
