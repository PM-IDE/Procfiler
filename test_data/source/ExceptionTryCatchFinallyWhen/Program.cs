using System.Runtime.CompilerServices;

try
{
  Throw();
}
catch (Exception ex) when (ex.GetType() == typeof(OperationCanceledException))
{
  Console.WriteLine("Operation cancel");
}
catch (Exception ex)
{
  Console.WriteLine(ex.Message);
}

[MethodImpl(MethodImplOptions.NoInlining)]
void Throw()
{
  var source = new CancellationTokenSource();
  source.Cancel();
  source.Token.ThrowIfCancellationRequested();
}