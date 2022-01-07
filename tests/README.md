This folder contains some micro benchmark tests from dotnet/performance test extracted to standalone app for profiling purposes. This does not have any timer implementation.

Inorder to execute this, run the following from individual tests folders from command line

`dotnet restore`

`dotnet publish -c Release`

You can find the .exe, .dll and .pdb file with symbols in the build folder. The dll can be executed with corerun.exe inorder to profile.


