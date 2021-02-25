namespace GameLoop.Networking.Transport.Simulators
{
    public interface INetworkLossSimulator
    {
        bool IsLost();
    }
}