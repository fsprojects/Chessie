# A Tale of 3 Nightclubs

This C# tutorial is based on a [Scalaz tutorial](https://gist.github.com/oxbowlakes/970717) by Chris Marshall and was originally ported to [fsharpx](https://github.com/fsprojects/fsharpx/blob/master/tests/FSharpx.CSharpTests/ValidationExample.cs) by Mauricio Scheffer.

Additional resources:

* Railway Oriented Programming by Scott Wlaschin - A functional approach to error handling
	* [Blog post](http://fsharpforfunandprofit.com/posts/recipe-part2/)
    * [Slide deck](http://www.slideshare.net/ScottWlaschin/railway-oriented-programming)
    * [Video](https://vimeo.com/97344498)

## Part Zero : 10:15 Saturday Night

We start by referencing Chessie and opening the ErrorHandling module and define a simple domain for nightclubs:

    [lang=csharp]
    using Chessie.ErrorHandling;
    using Chessie.ErrorHandling.CSharp;


    enum Sobriety { Sober, Tipsy, Drunk, Paralytic, Unconscious }
    enum Gender { Male, Female }

    class Person
    {
        public Gender Gender { get; private set; }
        public int Age { get; private set; }
        public List<string> Clothes { get; private set; }
        public Sobriety Sobriety { get; private set; }

        public Person(Gender gender, int age, List<string> clothes, Sobriety sobriety)
        {
            this.Gender = gender;
            this.Age = age;
            this.Clothes = clothes;
            this.Sobriety = sobriety;
        }
    }

Now we define some validation methods that all nightclubs will perform:

    [lang=csharp]
    class Club
    {
        public static Result<Person, string> CheckAge(Person p)
        {
            if (p.Age < 18)
                return Result<Person, string>.FailWith("Too young!");
            if (p.Age > 40)
                return Result<Person, string>.FailWith("Too old!");
            return Result<Person, string>.Succeed(p);
        }

        public static Result<Person, string> CheckClothes(Person p)
        {
            if (p.Gender == Gender.Male && !p.Clothes.Contains("Tie"))
                return Result<Person, string>.FailWith("Smarten up!");
            if (p.Gender == Gender.Female && p.Clothes.Contains("Trainers"))
                return Result<Person, string>.FailWith("Wear high heels!");
            return Result<Person, string>.Succeed(p);
        }

        public static Result<Person, string> CheckSobriety(Person p)
        {
            if (new[] { Sobriety.Drunk, Sobriety.Paralytic, Sobriety.Unconscious }.Contains(p.Sobriety))
                return Result<Person, string>.FailWith("Sober up!");
            return Result<Person, string>.Succeed(p);
        }
    }

## Part One : Clubbed to Death

Now let's compose some validation checks via syntactic sugar and LINQ:

    [lang=csharp]
    class ClubbedToDeath
    {
        public static Result<decimal, string> CostToEnter(Person p)
        {
            return from a in Club.CheckAge(p)
                   from b in Club.CheckClothes(a)
                   from c in Club.CheckSobriety(b)
                   select c.Gender == Gender.Female ? 0m : 5m;
        }
    }
	
Let's see how the validation works in action:

    [lang=csharp]
    var Dave = new Person(Gender.Male, 41, new List<string> { "Tie", "Jeans" }, Sobriety.Sober);
    var costDave = ClubbedToDeath.CostToEnter(Dave); // Error "Too old!"

    var Ken = new Person(Gender.Male, 28, new List<string> { "Tie", "Shirt" }, Sobriety.Tipsy);
    var costKen = ClubbedToDeath.CostToEnter(Ken);  // Success 5


We can even use pattern matching on the result:

    [lang=csharp]
    var Ruby = new Person(Gender.Female, 25, new List<string> { "High heels" }, Sobriety.Tipsy);
    var costRuby = ClubbedToDeath.CostToEnter(Ruby);
    costRuby.Match(
        (x, msgs) => {
            Console.WriteLine("Cost for Ruby: {0}", x);
        },
        msgs => {
            Console.WriteLine("Ruby is not allowed to enter: {0}", msgs);
        });

The thing to note here is how the Validations can be composed together in a computation expression.
The type system is making sure that failures flow through your computation in a safe manner.