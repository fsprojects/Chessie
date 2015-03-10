Using Chessie for Railway-oriented programming (ROP)
====================================================

This C# tutorial is based on an article about [Railway-oriented programming](http://fsharpforfunandprofit.com/posts/recipe-part2/) by Scott Wlaschin.

Additional resources:

* Railway Oriented Programming - A functional approach to error handling 
    * [Slide deck](http://www.slideshare.net/ScottWlaschin/railway-oriented-programming)
    * [Video](https://vimeo.com/97344498)

We start by referencing Chessie and opening the ErrorHandling module:

    [lang=csharp]
    using Chessie.ErrorHandling;
    using Chessie.ErrorHandling.CSharp;


Now we define some simple validation functions:

    [lang=csharp]
    public class Request
    {
        public string Name { get; set; }
        public string EMail { get; set; }
    }


    public class Validation
    {
        public static Result<Request, string> ValidateInput(Request input)
        {
            if (input.Name == "")
                return Result<Request, string>.FailWith("Name must not be blank");
            if (input.EMail == "")
                return Result<Request, string>.FailWith("Email must not be blank");
            return Result<Request, string>.Succeed(input);

        }
    }

Let's use the validation:

    [lang=csharp]
    var request = new Request { Name = "Steffen", EMail = "mail@support.com" };
    var result = Validation.ValidateInput(request); // Success
	
We can even pattern match on the result:

    [lang=csharp]
    result.Match(
        (x, msgs) => {
            Console.WriteLine("Value: {0}", x);
            Console.WriteLine("Messages: {0}", msgs);
        },
        msgs => {
            Console.WriteLine("Error-Messages: {0}", msgs);
        });