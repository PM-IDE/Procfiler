The Procfiler has following structure:

- docs/
  - Events/ - contains documents about CLR events
    - CLREvents.md - contains information (description, attributes, where event is fired in runtime) about interesting CLR events
    - TasksContext.md - some information about dotnet Threading
  - Experiments/ - contains detailed inforamtion about experiments, with links to analyzed data and images
  - Research proposal - contains the research proposal

- src/
  - Procfiler/ 
    - For now it is a console application which allows (the detailed info can be found with "-h" option):
      - Attach to running process (or launching application N times) for a given duration and collect CLR events with following serialization to
        - CSV
        - XES
      - Collecting metadata about events
      - Splitting collected events by methods, the split can be customized with filter-regex, inline option, which tells the program to inline events from inner methods execution and with repeat option, which allows to launch the specified program several times and merge all traces for each method
      - Splitted events by managed threads, for example, can be serialized into ".mtree" format, which is a method call tree format, which shows only event names and using indents in order to indicate methods' start and stop
      - Serializing events with undefined thread ID to separate XES file