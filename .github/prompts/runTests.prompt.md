---
mode: agent
description: "Run the project's test suite to ensure everything is functioning correctly."
---
Test the project by running its test suite, starting with only the fast tests and then proceeding to a full run:

1. Run the command `dotnet test Zilf.sln -c Debug --filter "TestCategory!=Slow" --logger "console;verbosity=minimal"` in the project's root directory to execute the test suite. This will run only the fast tests.

2. If there were any test failures in step 1, stop here. Otherwise, go ahead and run the full test suite with the command `dotnet test Zilf.sln -c Debug`.
