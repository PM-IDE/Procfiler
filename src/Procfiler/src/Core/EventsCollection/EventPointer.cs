using Procfiler.Core.Exceptions;

namespace Procfiler.Core.EventsCollection;

public readonly record struct EventPointer(bool IsInFirstEvents, int IndexInArray, int IndexInInsertionMap)
{
  public static EventPointer ForFirstEvent(int indexInFirstEvents) => new(true, -1, indexInFirstEvents);
  public static EventPointer ForInitialArray(int indexInInitialArray) => new(false, indexInInitialArray, -1);
  public static EventPointer ForInsertionMap(int indexInInitialArray, int indexInInsertionMap) =>
    new(false, indexInInitialArray, indexInInsertionMap);
  
  
  public bool IsInInitialArray => IndexInInsertionMap == -1 && IndexInArray >= 0;

  public bool IsInInsertedMap => IndexInInsertionMap >= 0 &&
                                 (IndexInArray >= 0 && !IsInFirstEvents || IsInFirstEvents && IndexInArray == -1);
  

  public void AssertStateOrThrow()
  {
    if (!((IsInInitialArray && !IsInInsertedMap) || (!IsInInitialArray && IsInInsertedMap)))
    {
      throw new InvalidStateException($"array: {IsInInitialArray} , map: {IsInInsertedMap}");
    }
  }
}