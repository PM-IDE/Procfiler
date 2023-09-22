namespace Procfiler.Core.CppProcfiler;

public enum CppProfilerMode
{
  Disabled,
  SingleFileBinStack,
  PerThreadBinStacksFiles
}

public static class CppProfilerModeExtensions
{
  public static bool IsDisabled(this CppProfilerMode mode) => mode == CppProfilerMode.Disabled;
  public static bool IsEnabled(this CppProfilerMode mode) => !mode.IsDisabled();
}