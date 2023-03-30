namespace Procfiler.Core;

public interface IVisibleToUserCommand : ICommandHandler
{
  Command CreateCommand();
}

public interface ICommandWithContext<in TContext> : IVisibleToUserCommand
{
  ValueTask ExecuteAsync(TContext context);
}