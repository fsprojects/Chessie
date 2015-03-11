using Chessie.ErrorHandling;
using Chessie.ErrorHandling.CSharp;
using Microsoft.FSharp.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chessie.CSharp.Test
{
    // originall from https://github.com/fsprojects/fsharpx/blob/master/tests/FSharpx.CSharpTests/ValidationExample.cs


    // First let's define a domain.

    enum Sobriety { Sober, Tipsy, Drunk, Paralytic, Unconscious }
    enum Gender { Male, Female }

    class Person
    {
        private readonly Gender gender;
        private readonly int age;
        private readonly FSharpSet<string> clothes;
        private readonly Sobriety sobriety;

        public Gender Gender
        {
            get { return gender; }
        }

        public int Age
        {
            get { return age; }
        }

        public FSharpSet<string> Clothes
        {
            get { return clothes; }
        }

        public Sobriety Sobriety
        {
            get { return sobriety; }
        }

        public Person(Gender gender, int age, FSharpSet<string> clothes, Sobriety sobriety)
        {
            this.gender = gender;
            this.age = age;
            this.clothes = clothes;
            this.sobriety = sobriety;
        }
    }


    // Let's define the checks that *all* nightclubs make!
    class Club
    {
        public static readonly Func<Person, Result<Person, string>>
            CheckAge =
                p =>
                {
                    if (p.Age < 18)
                        return Result<Person, string>.FailWith("Too young!");
                    if (p.Age > 40)
                        return Result<Person, string>.FailWith("Too old!");
                    return Result<Person, string>.Succeed(p);
                };

        public static readonly Func<Person, Result<Person, string>>
            CheckClothes =
                p =>
                {
                    if (p.Gender == Gender.Male && !p.Clothes.Contains("Tie"))
                        return Result<Person, string>.FailWith("Smarten up!");
                    if (p.Gender == Gender.Female && p.Clothes.Contains("Trainers"))
                        return Result<Person, string>.FailWith("Wear high heels!");
                    return Result<Person, string>.Succeed(p);
                };

        public static readonly Func<Person, Result<Person, string>>
            CheckSobriety =
                p =>
                {
                    if (new[] { Sobriety.Drunk, Sobriety.Paralytic, Sobriety.Unconscious }.Contains(p.Sobriety))
                        return Result<Person, string>.FailWith("Sober up!");
                    return Result<Person, string>.Succeed(p);
                };
    }

    // Now let's compose some validation checks
    class ClubbedToDeath
    {
        // PERFORM THE CHECKS USING Monadic SUGAR (LINQ)
        public static Result<decimal, string> CostToEnter(Person p)
        {
            return from a in Club.CheckAge(p)
                   from b in Club.CheckClothes(a)
                   from c in Club.CheckSobriety(b)
                   select c.Gender == Gender.Female ? 0m : 5m;
        }
    }

    // Now let's see these in action
    [TestFixture]
    class Test1
    {
        public static readonly Person Ken = new Person(
            gender: Gender.Male,
            age: 28,
            clothes: (new FSharpSet<string>(new List<string> { "Tie", "Shirt" })),
            sobriety: Sobriety.Tipsy);

        public static readonly Person Dave = new Person(
            gender: Gender.Male,
            age: 41,
            clothes: (new FSharpSet<string>(new List<string> { "Tie", "Jeans" })),
            sobriety: Sobriety.Sober);

        public static readonly Person Ruby = new Person(
            gender: Gender.Female,
            age: 25,
            clothes: (new FSharpSet<string>(new List<string> { "High heels" })),
            sobriety: Sobriety.Tipsy);

        // let's go clubbing!

        [Test]
        public void Part1()
        {
            var costDave = ClubbedToDeath.CostToEnter(Dave);
            Assert.AreEqual("Too old!", costDave.FailedWith().First());

            var costKen = ClubbedToDeath.CostToEnter(Ken);
            Assert.AreEqual(Result<decimal, string>.Succeed(5m), costKen);

            var costRuby = ClubbedToDeath.CostToEnter(Ruby);
            Assert.AreEqual(Result<decimal, string>.Succeed(0m), costRuby);

            var Ruby17 = new Person(
                age: 17,
                clothes: Ruby.Clothes,
                sobriety: Ruby.Sobriety,
                gender: Ruby.Gender);
            var costRuby17 = ClubbedToDeath.CostToEnter(Ruby17);
            Assert.AreEqual("Too young!", costRuby17.FailedWith().First());

            var KenUnconscious = new Person(
                age: Ken.Age,
                clothes: Ken.Clothes,
                gender: Ken.Gender,
                sobriety: Sobriety.Unconscious);
            var costKenUnconscious = ClubbedToDeath.CostToEnter(KenUnconscious);

            Assert.AreEqual("Sober up!", costKenUnconscious.FailedWith().First());

            /**
             * The thing to note here is how the Validations can be composed together in a computation expression.
             * The type system is making sure that failures flow through your computation in a safe manner.
             */
        }
    }
}

