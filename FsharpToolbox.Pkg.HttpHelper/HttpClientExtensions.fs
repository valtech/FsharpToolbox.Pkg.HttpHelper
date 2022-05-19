namespace FsharpToolbox.Pkg.HttpHelper

open System
open System.Text
open System.Net.Http
open FsharpToolbox.Pkg.Logging
open FsharpToolbox.Pkg.FpUtils
open FsharpToolbox.Pkg.Serialization.Json.Serializer

/// Request parameters without entity
type Parameters = {
  path: string
  query: Map<string, string> option
}

/// Request parameters with entity of type 'body
type Parameters<'body> = {
  path: string
  query: Map<string, string> option
  body: 'body
}

/// Used when calling endpoints not conforming to RFC 2661 (HTTP 1.1)
type NonRFCConformantParameters = {
  path: string
  query: Map<string, string> option
}

type PostParameters<'body> = Parameters<'body>
type PutParameters<'body> = Parameters<'body>
type PatchParameters<'body> = Parameters<'body>

type ParametersBuilder<'body>(path, query, body) =
  member _.build() : Parameters<'body> =
    {
      path = path
      query = query
      body = body
    }

type ParametersBuilderWithQuery(path: string, query) =
  member _.withBody wantedBody = ParametersBuilder<'a>(path, query, wantedBody)
  member _.build() : Parameters =
    {
      path = path
      query = query
    }

type ParametersBuilder(path: string) =
  let defaultQuery = None

  member _.withQuery (wantedQuery: Map<string, string>) = ParametersBuilderWithQuery(path, Some wantedQuery)
  member _.withBody wantedBody = ParametersBuilder<'a>(path, defaultQuery, wantedBody)
  member _.build() : Parameters =
    {
      path = path
      query = defaultQuery
    }

[<AutoOpen>]
module HttpClientExtensions =

  let private awaitHttpCall (call: System.Threading.Tasks.Task<HttpResponseMessage>) =
   async {
     try
       let! response = call |> Async.AwaitTask
       return response |> Ok
     with
       | ex ->
         L.Error(ex, "Error calling service")
         return ex.Message |> NetworkError |> Error
   }

  let private createContent payload = new StringContent(payload, Encoding.UTF8, "application/json")

  let private emptyContent = {||} |> jsonSerialize |> createContent

  let private buildContent body = body |> jsonSerialize |> createContent

  let private toQueryString (queryParameters: Map<string, string>) : string =
   let query = System.Web.HttpUtility.ParseQueryString(String.Empty);
   queryParameters
   |> Map.iter (fun key value ->
     query.[key] <- value)
   sprintf "?%s" (query.ToString())

  let private getQueryString (queryParameters: Map<string, string> option) : string =
   queryParameters
   |> Option.map toQueryString
   |> Option.defaultValue String.Empty

  let private buildUrl
   (path : string)
   (queryParameters : Map<string, string> option)
   : string =
   path + (getQueryString queryParameters)

  let private callAsync
    (httpClient : HttpClient)
    (path : string)
    (queryParameters : Map<string, string> option)
    (content: StringContent option)
    (method: HttpMethod)
    : Async<Result<HttpResponseMessage, HttpHelperError>> =
    let url = buildUrl path queryParameters
    let message = new HttpRequestMessage(method, url)
    if content.IsSome then message.Content <- content.Value
    httpClient.SendAsync(message)
    |> awaitHttpCall

  let private legacyCallAsync
    (httpClient : HttpClient)
    (parameters: NonRFCConformantParameters)
    (httpMethod: HttpMethod) =
    let content = emptyContent |> Some
    callAsync httpClient parameters.path parameters.query content httpMethod

  type HttpClient with
    member client.getAsync (parameters : Parameters) =
      callAsync client parameters.path parameters.query None HttpMethod.Get

    member client.postAsync<'body> (parameters: PostParameters<'body>) =
      let content = parameters.body |> buildContent |> Some
      callAsync client parameters.path parameters.query content HttpMethod.Post

    member client.putAsync<'body> (parameters: PostParameters<'body>) =
      let content = parameters.body |> buildContent |> Some
      callAsync client parameters.path parameters.query content HttpMethod.Put

    member client.patchAsync<'body> (parameters: PostParameters<'body>) =
      let content = parameters.body |> buildContent |> Some
      callAsync client parameters.path parameters.query content HttpMethod.Patch

    member client.deleteAsync (parameters : Parameters) =
      callAsync client parameters.path parameters.query None HttpMethod.Delete
