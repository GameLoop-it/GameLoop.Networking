namespace GameLoop.Networking.Memory
{
    public interface IMemoryAllocator
    {
        byte[] Allocate(int size);
    }
}