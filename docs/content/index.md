Chessie
=======

This project brings railway-oriented programming to .NET.

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The Chessie library can be <a href="https://nuget.org/packages/Chessie">installed from NuGet</a>:
      <pre>PM> Install-Package Chessie</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Using Chessie with Paket
------------------------

Chessie is a single-file module, so it's convienient to get it with [Paket GitHub dependencies][deps].
To do so, just add following line to your `paket.dependencies` file:

    github fsprojects/Chessie src/Chessie/ErrorHandling.fs

and following line to your `paket.references` file for the desired project:

    File:ErrorHandling.fs . 


Samples & documentation
-----------------------

* Read the [tutorial](railway.html) to see how to use Chessie for railway-oriented programming.
* [API Reference](reference/index.html) contains automatically generated documentation for all types, modules and functions in the library. 
This includes additional brief samples on using most of the functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Apache 2.0 license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/FSharpx.Async/tree/master/docs/content
  [gh]: https://github.com/fsprojects/FSharpx.Async
  [issues]: https://github.com/fsprojects/FSharpx.Async/issues
  [readme]: https://github.com/fsprojects/FSharpx.Async/blob/master/README.md
  [license]: https://github.com/fsprojects/FSharpx.Async/blob/master/LICENSE.txt
  [deps]: https://fsprojects.github.io/Paket/github-dependencies.html