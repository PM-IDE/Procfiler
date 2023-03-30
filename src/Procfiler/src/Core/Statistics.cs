using Procfiler.Utils;

namespace Procfiler.Core;

internal struct Statistics
{
  public int EventsCount { get; set; }
  public int EventsWithStackTraces { get; set; }
  public PercentValue EventsWithManagedThreadIs { get; }
  public Dictionary<string, int> StackTracesPerEvents { get; }
  public Dictionary<string, int> EventsCountMap { get; }
  public Dictionary<string, List<string>> EventNamesToPayloadProperties { get; }


  public Statistics()
  {
    EventsWithStackTraces = 0;
    StackTracesPerEvents = new Dictionary<string, int>();
    EventsWithManagedThreadIs = new PercentValue();
    EventsCountMap = new Dictionary<string, int>();
    EventNamesToPayloadProperties = new Dictionary<string, List<string>>();
    EventsCount = 0;
  }

  
  public void LogMyself(IProcfilerLogger logger)
  {
    var sb = new StringBuilder();
    sb.LogPrimitiveValue(nameof(EventsWithStackTraces), EventsWithStackTraces)
      .LogPrimitiveValue(nameof(EventsCount), EventsCount)
      .LogPrimitiveValue(nameof(EventsWithManagedThreadIs), EventsWithManagedThreadIs.Percent)
      .LogDictionary(nameof(StackTracesPerEvents), StackTracesPerEvents)
      .LogDictionary(nameof(EventsCountMap), EventsCountMap)
      .LogDictionary(nameof(EventNamesToPayloadProperties), EventNamesToPayloadProperties);

    logger.LogTrace("Statistics:\n{Value}\n", sb.ToString());
  }
}