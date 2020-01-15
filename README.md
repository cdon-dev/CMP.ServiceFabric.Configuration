[![NuGet Version and Downloads count](https://buildstats.info/nuget/CMP.ServiceFabric.Configuration?includePreReleases=true)](https://www.nuget.org/packages/CMP.ServiceFabric.Configuration/)

# CMP.Configuration
Opinionated configuration providers setup for .NET Core and Service Fabric.


```csharp
var config = new ConfigurationBuilder().AddCmpConfiguration(isInCluster);
```