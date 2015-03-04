using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Chessie.CSharp.Test
{
    public class Request
    {
        public string Name { get; set; }
        public string EMail { get; set; }
    }

    public class Validation
    {
        public static Chessie.ErrorHandling.Result<Request, string> ValidateInput(Request input)
        {
            if (input.Name == "")
                return Chessie.ErrorHandling.fail<Request, string>("Name must not be blank");
            if (input.EMail == "")
                return Chessie.ErrorHandling.fail<Request, string>("Email must not be blank");
            return Chessie.ErrorHandling.ok<Request, string>(input);

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
            Assert.AreEqual(Chessie.ErrorHandling.fail<Request, string>("Name must not be blank"), result);
        }
    }
}
