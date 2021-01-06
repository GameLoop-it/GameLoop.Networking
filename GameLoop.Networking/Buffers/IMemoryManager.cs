namespace GameLoop.Networking.Buffers
{
    public interface IMemoryManager
    {
        byte[] Allocate(int size);
        void   Free(byte[]  block);
    }
}