namespace SogigiMind.Authentication
{
    public class SogigiMindJwtOptions
    {
        public string Issuer { get; set; } = "SogigiMind";

        public string Audience { get; set; } = "SogigiMind";

        public byte[]? IssuerSigningKey { get; set; }

        public byte[][]? IssuerSigningKeys { get; set; }
    }
}
