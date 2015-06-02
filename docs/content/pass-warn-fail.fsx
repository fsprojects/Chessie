(*** hide ***)
#I "../../bin"

#r "Chessie.dll"
open Chessie.ErrorHandling

open System
open System.Drawing

(**
Combining terminal and non-terminal errors
===

One feature of programming with "two-track" data is that once a value is on the _failure track_ 
operations against it may be skipped if they expect a value on the _passing track_. However, this 
is not always the desired behavior. In many cases, we will want to perform mulitple operations on a 
value and record any unusual or interesting results, but still keep thing on the _passing track_. 
One obvious scenario is when we want to validate multiple pieces of input (such as might be 
received from an HTTP request).
*)

type Applicant = 
  { FullName      :string * string
    DateOfBirth   :DateTime
    FavoriteColor :Color option }

(**
Given some aggregate user-supplied data, we want to check each datum.
*)

let checkName getName msg request =
  let name = getName request
  if String.IsNullOrWhiteSpace name
    then  // note the invalid datum, but stay on the _passing_ track
          request |> warn msg
    else  // no issues, proceed on the "happy path"
          request |> pass

let checkFirstName  = checkName (fun {FullName = (name,_)} -> name) "First name is missing"
let checkLastName   = checkName (fun {FullName = (_,name)} -> name) "Last name is missing"

let checkAge request =
  let dob   = request.DateOfBirth
  let diff  = DateTime.Today.Subtract dob
  if  diff  < TimeSpan.FromDays (18.0 * 365.0)
    then  // note the invalid datum, but stay on the _passing_ track
          request |> warn "DateOfBirth is too recent"
    else  // no issues, proceed on the "happy path"
          request |> pass

(**
Now we can combine our individual checks, returning the original value along-side any issues.
*)

(*** define-output: warnings ***)
let checkApplicant request =  
  request 
  |> checkAge
  |> bind checkFirstName
  |> bind checkLastName

let processRequest request = 
  match checkApplicant request with
  | Pass _    ->  printfn "All Good!!!"
  | Warn log  ->  printfn "Got some issues:"
                  for msg in log do printfn "  %s" msg
  | _         ->  printfn "Something went horribly wrong."

            
// good request
processRequest {FullName      = "John","Smith" 
                DateOfBirth   = DateTime (1995,12,21)
                FavoriteColor = None}
// bad request
processRequest {FullName      = "Beck","" 
                DateOfBirth   = DateTime (2005,04,13)
                FavoriteColor = Some Color.Gold}

(*** include-output: warnings ***)

(**
We can also mixed warnings in with operations that are terminal. In other words, we can still build 
workflows where certain operations switch the data over to the _failure track_.
*)

(*** define-output: two-and-three-track ***)
let disallowPink request =
  match request.FavoriteColor with
  | Some c // no idea why we're being mean to this particular color
    when c = Color.Pink -> fail "Get outta here with that color!"
  | _                   -> pass request

let recheckApplicant request =
  request
  |> checkAge
  |> bind disallowPink
  |> bind checkFirstName
  |> bind checkLastName

let reportMessages request =
  match recheckApplicant request with
  | Pass  _       ->  printfn "Nothing to report"
  | Warn log      ->  printfn "Got some issues:"
                      for msg in log do printfn "  %s" msg
  | Fail  errors  ->  printfn "Got errors:"
                      for msg in errors do printfn "  %s" msg

// terminal request
reportMessages {FullName      = "John","Smith" 
                DateOfBirth   = DateTime (1995,12,21)
                FavoriteColor = Some Color.Pink}

// non-terminal request with warnings
reportMessages {FullName      = "","Smith" 
                DateOfBirth   = DateTime (1995,12,21)
                FavoriteColor = Some Color.Green}
// good request
reportMessages {FullName      = "Bob","Smith" 
                DateOfBirth   = DateTime (1995,12,21)
                FavoriteColor = Some Color.Green}

(*** include-output: two-and-three-track ***)

(**
In effect, we've turned "two-track" data into "three-track" data. But we can also flip this around. 
That's is, run operations over the data accumlating warnings. Then at the end, if we have any 
messages at all, switch to the _failure track_.
*)

(*** define-output: fail-on-warn ***)
let warnOnNoColor request =
  match request.FavoriteColor with
  | None    -> request |> warn "No color provided"
  | Some _  -> pass request

let ``checkApplicant (again)`` request =
  request
  |> checkAge
  |> bind checkFirstName
  |> bind checkLastName
  |> bind warnOnNoColor

let ``processRequest (again)`` request =
  // turn any warning messages into failure messages
  let result =  request
                |> ``checkApplicant (again)``
                |> failOnWarnings id
  // now we only have 2 tracks on which the data may lay
  match result with
  | Fail errors ->  for x in errors do printfn "ERROR! %s" x
  | _           ->  printfn "SUCCESS!!!"

// terminal request
``processRequest (again)`` {FullName      = "","" 
                            DateOfBirth   = DateTime.Today
                            FavoriteColor = None}

(*** include-output: fail-on-warn ***)
