namespace Procfiler.Utils.Container;

public class EventMutatorAttribute : AppComponentAttribute
{
  public int Pass { get; }

  
  public EventMutatorAttribute(int pass)
  {
    Pass = pass;
  }
}