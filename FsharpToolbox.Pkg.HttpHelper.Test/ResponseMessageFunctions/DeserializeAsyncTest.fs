module FsharpToolbox.Pkg.HttpHelper.Test.DeserializeAsyncTest


open NUnit.Framework
open FsharpToolbox.Pkg.HttpHelper
open FsharpToolbox.Pkg.FpUtils
open System.Net.Http

type Foo = {
  bar: string
  baz: int
}

[<Test>]
let ``When deserialization fails, the content is included in the error`` () =
  use client = createMockClient(new HttpResponseMessage(Content = new StringContent("not exactly JSON")))
  let result =
    client.getAsync(ParametersBuilder("https://httpbin.org/status/200").build())
    %>>= deserializeAsync<Foo>
    |> Async.RunSynchronously

  match result with
  | Error (DeserializeError e) ->
    match e.content with
    | Some content -> Assert.That(content, Is.EqualTo("not exactly JSON"))
    | None -> Assert.Fail("Content should be present")
  | other ->
    other
    |> sprintf "Expected DeserializeError, was %A"
    |> Assert.Fail

[<Test>]
let ``When deserialization of empty content fails, content is not included in the error`` () =
  use client = createMockClient(new HttpResponseMessage(Content = null))
  let result =
    client.getAsync(ParametersBuilder("https://httpbin.org/status/201").build())
    %>>= deserializeAsync<Foo>
    |> Async.RunSynchronously

  match result with
  | Error (DeserializeError e) ->
    match e.content with
    | Some content -> Assert.Fail(sprintf "Content should NOT be present, was %s" content)
    | None -> ()
  | other ->
    other
    |> sprintf "Expected DeserializeError, was %A"
    |> Assert.Fail
