module FsSnip.Recaptcha
#load "../packages.fsx" "../common/common.fsx" "error.fsx"

open Suave
open System
open FSharp.Data

// -------------------------------------------------------------------------------------------------
// Helpers for reCAPTCHA validation
// -------------------------------------------------------------------------------------------------

type RecaptchaResponse = JsonProvider<"""{"success":true}""">

/// reCAPTCHA secret 
let recaptchaSecret = 
    Environment.GetEnvironmentVariable("RECAPTCHA_SECRET")

/// Validates that reCAPTCHA has been entered properly
let validateRecaptcha form = async {
  let formValue = form |> Seq.tryPick (fun (k, v) -> 
      if k = "g-recaptcha-response" then v else None)
  let response = defaultArg formValue ""
  let! response = 
      Http.AsyncRequestString
        ( "https://www.google.com/recaptcha/api/siteverify", httpMethod="POST", 
          body=HttpRequestBody.FormValues ["secret", recaptchaSecret; "response", response])
  return RecaptchaResponse.Parse(response).Success }

/// Reports an reCAPTCHA validation error
let recaptchaError = 
    let details = 
        "Sorry, we were not able to verify that you are a human! "+
        "Consider turning on JavaScript or using a browser supported by reCAPTCHA."
    Error.reportError HTTP_400 "Human validation failed!" details 
