using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Zitadel.Authentication;
using Zitadel.Authentication.Credentials;
using Zitadel.Authentication.Models;

namespace WirePact.PoC.Translator
{
    public class ZitadelValidator : IDisposable
    {
        private const string JwtKey = @"{
            ""type"": ""application"",
            ""keyId"": ""102563298095980576"",
            ""key"": ""-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEAwoddGZqwz9sqBBe3Dh9YgPALsRbd5oUwpJtoFIC+v3YhIyMj\n9YTCRZQ2B8NCGyudXG7kQPo6SF9mNLXODfpviVKTnGesrYMJANUKnlm1+2j+QsQc\nIe63OTsLuDShj1eDq9uCEAGmZQQAbPWmb1GKb+KyfgYMbeMQED5AH6qRxCH9JXxK\nTThpJ4PKl5ok03MA+4Xq/nSOag6lWNgB0s86302mU+Jg66pvoGMPY5C2FzObhG/8\nlYaK/SuWYRf6AK/TRpZTsE9u+V6KYEqKWOgMmXG5LqICXCDCJEW5EOEWCJ08PMt2\n4t2S2qxgGIE+S7+G9kN//KEd8GqgRHUf+zJ8TQIDAQABAoIBAQC5qB9+1FhzyKQt\nC8U1wUziojdGaKCX5f4q2/dVuhpS+RdfRaaIKJCRf4ahmgV5kQK6uUs3iJofgI03\nOVzTknTpBtrCp1/yqeDp3mon/07kB6zDZA/FguNzx5rDz9dxywBsAS/8vtZkCpGt\nbDXQgG4zopTgGj26kr8+AWuJzW0ZVfzHsw/lN36CLrRxU8MTz5uZJHvHiy5RNjhR\n6tGskrFrMSRc53FaYfK80Kx0nYI7Ch52RmyxKMYHT4oVo/QFxLY5PUrB/DWkOj7x\nOnXsEyyS4wbmv7jEMA0kCK5FvkW0AVGY0WYJo1GJ8rFVvaqs2du4KTSHHc9Ij26K\nKWGz2Z4xAoGBANZJyq1G4mx9ceuZ7vksZagbKGv1UPjYOPCfgeaiSN45uSlmJ90+\nUbiib9hh8kDLEm7OCV24ws/HcOYbSKaTvp/Z/y+fP2SH1AiPSVCwxNcOZASPz0nh\nh3vvTjlXHM5caKjh5K0IybzrOEUQHNLzrf8ZM4G9pA3RF3ReFXhnZChjAoGBAOhk\n76V4pMilPDrE2+Pl+RC9qE9UScE7DDM95jm9uhZjqH7mtL4M2AwUUNquvSBoxzUg\nNKzxiK7Q/UxWMHeVPuIhzsVglTlQKQCVIPTeu1RteNU1acPwdApJgWcC4RiQER/6\nFcNGRUwQ/2WJkSDDCYXTQJB5bMnDv5Y1nJbwX2+PAoGAeDwHQJpZSU5JsUw5zg2f\nLGewkoKe4EFSEZEuLd44zJfO0O/ExlrFN7fM0biDWN+YhBuPHcshY7wgGLyOUwjr\nGH4UtP/BbgLYccUT+fZ1O8WTUQvv3yBwCJYJahr6yr8G6lt1F7GqoBnLBNU5lxOr\nOUtGnzFs72O2qVBw85HvnMkCgYBX6n+CsP5d7ay4auro74Jm4+j9gdyZHlaCOCOV\nqoHwoGkJAQxVbZR6FWF1KW/hB+J85Z8n4gQcZnG85EpXov10HOgVYXFyijHIx6H9\nIRnrWbLBrgBCIQA1OBOdPcicOzxPRZPgGQB0Q2XrKrzdkQtUNlQRk+4k8knTcJq5\nu1vPXwKBgGwWD8BBJsENEa5sINAc+nTrsri0mPKqpOdNMJA8ujoeUaKZpXPeor+5\nzxcbeanKWuiQswBXRS1fLjZmAzyYJLebikPu1yQQRZC11a3itbjW7Wkk8W0w+wYM\nV8Q1f//Lg4O9RTqNB2QBJPtoNIOEsEaYubGFg7V/DqHY1E4oiam6\n-----END RSA PRIVATE KEY-----\n"",
            ""appId"": ""102538455518604021"",
            ""clientId"": ""102538455518669557@poc_showcase""
        }";

        private static readonly Application Application = Application.LoadFromJsonString(JwtKey);
        private static readonly HttpClient Client = new();
        private static readonly OpenIdConnectConfiguration OidcConfiguration;

        private static readonly ConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager = new(
            ZitadelDefaults.DiscoveryEndpoint,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(Client));

        private readonly IDisposable _jwtRenewal;
        private string? _jwt;

        static ZitadelValidator()
        {
            OidcConfiguration = ConfigurationManager.GetConfigurationAsync().Result;
        }

        public ZitadelValidator()
        {
            _jwtRenewal = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMinutes(55))
                .Select(_ => Application.GetSignedJwt(ZitadelDefaults.Issuer))
                .Subscribe(appJwt => _jwt = appJwt);
        }

        public void Dispose()
        {
            _jwtRenewal.Dispose();
        }

        public async Task<bool> IsValidToken(string token)
        {
            _jwt ??= await Application.GetSignedJwtAsync(ZitadelDefaults.Issuer);

            var response = await Client.SendAsync(
                new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new(OidcConfiguration.IntrospectionEndpoint),
                    Content = new FormUrlEncodedContent(
                        new[]
                        {
                            new KeyValuePair<string?, string?>(
                                "client_assertion_type",
                                "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                            new KeyValuePair<string?, string?>(
                                "client_assertion",
                                $"{_jwt}"),
                            new KeyValuePair<string?, string?>("token", token),
                        }),
                });

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var introspect = await response.Content.ReadFromJsonAsync<IntrospectResponse>();
            if (introspect?.Active != true)
            {
                return false;
            }

            return true;
        }
    }
}
