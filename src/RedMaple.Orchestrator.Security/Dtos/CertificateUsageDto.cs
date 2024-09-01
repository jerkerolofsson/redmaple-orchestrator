namespace RedMaple.Orchestrator.Security.Dtos
{
    public enum CertificateUsageDto
    {
        DigitalSignature,
        KeyAgreement,
        KeyEncipherment,
        DataEncipherment,
        DecipherOnly,
        EncipherOnly,
        CertificateSigning,
        CrlSigning
    }
}
