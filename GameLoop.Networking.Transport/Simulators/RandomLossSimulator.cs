using System;
#if ENABLE_TRANSPORT_LOGS
using GameLoop.Utilities.Logs;
#endif

namespace GameLoop.Networking.Transport.Simulators
{
    public class RandomLossSimulator : INetworkLossSimulator
    {
        public double SimulatedLossThreshold;
        
        private readonly Random          _random;

        public RandomLossSimulator(double simulatedLossThreshold)
        {
            SimulatedLossThreshold = simulatedLossThreshold;
            _random                = new Random(Environment.TickCount);
        }
        
        public bool IsLost()
        {
            if (SimulatedLossThreshold > 0)
            {
                if (_random.NextDouble() <= SimulatedLossThreshold)
                {
#if ENABLE_TRANSPORT_LOGS
                    Logger.DebugWarning($"[Loss Simulator] Lost {receivedBytes} bytes from {address}");
#endif
                    return true;
                }
            }

            return false;
        }
    }
}