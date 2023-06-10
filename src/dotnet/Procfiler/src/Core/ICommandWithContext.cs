namespace Procfiler.Core;

public interface IVisibleToUserCommand : ICommandHandler
{
  Command CreateCommand();
}

public interface ICommandWithContext<in TContext> : IVisibleToUserCommand
{
  void Execute(TContext context);
}