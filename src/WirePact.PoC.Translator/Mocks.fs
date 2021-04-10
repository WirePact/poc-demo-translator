module WirePact.PoC.Translator.Mocks

open System.Text
open DotnetKubernetesClient
open Moq
open k8s.Models

let kubernetesClient =
    let mock = Mock<IKubernetesClient>()

    mock
        .Setup(fun c -> c.GetCurrentNamespace())
        .ReturnsAsync("namespace")
    |> ignore

    mock
        .Setup(fun c -> c.Get<V1Secret>(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(V1Secret
                          (Data =
                              dict [ "username", Encoding.UTF8.GetBytes "Admin"
                                     "password", Encoding.UTF8.GetBytes "AdminPass" ]))
    |> ignore

    mock.Object
