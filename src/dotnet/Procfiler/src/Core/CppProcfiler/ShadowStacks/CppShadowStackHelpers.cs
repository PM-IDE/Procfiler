namespace Procfiler.Core.CppProcfiler.ShadowStacks;

public static class CppShadowStackHelpers
{
  public static void ReadManagedThreadIdAndFramesCount(BinaryReader reader, out long managedThreadId, out long framesCount)
  {
    managedThreadId = reader.ReadInt64();
    framesCount = reader.ReadInt64();
  }

  public static long CalculateByteLength(long framesCount)
  {
    //start or end indicator + timestamp + function id
    const long OneFrameLength = 1 + 8 + 8;
    //managed thread id + framesCount
    const long HeaderSize = 8 + 8;

    return framesCount * OneFrameLength + HeaderSize;
  }

  public static void SeekToPositionAndSkipHeader(BinaryReader reader, long position)
  {
    reader.BaseStream.Seek(position, SeekOrigin.Begin);
    reader.ReadInt64();
    reader.ReadUInt64();
  }
}