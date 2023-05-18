using Autofac;
using Microsoft.Extensions.Logging;
using Procfiler.Core.Collector;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace ProcfilerTests.Core;

public abstract class TestWithContainerBase
{
  protected readonly IContainer Container;
  
  
  protected TestWithContainerBase()
  {
    var assembly = typeof(IClrEventsCollector).Assembly;
    var builder = ProcfilerContainerBuilder.BuildFromAssembly(LogLevel.Trace, assembly);
    builder.RegisterInstance(TestLogger.CreateInstance()).As<IProcfilerLogger>();
    Container = builder.Build();
  }
}