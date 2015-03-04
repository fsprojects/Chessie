using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Chessie.ErrorHandling;

namespace Chessie.CSharp.Test
{
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

    [TestFixture]
    public class SimpleValidation
    {
        [Test]
        public void CanCreateSuccess()
        {
            var request = new Request { Name = "", EMail = "" };
            var result = Validation.ValidateInput(request);
            Assert.AreEqual(Result<Request, string>.FailWith("Name must not be blank"), result);
        }
    }
}
