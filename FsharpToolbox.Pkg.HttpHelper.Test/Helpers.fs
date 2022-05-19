namespace FsharpToolbox.Pkg.HttpHelper.Test

open System.Net.Http

[<AutoOpen>]
module Helpers =
  type MockHttpMessageHandler(response) =
   inherit HttpMessageHandler()
   override _.SendAsync(_, _) =
    System.Threading.Tasks.Task.FromResult(response)

  let createMockClient mockedResponse =
    let mockedMessageHandler = new MockHttpMessageHandler(mockedResponse)
    new HttpClient(mockedMessageHandler)

