namespace WirePact.PoC.Translator

open System
open System.Text
open DotnetKubernetesClient
open FSharp.Control.Tasks
open k8s.Models

type CredentialReader(client: IKubernetesClient) =
    let defaultUsernameProperty = "username"
    let defaultPasswordProperty = "password"

    let secretName =
        match Environment.GetEnvironmentVariable "CREDENTIALS_SECRET_NAME" with
        | ""
        | null -> raise (ApplicationException("No k8s BasicAuth secret defined."))
        | value -> value

    let usernameProperty =
        match Environment.GetEnvironmentVariable "CREDENTIALS_USER_PROPERTY" with
        | ""
        | null -> defaultUsernameProperty
        | value -> value

    let passwordProperty =
        match Environment.GetEnvironmentVariable "CREDENTIALS_PASS_PROPERTY" with
        | ""
        | null -> defaultPasswordProperty
        | value -> value

    let secret =
        let secret =
            (task {
                let! ns = client.GetCurrentNamespace()
                return! client.Get<V1Secret>(secretName, ns)
             })
                .Result

        if isNull secret
        then raise (ApplicationException($"K8s Secret {secretName} not found!"))
        else secret

    let read prop =
        if isNull secret
        then raise (ApplicationException($"Secret does not contain property {prop}."))
        else Encoding.UTF8.GetString(secret.Data.Item prop)

    member _.BasicAuthCredentials
        with public get () =
            Convert.ToBase64String(Encoding.UTF8.GetBytes $"{read usernameProperty}:{read passwordProperty}")
