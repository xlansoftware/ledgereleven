using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class RsaKeyServiceCollectionExtensions
{
    private const string DefaultKeyPath = "keys/rsa_key.xml";

    public static RsaSecurityKey AddSecurityKey(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RsaKey");

        string keyPath = configuration["RsaKeyPath"] ?? DefaultKeyPath;
        keyPath = Path.GetFullPath(keyPath);

        if (!Directory.Exists(Path.GetDirectoryName(keyPath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);
            logger.LogInformation("Created directory for RSA key at {Path}", Path.GetDirectoryName(keyPath));
        }

        RSA rsa;

        if (File.Exists(keyPath))
        {
            rsa = RSA.Create();
            var xml = File.ReadAllText(keyPath, Encoding.UTF8);
            rsa.FromXmlString(xml);
            logger.LogInformation("Loaded existing RSA key from {Path}", keyPath);
        }
        else
        {
            rsa = RSA.Create(2048);
            var xml = rsa.ToXmlString(includePrivateParameters: true);
            File.WriteAllText(keyPath, xml, Encoding.UTF8);
            logger.LogInformation("Generated new RSA key and saved to {Path}", keyPath);
        }

        var rsaKey = new RsaSecurityKey(rsa)
        {
            KeyId = Guid.NewGuid().ToString()
        };

        services.AddSingleton<SecurityKey>(rsaKey);
        return rsaKey;
    }

    public static void FromXmlString(this RSA rsa, string xmlString)
    {
        var parameters = new RSAParameters();

        var xmlDoc = new System.Xml.XmlDocument();
        xmlDoc.LoadXml(xmlString);

        if (xmlDoc.DocumentElement?.Name == "RSAKeyValue")
        {
            foreach (System.Xml.XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                switch (node.Name)
                {
                    case "Modulus": parameters.Modulus = Convert.FromBase64String(node.InnerText); break;
                    case "Exponent": parameters.Exponent = Convert.FromBase64String(node.InnerText); break;
                    case "P": parameters.P = Convert.FromBase64String(node.InnerText); break;
                    case "Q": parameters.Q = Convert.FromBase64String(node.InnerText); break;
                    case "DP": parameters.DP = Convert.FromBase64String(node.InnerText); break;
                    case "DQ": parameters.DQ = Convert.FromBase64String(node.InnerText); break;
                    case "InverseQ": parameters.InverseQ = Convert.FromBase64String(node.InnerText); break;
                    case "D": parameters.D = Convert.FromBase64String(node.InnerText); break;
                }
            }
        }

        rsa.ImportParameters(parameters);
    }

    public static string ToXmlString(this RSA rsa, bool includePrivateParameters)
    {
        var parameters = rsa.ExportParameters(includePrivateParameters);

        return $@"
<RSAKeyValue>
  <Modulus>{Convert.ToBase64String(parameters.Modulus!)}</Modulus>
  <Exponent>{Convert.ToBase64String(parameters.Exponent!)}</Exponent>
  {(includePrivateParameters ? $@"
  <P>{Convert.ToBase64String(parameters.P!)}</P>
  <Q>{Convert.ToBase64String(parameters.Q!)}</Q>
  <DP>{Convert.ToBase64String(parameters.DP!)}</DP>
  <DQ>{Convert.ToBase64String(parameters.DQ!)}</DQ>
  <InverseQ>{Convert.ToBase64String(parameters.InverseQ!)}</InverseQ>
  <D>{Convert.ToBase64String(parameters.D!)}</D>" : "")}
</RSAKeyValue>";
    }
}
