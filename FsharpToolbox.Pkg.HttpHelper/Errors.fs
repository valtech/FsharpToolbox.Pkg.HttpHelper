namespace FsharpToolbox.Pkg.HttpHelper

open System.Net
open System.Net.Http

type StatusCodeError = {
  actual: HttpStatusCode
  expected: HttpStatusCode
  responseBody: string option
}

type DeserializationErrorDetails = {
  message: string
  content: string option
}

type HttpHelperError =
  | NetworkError of string
  | StatusCodeError of StatusCodeError
  | AcceptedStatusCodeError of HttpResponseMessage
  | DeserializeError of DeserializationErrorDetails
