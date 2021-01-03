namespace GameLoop.Networking.Memory
{
    public interface IMemoryAllocator
    {
        T[] Allocate<T>(int size);
    }
}