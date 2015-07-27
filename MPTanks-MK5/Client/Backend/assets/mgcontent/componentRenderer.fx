﻿float4x4 view;
float4x4 projection;
sampler txt : register(s0);

struct VertexShaderInput
{
	float2 Position : SV_Position;
	float2 RotationOrigin : TEXCOORD0;
	float2 Scale : TEXCOORD1;
	float2 Size : TEXCOORD2;
	float Rotation : TEXCOORD3;
	float4 Color : COLOR0;
	float4 TextureInfo : TEXCOORD4;
	float2 TexCoord : TEXCOORD5;
};

struct VertexShaderOutput
{
	float4 Position : SV_Position;
	float4 Color : COLOR0;
	float4 TextureInfo : TEXCOORD0;
	float2 TexCoord : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	//compute world pos
	float2 posTemp = input.Position;
	//Shift for rotation
	posTemp = posTemp - input.RotationOrigin;
	//Rotate
	posTemp.x = posTemp.x * cos(input.Rotation) - posTemp.y * sin(input.Rotation);
	posTemp.y = posTemp.x * sin(input.Rotation) + posTemp.y * cos(input.Rotation);
	posTemp = posTemp + input.RotationOrigin;
	//Center the point
	posTemp = posTemp - (input.Size / 2);

	posTemp = posTemp * input.Scale;


	float4 viewPosition = mul(posTemp, view);
	output.Position = mul(viewPosition, projection);
	output.Color = input.Color;
	output.TextureInfo = input.TextureInfo;
	output.TexCoord = input.TexCoord;

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float texCoordX = (input.TexCoord.x %
	(input.TextureInfo.z - input.TextureInfo.x)) + input.TextureInfo.x;
	float texCoordY = (input.TexCoord.y %
	(input.TextureInfo.w - input.TextureInfo.y)) + input.TextureInfo.y;
	float2 texCoord = float2(texCoordX, texCoordY);
	return tex2D(txt, texCoord) * input.Color;
}

technique Draw
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 VertexShaderFunction();
		PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
#else
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
#endif
	}
}