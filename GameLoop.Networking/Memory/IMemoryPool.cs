namespace GameLoop.Networking.Memory
{
    public interface IMemoryPool
    {
        byte[] Rent(int minimumSize);
        void Release(byte[] chunk);
    }
}