void Ripple_float (
	float3 PositionIn,
	float Period, float Speed, float Amplitude,
	out float3 PositionOut
) {
    PositionOut.y = Amplitude * sin(Speed * _Time.y + Period * length(PositionIn.xz));
    PositionOut.xz = PositionIn.xz;
}