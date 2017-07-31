# ScaleOut Time Windowing Library for .NET

This library provides methods and collection wrappers that perform
time windowing transformations of IEnumerable<T> collections.

Nuget: TBD

Documentation: https://scaleoutsoftware.github.io/ScaleOut.Client.Streaming

## Motivation: 

This time windowing library was written by ScaleOut Software to
support stateful stream processing in its
[StreamServer](https://www.scaleoutsoftware.com/products/stateserver/)
product, which allows events to be processed directly on the
distributed in-memory data grid that manages state.

Events associated with persisted state are typically stored as a
serialized collection of elements, so this library is designed to be
used with ordered IEnumerable collections. It is targeted to
developers who prefer simple LINQ-style operators and would rather not
convert collections to Rx observables to perform windowing.

This library is open source and has no dependencies on other ScaleOut 
Software products. 

License: Apache 2 

