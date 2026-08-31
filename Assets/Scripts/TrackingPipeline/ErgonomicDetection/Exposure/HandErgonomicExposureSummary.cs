public struct HandErgonomicExposureSummary
{
    public HandType handType;
    // Identidad de configuración solo durante esta ejecución; no se persiste.
    public int calibrationProfileId;
    public ErgonomicExposureDimensionSummary wristFlexionExtension;
    public ErgonomicExposureDimensionSummary wristRadialUlnarDeviation;
    public ErgonomicExposureDimensionSummary wristPronationSupination;
}
