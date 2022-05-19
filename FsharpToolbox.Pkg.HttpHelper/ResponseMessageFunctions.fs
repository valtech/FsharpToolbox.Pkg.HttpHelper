namespace FsharpToolbox.Pkg.HttpHelper

open System.Net
open System.Net.Http

open FsharpToolbox.Pkg.Logging
open FsharpToolbox.Pkg.FpUtils
open FsharpToolbox.Pkg.Serialization.Json.Serializer

open FsharpToolbox.Pkg.HttpHelper


[<AutoOpen>]
module ResponseMessageFunctions =
  open Microsoft.FSharp.Linq.NullableOperators
  let private getBody (response: HttpResponseMessage) =
    async {
      if
        response.Content = null || response.Content.Headers.ContentLength ?= 0L
      then
        return None
      else
        let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return Some content
    }

  let private getStatusCodeError (expected: HttpStatusCode) (response: HttpResponseMessage) =
    async {
      let! errorBody = getBody response
      L.Error(
        ("Failed to call service. Expected StatusCode {ExpectedStatusCode}, was {StatusCode}. Response: {Response}",
          expected, response.StatusCode, errorBody))
      return
        {
          actual = response.StatusCode
          expected = expected
          responseBody = errorBody
        }
        |> StatusCodeError
    }

  let ensureStatusCode (expected: HttpStatusCode) (response: HttpResponseMessage) =
    async {
      if response.StatusCode = expected then
        return Ok response
      else
        let! error = getStatusCodeError  expected response
        return error |> Error
    }

  let ensureStatusCodes (expected: HttpStatusCode) (accepted: HttpStatusCode list) (response: HttpResponseMessage) =
    async {
      if response.StatusCode = expected then
        return Ok response
      elif accepted |> List.contains response.StatusCode then
        return response |> AcceptedStatusCodeError |> Error
      else
        let! error = getStatusCodeError expected response
        return error |> Error
    }

  let private tryDeserialize<'dto> (content: string) =
    content
    |> tryDeserialize<'dto> DeserializeSettings.AllowAll
    |=! fun ex -> L.Error(ex, "Unable to deserialize content")
    |>! fun _ -> { message = "Unable to deserialize content"; content = Some content } |> DeserializeError
    |> Async.retn

  let deserializeAsync<'dto> (response: HttpResponseMessage) : Async<Result<'dto, HttpHelperError>> =
    getBody response
    |> Async.map (Option.ToResult ({ message = "No content to deserialize"; content = None } |> DeserializeError))
    |> AsyncResult.bind tryDeserialize<'dto>
