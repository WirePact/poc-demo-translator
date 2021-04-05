using System;
using System.Text;
using DotnetKubernetesClient;
using k8s.Models;

namespace WirePact.PoC.Translator.Kubernetes
{
    public class CredentialsReader
    {
        private const string DefaultUsernameProperty = "username";
        private const string DefaultPasswordProperty = "password";

        private V1Secret? _secret;

        public CredentialsReader(IKubernetesClient client)
        {
            Initialize(client);
        }

        public string BasicAuthCredentials =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Read(UserProperty)}:{Read(PasswordProperty)}"));

        private static string SecretName => Environment.GetEnvironmentVariable("CREDENTIALS_SECRET_NAME") ??
                                            throw new ApplicationException("No k8s BasicAuth secret defined.");

        private static string UserProperty => Environment.GetEnvironmentVariable("CREDENTIALS_USER_PROPERTY") ??
                                              DefaultUsernameProperty;

        private static string PasswordProperty => Environment.GetEnvironmentVariable("CREDENTIALS_PASS_PROPERTY") ??
                                                  DefaultPasswordProperty;

        private async void Initialize(IKubernetesClient client)
        {
            _secret = await client.Get<V1Secret>(SecretName, await client.GetCurrentNamespace());
            if (_secret == null)
            {
                throw new ApplicationException($"K8s Secret {SecretName} not found!");
            }
        }

        private string Read(string prop) => _secret?.Data.ContainsKey(prop) == true
            ? Encoding.UTF8.GetString(_secret?.Data[prop] ?? Array.Empty<byte>())
            : throw new ApplicationException($"Secret does not contain property {prop}.");
    }
}
