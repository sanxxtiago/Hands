public struct FrameErgonomicExposureData
{
    public long frameId;
    public float timestamp;
    public HandType handType;

    public ErgonomicExposureDimensionData wristFlexionExtension;
    public ErgonomicExposureDimensionData wristRadialUlnarDeviation;
    public ErgonomicExposureDimensionData wristPronationSupination;
}
