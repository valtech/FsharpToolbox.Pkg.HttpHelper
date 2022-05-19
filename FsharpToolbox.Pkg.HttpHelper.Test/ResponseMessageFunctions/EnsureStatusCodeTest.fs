module FsharpToolbox.Pkg.HttpHelper.Test.EnsureStatusCodeTest

open NUnit.Framework
open FsharpToolbox.Pkg.HttpHelper
open FsharpToolbox.Pkg.FpUtils
open System.Net.Http
open System.Net

let canValidateEmptyContent mockedResponse =
  use client = createMockClient mockedResponse
  let parameters = ParametersBuilder("https://httpbin.org/status/201").build()
  let result =
    client.getAsync(parameters)
    %>>= ensureStatusCode System.Net.HttpStatusCode.OK
    |> Async.RunSynchronously

  match result with
  | Error (StatusCodeError sce) ->
    Assert.IsTrue(sce.responseBody.IsNone)
  | other ->
    other
    |> sprintf "Expected StatusCodeError, was %A"
    |> Assert.Fail

[<Test>]
let ``Can validate status code even when response content is empty`` () =
  let response = new HttpResponseMessage(HttpStatusCode.NoContent)
  let content = new ByteArrayContent([||])
  response.Content <- content

  canValidateEmptyContent response


[<Test>]
let ``Can validate status code even when response content is null`` () =
  let response = new HttpResponseMessage(HttpStatusCode.NoContent)
  response.Content <- null

  canValidateEmptyContent response
