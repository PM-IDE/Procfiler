using Autofac;
using Procfiler.Core;
using Procfiler.Utils;
using Procfiler.Utils.Container;

var builder = ProcfilerContainerBuilder.BuildFromAssembly(LogLevel.Information);
builder.RegisterType(typeof(ProcfilerLogger)).As<IProcfilerLogger>();

var container = builder.Build();
Command root = new("procfiler");
CommandLineBuilder cmdBuilder = new(root);

foreach (var command in container.Resolve<IEnumerable<IVisibleToUserCommand>>())
{
  root.AddCommand(command.CreateCommand());
}

cmdBuilder.UseDefaults();

var parser = cmdBuilder.Build();

using var cookie = new PerformanceCookie("Program", container.Resolve<IProcfilerLogger>());
await parser.InvokeAsync(args);