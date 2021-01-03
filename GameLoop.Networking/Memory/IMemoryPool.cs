namespace GameLoop.Networking.Memory
{
    public interface IMemoryPool
    {
        T[] Rent<T>(int minimumSize);
        void Release<T>(T[] chunk);
    }
}