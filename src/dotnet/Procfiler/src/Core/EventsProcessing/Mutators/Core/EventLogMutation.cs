namespace Procfiler.Core.EventsProcessing.Mutators.Core;

public record EventLogMutation(string EventClass);

public record AttributeRemovalMutation(string EventClass, string AttributeName) : EventLogMutation(EventClass);

public record AttributeRenameMutation(string EventClass, string OldName, string NewName) : EventLogMutation(EventClass);

public record AttributeToNameAppendMutation(
  string EventClass, 
  string AttributeName, 
  bool RemoveFromMetadata
) : EventLogMutation(EventClass);

public record NewAttributeCreationMutation(string EventClass, string AttributeName) : EventLogMutation(EventClass);
public record AddEventMutation(string EventClass) : EventLogMutation(EventClass);
public record AddLifecycleTransitionAttributeMutation(string EventClass, string LifecycleTransition) : EventLogMutation(EventClass);
public record ActivityIdCreation(string EventClass, string ActivityIdTemplate) : EventLogMutation(EventClass);